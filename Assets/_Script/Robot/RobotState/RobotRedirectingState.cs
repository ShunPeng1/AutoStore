using System;
using Shun_State_Machine;

namespace _Script.Robot
{

    public abstract partial class Robot
    {

        public class RobotRedirectingState : RobotMovingState
        {
            public RobotRedirectingState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null) : base(robot, executeEvents, exitEvents, enterEvents)
            {
            }
        }
    }
}