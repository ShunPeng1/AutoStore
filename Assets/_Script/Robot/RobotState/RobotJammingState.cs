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
            
            public RobotJammingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += JamSetUp;
                ExecuteEvents += Jamming;
            }

            private void JamSetUp(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                _currentWaitTime = 0;
                
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
            
            }

            private void Jamming(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                _currentWaitTime += Time.fixedDeltaTime;

                //if (_currentWaitTime >= RobotUtility.GetTimeMoveTo1Cell(Robot) + 0.001)
                if (_currentWaitTime >= RobotUtility.GetTimeMoveTo1Cell(Robot)/2)
                {
                    Robot.RobotStateMachine.RestoreState();
                }
            }
            
        }
    }
}