namespace GameLogic
{
    /// <summary>
    /// UI的层级枚举
    /// </summary>
    public enum UILayer : byte
    {
        /// <summary>
        /// 底层
        /// </summary>
        Bottom = 0,

        /// <summary>
        /// UI层
        /// </summary>
        UI = 1,

        /// <summary>
        /// 顶层
        /// </summary>
        Top = 2,

        /// <summary>
        /// 提示层
        /// </summary>
        Tips = 3,

        /// <summary>
        /// 系统级
        /// </summary>
        System = 4,
    }
}