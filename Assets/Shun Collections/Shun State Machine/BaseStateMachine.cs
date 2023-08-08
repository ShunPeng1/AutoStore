using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

namespace Shun_State_Machine
{
    public class BaseStateMachine<TStateEnum> where TStateEnum : Enum 
    {
        public BaseState<TStateEnum> CurrentBaseState = new (default);
        private Dictionary<TStateEnum, BaseState<TStateEnum>> _states = new ();

        [Header("History")] 
        public IStateHistoryStrategy<TStateEnum> StateHistoryStrategy;

        
        public void SetHistoryStrategy(IStateHistoryStrategy<TStateEnum> historyStrategy)
        {
            StateHistoryStrategy = historyStrategy;
        }

        public void ExecuteState(object[] parameters = null)
        {
            CurrentBaseState.ExecuteState(parameters);
        }
        public void AddState(BaseState<TStateEnum> baseState)
        {
            _states[baseState.MyStateEnum] = baseState;
        }

        public void RemoveState(TStateEnum stateEnum)
        {
            _states.Remove(stateEnum);
        }

        public void SetToState(TStateEnum stateEnum, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            if (_states.TryGetValue(stateEnum, out BaseState<TStateEnum> nextState))
            {
                StateHistoryStrategy?.Save(nextState, exitOldStateParameters, enterNewStateParameters);
                SwitchState(nextState, exitOldStateParameters, enterNewStateParameters);
            }
            else
            {
                Debug.LogWarning($"State {stateEnum} not found in state machine.");
            }
        }
        
        public TStateEnum GetState()
        {
            return CurrentBaseState.MyStateEnum;
        }

        private void SwitchState(BaseState<TStateEnum> nextState , object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            CurrentBaseState.OnExitState(nextState.MyStateEnum,exitOldStateParameters);
            TStateEnum lastStateEnum = CurrentBaseState.MyStateEnum;
            CurrentBaseState = nextState;
            nextState.OnEnterState(lastStateEnum,enterNewStateParameters);
        }
        
    }
}
