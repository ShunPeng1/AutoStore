using Shun_State_Machine;

namespace _Script.Robot
{
    public class RobotJammingTask : ITransitionData
    {
        public IState FromState { get; set; }
        public ITransition Transition { get; set; }
        
        public bool IsWaitingForGoal;
    }
}