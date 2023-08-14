using System;
using System.Collections.Generic;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{

    public abstract partial class Robot
    {
        
        public class RobotMovingState : RobotState
        {
            protected RobotTask CurrentTask;
        
            protected enum DetectDecision
            {
                Ignore = 0,
                Wait = 1,
                Dodge = 2,
                Deflected = 3
            }
            
            public RobotMovingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += AssignTask;
                ExecuteEvents += MovingStateExecute;
            }

            private void AssignTask(RobotStateEnum lastRobotState, IStateParameter enterParameters)
            {
                if (enterParameters == null) return;

                CurrentTask = enterParameters.Get<RobotTask>();
                if (CurrentTask == null) return;

                switch (CurrentTask.StartCellPosition)
                {
                    case RobotTask.StartPosition.LastCell:
                        CreateInitialPath(Robot.LastCellPosition, CurrentTask.GoalCellPosition);
                        ExtractNextCellInPath();
                        break;

                    case RobotTask.StartPosition.NextCell:
                        CreateInitialPath(Robot.NextCellPosition, CurrentTask.GoalCellPosition);
                        Robot.MovingPath.RemoveFirst();
                        break;

                    case RobotTask.StartPosition.NearestCell:
                        Vector3 nearestCellPosition = Grid.GetWorldPositionOfNearestCell(RobotTransform.position);

                        if (nearestCellPosition == Robot.LastCellPosition)
                        {
                            CreateInitialPath(nearestCellPosition, CurrentTask.GoalCellPosition);
                            ExtractNextCellInPath();
                        }
                        else if (nearestCellPosition == Robot.NextCellPosition)
                        {
                            if (CreateInitialPath(Robot.NextCellPosition, CurrentTask.GoalCellPosition))
                                Robot.MovingPath.RemoveFirst();
                        }
                        else
                        {
                            Debug.LogError(Robot.gameObject.name + " THE NEAREST CELL IS NOT LAST OR NEXT CELL " +
                                           nearestCellPosition);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CheckArriveGoalCell();
            }
            
            private void MovingStateExecute(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                if (RobotUtility.CheckArriveOnNextCell(Robot))
                {
                    Robot.IsMidwayMove = false;
                    ExtractNextCellInPath();
                    
                    if (!CheckArriveGoalCell()) return; // change state during executing this function
                }

                Robot.DetectNearByRobot();
                if (!DecideFromRobotDetection()) return; // change state during executing this function
                
                Robot.IsMidwayMove = true;
                Robot.MoveAlongGrid();
            }

            protected virtual bool CheckArriveGoalCell()
            {
                if (! RobotUtility.CheckRobotOnGoal(Grid, Robot, CurrentTask)) return true;
                
                CurrentTask.GoalArrivalAction?.Invoke();
                return false;

            }

            #region DETECTION_DECISION
            

            protected virtual bool DecideFromRobotDetection()
            {
                List<GridXZCell<CellItem>> dynamicObstacle = new();
                DetectDecision finalDecision = DetectDecision.Ignore; 
                
                foreach (var detectedRobot in Robot.NearbyRobots)
                {
                    
                    DetectDecision decision = DecideOnDetectedRobot(detectedRobot);
                    finalDecision = (DetectDecision) Mathf.Max((int)decision, (int)finalDecision);

                    dynamicObstacle.Add(Grid.GetCell(detectedRobot.LastCellPosition));
                    if(detectedRobot.IsMidwayMove) dynamicObstacle.Add(Grid.GetCell(detectedRobot.NextCellPosition));
                }
                
                switch (finalDecision)
                {
                    case DetectDecision.Ignore:
                        return true;
                    
                    case DetectDecision.Wait: // We set the robot to jam state
                        //Debug.Log(gameObject.name +" Jam! ");
                        Robot.SetToJam();
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

            protected virtual DetectDecision DecideOnDetectedRobot(Robot detectedRobot)
            {
                float dotProductOf2RobotDirection = RobotUtility.DotOf2RobotMovingDirection(Robot, detectedRobot);
                bool isUnsafeDistanceOf2Robot = Robot.CheckRobotSafeDistance(detectedRobot);
                bool isBlockAHead = RobotUtility.CheckRobotBlockAHead(detectedRobot, Robot.NextCellPosition);
                bool isBlockingGoal = RobotUtility.CheckRobotBlockGoal(detectedRobot, CurrentTask);
                
                switch (detectedRobot.CurrentRobotState)
                {
                    /* Idle state cases */
                    case RobotStateEnum.Idle when isBlockingGoal || isBlockAHead: // If they are standing on this robot goal or blocking ahead of this robot
                        return TryRedirectRobot(detectedRobot);

                    case RobotStateEnum.Idle: // Not blocking at all
                        return DetectDecision.Ignore;
                    
                    
                    /* Jamming state cases */
                    case RobotStateEnum.Jamming when isBlockAHead: // Currently blocking in between the next cell
                        //return DetectDecision.Dodge;
                        return TryRedirectRobot(detectedRobot);

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
                                return TryRedirectRobot(detectedRobot);

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

            private DetectDecision TryRedirectRobot(Robot detectedRobot)
            {
                return detectedRobot.RedirectToOrthogonalCell(Robot, Robot.NextCellPosition) ? DetectDecision.Wait : 
                    Robot.RedirectToOrthogonalCell(detectedRobot, detectedRobot.NextCellPosition) ? DetectDecision.Deflected : DetectDecision.Dodge;
            }
            
            #endregion

            #region PATHFINDING

            private void ExtractNextCellInPath()
            {
                if (Robot.MovingPath == null || Robot.MovingPath.Count == 0)
                {
                    Robot.LastCellPosition = Robot.NextCellPosition;
                    return;
                }
                var nextNextCell = Robot.MovingPath.First.Value;
                Robot.MovingPath.RemoveFirst(); // the next standing node

                Vector3 nextNextCellPosition = Grid.GetWorldPositionOfNearestCell(nextNextCell.XIndex, nextNextCell.YIndex);

                Robot.LastCellPosition = Robot.NextCellPosition;
                Robot.NextCellPosition = nextNextCellPosition;
            
            }

            private bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition)
            {
                var startCell = Grid.GetCell(startPosition);
                var endCell = Grid.GetCell(endPosition);
        
                Robot.MovingPath = Robot._pathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

                if (Robot.MovingPath != null) return true; 
        
                // No destination was found
                Robot.RedirectToNearestCell();

                return false;

            }

            private bool UpdateInitialPath(List<GridXZCell<CellItem>> dynamicObstacle)
            {
                Vector3 nearestCellPosition = Grid.GetWorldPositionOfNearestCell(RobotTransform.position);
                var currentStartCell = Grid.GetCell(nearestCellPosition);

                Robot.MovingPath = Robot._pathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);

                if (Robot.MovingPath == null) // The path to goal is block
                {
                    Robot.RedirectToNearestCell();
                    return false;
                }
        
                if (nearestCellPosition == Robot.LastCellPosition)
                {
                    ExtractNextCellInPath();
                    return true;
                }
        
                if (nearestCellPosition == Robot.NextCellPosition)
                {
                    Robot.MovingPath.RemoveFirst();
                    return true;
                }

                //Debug.LogError( gameObject.name+" THE NEAREST CELL IS NOT LAST OR NEXT CELL "+ nearestCellPosition);

                return false;
            }
            
            #endregion
        }
    }
}