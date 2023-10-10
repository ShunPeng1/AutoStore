using System;
using System.Collections.Generic;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{

    public abstract partial class Robot
    {
        
        public class RobotMovingStateNoServer : RobotState
        {
            protected RobotMovingTask CurrentMovingTask;
        
            protected enum DetectDecision
            {
                Ignore = 0,
                Wait = 1,
                Dodge = 2,
                Deflected = 3
            }
            
            public RobotMovingStateNoServer(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += AssignTask;
                ExecuteEvents += Execute;
            }

            private void AssignTask(RobotStateEnum lastRobotState, IStateParameter enterParameters)
            {
                if (enterParameters == null) return;

                CurrentMovingTask = enterParameters.Get<RobotMovingTask>();
                if (CurrentMovingTask == null) return;

                switch (CurrentMovingTask.StartCellPosition)
                {
                    case RobotMovingTask.StartPosition.LastCell:
                        CreateInitialPath(Robot.LastCellPosition, CurrentMovingTask.GoalCellPosition);
                        ExtractNextCellInPath();
                        break;

                    case RobotMovingTask.StartPosition.NextCell:
                        CreateInitialPath(Robot.NextCellPosition, CurrentMovingTask.GoalCellPosition);
                        Robot.MovingPath.RemoveFirst();
                        break;

                    case RobotMovingTask.StartPosition.NearestCell:
                        Vector3 nearestCellPosition = Grid.GetWorldPositionOfNearestCell(RobotTransform.position);

                        if (nearestCellPosition == Robot.LastCellPosition)
                        {
                            CreateInitialPath(nearestCellPosition, CurrentMovingTask.GoalCellPosition);
                            ExtractNextCellInPath();
                        }
                        else if (nearestCellPosition == Robot.NextCellPosition)
                        {
                            if (CreateInitialPath(Robot.NextCellPosition, CurrentMovingTask.GoalCellPosition))
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

                if (RobotUtility.CheckArriveOnNextCell(Robot))
                {
                    Robot.IsMidwayMove = false;
                    ExtractNextCellInPath();
                    
                    if (!CheckArriveGoalCell()) return; // change state during executing this function
                }
            }
            
            private void Execute(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                if (RobotUtility.CheckArriveOnNextCell(Robot))
                {
                    Robot.IsMidwayMove = false;
                    ExtractNextCellInPath();
                    
                    if (!CheckArriveGoalCell()) return; // change state during executing this function
                }

                Robot.DetectNearByRobot();
                if (!DecideFromRobotDetection()) return; // change state during executing this function
                
                Robot.MoveAlongGrid();
            }

            protected virtual bool CheckArriveGoalCell()
            {
                if (! RobotUtility.CheckRobotOnGoal(Grid, Robot, CurrentMovingTask)) return true;
                
                CurrentMovingTask.GoalArrivalAction?.Invoke();
                return false;

            }

            #region DETECTION_DECISION
            

            protected virtual bool DecideFromRobotDetection()
            {
                List<GridXZCell<CellItem>> dynamicObstacle = new();
                DetectDecision finalDecision = DetectDecision.Ignore; 
                
                foreach (var detectedRobot in Robot.NearbyRobots)
                {
                    var nearestCellPosition = Grid.GetWorldPositionOfNearestCell(detectedRobot.transform.position);
                    var nearestCell = Grid.GetCell(nearestCellPosition);
                    dynamicObstacle.Add(nearestCell);
                    
                    foreach (var baseGridCell2D in nearestCell.OutDegreeCells)
                    {
                        var outCell = (GridXZCell<CellItem>)baseGridCell2D;
                        var xDirection = outCell.XIndex - nearestCell.XIndex;
                        var zDirection = outCell.YIndex - nearestCell.YIndex;
                        if (MapManager.Instance.IsMovableFromDirection(xDirection,zDirection)) dynamicObstacle.Add(outCell);
                    }
                    
                }
                if (Robot.NearbyRobots.Count != 0) finalDecision = DetectDecision.Dodge;
                
                
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
                bool isBlockingGoal = RobotUtility.CheckRobotBlockGoal(detectedRobot, CurrentMovingTask);
                
                switch (detectedRobot.CurrentRobotState)
                {
                    /* Idle state cases */
                    case RobotStateEnum.Idling when isBlockingGoal || isBlockAHead: // If they are standing on this robot goal or blocking ahead of this robot
                        return TryRedirectRobot(detectedRobot);

                    case RobotStateEnum.Idling: // Not blocking at all
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
                    Robot.LastCell = Robot.NextCell;
                    return;
                }
                var nextNextCell = Robot.MovingPath.First.Value;
                Robot.MovingPath.RemoveFirst(); // the next standing node
                
                Robot.LastCell = Robot.NextCell;
                Robot.NextCell = nextNextCell;
            
            }

            private bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition)
            {
                var startCell = Grid.GetCell(startPosition);
                var endCell = Grid.GetCell(endPosition);
        
                Robot.MovingPath = Robot._pathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

                if (Robot.MovingPath != null && Robot.MovingPath.Count != 0) return true; 
        
                // No destination was found
                Robot.RedirectToNearestCell();

                return false;

            }

            private bool UpdateInitialPath(List<GridXZCell<CellItem>> dynamicObstacle)
            {
                Vector3 nearestCellPosition = Grid.GetWorldPositionOfNearestCell(RobotTransform.position);
                var currentStartCell = Grid.GetCell(nearestCellPosition);

                Robot.MovingPath = Robot._pathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle, true);

                if (Robot.MovingPath == null || Robot.MovingPath.Count == 0) // The path to goal is block
                {
                    Robot.RedirectToNearestCell();
                    return false;
                }
        
                if (nearestCellPosition == Robot.LastCellPosition)
                {
                    var nextNextCell = Robot.MovingPath.First.Value;

                    Robot.MovingPath.RemoveFirst();

                    if (Robot.MovingPath.First != null && Robot.MovingPath.First.Value == Robot.NextCell)
                    {
                        Robot.MovingPath.RemoveFirst();
                    }
                    else
                    {
                        Robot.LastCell = Robot.NextCell;
                        Robot.NextCell = nextNextCell;
                    }
                    
                    return true;
                }
                
                if (nearestCellPosition == Robot.LastCellPosition && Robot.IsMidwayMove)
                {
                    Robot.MovingPath.RemoveFirst();
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