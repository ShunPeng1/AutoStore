using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;

public class B1Robot : Robot
{
    [Header("Debug")] 
    [SerializeField] private LineRenderer _debugLineRenderer;
    
    [Header("Casting")] 
    [SerializeField] private Transform centerBodyCast;
    [SerializeField] private float castRadius;
    [SerializeField] private LayerMask robotLayerMask;
    
    
    void FixedUpdate()
    {
        if (CurrentGrid == null) return;
        DetectNearByRobot();
        MoveAlongGrid();
        ShowPath();
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(centerBodyCast.position, castRadius);
    }

    #region RobotDetect

    private enum DetectDecision
    {
        Wait,
        Dodge,
        Continue
    }
    protected override void DetectNearByRobot()
    {
        if (RobotState is RobotStateEnum.Idle or RobotStateEnum.Jamming) return;
        
        var hits = Physics.OverlapSphere(centerBodyCast.position, castRadius, robotLayerMask); // Find robot in a circle 

        List<GridXZCell> dynamicObstacle = new(); 
        foreach (var hitCollider in hits)
        { 
            var detectedRobot = hitCollider.gameObject.GetComponent<Robot>();
            if (detectedRobot == this) // This robot itself
            {
                continue;
            }
            
            switch (CheckDetection(detectedRobot))
            {
                case DetectDecision.Wait: // We set the robot to jam state
                    Debug.Log(gameObject.name +" Jam with "+detectedRobot.gameObject.name);
                    StartCoroutine(nameof(Jamming));
                    break;
                case DetectDecision.Dodge: // We add the detected robot cell as obstacle
                    Debug.Log(gameObject.name +" Dodge "+detectedRobot.gameObject.name);
                    dynamicObstacle.Add(CurrentGrid.GetItem(detectedRobot.LastCellPosition));
                    dynamicObstacle.Add(CurrentGrid.GetItem(detectedRobot.NextCellPosition));
                    break;
                case DetectDecision.Continue:
                    break;
            }
        }
        
        // Update Path base on dynamic obstacle
        if (dynamicObstacle.Count != 0) UpdatePathFinding(dynamicObstacle);
    }

    private DetectDecision CheckDetection(Robot detectedRobot)
    {
        float angleBetweenMyDirectionAndRobotDistance = Vector3.Angle(detectedRobot.transform.position - transform.position, NextCellPosition - transform.position) ;
        float dotProductOf2RobotDirection = Vector3.Dot(NextCellPosition - LastCellPosition,detectedRobot.NextCellPosition - detectedRobot.LastCellPosition);

        
        if (detectedRobot.RobotState is RobotStateEnum.Idle) 
        {
            if (!IsBlockAHead(angleBetweenMyDirectionAndRobotDistance, 5)) return DetectDecision.Continue;
            
            detectedRobot.RedirectOrthogonal(this);
            return RobotState == RobotStateEnum.Redirecting ? DetectDecision.Continue : DetectDecision.Wait;
        }

        if (RobotState == RobotStateEnum.Redirecting) return DetectDecision.Continue;

        if (Math.Abs(dotProductOf2RobotDirection - (-1)) < 0.01f || // opposite direction
            detectedRobot.RobotState is RobotStateEnum.Idle or RobotStateEnum.Jamming) 
        {
            return IsBlockAHead(angleBetweenMyDirectionAndRobotDistance, 5) ? DetectDecision.Dodge : DetectDecision.Continue; // same row or column
        }
        
        if (dotProductOf2RobotDirection == 0) // perpendicular direction
        {
            return angleBetweenMyDirectionAndRobotDistance < 45 ? DetectDecision.Wait : DetectDecision.Continue;
        }
        

        return DetectDecision.Continue;
    }

    private bool IsBlockAHead(float angleBetweenMyDirectionAndRobotDistance, float isHeadAngleThreshold)
    {
        if (angleBetweenMyDirectionAndRobotDistance >= isHeadAngleThreshold  // Not block ahead 
                || MovingPath == null|| MovingPath.Count == 0) return false; // or the NextCellPosition is the goal or no more way
        
        // If the direction ahead is a corner or a goal, so we assume it doesn't block
        GridXZCell nextNextCell = MovingPath.First.Value;

        Vector3 nextNextCellPosition = CurrentGrid.GetWorldPosition(nextNextCell.XIndex, nextNextCell.ZIndex) + Vector3.up * transform.position.y;
        float dotOf2NextDirection = Vector3.Dot(NextCellPosition - LastCellPosition, nextNextCellPosition - NextCellPosition);
        
        return dotOf2NextDirection != 0; // perpendicular direction or not
    }
    #endregion

