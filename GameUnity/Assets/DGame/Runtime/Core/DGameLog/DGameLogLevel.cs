namespace DGame
{
    /// <summary>
    /// 日志等级
    /// </summary>
    public enum DGameLogLevel : byte
    {
        None = 0,

        /// <summary>
        /// 调试
        /// </summary>
        Debug = 1,

        /// <summary>
        /// 信息
        /// </summary>
        Info = 2,

        /// <summary>
        /// 警告
        /// </summary>
        Warning = 4,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 8,

        /// <summary>
        /// 严重错误
        /// </summary>
        Fatal = 16,

        All = Debug | Info | Warning | Error | Fatal
    }
}