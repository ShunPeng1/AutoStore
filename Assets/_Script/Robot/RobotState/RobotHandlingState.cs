using System;
using Shun_State_Machine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotHandlingState : RobotState
        {
            
            
            public RobotHandlingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += BeginHandling;
            }

            private void BeginHandling(RobotStateEnum arg1, IStateParameter arg2)
            {
                Robot.ExtendCable();
            }
            
        }
    }
}   