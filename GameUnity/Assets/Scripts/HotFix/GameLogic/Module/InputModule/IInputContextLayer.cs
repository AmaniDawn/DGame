#if ENABLE_INPUT_SYSTEM

namespace GameLogic
{
    /// <summary>
    /// 输入上下文解析器接口
    /// </summary>
    public interface IInputContextLayer : DGame.IMemory
    {
        /// <summary>
        /// 优先级
        /// <remarks>数值越大优先级越高</remarks>
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 根据按下输入解析命令
        /// </summary>
        GameplayCommand ResolveStarted();

        /// <summary>
        /// 根据持续按住输入解析命令
        /// </summary>
        GameplayCommand ResolvePerformed();

        /// <summary>
        /// 根据释放输入解析命令
        /// </summary>
        GameplayCommand ResolveCanceled();

        /// <summary>
        /// 按下事件通知
        /// <remarks>用于记录输入过程状态 不直接执行业务命令</remarks>
        /// </summary>
        void OnStarted();

        /// <summary>
        /// 持续按住事件通知
        /// <remarks>用于记录输入过程状态 不直接执行业务命令</remarks>
        /// </summary>
        void OnPerformed();

        /// <summary>
        /// 释放事件通知
        /// <remarks>用于记录输入过程状态 不直接执行业务命令</remarks>
        /// </summary>
        void OnCanceled();
    }
}

#endif