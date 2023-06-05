using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;

public class B1Robot : Robot
{
    [Header("Pathfinder")] 
    [SerializeField] private LineRenderer _debugLineRenderer;
    private DStarLitePathFinding _dStarLitePathFinding;
    
    [Header("Casting")] 
    [SerializeField] private Transform centerBodyCast;
    [SerializeField] private float castRadius;
    [SerializeField] private LayerMask robotLayerMask;
    
    
    IEnumerator Start()
    {
        GoalCellPosition = NextCellPosition = transform.position;
        yield return null;
        CurrentGrid = MapManager.Instance.StorageGrid;
        (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);
        
    }
    
    void Update()
    {
        if (CurrentGrid == null) return;
        DetectNearByRobot();
        MoveAlongGrid();
        ShowPath();
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(centerBodyCast.position, castRadius);

        Vector3 castDirection = (NextCellPosition - transform.position).normalized;
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
        
        var hits = Physics.OverlapSphere(centerBodyCast.position, castRadius, robotLayerMask);

        List<GridXZCell> dynamicObstacle = new(); 
        foreach (var hitCollider in hits)
        { 
            var robotHit = hitCollider.gameObject.GetComponent<Robot>();
            if (robotHit == this)
            {
                continue;
            }
            
            /*if (IsBlockMe(robotHit) || (robotHit.RobotState == RobotStateEnum.Idle ))
            {
                Debug.Log(name+" Jamming with "+ robotHit.gameObject.name + " with angle " + Vector3.Angle(hitCollider.transform.position - transform.position, NextCellPosition - transform.position));
                
                // TODO avoidance
                //StartCoroutine(nameof(Jamming));

                // Use the current and next cell to be a obstacle
                dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.LastCellPosition));
                dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.NextCellPosition));
            
            }
            */
            switch (CheckDetection(robotHit))
            {
                case DetectDecision.Wait:
                    Debug.Log(gameObject.name +" Jam with "+robotHit.gameObject.name);
                    StartCoroutine(nameof(Jamming));
                    break;
                case DetectDecision.Dodge:
                    Debug.Log(gameObject.name +" Dodge "+robotHit.gameObject.name);
                    dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.LastCellPosition));
                    dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.NextCellPosition));
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
        float dotProductOf2Direction = Vector3.Dot(NextCellPosition - LastCellPosition,detectedRobot.NextCellPosition - detectedRobot.LastCellPosition);
        
        if (Math.Abs(dotProductOf2Direction - (-1)) < 0.01f || // opposite direction
            detectedRobot.RobotState is RobotStateEnum.Idle or RobotStateEnum.Jamming) 
        {
            return angleBetweenMyDirectionAndRobotDistance < 5 ? DetectDecision.Dodge : DetectDecision.Continue; // same row or column
        }
        
        if (dotProductOf2Direction == 0) // perpendicular direction
        {
            return angleBetweenMyDirectionAndRobotDistance < 45 ? DetectDecision.Wait : DetectDecision.Continue;
        }
        

        return DetectDecision.Continue;
    } 
    private bool IsBlockMe(Robot robot)
    {
        float angleBetweenMyDirectionAndRobotDistance = Vector3.Angle(robot.transform.position - transform.position, NextCellPosition - transform.position) ;
        float dotProductOf2Direction = Vector3.Dot(NextCellPosition - LastCellPosition,robot.NextCellPosition - robot.LastCellPosition);

        if (dotProductOf2Direction == 0) // perpendicular direction
        {
            return angleBetweenMyDirectionAndRobotDistance < 45;
        }
        
        if (Math.Abs(dotProductOf2Direction - (-1)) < 0.01f) // opposite direction
        {
            return angleBetweenMyDirectionAndRobotDistance < 5; // same row or column
        }
        
        return false; // same direction
    }

    #endregion
    
    
    private void CreatePathFinding()
    {
        var startCell = CurrentGrid.GetItem(XIndex, ZIndex);
        var endCell = CurrentGrid.GetItem(GoalCellPosition);

        // TODO Choose a path finding 
        //MovingPath = MapManager.Instance.RequestPath(startCell, endCell);
        _dStarLitePathFinding = new DStarLitePathFinding(CurrentGrid);
        MovingPath = _dStarLitePathFinding.InitializePathFinding(startCell, endCell);

        
        if (MovingPath == null || MovingPath.Count <= 1) return;

        //MovingPath.RemoveFirst(); // the current standing node
      
        ForwardMoveNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }
    
    private void UpdatePathFinding(List<GridXZCell> dynamicObstacle)
    {
        var currentStartCell = CurrentGrid.GetItem(LastCellPosition);
         
        MovingPath = _dStarLitePathFinding.UpdatePathDynamicObstacle(currentStartCell, dynamicObstacle);
        
        if (MovingPath == null || MovingPath.Count <= 1) return;
        
        //MovingPath.RemoveFirst(); // the current standing node
        
        ForwardMoveNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }


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

    public override void IdleRedirect(Robot requestedRobot)
    {
        
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;
        GoalCellPosition = crate.transform.position;
        RobotState = RobotStateEnum.Retrieving;

        CreatePathFinding();
    }

    public override void PickUpCrate()
    {
        if (RobotState != RobotStateEnum.Retrieving || CurrentGrid.GetXZ(transform.position) !=
            CurrentGrid.GetXZ(HoldingCrate.transform.position)) return;
        
        GoalCellPosition = CurrentGrid.GetWorldPosition(HoldingCrate.storingX, HoldingCrate.storingZ);
        HoldingCrate.transform.SetParent(transform);
        RobotState = RobotStateEnum.Delivering;
            
        CreatePathFinding();
    }

    public override void DropDownCrate()
    {
        if (RobotState != RobotStateEnum.Delivering ||
            CurrentGrid.GetXZ(transform.position) != (HoldingCrate.storingX, HoldingCrate.storingZ)) return;
        
        Destroy(HoldingCrate.gameObject);
        HoldingCrate = null;
        RobotState = RobotStateEnum.Idle;
    }
}
