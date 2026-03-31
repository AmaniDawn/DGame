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

        public LogicUnit LogicUnit { get; }
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

        public LogicUnit LogicUnit { get; }

        public LogicUnitDestroyReason Reason { get; }
    }
}
