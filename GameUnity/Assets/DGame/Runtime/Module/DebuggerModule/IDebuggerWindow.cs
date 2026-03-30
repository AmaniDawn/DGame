namespace DGame
{
    public interface IDebuggerWindow
    {
        /// <summary>
        /// 初始化调试器窗口
        /// </summary>
        /// <param name="args">参数</param>
        void Initialize(params object[] args);

        /// <summary>
        /// 进入调试器窗口
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 调试器窗口轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑时间（秒）</param>
        /// <param name="realElapseSeconds">真实时间（秒）</param>
        void OnUpdate(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 调试器窗口绘制
        /// </summary>
        void OnDraw();

        /// <summary>
        /// 退出调试器窗口
        /// </summary>
        void OnExit();

        /// <summary>
        /// 销毁调试器窗口
        /// </summary>
        void OnDestroy();
    }
}