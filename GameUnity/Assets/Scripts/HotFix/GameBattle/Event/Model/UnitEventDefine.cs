namespace GameBattle
{
    /// <summary>
    /// 单位受伤事件。
    /// </summary>
    public readonly struct UnitDamagedEvent : IUnitEvent
    {
        /// <summary>
        /// 获取受伤单位 ID。
        /// </summary>
        public readonly ulong UnitID;

        /// <summary>
        /// 获取施加伤害的单位 ID。
        /// </summary>
        public readonly ulong CasterUnitId;

        /// <summary>
        /// 获取伤害值。
        /// </summary>
        public readonly int DamageValue;

        /// <summary>
        /// 初始化单位受伤事件。
        /// </summary>
        /// <param name="unitId">受伤单位 ID。</param>
        /// <param name="casterUnitId">施加伤害的单位 ID。</param>
        /// <param name="damageValue">伤害值。</param>
        public UnitDamagedEvent(ulong unitId, ulong casterUnitId, int damageValue)
        {
            UnitID = unitId;
            CasterUnitId = casterUnitId;
            DamageValue = damageValue;
        }
    }
}