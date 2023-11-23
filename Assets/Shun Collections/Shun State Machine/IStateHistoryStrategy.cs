using System;

namespace Shun_State_Machine
{
    /// <summary>
    /// Using Strategy Pattern to choose a History class
    /// </summary>
    /// <typeparam name="TStateEnum"></typeparam>
    public interface IStateHistoryStrategy<TStateEnum> where TStateEnum : Enum
    {
        void Save(BaseState<TStateEnum> state, IStateParameter exitOldStateParameters = null, IStateParameter enterNewStateParameters = null);
        (BaseState<TStateEnum> enterState, IStateParameter exitOldStateParameters, IStateParameter enterNewStateParameters) Restore(bool isRemoveRestore = true);
    }
}