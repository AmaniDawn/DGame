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

    /// <summary>
    /// 输入事件结构体
    /// <remarks>表示原始输入事件，例如按下、持续、释放</remarks>
    /// </summary>
    public struct InputEvent
    {
        /// <summary>
        /// 输入动作类型
        /// </summary>
        public InputActionType ActionType;

        /// <summary>
        /// 输入按键状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 输入发生时间
        /// </summary>
        public double Time;

        public InputEvent(InputActionType actionType, InputState inputState, double time)
        {
            ActionType = actionType;
            InputState = inputState;
            Time = time;
        }
    }

    /// <summary>
    /// 最终玩法命令结构体
    /// <remarks>表示最下游的具体执行命令，例如跳跃、冲刺、释放技能</remarks>
    /// </summary>
    public struct GameplayCommand
    {
        /// <summary>
        /// 命令类型
        /// </summary>
        public InputCommandType CommandType;

        /// <summary>
        /// 整数值 传递如技能ID等
        /// </summary>
        public int IntValue;

        public GameplayCommand(InputCommandType commandType, int intValue)
        {
            CommandType = commandType;
            IntValue = intValue;
        }
    }

    /// <summary>
    /// 输入上下文命令结构体
    /// <remarks>表示根据输入事件和当前上下文解析出的中间命令，包含输入来源与最终玩法命令</remarks>
    /// </summary>
    public struct InputContextCommand
    {
        /// <summary>
        /// 原始输入动作类型
        /// </summary>
        public InputActionType ActionType;

        /// <summary>
        /// 原始输入状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 输入时间
        /// </summary>
        public double Time;

        /// <summary>
        /// 原始输入命令
        /// </summary>
        public GameplayCommand Command;

        /// <summary>
        /// 命令索引
        /// </summary>
        public int Index;

        public InputContextCommand(InputActionType actionType, InputState inputState, double time, GameplayCommand command, int index)
        {
            ActionType = actionType;
            InputState = inputState;
            Time = time;
            Command = command;
            Index = index;
        }
    }

    /// <summary>
    /// 输入缓存结构体
    /// <remarks>用于临时存储输入事件</remarks>
    /// </summary>
    public struct InputCache
    {
        /// <summary>
        /// 缓存的动作类型
        /// </summary>
        public InputActionType ActionType;

        /// <summary>
        /// 缓存的状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 原始事件时间
        /// </summary>
        public double EventTime;

        /// <summary>
        /// 缓存创建时间
        /// </summary>
        public double Time;

        /// <summary>
        /// 缓存累计时间
        /// </summary>
        public double AccumulatedTime;
    }

    public sealed partial class InputDefine
    {
        private static GameInputActions m_inputActions; // Unity输入系统实例

        public static void Initialize()
        {
            m_inputActions = new GameInputActions();
            Enable();

            // RegisterInputActions();

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