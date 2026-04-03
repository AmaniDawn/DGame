using DGame;

namespace GameLogic
{
    /// <summary>
    /// 房间逻辑事件。
    /// </summary>
    [EventInterface(EEventGroup.GroupUI)]
    public interface IRoomLogicEvent
    {
        /// <summary>
        /// 当前房间数据发生变化。
        /// </summary>
        void OnRoomDataChange();
    }
}
