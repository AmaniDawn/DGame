namespace GameLogic
{
    /// <summary>
    /// 轴输入状态枚举
    /// </summary>
    public enum InputAxisType
    {
        /// <summary>
        /// 前后移动轴
        /// </summary>
        MoveForward,

        /// <summary>
        /// 左右移动轴
        /// </summary>
        MoveRight,

        /// <summary>
        /// 视角上下轴
        /// </summary>
        LookUp,

        /// <summary>
        /// 视角左右轴
        /// </summary>
        LookRight,

        /// <summary>
        /// 镜头拉近
        /// </summary>
        ZoomUp,

        /// <summary>
        /// 镜头拉远
        /// </summary>
        ZoomDown,
    }

    /// <summary>
    /// 命令类型
    /// </summary>
    public enum InputCommandType
    {
        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 技能
        /// </summary>
        Skill,

        /// <summary>
        /// 跳跃
        /// </summary>
        Jump,

        /// <summary>
        /// 冲刺
        /// </summary>
        Sprint,
    }

    /// <summary>
    /// 按键状态
    /// </summary>
    public enum InputState
    {
        /// <summary>
        /// 按下
        /// </summary>
        Started,

        /// <summary>
        /// 按住
        /// </summary>
        Performed,

        /// <summary>
        /// 抬起
        /// </summary>
        Canceled,
    }

    public partial class InputDefine
    {
        private static GameInputActions m_inputActions; // Unity输入系统实例

        public static void Initialize()
        {
            m_inputActions = new GameInputActions();
            Enable();

            #region 移动输入处理

            m_inputActions.GamePlay.Move.performed += ctx =>
            {

            };
            m_inputActions.GamePlay.Move.canceled += ctx =>
            {

            };

            #endregion

            #region 视角输入处理

            m_inputActions.GamePlay.Look.performed += ctx =>
            {

            };
            m_inputActions.GamePlay.Look.canceled += ctx =>
            {

            };

            #endregion

            #region 视角输入处理

            m_inputActions.GamePlay.Zoom.performed += ctx =>
            {

            };
            m_inputActions.GamePlay.Zoom.canceled += ctx =>
            {

            };

            #endregion
        }

        /// <summary>
        /// 启用输入系统
        /// </summary>
        public static void Enable() => m_inputActions?.Enable();

        /// <summary>
        /// 禁用输入系统
        /// </summary>
        public static void Disable() => m_inputActions?.Disable();
    }
}