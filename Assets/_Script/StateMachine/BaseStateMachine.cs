using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

namespace _Script.StateMachine
{
    public abstract class BaseStateMachine<TStateEnum> : MonoBehaviour where TStateEnum : Enum 
    {
        public BaseState<TStateEnum> CurrentBaseState;
        private Dictionary<TStateEnum, BaseState<TStateEnum>> _states = new ();

        [Header("History")] 
        protected IStateHistoryStrategy<TStateEnum> StateHistoryStrategy;

        protected virtual void Awake()
        {
            StateHistoryStrategy = new StackStateHistoryStrategy<TStateEnum>(10);
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
                StateHistoryStrategy.Save(nextState, exitOldStateParameters, enterNewStateParameters);
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
            Debug.Log(gameObject.name +" Change "+ CurrentBaseState.MyStateEnum+ " State To "+ nextState.MyStateEnum + " State");
            
            CurrentBaseState.OnExitState(nextState.MyStateEnum,exitOldStateParameters);
            TStateEnum lastStateEnum = CurrentBaseState.MyStateEnum;
            CurrentBaseState = nextState;
            nextState.OnEnterState(lastStateEnum,enterNewStateParameters);
        }
        
    }
}
