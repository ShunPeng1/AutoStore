using System;

namespace Shun_State_Machine
{
    public interface IState
    {
        void OnEnterState(ITransitionData enterTransitionData = null);
        void OnExitState(ITransitionData exitTransitionData = null);
        void UpdateState();
        void FixedUpdateState();
    }
}