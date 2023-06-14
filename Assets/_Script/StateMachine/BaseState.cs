using System;
using UnityEngine;

namespace _Script.StateMachine
{
    public abstract class BaseState<TStateEnum> where TStateEnum : Enum
    {
        [Header("State Machine ")]
        public readonly TStateEnum MyStateEnum;
        
        protected Action<TStateEnum, object[]> EnterEvents;
        protected Action<TStateEnum, object[]> ExecuteEvents;
        protected Action<TStateEnum, object[]> ExitEvents;

        protected BaseState(TStateEnum myStateEnum, Action<TStateEnum, object[]> enterEvents, Action<TStateEnum, object[]> executeEvents, Action<TStateEnum, object[]> exitEvents)
        {
            MyStateEnum = myStateEnum;
            EnterEvents = enterEvents;
            ExecuteEvents = executeEvents;
            ExitEvents = exitEvents;
        }
        
        public enum StateEvent
        {
            EnterState,
            ExitState,
            ExecuteState
        }

        public void OnExitState(TStateEnum enterState = default, object [] parameters = null)
        {
            ExitEvents.Invoke(enterState, parameters);
        }
        
        public void OnEnterState(TStateEnum exitState = default, object [] parameters = null)
        {
            ExitEvents.Invoke(exitState, parameters);
        }

        public void ExecuteState(object [] parameters = null)
        {
            ExecuteEvents.Invoke(MyStateEnum, parameters);
        }

        public void SubscribeToState(StateEvent stateEvent, Action<TStateEnum, object[]>[] actions )
        {
            foreach (var action in actions)
            {
                switch (stateEvent)
                {
                    case StateEvent.EnterState:
                        EnterEvents += action;
                        break;
                    case StateEvent.ExitState:
                        ExitEvents += action;
                        break;
                    case StateEvent.ExecuteState:
                        ExecuteEvents += action;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stateEvent), stateEvent, null);
                }
            }
        }

        private void UnsubscribeToState(StateEvent stateEvent, Action<TStateEnum, object[]>[] actions )
        {
            foreach (var action in actions)
            {
                switch (stateEvent)
                {
                    case StateEvent.EnterState:
                        EnterEvents -= action;
                        break;
                    case StateEvent.ExitState:
                        ExitEvents -= action;
                        break;
                    case StateEvent.ExecuteState:
                        ExecuteEvents -= action;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stateEvent), stateEvent, null);
                }
            }
        }
        
    }
    
}