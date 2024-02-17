namespace Shun_State_Machine
{
    public interface ITransition
    {
        public IState ToState { get; }
        public IPredicate Condition { get; }
        
    }

}