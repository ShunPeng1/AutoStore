using System;
using System.Linq;
using System.Reflection;

namespace Shun_State_Machine
{
    public interface ITransitionData
    {
        public IState FromState { get; set; }
        public ITransition Transition { get; set; }
        
        public T CastTo<T>() where T : ITransitionData
        {
            if (this is T casted)
            {
                return casted;
            }
            throw new InvalidCastException($"Cannot cast {GetType().Name} to {typeof(T).Name}");
        }
        
    }
}