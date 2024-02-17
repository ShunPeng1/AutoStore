using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shun_State_Machine
{
    /// <summary>
    /// This is a StateHistory using a stack, save and restore form the top of the stack
    /// </summary>
    /// <typeparam name="TStateEnum"></typeparam>
    public class StackStateHistoryStrategy : IStateHistoryStrategy
    {
        private int _maxSize = 0;
        private LinkedList<(IState, ITransitionData)> _historyStates = new(); // act as a stack

        public StackStateHistoryStrategy(int maxSize = 100)
        {
            _maxSize = maxSize;
        }

        public void Save(IState transitionState, ITransitionData transitionData)
        {
            if (_historyStates.Count >= _maxSize)
            {
                _historyStates.RemoveLast();
            }

            _historyStates.AddFirst((transitionState, transitionData));
        }

        (IState transitionState, ITransitionData transitionData) IStateHistoryStrategy.Restore(bool isRemoveRestore)
        {
            if (_historyStates.Count != 0)
            {
                var (lastState, lastTransitionData) = _historyStates.First.Value;
                 
                if(isRemoveRestore) _historyStates.RemoveFirst();
                
                return (lastState, lastTransitionData);
            }
            else
            {
                Debug.LogError("No state in the history state machine");
                return (default, default);
            }
        }

        
    }
}