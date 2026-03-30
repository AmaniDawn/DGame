namespace DGame
{
    public enum InputType
    {
        Down,
        Up,
        Stay,

        /// <summary>
        /// 获取 Axis 值: -1 ~ 1
        /// </summary>
        Axis,

        /// <summary>
        /// 获取 AxisRaw 值: -1 0 1
        /// </summary>
        AxisRaw,
    }

    public enum HotKeyType
    {
        None,
        Horizontal,
        Vertical,
    }
}