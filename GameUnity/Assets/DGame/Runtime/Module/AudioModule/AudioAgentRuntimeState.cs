namespace DGame
{
    /// <summary>
    /// 音频代理器运行状态
    /// </summary>
    public enum AudioAgentRuntimeState : byte
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None = 0,

        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 渐入播放
        /// </summary>
        FadingIn,

        /// <summary>
        /// 播放中
        /// </summary>
        Playing,

        /// <summary>
        /// 渐出消失
        /// </summary>
        FadingOut,

        /// <summary>
        /// 结束
        /// </summary>
        End,
    }
}