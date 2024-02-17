using System;
using UnityEngine;

namespace Shun_State_Machine
{
    [Serializable]
    public class BaseState : IState
    {
        public void OnEnterState(ITransitionData enterTransitionData = null)
        {
            Debug.Log("Enter State");
        }

        public void OnExitState(ITransitionData exitTransitionData = null)
        {
            Debug.Log("Exit State");
        }

        public void UpdateState()
        {
            Debug.Log("Update State");
        }

        public void FixedUpdateState()
        {
            Debug.Log("Fixed Update State");
        }
    }
    
}