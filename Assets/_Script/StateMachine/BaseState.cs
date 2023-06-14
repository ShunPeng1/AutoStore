using System;
using UnityEngine;

namespace _Script.StateMachine
{
    public class BaseState<TStateEnum> where TStateEnum : Enum
    {
        [Header("State Machine ")]
        public readonly TStateEnum MyStateEnum;
        protected object[] Objects;
        
        protected Action<TStateEnum, object[]> EnterEvents;
        protected Action<TStateEnum, object[]> ExecuteEvents;
        protected Action<TStateEnum, object[]> ExitEvents;

        public BaseState(TStateEnum myStateEnum,
            Action<TStateEnum, object[]> executeEvents = null,
            Action<TStateEnum, object[]> exitEvents = null,
            Action<TStateEnum, object[]> enterEvents = null)
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

        public virtual void OnExitState(TStateEnum enterState = default, object [] parameters = null)
        {
            ExitEvents?.Invoke(enterState, parameters);
        }
        
        public virtual void OnEnterState(TStateEnum exitState = default, object [] parameters = null)
        {
            ExitEvents?.Invoke(exitState, parameters);
        }

        public virtual void ExecuteState(object [] parameters = null)
        {
            ExecuteEvents?.Invoke(MyStateEnum, parameters);
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