using System;
using Shun_State_Machine;

namespace _Script.Robot
{
    
    public class RobotState : BaseState<RobotStateEnum>
    {
        protected Robot Robot;
        
        public RobotState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(myStateEnum, executeEvents, exitEvents, enterEvents)
        {
            Robot = robot;
        }
        
        
    }
}