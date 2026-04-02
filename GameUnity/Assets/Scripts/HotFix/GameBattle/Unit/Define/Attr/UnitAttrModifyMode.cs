namespace GameBattle
{
    /// <summary>
    /// 逻辑单位属性修正模式。
    /// </summary>
    public enum UnitAttrModifyMode
    {
        /// <summary>
        /// 无效加成
        /// </summary>
        None = 0,

        /// <summary>
        /// 固定值加成
        /// </summary>
        Flat = 1,

        /// <summary>
        /// 比例加成，0.2 表示 +20%
        /// </summary>
        Ratio = 2,
    }
}