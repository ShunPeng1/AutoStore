using System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{

    public abstract partial class Robot
    {
        public class RobotMovingState : RobotState
        {
            protected RobotTask CurrentTask;
            
            public RobotMovingState(Robot robot, RobotStateEnum myStateEnum,
                Action<RobotStateEnum, IStateParameter> executeEvents = null,
                Action<RobotStateEnum, IStateParameter> exitEvents = null,
                Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents,
                exitEvents, enterEvents)
            {
                enterEvents += AssignTask;
                //executeEvents += MovingStateExecute;
            }

            private void AssignTask(RobotStateEnum lastRobotState, IStateParameter enterParameters)
            {
                if (enterParameters == null) return;

                CurrentTask = enterParameters.Get<RobotTask>();
                if (CurrentTask == null) return;

                switch (CurrentTask.StartCellPosition)
                {
                    case RobotTask.StartPosition.LastCell:
                        Robot.CreateInitialPath(Robot.LastCellPosition, CurrentTask.GoalCellPosition);
                        ExtractNextCellInPath();
                        break;

                    case RobotTask.StartPosition.NextCell:
                        Robot.CreateInitialPath(Robot.NextCellPosition, CurrentTask.GoalCellPosition);
                        Robot.MovingPath.RemoveFirst();
                        break;

                    case RobotTask.StartPosition.NearestCell:
                        Vector3 nearestCellPosition = Robot.CurrentGrid.GetWorldPositionOfNearestCell(Robot.transform.position);

                        if (nearestCellPosition == Robot.LastCellPosition)
                        {
                            Robot.CreateInitialPath(nearestCellPosition, CurrentTask.GoalCellPosition);
                            ExtractNextCellInPath();
                        }
                        else if (nearestCellPosition == Robot.NextCellPosition)
                        {
                            if (Robot.CreateInitialPath(Robot.NextCellPosition, CurrentTask.GoalCellPosition))
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

                CheckArriveCell();
            }

            bool CheckBetween2Cell()
            {
                Robot.IsBetween2Cells = Vector3.Distance(Robot.transform.position, Robot.NextCellPosition) != 0;
                return Robot.IsBetween2Cells;
            }
            
            bool CheckArriveCell()
            {
                if (CurrentTask != null &&
                    Robot.CurrentGrid.GetIndex(Robot.transform.position) == Robot.CurrentGrid.GetIndex(CurrentTask.GoalCellPosition))
                {
                    CurrentTask.GoalArrivalAction?.Invoke();
                    ExtractNextCellInPath();
                    return false;
                }
                
                ExtractNextCellInPath();
                return true;
            }
            
            
            protected void ExtractNextCellInPath()
            {
                if (Robot.MovingPath == null || Robot.MovingPath.Count == 0)
                {
                    Robot.LastCellPosition = Robot.NextCellPosition;
                    return;
                }
                var nextNextCell = Robot.MovingPath.First.Value;
                Robot.MovingPath.RemoveFirst(); // the next standing node

                Vector3 nextNextCellPosition = Robot.CurrentGrid.GetWorldPositionOfNearestCell(nextNextCell.XIndex, nextNextCell.YIndex);

                Robot.LastCellPosition = Robot.NextCellPosition;
                Robot.NextCellPosition = nextNextCellPosition;
            
            }
            
            

        }
    }
}