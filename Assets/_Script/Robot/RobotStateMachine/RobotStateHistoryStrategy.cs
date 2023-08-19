using Shun_State_Machine;

namespace _Script.Robot
{
    public class RobotStateHistoryStrategy : IStateHistoryStrategy<RobotStateEnum>
    {
        private BaseState<RobotStateEnum> _oldStateEnum;
        private IStateParameter _exitOldStateParameters = null;
        private IStateParameter _enterNewStateParameters = null;
            
        public void Save(BaseState<RobotStateEnum> stateEnum, IStateParameter exitOldStateParameters = null, IStateParameter enterNewStateParameters = null)
        {
            if (stateEnum.MyStateEnum is RobotStateEnum.Jamming or RobotStateEnum.Redirecting) return;
            
            _oldStateEnum = stateEnum;
            _exitOldStateParameters = exitOldStateParameters;
            _enterNewStateParameters = enterNewStateParameters;

        }

        public (BaseState<RobotStateEnum> enterStateEnum, IStateParameter exitOldStateParameters, IStateParameter enterNewStateParameters) Restore(
            bool isRemoveRestore = true)
        {
            return (_oldStateEnum, _exitOldStateParameters, _enterNewStateParameters);
        }
    }
}