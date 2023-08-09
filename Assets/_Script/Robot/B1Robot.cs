using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;
using Random = UnityEngine.Random;

public class B1Robot : Robot
{
    private float MIN_BLOCK_AHEAD_ANGLE => Mathf.Atan((CastRadius + BoxColliderSize/2)/(0.5f + BoxColliderSize/2)) * Mathf.PI;
    private float MAX_BLOCK_AHEAD_ANGLE = 45f;

    #region DETECTION

    private enum DetectDecision
    {
        Ignore = 0,
        Wait = 1,
        Dodge = 2,
        Deflected = 3
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, CastRadius);
    }

    protected override void DetectNearByRobot()
    {
        NearbyRobots = new List<Robot>();

        var colliders = Physics.OverlapSphere(transform.position, CastRadius, RobotLayerMask);
        foreach (var colliderHit in colliders)
        {
            Robot detectedRobot = colliderHit.gameObject.GetComponent<Robot>();
            if (detectedRobot == null || detectedRobot == this) continue;
            NearbyRobots.Add(detectedRobot);
        }
    }
    
    protected override bool DecideFromRobotDetection()
    {
        List<GridXZCell<CellItem>> dynamicObstacle = new();
        DetectDecision finalDecision = DetectDecision.Ignore; 
        
        foreach (var detectedRobot in NearbyRobots)
        {
            
            DetectDecision decision = CheckDetection(detectedRobot);
            finalDecision = (DetectDecision) Mathf.Max((int)decision, (int)finalDecision);

            dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.LastCellPosition));
            if(detectedRobot.IsBetween2Cells) dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.NextCellPosition));
        }
        
        switch (finalDecision)
        {
            case DetectDecision.Ignore:
                return true;
            
            case DetectDecision.Wait: // We set the robot to jam state
                //Debug.Log(gameObject.name +" Jam! ");
                SetToJam();
                return false;
            
            case DetectDecision.Dodge: // We add the detected robot cell as obstacle
                //Debug.Log(gameObject.name +" Dodge ");
                return UpdateInitialPath(dynamicObstacle); // Update Path base on dynamic obstacle
             
            case DetectDecision.Deflected:
                return false;
            
            default:
                return true;
        }
    }

    private DetectDecision CheckDetection(Robot detectedRobot)
    {
        float dotProductOf2RobotDirection = Vector3.Dot(NextCellPosition - LastCellPosition,detectedRobot.NextCellPosition - detectedRobot.LastCellPosition);
        bool isUnsafeDistanceOf2Robot = Vector3.Distance(transform.position, detectedRobot.transform.position) <= CastRadius ;
        bool isBlockAHead = IsBlockAHead(detectedRobot, NextCellPosition);
        bool isBlockingGoal = detectedRobot.NextCellPosition == CurrentTask.GoalCellPosition 
                              || detectedRobot.LastCellPosition == CurrentTask.GoalCellPosition;
        switch (detectedRobot.CurrentRobotState)
        {
            /* Idle state cases */
            case RobotStateEnum.Idle when isBlockingGoal || isBlockAHead: // If they are standing on this robot goal or blocking ahead of this robot
                return detectedRobot.RedirectToOrthogonalCell(this, NextCellPosition) ? DetectDecision.Wait : 
                    RedirectToOrthogonalCell(detectedRobot, detectedRobot.NextCellPosition) ? DetectDecision.Deflected : DetectDecision.Dodge;

            case RobotStateEnum.Idle: // Not blocking at all
                return DetectDecision.Ignore;
            
            
            /* Jamming state cases */
            case RobotStateEnum.Jamming when isBlockAHead: // Currently blocking in between the next cell
                //return DetectDecision.Dodge;
                return detectedRobot.RedirectToOrthogonalCell(this, NextCellPosition) ? DetectDecision.Wait : 
                    RedirectToOrthogonalCell(detectedRobot, detectedRobot.NextCellPosition) ? DetectDecision.Deflected : DetectDecision.Dodge;

            case RobotStateEnum.Jamming: //  is not blocking ahead in between the next cell
                return DetectDecision.Ignore; 
            
            
            /* Handling states cases */
            case RobotStateEnum.Handling when !isBlockAHead: //  is not block ahead
                return DetectDecision.Ignore; 
            
            // Currently blocking in between the next cell , and they are standing on this robot goal
            case RobotStateEnum.Handling when isBlockingGoal:
                return DetectDecision.Wait;
            
            case RobotStateEnum.Handling: // Currently blocking in between the next cell, and not on this robot goal
                return DetectDecision.Dodge;
            

            /* These are the rest of moving state */
            case RobotStateEnum.Delivering:
            case RobotStateEnum.Approaching:
            case RobotStateEnum.Redirecting:
            default:
                if (Math.Abs(dotProductOf2RobotDirection - (-1)) < 0.01f ) // opposite direction
                {
                    if(!isBlockAHead) return DetectDecision.Ignore; // same row or column
            
                    // Is block ahead
                    if (isBlockingGoal) // If they are standing on this robot goal
                    {
                        return detectedRobot.RedirectToOrthogonalCell(this, NextCellPosition) ? DetectDecision.Wait : 
                            RedirectToOrthogonalCell(detectedRobot, detectedRobot.NextCellPosition) ? DetectDecision.Deflected : DetectDecision.Dodge;

                    }
                    else return DetectDecision.Dodge;
                }

                if (Math.Abs(dotProductOf2RobotDirection - 1) < 0.01f && isBlockAHead && isUnsafeDistanceOf2Robot) 
                    // 2 robot moving same direction and smaller than safe distance
                {
                    //Debug.Log(gameObject.name + " Keep safe distance ahead with "+detectedRobot.gameObject.name);
                    return DetectDecision.Wait;
                }
                
                if (dotProductOf2RobotDirection == 0) // perpendicular direction
                {
                    return isBlockAHead ? DetectDecision.Wait : DetectDecision.Ignore;
                }
                
                return DetectDecision.Ignore;
        }
        
    }

    private bool IsBlockAHead(Robot detectedRobot, Vector3 checkPosition)
    {
        return (checkPosition == detectedRobot.NextCellPosition && detectedRobot.IsBetween2Cells) ||
               checkPosition == detectedRobot.LastCellPosition; // definitely being block by detected robot's last cell or next cell
    }
    
    #endregion

    #region PATH_FINDING

    protected override bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition)
    {
        var startCell = CurrentGrid.GetCell(startPosition);
        var endCell = CurrentGrid.GetCell(endPosition);
        
        MovingPath = PathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

        if (MovingPath != null) return true; 
        
        // No destination was found
        RedirectToNearestCell();

        return false;

    }
    
    protected override bool UpdateInitialPath(List<GridXZCell<CellItem>> dynamicObstacle)
    {
        Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position) + Vector3.up * transform.position.y;
        var currentStartCell = CurrentGrid.GetCell(nearestCellPosition);

        MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);

        if (MovingPath == null) // The path to goal is block
        {
            RedirectToNearestCell();
            return false;
        }
        
        if (nearestCellPosition == LastCellPosition)
        {
            ExtractNextCellInPath();
            return true;
        }
        
        if (nearestCellPosition == NextCellPosition)
        {
            MovingPath.RemoveFirst();
            return true;
        }

        //Debug.LogError( gameObject.name+" THE NEAREST CELL IS NOT LAST OR NEXT CELL "+ nearestCellPosition);

        return false;
    }

    #endregion

    #region TASK_FUNCTIONS

    protected override void RedirectToNearestCell()
    {
        Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position) + Vector3.up * transform.position.y;
        
        //Debug.Log( gameObject.name+ " Redirect To Nearest Cell " + nearestCellPosition);
            
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NearestCell, nearestCellPosition, SetToJam);
        RobotStateMachine.SetToState(RobotStateEnum.Redirecting,
            new object[] { CurrentTask },
            new object[] { robotTask });
    }

    
    /// <summary>
    /// This function will be requested when the robot is Idle, or standing on others goal. To move to a other direction
    /// The direction is right, left, backward, prefer mostly the direction which is not blocking
    /// </summary>
    /// <param name="requestedRobot"></param>
    public override bool RedirectToOrthogonalCell(Robot requestedRobot, Vector3 requestedRobotGoalPosition)
    {
        if (CurrentRobotState == RobotStateEnum.Redirecting)
        {
            return true;  // Cannot redirect twice, but this is already redirecting
        }
        
        // Calculate the direction
        Vector3 requestedRobotDistance = (CurrentGrid.GetWorldPositionOfNearestCell(requestedRobot.transform.position) - CurrentGrid.GetWorldPositionOfNearestCell(transform.position)).normalized;
        
        Vector3 roundDirection = new Vector3(
            Mathf.FloorToInt(Mathf.Abs(requestedRobotDistance.x)), // -1 or 1, or 0 when -1<x<1 
            0,
            Mathf.Sign(requestedRobotDistance.z) * Mathf.CeilToInt(Mathf.Abs(requestedRobotDistance.z)) // 0 or -1 when -1<=z<0 or -1 when 0<z<=1
        );
        Vector3 orthogonalDirection = Vector3.Cross(Vector3.up, roundDirection).normalized; // find the orthogonal vector
        
        
        // Check validity and detect obstacles for redirecting right, left, and backward
        bool goRightValid = IsValidRedirectPosition(orthogonalDirection * -1, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectRightGoalCellPosition, out bool isBlockRight);
        bool goLeftValid = IsValidRedirectPosition(orthogonalDirection, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectLeftGoalCellPosition, out bool isBlockLeft);
        bool goBackwardValid = IsValidRedirectPosition(roundDirection * -1, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectBackwardGoalCellPosition, out bool isBlockBackward); 
        bool goForwardValid = IsValidRedirectPosition(roundDirection, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectForwardGoalCellPosition, out bool isBlockForward);
        
        // Determine the final redirect goal position based on validity and obstacles
        Vector3 redirectGoalCellPosition;
        List<Vector3> potentialRedirectGoalCells = new();
        
        // Select randomly no blocking redirect goal
        if (goRightValid && ! isBlockRight)
            potentialRedirectGoalCells.Add(redirectRightGoalCellPosition); 
        if (goLeftValid && ! isBlockLeft)
            potentialRedirectGoalCells.Add(redirectLeftGoalCellPosition);
        if (goBackwardValid && ! isBlockBackward)
            potentialRedirectGoalCells.Add(redirectBackwardGoalCellPosition);
        if (goForwardValid && ! isBlockForward)
            potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
        
        if (potentialRedirectGoalCells.Count != 0) // There is a non-blocking redirect goal
        {
            redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
        }
        else // All redirect goal are block, randomly choose a path that is valid and may redirect other if needed
        {
            if (goRightValid) potentialRedirectGoalCells.Add(redirectRightGoalCellPosition);
            if (goLeftValid) potentialRedirectGoalCells.Add( redirectLeftGoalCellPosition);
            if (goBackwardValid) potentialRedirectGoalCells.Add( redirectBackwardGoalCellPosition);
            if (goForwardValid) potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
            
            if (potentialRedirectGoalCells.Count == 0) // No valid path was found either (usually at corner)
            {
                RedirectToNearestCell(); // Redirect to fit the cell and wait 
                return false;
            }

            redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
        }

        if (CurrentRobotState == RobotStateEnum.Jamming)
        {
            StopCoroutine(JamCoroutine);
        }
        
        //Debug.Log(requestedRobot.gameObject.name + " requested to move " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(redirectGoalCellPosition));
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NearestCell, redirectGoalCellPosition, SetToJam);
        RobotStateMachine.SetToState(RobotStateEnum.Redirecting,
            new object[] { CurrentTask },
            new object[] { robotTask });
        return true;
    }

    private bool IsValidRedirectPosition(Vector3 direction, Vector3 exceptDirection, Vector3 detectedRobotGoalPosition, out Vector3 redirectGoalCellPosition, out bool isBlockAhead)
    {
        var redirectIndex = CurrentGrid.GetIndex(transform.position + direction * 1);
        redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectIndex.x, redirectIndex.y) + Vector3.up * transform.position.y;
        isBlockAhead = false;
        
        foreach (var nearbyRobot in NearbyRobots)
        {
            bool isBlockingGoal = IsBlockAHead(nearbyRobot, redirectGoalCellPosition);
            if (isBlockingGoal)
            {
                switch (nearbyRobot.CurrentRobotState)
                {
                    case RobotStateEnum.Idle:
                        isBlockAhead = false;
                        break;
                    case RobotStateEnum.Handling:
                        isBlockAhead = true;
                        return false;
                    case RobotStateEnum.Delivering:
                    case RobotStateEnum.Approaching:
                    case RobotStateEnum.Jamming:
                    case RobotStateEnum.Redirecting:
                    default:
                        isBlockAhead = true;
                        break;
                }
            }
        }
        
        return CurrentGrid.CheckValidCell(redirectIndex.x, redirectIndex.y) && exceptDirection != direction && detectedRobotGoalPosition != redirectGoalCellPosition;
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;

        Vector3 goalCellPosition = crate.transform.position + Vector3.up * transform.position.y;
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, ArriveCrateSource, 0);
        
        RobotStateMachine.SetToState(RobotStateEnum.Approaching, 
            new object[]{CurrentTask}, 
            new object[]{robotTask});
    }

    #endregion
}
