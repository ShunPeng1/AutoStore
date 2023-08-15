using System;
using Shun_State_Machine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotIdlingState : RobotState
        {
            public RobotIdlingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += MoveIntoCell;

            }
            
            private void MoveIntoCell(RobotStateEnum currentState, IStateParameter enterParameters)
            {
                if (Robot.transform.position != Robot.LastCellPosition) 
                    Robot.RedirectToNearestCell();
            }
        }
    }
}