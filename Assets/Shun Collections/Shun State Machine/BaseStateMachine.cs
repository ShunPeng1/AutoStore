using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shun_State_Machine
{
    [Serializable]
    public class BaseStateMachine
    {
        [SerializeField] protected StateNode CurrentState = default;
        private Dictionary<Type, StateNode> _nodes = new();
        private HashSet<ITransition> _anyStateTransitions = new();

        private ITransitionData _lastTransitionData;

        [Header("History")] protected IStateHistoryStrategy StateHistoryStrategy;

        protected class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }

            public StateNode(IState state, HashSet<ITransition> transitions = null)
            {
                State = state;
                Transitions = transitions ?? new HashSet<ITransition>();
            }

            public void AddTransition(IState to, IPredicate condition)
            {
                Transitions.Add(new Transition(to, condition));
            }

            public void RemoveTransition(IState to, IPredicate condition)
            {
                Transitions.RemoveWhere(transition => transition.ToState == to && transition.Condition == condition);
            }
        }

        private BaseStateMachine(StateNode initialStateNode, bool onEnterCall = false, ITransitionData enterData = null)
        {
            _nodes[initialStateNode.State.GetType()] = initialStateNode;
            CurrentState = initialStateNode;
            
            if (onEnterCall)
            {
                initialStateNode.State.OnEnterState(enterData);
            }
            
            
        }
        
        public class Builder
        {
            StateNode _initialStateNode;
            bool _onEnterCall;
            ITransitionData _enterData;
            
            IStateHistoryStrategy _stateHistoryStrategy;
            
            
            public BaseStateMachine Build()
            {
                var stateMachine = new BaseStateMachine(_initialStateNode, _onEnterCall, _enterData)
                    {
                        StateHistoryStrategy = _stateHistoryStrategy
                    };
                return stateMachine;
            }
            
            public Builder WithInitialState(IState initialState, bool onEnterCall = false, ITransitionData enterData = null)
            {
                _initialStateNode = new StateNode(initialState);
                _onEnterCall = onEnterCall;
                _enterData = enterData;
                
                return this;
            }
            
            public Builder WithHistoryStrategy(IStateHistoryStrategy historyStrategy = null)
            {
                _stateHistoryStrategy = historyStrategy;
                return this;
            }
            
            
        }
        
        public void Update(ITransitionData parameter = null)
        {
            var transition = GetTransition();

            if (transition != null)
            {
                TransitionData transitionData = new TransitionData()
                {
                    FromState = CurrentState.State,
                    Transition = transition
                };
                SetToState(transition.ToState, transitionData);
                return;
            }
            CurrentState?.State.UpdateState();
            
        }
        
        public void FixedUpdate(ITransitionData parameter = null)
        {
            var transition = GetTransition();
            
            if (transition != null)
            {
                TransitionData transitionData = new TransitionData()
                {
                    FromState = CurrentState.State,
                    Transition = transition
                };
                SetToState(transition.ToState, transitionData);
                return;
            }
            CurrentState?.State.FixedUpdateState();
        }

        private ITransition GetTransition()
        {
            foreach (var transition in _anyStateTransitions.Where(transition => transition.Condition.Evaluate()))
            {
                return transition;
            }

            return CurrentState?.Transitions.FirstOrDefault(transition => transition.Condition.Evaluate());
        }
        

        public void AddOrOverwriteState(IState baseState, HashSet<ITransition> transitions = null)
        {
            _nodes[baseState.GetType()] = new StateNode(baseState, transitions);
        }
        
        public void RemoveState(IState stateEnum)
        {
            _nodes.Remove(stateEnum.GetType());
        }
        
        public void AddTransition(IState fromState, IState toState, IPredicate predicate)
        {
            GetOrAddNode(fromState).AddTransition(toState, predicate);
        }
        
        public void AddAnyTransition(IState toState, IPredicate predicate)
        {
            _anyStateTransitions.Add(new Transition(toState, predicate));
        }
        
        public void RemoveTransition(IState fromState, IState toState, IPredicate predicate)
        {
            GetOrAddNode(fromState).RemoveTransition(toState, predicate);
        }
        
        public void RemoveAnyTransition(IState toState, IPredicate predicate)
        {
            _anyStateTransitions.RemoveWhere(transition => transition.ToState == toState && transition.Condition == predicate);
        }
        
        private StateNode GetOrAddNode(IState state)
        {
            var node = _nodes.GetValueOrDefault(state.GetType());
            
            if (node == null)
            {
                node = new StateNode(state);
                _nodes[state.GetType()] = node;
            }
            
            return node;
        }
        public void SetToState(IState toState, ITransitionData transitionData = null)
        {
            if (toState == null || toState == CurrentState.State) return;
            
            if (_nodes.TryGetValue(toState.GetType(), out StateNode nextState))
            {
                StateHistoryStrategy?.Save(nextState.State, transitionData);
                SwitchState(nextState, transitionData);
            }
            else
            {
                Debug.LogWarning($"State {toState.GetType()} not found in state machine.");
            }
        }
        
        public IState GetCurrentState()
        {
            return CurrentState?.State;
        }
        
        public Type GetCurrentStateType()
        {
            return CurrentState?.State.GetType();
        }
        
        public void RestoreState()
        {
            if (StateHistoryStrategy == null) return;
            var (enterState, exitOldStateParameters) = StateHistoryStrategy.Restore();
            if (enterState != null)
            {
                SetToState(enterState, exitOldStateParameters);
            }
            else SetToState(default);
            
        }

        public (IState transitedState, ITransitionData transitionData) PeakHistory()
        {
            if (StateHistoryStrategy == null) return (default, default);
            var (enterState, exitOldStateParameters) = StateHistoryStrategy.Restore(false);
            return (enterState, exitOldStateParameters);
        }

        private void SwitchState(StateNode nextState, ITransitionData transitionData = null)
        {
            CurrentState.State.OnExitState(transitionData);
            var lastStateEnum = CurrentState;
            CurrentState = nextState;
            nextState.State.OnEnterState(transitionData);
        }

        
    }
}
