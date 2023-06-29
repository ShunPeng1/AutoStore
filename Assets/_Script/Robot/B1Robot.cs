using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using Random = UnityEngine.Random;

public class B1Robot : Robot
{
    [Header("Collider Casting")] 
    [SerializeField] private Transform _centerBodyCast;
    [SerializeField] private float _castRadius = 1.5f;
    [SerializeField] private LayerMask _robotLayerMask;
    [SerializeField] private float _safeDistanceAhead = 1.5f;

    private float MIN_BLOCK_AHEAD_ANGLE => Mathf.Atan((_castRadius + BoxColliderSize/2)/(0.5f + BoxColliderSize/2)) * Mathf.PI;
    private float MAX_BLOCK_AHEAD_ANGLE = 45f;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(_centerBodyCast.position, _castRadius);
    }

    #region DETECTION

    private enum DetectDecision
    {
        Ignore = 0,
        Wait = 1,
        Dodge = 2
    }
    protected override void DetectNearByRobot(RobotStateEnum currentRobotState, object[] parameters)
    {
        var hits = Physics.OverlapSphere(_centerBodyCast.position, _castRadius, _robotLayerMask); // Find robot in a circle 

        List<GridXZCell<StackStorage>> dynamicObstacle = new();
        DetectDecision finalDecision = DetectDecision.Ignore; 
        
        foreach (var hitCollider in hits)
        { 
            var detectedRobot = hitCollider.gameObject.GetComponent<Robot>();
            if (detectedRobot == this) // This robot itself
            {
                continue;
            }

            DetectDecision decision = CheckDetection(detectedRobot);
            finalDecision = (DetectDecision) Mathf.Max((int)decision, (int)finalDecision);

            dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.LastCellPosition));
            dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.NextCellPosition));
        }
        
        switch (finalDecision)
        {
            case DetectDecision.Wait: // We set the robot to jam state
                Debug.Log(gameObject.name +" Jam! ");
                SetToJam();
                break;
            case DetectDecision.Dodge: // We add the detected robot cell as obstacle
                Debug.Log(gameObject.name +" Dodge ");
                UpdateInitialPath(dynamicObstacle); // Update Path base on dynamic obstacle
                break;
            case DetectDecision.Ignore:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    private DetectDecision CheckDetection(Robot detectedRobot)
    {
        float dotProductOf2RobotDirection = Vector3.Dot(NextCellPosition - LastCellPosition,detectedRobot.NextCellPosition - detectedRobot.LastCellPosition);
        bool isUnsafeDistanceOf2Robot = Vector3.Distance(transform.position, detectedRobot.transform.position) <= _safeDistanceAhead ;
        bool isMinBlockingAhead = IsBlockAHead(detectedRobot, MIN_BLOCK_AHEAD_ANGLE);
        bool isMaxBlockingAhead = IsBlockAHead(detectedRobot, MAX_BLOCK_AHEAD_ANGLE);
        bool isOccupyingGoal = detectedRobot.NextCellPosition == CurrentTask.GoalCellPosition 
                               || detectedRobot.LastCellPosition == CurrentTask.GoalCellPosition;
        switch (detectedRobot.CurrentBaseState.MyStateEnum)
        {
            /* Idle state cases */
            case RobotStateEnum.Idle when isOccupyingGoal || isMinBlockingAhead: 
                // If they are standing on this robot goal or blocking ahead of this robot
                return detectedRobot.RedirectToOrthogonalCell(this,  CurrentTask.GoalCellPosition) ? DetectDecision.Wait : DetectDecision.Dodge;
            
            case RobotStateEnum.Idle: // Not blocking at all
                return DetectDecision.Ignore;
            
            
            /* Jamming state cases */
            case RobotStateEnum.Jamming when !isMaxBlockingAhead: //  is not block in between the next cell
                return DetectDecision.Ignore; 
            
            // Currently blocking in between the next cell , and they are standing on this robot goal
            case RobotStateEnum.Jamming when isOccupyingGoal:
                return detectedRobot.RedirectToOrthogonalCell(this, CurrentTask.GoalCellPosition) ? DetectDecision.Wait : DetectDecision.Dodge;
            
            case RobotStateEnum.Jamming: // Currently blocking in between the next cell, and not on this robot goal
                return DetectDecision.Dodge;
            
            
            /* Handling states cases */
            case RobotStateEnum.Handling when !isMinBlockingAhead: //  is not block ahead
                return DetectDecision.Ignore; 
            
            // Currently blocking in between the next cell , and they are standing on this robot goal
            case RobotStateEnum.Handling when isOccupyingGoal:
                return DetectDecision.Wait;
            
            case RobotStateEnum.Handling: // Currently blocking in between the next cell, and not on this robot goal
                return DetectDecision.Dodge;
            

            /* These are the rest of moving state */
            case RobotStateEnum.Delivering:
            case RobotStateEnum.Approaching:
            case RobotStateEnum.Redirecting:
            default:
                if (Math.Abs(dotProductOf2RobotDirection - (-1)) < 0.01f) // 2 robots moving at opposite direction 
                {
                    if(!isMinBlockingAhead) return DetectDecision.Ignore; // same row or column
            
                    // Is block ahead
                    if (isOccupyingGoal) // If they are standing on this robot goal
                    {
                        return detectedRobot.RedirectToOrthogonalCell(this,  CurrentTask.GoalCellPosition) ? DetectDecision.Wait : DetectDecision.Dodge;
                    }
                    else return DetectDecision.Dodge;
                }

                if (Math.Abs(dotProductOf2RobotDirection - 1) < 0.01f && isMinBlockingAhead && isUnsafeDistanceOf2Robot) 
                    // 2 robot moving same direction and smaller than safe distance
                {
                    Debug.Log(gameObject.name + " Keep safe distance ahead with "+detectedRobot.gameObject.name);
                    return DetectDecision.Wait;
                }
                
                if (dotProductOf2RobotDirection == 0) // perpendicular direction
                {
                    return isMaxBlockingAhead ? DetectDecision.Wait : DetectDecision.Ignore;
                }
        
                return DetectDecision.Ignore;
        }
        
    }

    private bool IsBlockAHead(Robot detectedRobot, float isHeadAngleThreshold)
    {
         /*
        var thisRobotCenterPosition = this.transform.position;
        var detectedRobotCenterPosition = detectedRobot.transform.position;
        float angleBetweenMyDirectionAndRobotDistance = Vector3.Angle(detectedRobotCenterPosition - thisRobotCenterPosition, this.NextCellPosition - thisRobotCenterPosition) ;

        if (angleBetweenMyDirectionAndRobotDistance >= isHeadAngleThreshold  )  // Not block ahead when larger than angle threadhold 
            return false;
        // */
        
        if (this.NextCellPosition == detectedRobot.NextCellPosition ||
            this.NextCellPosition == detectedRobot.LastCellPosition) // definitely being block by detected robot's last cell or next cell
            return true;
        else return false;
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
    
    protected override bool UpdateInitialPath(List<GridXZCell<StackStorage>> dynamicObstacle)
    {
        Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position) + Vector3.up * transform.position.y;
        var currentStartCell = CurrentGrid.GetCell(nearestCellPosition);

        if (nearestCellPosition == LastCellPosition)
        {
            MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);
            
            if (MovingPath == null) // The path to goal is block
            {
                RedirectToNearestCell();
                return false;
            }
            ExtractNextCellInPath();
            return true;
        }
        if (nearestCellPosition == NextCellPosition)
        {
            MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);
                
            if (MovingPath == null) // The path to goal is block
            {
                RedirectToNearestCell();
                return false;
            }
            MovingPath.RemoveFirst();
            return true;
        }

        return false;
    }

    #endregion

    #region TASK_FUNCTIONS

    protected override void RedirectToNearestCell()
    {
        Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) + Vector3.up * transform.position.y;
            
        Debug.Log( gameObject.name+ " Redirect To Nearest Cell " + nearestCellPosition);
            
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NearestCell, nearestCellPosition, SetToJam);
        SetToState(RobotStateEnum.Redirecting,
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
        if (CurrentBaseState.MyStateEnum == RobotStateEnum.Jamming)
        {
            StopCoroutine(JamCoroutine); // Destroy the Jamming State, to restore the LastRobotState
        }

        if (CurrentBaseState.MyStateEnum == RobotStateEnum.Redirecting)
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
        bool goRightValid = IsValidRedirectPosition(orthogonalDirection * -1, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectRightGoalCellPosition, out bool isNotBlockRight);
        bool goLeftValid = IsValidRedirectPosition(orthogonalDirection, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectLeftGoalCellPosition, out bool isNotBlockLeft);
        bool goBackwardValid = IsValidRedirectPosition(roundDirection * -1, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectBackwardGoalCellPosition, out bool isNotBlockBackward); 
        bool goForwardValid = IsValidRedirectPosition(roundDirection, requestedRobotDistance, requestedRobotGoalPosition, out Vector3 redirectForwardGoalCellPosition, out bool isNotBlockForward);
        
        // Determine the final redirect goal position based on validity and obstacles
        Vector3 redirectGoalCellPosition;
        List<Vector3> potentialRedirectGoalCells = new();
        if (goRightValid && isNotBlockRight)
            redirectGoalCellPosition = redirectRightGoalCellPosition;
        else if (goLeftValid && isNotBlockLeft)
            redirectGoalCellPosition = redirectLeftGoalCellPosition;
        else if (goBackwardValid && isNotBlockBackward)
            redirectGoalCellPosition = redirectBackwardGoalCellPosition;
        else if (goForwardValid && isNotBlockForward)
            redirectGoalCellPosition = redirectForwardGoalCellPosition;
        else
        {
            if (goRightValid) potentialRedirectGoalCells.Add(redirectRightGoalCellPosition);
            if (goLeftValid) potentialRedirectGoalCells.Add( redirectLeftGoalCellPosition);
            if (goBackwardValid) potentialRedirectGoalCells.Add( redirectBackwardGoalCellPosition);
            if (goForwardValid) potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
            
            if (potentialRedirectGoalCells.Count == 0)
            {
                SetToJam(); // the only choice is staying where it is 
                return false;
            }

           redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
        }
        
        Debug.Log(requestedRobot.gameObject.name + " requested to move " + gameObject.name + " from " + CurrentGrid.GetXZ(transform.position) + " to " + redirectGoalCellPosition);
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NearestCell, redirectGoalCellPosition, SetToJam);
        SetToState(RobotStateEnum.Redirecting,
            new object[] { CurrentTask },
            new object[] { robotTask });
        return true;
    }

    private bool IsValidRedirectPosition(Vector3 direction, Vector3 exceptDirection, Vector3 detectedRobotGoalPosition, out Vector3 redirectGoalCellPosition, out bool isNotBlockAHead)
    {
        var raycast = Physics.RaycastAll(transform.position, direction, _castRadius, _robotLayerMask);
        var (redirectX, redirectZ) = CurrentGrid.GetXZ(transform.position + direction * 1);

        isNotBlockAHead = raycast.Length == 0;
        redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectX, redirectZ) + Vector3.up * transform.position.y;

        return CurrentGrid.IsValidCell(redirectX, redirectZ) && exceptDirection != direction && detectedRobotGoalPosition != redirectGoalCellPosition;
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;

        Vector3 goalCellPosition = crate.transform.position + Vector3.up * transform.position.y;
        RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, ArriveCrateSource, 0);
        
        SetToState(RobotStateEnum.Approaching, 
            new object[]{CurrentTask}, 
            new object[]{robotTask});
    }

    #endregion
}
