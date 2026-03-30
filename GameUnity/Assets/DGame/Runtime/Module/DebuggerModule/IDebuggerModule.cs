namespace DGame
{
    public interface IDebuggerModule
    {
        /// <summary>
        /// 调试器窗口是否激活
        /// </summary>
        bool ActiveWindow { get; set; }

        /// <summary>
        /// 调试器窗口根节点
        /// </summary>
        IDebuggerWindowGroup DebuggerWindowRoot { get; }

        /// <summary>
        /// 注册调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <param name="debuggerWindow">调试器窗口</param>
        /// <param name="args">调试器窗口参数</param>
        void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args);

        /// <summary>
        /// 解除注册调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <returns></returns>
        bool UnRegisterDebuggerWindow(string path);

        /// <summary>
        /// 获取调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <returns></returns>
        IDebuggerWindow GetDebuggerWindow(string path);

        /// <summary>
        /// 选中调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <returns></returns>
        bool SelectDebuggerWindow(string path);
    }
}