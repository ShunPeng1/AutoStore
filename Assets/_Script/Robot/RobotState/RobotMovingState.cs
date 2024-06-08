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
            protected RobotMovingTask CurrentMovingTask;
            private IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> _pathfindingAlgorithm;
        
            protected enum DetectDecision
            {
                Ignore = 0,
                Wait = 1,
                Dodge = 2,
                Deflected = 3
            }
            
            public RobotMovingState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null) : base(robot, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += AssignTask;
                ExecuteEvents += Execute;
                
                _pathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
            }

            private void AssignTask(ITransitionData enterParameters)
            {
                if (enterParameters == null) return;

                CurrentMovingTask = enterParameters.CastTo<RobotMovingTask>();
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
                
                //UpdateInitialPath();

                if (RobotUtility.CheckArriveOnNextCell(Robot))
                {
                    Robot.IsMidwayMove = false;
                    ExtractNextCellInPath();
                    
                    if (!CheckArriveGoalCell()) return; // change state during executing this function
                }
            }
            
            private void Execute(ITransitionData enterParameters)
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
                
                Vector3 distance = Robot.MoveAlongGrid();
                
                RecordDistance(distance);   
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
                DetectDecision finalDecision = DetectDecision.Ignore;
                bool isWaitingForGoal = false;
                
                foreach (var detectedRobot in Robot.NearbyRobots)
                {
                    float dotProductOf2RobotDirection = RobotUtility.DotOf2RobotMovingDirection(Robot, detectedRobot);
                    bool isUnsafeDistanceOf2Robot = Robot.CheckRobotSafeDistance(detectedRobot);
                    bool isBlockAHead = RobotUtility.CheckRobotBlockAHead(detectedRobot, Robot.NextCellPosition);
                    bool isBlockingGoal = RobotUtility.CheckRobotBlockGoal(detectedRobot, CurrentMovingTask);
                    
                    DetectDecision decision = DecideOnDetectedRobot(detectedRobot, dotProductOf2RobotDirection, isUnsafeDistanceOf2Robot, isBlockAHead, isBlockingGoal);
                    finalDecision = (DetectDecision) Mathf.Max((int)decision, (int)finalDecision);
                    
                    if (isBlockingGoal && decision == DetectDecision.Wait) isWaitingForGoal = true;
                }
                
                switch (finalDecision)
                {
                    case DetectDecision.Ignore:
                        return true;
                    
                    case DetectDecision.Wait: // We set the robot to jam state
                        //Debug.Log(gameObject.name +" Jam! ");
                        Robot.SetToJam(isWaitingForGoal);
                        return false;
                    
                    case DetectDecision.Dodge: // We add the detected robot cell as obstacle
                        //Debug.Log(gameObject.name +" Dodge ");
                        return UpdateInitialPath(); // Update Path base on dynamic obstacle
                     
                    case DetectDecision.Deflected:
                        return false;
                    
                    default:
                        return true;
                }
            }

            protected virtual DetectDecision DecideOnDetectedRobot(Robot detectedRobot, float dotProductOf2RobotDirection, bool isUnsafeDistanceOf2Robot, bool isBlockAHead, bool isBlockingGoal)
            {
                
                switch (detectedRobot.CurrentRobotState)
                {
                    /* Idle state cases */
                    case RobotIdlingState when isBlockingGoal || isBlockAHead: // If they are standing on this robot goal or blocking ahead of this robot
                        return TryDeflectRobot(detectedRobot);

                    case RobotIdlingState: // Not blocking at all
                        return DetectDecision.Ignore;
                    
                    
                    /* Jamming state cases */
                    case RobotJammingState when isBlockAHead: // Currently blocking in between the next cell
                        //return DetectDecision.Dodge;
                        return TryDeflectRobot(detectedRobot);

                    case RobotJammingState: //  is not blocking ahead in between the next cell
                        return DetectDecision.Ignore; 
                    
                    
                    /* Handling states cases */
                    case RobotHandlingState when !isBlockAHead: //  is not block ahead
                        return DetectDecision.Ignore; 
                    
                    // Currently blocking in between the next cell , and they are standing on this robot goal
                    case RobotHandlingState when isBlockingGoal:
                        return DetectDecision.Wait;
                    
                    case RobotHandlingState: // Currently blocking in between the next cell, and not on this robot goal
                        return DetectDecision.Dodge;
                    

                    /* These are the rest of moving state */
                    default:
                        if (Math.Abs(dotProductOf2RobotDirection - (-1)) < 0.01f ) // opposite direction
                        {
                            if(!isBlockAHead) return DetectDecision.Ignore; // same row or column
                    
                            // Is block ahead
                            if (isBlockingGoal) // If they are standing on this robot goal
                            {
                                return TryDeflectRobot(detectedRobot);

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

            private DetectDecision TryDeflectRobot(Robot detectedRobot)
            {
                return detectedRobot.RedirectRequest(Robot, Robot.NextCellPosition, Robot.GoalCellPosition) ? DetectDecision.Wait : 
                    Robot.RedirectRequest(detectedRobot, Robot.NextCellPosition,  detectedRobot.GoalCellPosition) ? DetectDecision.Deflected : DetectDecision.Dodge;
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
                var lastCell = Robot.LastCell;
                var nextCell = Robot.NextCell;
                
                Robot.MovingPath.RemoveFirst(); // the next standing node
                
                Robot.LastCell = Robot.NextCell;
                Robot.NextCell = nextNextCell;

                RecordPathUpdate();
                
                CheckTurn(lastCell, nextCell, nextNextCell);
                
            }

            private void CheckTurn(GridXZCell<CellItem> lastCell, GridXZCell<CellItem> nextCell, GridXZCell<CellItem> nextNextCell)
            {
                var lastCellPosition = Grid.GetWorldPositionOfNearestCell(lastCell);
                var nextCellPosition = Grid.GetWorldPositionOfNearestCell(nextCell);
                var nextNextCellPosition = Grid.GetWorldPositionOfNearestCell(nextNextCell);

                if (nextCellPosition - lastCellPosition == nextNextCellPosition - nextCellPosition) return;
                
                
                RecordPathTurn();
            }

            private bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition)
            {
                var startCell = Grid.GetCell(startPosition);
                var endCell = Grid.GetCell(endPosition);
        
                Robot.MovingPath = _pathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

                RecordPathChange();
                if (Robot.MovingPath != null) return true; 
        
                // No destination was found
                Robot.RedirectToNearestCell();

                return false;

            }

            private bool UpdateInitialPath()
            {
                List<GridXZCell<CellItem>> allRobotObstacleCells = Robot.GetAllRobotObstacleCells();
                
                Vector3 nearestCellPosition = Grid.GetWorldPositionOfNearestCell(RobotTransform.position);
                var currentStartCell = Grid.GetCell(nearestCellPosition);

                Robot.MovingPath = _pathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, allRobotObstacleCells);

                if (Robot.MovingPath == null) // The path to goal is block
                {
                    //List<GridXZCell<CellItem>> nonIdleRobotObstacleCells = Robot.GetNonIdleRobotObstacleCells();
                    //Robot.MovingPath = Robot._pathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, nonIdleRobotObstacleCells);
                    
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

                RecordPathChange();
                
                return false;
            }
            
            #endregion
        }
    }
}