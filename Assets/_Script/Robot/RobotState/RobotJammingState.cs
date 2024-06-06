using System;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotJammingState : RobotState
        {
            private IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> _pathfindingAlgorithm;
            
            private float _currentWaitTime;
            private bool _isWaitingForGoal;
            
            public RobotJammingState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null) : base(robot, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += JamSetUp;
                ExecuteEvents += Jamming;
                ExitEvents += EndJamming;
                
                _pathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
            }

            private void JamSetUp(ITransitionData enterParameters)
            {
                _currentWaitTime = 0;

                var task = enterParameters.CastTo<RobotJammingTask>();
                _isWaitingForGoal = task.IsWaitingForGoal;
                
                
                if (Robot.LastCellPosition == RobotTransform.position)
                {
                    Robot.NextCell = Robot.LastCell;
                    Robot.IsMidwayMove = false;
                }

                if (Robot.NextCellPosition == Robot.transform.position)
                {
                    Robot.LastCell = Robot.NextCell;
                    Robot.IsMidwayMove = false;
                }
                
                var (transitedState, transitionData) = Robot.RobotStateMachine.PeakHistory();
                if (transitionData == null) return;

                var movingTask = transitionData.CastTo<RobotMovingTask>();
                Robot.MovingPath = _pathfindingAlgorithm.FirstTimeFindPath(Robot.LastCell, Grid.GetCell(movingTask.GoalCellPosition));

            }
            

            private void Jamming(ITransitionData enterParameters)
            {
                _currentWaitTime += Time.fixedDeltaTime;

                if (_currentWaitTime >= RobotUtility.GetTimeMoveTo1Cell(Robot) + 0.001)
                {
                    Robot.RobotStateMachine.RestoreState();
                }
            }
            
            
            private void EndJamming(ITransitionData enterParameters)
            {
                if (Robot.CurrentBinTransportTask != null)
                {
                    if (_isWaitingForGoal)
                    {
                        Robot.CurrentBinTransportTask.WaitingForGoalTime += _currentWaitTime;
                    }
                    else
                    {
                        Robot.CurrentBinTransportTask.JammingTime += _currentWaitTime;
                    }

                }

            }
        }
    }
}