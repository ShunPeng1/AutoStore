using Shun_State_Machine;

namespace _Script.Robot
{
    public class RobotStateHistoryStrategy : IStateHistoryStrategy
    {
        private IState _oldState;
        private ITransitionData _exitOldStateParameters = null;
        

        public void Save(IState transitionState, ITransitionData transitionData)
        {
            if (transitionState is Robot.RobotJammingState or Robot.RobotRedirectingState) return;
            
            _oldState = transitionState;
            _exitOldStateParameters = transitionData;
        }

        public (IState transitionState, ITransitionData transitionData) Restore(bool isRemoveRestore = true)
        {
            return (_oldState, _exitOldStateParameters);
        }

        
    }
}