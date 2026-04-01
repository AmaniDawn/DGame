namespace GameBattle
{
    /// <summary>
    /// 逻辑单位创建完成后发布的战斗事件。
    /// </summary>
    public readonly struct LogicUnitSpawnedEvent : IBattleEvent
    {
        public LogicUnitSpawnedEvent(LogicUnit logicUnit)
        {
            LogicUnit = logicUnit;
        }

        public readonly LogicUnit LogicUnit { get; }
    }

    /// <summary>
    /// 逻辑单位即将销毁时发布的战斗事件。
    /// </summary>
    public readonly struct LogicUnitDestroyingEvent : IBattleEvent
    {
        public LogicUnitDestroyingEvent(LogicUnit logicUnit, LogicUnitDestroyReason reason)
        {
            LogicUnit = logicUnit;
            Reason = reason;
        }

        public readonly LogicUnit LogicUnit { get; }

        public readonly LogicUnitDestroyReason Reason { get; }
    }
}