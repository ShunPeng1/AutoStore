using System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotJammingState : RobotState
        {
            private float _currentWaitTime;
            private bool _isWaitingForGoal;
            
            public RobotJammingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += JamSetUp;
                ExecuteEvents += Jamming;
                ExitEvents += EndJamming;
            }

            private void JamSetUp(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                _currentWaitTime = 0;

                var task = enterParameters.Get<RobotJammingTask>();
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
                
                var (enterState, exitOldStateParameters, enterNewStateParameters) = Robot.RobotStateMachine.PeakHistory();
                if (enterNewStateParameters == null) return;

                var movingTask = enterNewStateParameters.Get<RobotMovingTask>();
                Robot.MovingPath = Robot._pathfindingAlgorithm.FirstTimeFindPath(Robot.LastCell, Grid.GetCell(movingTask.GoalCellPosition));

            }
            

            private void Jamming(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                _currentWaitTime += Time.fixedDeltaTime;

                if (_currentWaitTime >= RobotUtility.GetTimeMoveTo1Cell(Robot) + 0.001)
                {
                    Robot.RobotStateMachine.RestoreState();
                }
            }
            
            
            private void EndJamming(RobotStateEnum currentState, IStateParameter enterParameters)
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