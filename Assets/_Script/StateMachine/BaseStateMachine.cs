using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

namespace _Script.StateMachine
{
    public class BaseStateMachine<TStateEnum> : MonoBehaviour where TStateEnum : Enum 
    {
        protected BaseState<TStateEnum> CurrentBaseState;
        private Dictionary<TStateEnum, BaseState<TStateEnum>> _states = new ();

        [Header("History")] 
        [SerializeField, Range(0,20)] private int _maxHistoryLength = 10;
        private LinkedList<(BaseState<TStateEnum>, object[], object[])> _historyStates = new();

        public void AddState(TStateEnum stateEnum, BaseState<TStateEnum> baseState)
        {
            _states[stateEnum] = baseState;
        }

        public void RemoveState(TStateEnum stateEnum)
        {
            _states.Remove(stateEnum);
        }

        public void SetToState(TStateEnum stateEnum, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            if (_states.TryGetValue(stateEnum, out BaseState<TStateEnum> nextState))
            {
                AddStateToHistory(nextState, exitOldStateParameters, enterNewStateParameters);
            }
            else
            {
                Debug.LogWarning($"State {stateEnum} not found in state machine.");
            }
        }
        
        public void SetToLastState()
        {
            if (_historyStates.Count != 0)
            {
                 (var lastState, var exitOldStateParameters, var enterNewStateParameters ) = _historyStates.First.Value;
                 
                 SwitchState(lastState, exitOldStateParameters, enterNewStateParameters);
                 _historyStates.RemoveFirst();
            }
            else
            {
                Debug.LogError("No state in the history state machine");
            }
            
        }

        private void AddStateToHistory(BaseState<TStateEnum> baseState, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null)
        {
            if (_historyStates.Count >= _maxHistoryLength)
            {
                _historyStates.RemoveLast();
            }

            _historyStates.AddFirst((baseState, exitOldStateParameters, enterNewStateParameters));
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
