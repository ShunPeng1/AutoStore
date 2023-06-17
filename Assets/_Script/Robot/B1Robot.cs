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


    private float MIN_BLOCK_AHEAD_ANGLE => Mathf.Atan((castRadius + BoxColliderSize/2)/(0.5f + BoxColliderSize/2)) * Mathf.PI;
    private float MAX_BLOCK_AHEAD_ANGLE = 45f;


    void FixedUpdate()
    {
        CurrentBaseState.ExecuteState();
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
    protected override void DetectNearByRobot(RobotStateEnum currentRobotState, object[] parameters)
    {
        var hits = Physics.OverlapSphere(centerBodyCast.position, castRadius, robotLayerMask); // Find robot in a circle 

        List<GridXZCell<StackStorage>> dynamicObstacle = new();
        DetectDecision finalDecision; 
        
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
                    dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.LastCellPosition));
                    dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.NextCellPosition));
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
        float dotProductOf2RobotDirection = Vector3.Dot(NextCellPosition - LastCellPosition,detectedRobot.NextCellPosition - detectedRobot.LastCellPosition);
        
        if (detectedRobot.CurrentBaseState.MyStateEnum is RobotStateEnum.Idle) 
        {
            if (!IsBlockAHead(detectedRobot, MIN_BLOCK_AHEAD_ANGLE)) return DetectDecision.Continue;
            
            // Is block ahead
            detectedRobot.RedirectOrthogonal(this);
            
            return DetectDecision.Wait;
        }

        //if (CurrentBaseState.MyStateEnum == RobotStateEnum.Redirecting) return DetectDecision.Continue;
        
        if (detectedRobot.CurrentBaseState.MyStateEnum is RobotStateEnum.Jamming) 
        {
            if(!IsBlockAHead(detectedRobot, MIN_BLOCK_AHEAD_ANGLE)) return DetectDecision.Continue; // same row or column
            
            // Is block ahead
            if (detectedRobot.LastCellPosition == CurrentTask.GoalCellPosition
                || detectedRobot.NextCellPosition == CurrentTask.GoalCellPosition) // If they are standing on this robot goal
            {
                detectedRobot.RedirectOrthogonal(this);
                return DetectDecision.Wait;
            }
            else return DetectDecision.Dodge;
        }
        
        if (Math.Abs(dotProductOf2RobotDirection - (-1)) < 0.01f || // opposite direction
            detectedRobot.CurrentBaseState.MyStateEnum is RobotStateEnum.Jamming) 
        {
            if(!IsBlockAHead(detectedRobot, MIN_BLOCK_AHEAD_ANGLE)) return DetectDecision.Continue; // same row or column
            
            // Is block ahead
            if (detectedRobot.LastCellPosition == CurrentTask.GoalCellPosition
                || detectedRobot.NextCellPosition == CurrentTask.GoalCellPosition) // If they are standing on this robot goal
            {
                detectedRobot.RedirectOrthogonal(this);
                return DetectDecision.Wait;
            }
            else return DetectDecision.Dodge;
        }
        
        if (dotProductOf2RobotDirection == 0) // perpendicular direction
        {
            return IsBlockAHead(detectedRobot, MAX_BLOCK_AHEAD_ANGLE) ? DetectDecision.Wait : DetectDecision.Continue;
        }
        

        return DetectDecision.Continue;
    }

    private bool IsBlockAHead(Robot detectedRobot, float isHeadAngleThreshold)
    {
        float angleBetweenMyDirectionAndRobotDistance = Vector3.Angle(detectedRobot.transform.position - transform.position, NextCellPosition - transform.position) ;

        if (angleBetweenMyDirectionAndRobotDistance >= isHeadAngleThreshold  )  // Not block ahead when larger than angle threadhold 
            return false;

        if (NextCellPosition == detectedRobot.NextCellPosition ||
            NextCellPosition == detectedRobot.LastCellPosition) // definitely block by its last cell or next cell
            return true;
        else return false;
        
            // If the direction ahead is the goal
        if (NextCellPosition == CurrentTask.GoalCellPosition)
            return false;
        
        if (MovingPath == null || MovingPath.Count == 0) // The NextCellPosition is goal and will be block
            return true;
        
        // Check for corner
        GridXZCell<StackStorage> nextNextCell = MovingPath.First.Value;
        Vector3 nextNextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(nextNextCell.XIndex, nextNextCell.ZIndex) + Vector3.up * transform.position.y;
        if (nextNextCellPosition == NextCellPosition)
        {
            MovingPath.RemoveFirst();
            if (MovingPath.Count == 0) return true;
            nextNextCell = MovingPath.First.Value;
            nextNextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(nextNextCell.XIndex, nextNextCell.ZIndex) + Vector3.up * transform.position.y;
        }
        
        float dotOf2NextDirection = Vector3.Dot(NextCellPosition - LastCellPosition, nextNextCellPosition - NextCellPosition);
        
        return !(dotOf2NextDirection == 0 ); // perpendicular direction and not the same corner of the detected robot
    }
    #endregion

    #region Pathfinding

    protected override void CreatePathFinding(Vector3 startPosition, Vector3 endPosition)
    {
        var startCell = CurrentGrid.GetCell(startPosition);
        var endCell = CurrentGrid.GetCell(endPosition);
        
        
        MovingPath = PathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

        if (MovingPath == null) // No destination was found
        {
            StartCoroutine(nameof(Jamming));
        }
    }
    
    protected override void UpdatePathFinding(List<GridXZCell<StackStorage>> dynamicObstacle)
    {
        var currentStartCell = CurrentGrid.GetCell(LastCellPosition);
         
        MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);
       
        if (MovingPath == null) // The path to goal is block
        {
            StartCoroutine(nameof(Jamming));
            return;
        }
        
        ExtractNextCellInPath(); // return to the last cell
    }

    #endregion
    


    void ShowPath()
    {
        if (CurrentBaseState.MyStateEnum == RobotStateEnum.Idle || MovingPath == null)
        {
            _debugLineRenderer.positionCount = 0;
            return;
        }
        
        _debugLineRenderer.positionCount = MovingPath.Count + 1;
        _debugLineRenderer.SetPosition(0, transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            _debugLineRenderer.SetPosition(itr, CurrentGrid.GetWorldPositionOfNearestCell(cell.XIndex,cell.ZIndex));
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
        Vector3 requestedRobotDistance = CurrentGrid.GetWorldPositionOfNearestCell(transform.position) - CurrentGrid.GetWorldPositionOfNearestCell(requestedRobot.transform.position);
        Vector3 crossProduct = Vector3.Cross(Vector3.up, requestedRobotDistance).normalized; // find the orthogonal vector 
        
        //Debug.Log("Cross "+ crossProduct);
        (var redirectX, var redirectZ) = CurrentGrid.GetXZ(transform.position + crossProduct * 1);

        Vector3 redirectGoalCellPosition;
        if (CurrentGrid.IsValidCell(redirectX, redirectZ))
        {
            redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectX, redirectZ) + Vector3.up * transform.position.y;
        }
        else
        {
            (var redirectX2, var redirectZ2) = CurrentGrid.GetXZ(transform.position + crossProduct * -1); // the other direction
            redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectX2, redirectZ2) + Vector3.up * transform.position.y;
        }

        Debug.Log(requestedRobot.gameObject.name+" requested to move "+ gameObject.name + " from "+  CurrentGrid.GetXZ(transform.position) + " to "+ redirectGoalCellPosition);

        if (CurrentBaseState.MyStateEnum == RobotStateEnum.Jamming) // Destroy the Jamming State, to restore the LastRobotState
        {
            StopCoroutine(Jamming());
        }

        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.LastCell, redirectGoalCellPosition, RestoreState);
        
        SetToState(RobotStateEnum.Redirecting, 
            new object[]{CurrentTask}, 
            new object[]{robotTask});
        
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;

        Vector3 goalCellPosition = crate.transform.position + Vector3.up * transform.position.y;
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, PickUpCrate, 0);
        
        SetToState(RobotStateEnum.Approaching, 
            new object[]{CurrentTask}, 
            new object[]{robotTask});
    }

    protected override void PickUpCrate()
    {
        HoldingCrate.transform.SetParent(transform);

        var goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(HoldingCrate.storingX, HoldingCrate.storingZ) + Vector3.up * transform.position.y;
        
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, DropDownCrate, 0);
        
        SetToState(RobotStateEnum.Delivering, 
            new object[]{CurrentTask}, 
            new object[]{robotTask});
        
    }

    protected override void DropDownCrate()
    {
        Destroy(HoldingCrate.gameObject);
        HoldingCrate = null;
        
        SetToState(RobotStateEnum.Idle, new object[]{CurrentTask});
        
    }
    
    
    #endregion
}
