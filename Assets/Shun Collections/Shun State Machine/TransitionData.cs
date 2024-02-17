namespace Shun_State_Machine
{
    public class TransitionData : ITransitionData
    {
        public IState FromState { get; set; }
        public ITransition Transition { get; set; }
    }
}