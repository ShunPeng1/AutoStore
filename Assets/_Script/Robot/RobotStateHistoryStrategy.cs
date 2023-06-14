using _Script.StateMachine;

namespace _Script.Robot
{
    public class RobotStateHistoryStrategy : IStateHistoryStrategy<RobotStateEnum>
    {
        private BaseState<RobotStateEnum> _oldStateEnum;
        private object[] _exitOldStateParameters = null;
        private object[] _enterNewStateParameters = null;
            
        public void Save(BaseState<RobotStateEnum> stateEnum, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            if (stateEnum.MyStateEnum is RobotStateEnum.Jamming or RobotStateEnum.Redirecting) return;
            
            _oldStateEnum = stateEnum;
            _exitOldStateParameters = exitOldStateParameters;
            _enterNewStateParameters = enterNewStateParameters;

        }

        public (BaseState<RobotStateEnum> enterStateEnum, object[] exitOldStateParameters, object[] enterNewStateParameters) Restore(
            bool isRemoveRestore = true)
        {
            return (_oldStateEnum, _exitOldStateParameters, _enterNewStateParameters);
        }
    }
}