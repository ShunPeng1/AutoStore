using Shun_State_Machine;

namespace _Script.Robot
{
    public class RobotStateHistoryStrategy : IStateHistoryStrategy<RobotStateEnum>
    {
        private BaseState<RobotStateEnum> _oldState;
        private IStateParameter _exitOldStateParameters = null;
        private IStateParameter _enterNewStateParameters = null;
            
        public void Save(BaseState<RobotStateEnum> state, IStateParameter exitOldStateParameters = null, IStateParameter enterNewStateParameters = null)
        {
            if (state.MyStateEnum is RobotStateEnum.Jamming or RobotStateEnum.Redirecting) return;
            
            _oldState = state;
            _exitOldStateParameters = exitOldStateParameters;
            _enterNewStateParameters = enterNewStateParameters;

        }

        public (BaseState<RobotStateEnum> enterState, IStateParameter exitOldStateParameters, IStateParameter enterNewStateParameters) Restore(
            bool isRemoveRestore = true)
        {
            return (_oldState, _exitOldStateParameters, _enterNewStateParameters);
        }
    }
}