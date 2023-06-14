using System;

namespace _Script.StateMachine
{
    /// <summary>
    /// Using Strategy Pattern to choose a History class
    /// </summary>
    /// <typeparam name="TStateEnum"></typeparam>
    public interface IStateHistoryStrategy<TStateEnum> where TStateEnum : Enum
    {
        void Save(BaseState<TStateEnum> stateEnum, object[] exitOldStateParameters = null, object[] enterNewStateParameters = null);
        (BaseState<TStateEnum> enterStateEnum, object[] exitOldStateParameters, object[] enterNewStateParameters) Restore(bool isRemoveRestore = true);
    }
}