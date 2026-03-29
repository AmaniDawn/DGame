namespace GameBattle
{
    /// <summary>
    /// 单位状态脏标记。
    /// </summary>
    [System.Flags]
    public enum UnitStateDirtyFlags
    {
        /// <summary>
        /// 无脏标记。
        /// </summary>
        None = 0,

        /// <summary>
        /// 变换相关状态。
        /// </summary>
        Transform = 1 << 0,

        /// <summary>
        /// 属性相关状态。
        /// </summary>
        Attr = 1 << 1,

        /// <summary>
        /// 单位状态相关数据。
        /// </summary>
        State = 1 << 2,

        /// <summary>
        /// 目标相关状态。
        /// </summary>
        Target = 1 << 3,

        /// <summary>
        /// Buff 相关状态。
        /// </summary>
        Buff = 1 << 4,

        /// <summary>
        /// 技能相关状态。
        /// </summary>
        Skill = 1 << 5,
    }
}