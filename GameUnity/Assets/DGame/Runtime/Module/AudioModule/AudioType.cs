namespace DGame
{
    /// <summary>
    /// 音频分类 用于分别开启/关闭对应分类音频
    /// </summary>
    /// <remarks>和AudioMixer中分类保持一致</remarks>
    public enum AudioType : byte
    {
        /// <summary>
        /// 背景音乐
        /// </summary>
        Music = 0,

        /// <summary>
        /// 声音音效
        /// </summary>
        Sound = 1,

        /// <summary>
        /// UI音效
        /// </summary>
        UISound = 2,

        /// <summary>
        /// 人声音效
        /// </summary>
        Voice = 3,

        /// <summary>
        /// 最大
        /// </summary>
        Max = 4
    }
}