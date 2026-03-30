namespace GameLogic
{
    /// <summary>
    /// 红点显示类型
    /// </summary>
    public enum RedDotType
    {
        /// <summary>
        /// 普通红点（小圆点）
        /// </summary>
        Dot = 0,

        /// <summary>
        /// 数字红点（显示数量）
        /// </summary>
        Number = 1,

        /// <summary>
        /// NEW标签
        /// </summary>
        New = 2,

        /// <summary>
        /// 自定义类型（可扩展）
        /// </summary>
        Custom = 100
    }

    /// <summary>
    /// 红点聚合策略
    /// </summary>
    public enum RedDotAggregateStrategy
    {
        /// <summary>
        /// 求和（用于数字红点）
        /// </summary>
        Sum,

        /// <summary>
        /// 逻辑或（任一子节点有红点则显示）
        /// </summary>
        Or,

        /// <summary>
        /// 取最大值
        /// </summary>
        Max
    }
}