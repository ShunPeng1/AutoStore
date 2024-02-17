using System;
using Shun_State_Machine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotIdlingState : RobotState
        {
            public RobotIdlingState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null) : base(robot, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += MoveIntoCell;

            }
            
            private void MoveIntoCell(ITransitionData enterParameters)
            {
                if (Robot.transform.position != Robot.LastCellPosition) 
                    Robot.RedirectToNearestCell();
            }
        }
    }
}