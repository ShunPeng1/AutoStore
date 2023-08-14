using System;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    
    public class RobotState : BaseState<RobotStateEnum>
    {
        protected readonly Robot Robot;
        protected readonly Transform RobotTransform;
        protected readonly GridXZ<CellItem> Grid;

        public RobotState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(myStateEnum, executeEvents, exitEvents, enterEvents)
        {
            Robot = robot;
            RobotTransform = Robot.transform;
            Grid = Robot.CurrentGrid;
        }
        
        
    }
}