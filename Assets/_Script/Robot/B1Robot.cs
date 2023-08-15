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

    protected override bool CheckRobotSafeDistance(Robot checkRobot)
    {
        return Vector3.Distance(transform.position, checkRobot.transform.position) <= CastRadius ;
    }

    
    #endregion

    #region TASK_FUNCTIONS

    protected override void RedirectToNearestCell()
    {
        Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);
        
        //Debug.Log( gameObject.name+ " Redirect To Nearest Cell " + nearestCellPosition);
            
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NearestCell, nearestCellPosition, SetToJam);
        RobotStateMachine.SetToState(RobotStateEnum.Redirecting, null , robotTask );
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
        RobotStateMachine.SetToState(RobotStateEnum.Redirecting, null, robotTask );
        return true;
    }

    private bool IsValidRedirectPosition(Vector3 direction, Vector3 exceptDirection, Vector3 detectedRobotGoalPosition, out Vector3 redirectGoalCellPosition, out bool isBlockAhead)
    {
        var redirectIndex = CurrentGrid.GetIndex(transform.position + direction * 1);
        redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectIndex.x, redirectIndex.y);
        isBlockAhead = false;
        
        foreach (var nearbyRobot in NearbyRobots)
        {
            bool isBlockingGoal = RobotUtility.CheckRobotBlockAHead(nearbyRobot, redirectGoalCellPosition);
            if (isBlockingGoal)
            {
                switch (nearbyRobot.CurrentRobotState)
                {
                    case RobotStateEnum.Idling:
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

        Vector3 goalCellPosition = crate.transform.position;
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, ArriveCrateSource, 0);
        
        RobotStateMachine.SetToState(RobotStateEnum.Approaching, null, robotTask);
    }

    #endregion
}