    #region Pathfinding

    private void CreatePathFinding()
    {
        var startCell = CurrentGrid.GetItem(NextCellPosition);
        var endCell = CurrentGrid.GetItem(GoalCellPosition);

        // TODO Choose a path finding 
        
        
        MovingPath = PathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

        if (MovingPath == null)
        {
            StartCoroutine("Jamming");
        }
    }
    
    
    private void UpdatePathFinding(List<GridXZCell> dynamicObstacle)
    {
        var currentStartCell = CurrentGrid.GetItem(LastCellPosition);
         
        MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);
        
        if (MovingPath == null)
        {
            StartCoroutine("Jamming");
        }
    }

    #endregion
    


    void ShowPath()
    {
        if (RobotState == RobotStateEnum.Idle || MovingPath == null) return;
        
        _debugLineRenderer.positionCount = MovingPath.Count + 1;
        _debugLineRenderer.SetPosition(0, transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            _debugLineRenderer.SetPosition(itr, cell.StackStorage.transform.position);
            itr++;
        }
    }

    #region AssignTask

    /// <summary>
    /// This function will be requested when the robot is Idle
    /// It will move to a empty cell to right orthogonal, if not valid to the left orthogonal instead
    /// </summary>
    /// <param name="requestedRobot"></param>
    public override void RedirectOrthogonal(Robot requestedRobot)
    {
        RobotState = RobotStateEnum.Redirecting;

        Vector3 requestedRobotDistance = transform.position - requestedRobot.transform.position;
        Vector3 crossProduct = Vector3.Cross(Vector3.up, requestedRobotDistance).normalized; // find the orthogonal vector 
        
        //Debug.Log("Cross "+ crossProduct);
        (var redirectX, var redirectZ) = CurrentGrid.GetXZ(transform.position + crossProduct * 2);

        if (CurrentGrid.IsValidCell(redirectX, redirectZ))
        {
            GoalCellPosition = CurrentGrid.GetWorldPosition(redirectX, redirectZ);
        }
        else
        {
            (var redirectX2, var redirectZ2) = CurrentGrid.GetXZ(transform.position + crossProduct * -2);
            GoalCellPosition = CurrentGrid.GetWorldPosition(redirectX2, redirectZ2);
        }
        
        ArrivalDestinationFuncs.Enqueue(BecomeIdle);
        CreatePathFinding();
        
    }

    public override void ApproachCrate(Crate crate)
    {
        RobotState = RobotStateEnum.Retrieving;
        HoldingCrate = crate;
        GoalCellPosition = crate.transform.position;
        
        ArrivalDestinationFuncs.Enqueue(PickUpCrate);
        CreatePathFinding();
    }

    protected override IEnumerator PickUpCrate()
    {
        if (RobotState != RobotStateEnum.Retrieving || CurrentGrid.GetXZ(transform.position) !=
            CurrentGrid.GetXZ(HoldingCrate.transform.position)) yield break;
        
        GoalCellPosition = CurrentGrid.GetWorldPosition(HoldingCrate.storingX, HoldingCrate.storingZ);
        HoldingCrate.transform.SetParent(transform);
        RobotState = RobotStateEnum.Delivering;
        
        ArrivalDestinationFuncs.Enqueue( DropDownCrate );
        CreatePathFinding();
        
    }

    protected override IEnumerator DropDownCrate()
    {
        if (RobotState != RobotStateEnum.Delivering ||
            CurrentGrid.GetXZ(transform.position) != (HoldingCrate.storingX, HoldingCrate.storingZ)) yield break;
        
        Destroy(HoldingCrate.gameObject);
        HoldingCrate = null;
        
        RobotState = RobotStateEnum.Idle;
        
    }
    
    
    #endregion
}
