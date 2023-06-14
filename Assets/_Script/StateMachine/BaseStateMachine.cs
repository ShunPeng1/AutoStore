using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

namespace _Script.StateMachine
{
    public abstract class BaseStateMachine<TStateEnum> : MonoBehaviour where TStateEnum : Enum 
    {
        protected BaseState<TStateEnum> CurrentBaseState;
        private Dictionary<TStateEnum, BaseState<TStateEnum>> _states = new ();

        [Header("History")] 
        protected IStateHistory<TStateEnum> StateHistory;

        protected virtual void Awake()
        {
            StateHistory = new StackStateHistory<TStateEnum>(10);
        }

        protected void ExecuteCurrentState( object[] parameters = null)
        {
            CurrentBaseState.ExecuteState(parameters);
        }

        protected void AddState(BaseState<TStateEnum> baseState)
        {
            _states[baseState.MyStateEnum] = baseState;
        }

        protected void RemoveState(TStateEnum stateEnum)
        {
            _states.Remove(stateEnum);
        }

        public void SetToState(TStateEnum stateEnum, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            if (_states.TryGetValue(stateEnum, out BaseState<TStateEnum> nextState))
            {
                StateHistory.Save(nextState, exitOldStateParameters, enterNewStateParameters);
                SwitchState(nextState, exitOldStateParameters, enterNewStateParameters);
            }
            else
            {
                Debug.LogWarning($"State {stateEnum} not found in state machine.");
            }
        }
        
        public void RestoreState()
        {
            var (lastBaseState, exitOldStateParameters, enterNewStateParameters) = StateHistory.Restore();
            SwitchState(lastBaseState, exitOldStateParameters, enterNewStateParameters);
        }

        public TStateEnum GetState()
        {
            return CurrentBaseState.MyStateEnum;
        }

        private void SwitchState(BaseState<TStateEnum> nextState , object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            Debug.Log("State Machine Manager Change"+ CurrentBaseState+ " To "+ nextState);
            
            CurrentBaseState.OnExitState(nextState.MyStateEnum,exitOldStateParameters);
            nextState.OnEnterState(CurrentBaseState.MyStateEnum,enterNewStateParameters);
            CurrentBaseState = nextState;
        }
        
    }
}
