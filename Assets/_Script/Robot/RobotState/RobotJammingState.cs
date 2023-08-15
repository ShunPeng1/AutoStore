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
                    Robot.NextCellPosition = Robot.LastCellPosition;
                    Robot.IsMidwayMove = false;
                }

                if (Robot.NextCellPosition == Robot.transform.position)
                {
                    Robot.LastCellPosition = Robot.NextCellPosition;
                    Robot.IsMidwayMove = false;
                }
            
            }

            private void Jamming(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                _currentWaitTime += Time.fixedDeltaTime;

                if (_currentWaitTime >= Robot.JamWaitTime)
                {
                    Robot.RobotStateMachine.RestoreState();
                }
            }
            
        }
    }
}