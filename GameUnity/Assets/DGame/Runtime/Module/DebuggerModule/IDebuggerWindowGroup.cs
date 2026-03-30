namespace DGame
{
    public interface IDebuggerWindowGroup : IDebuggerWindow
    {
        /// <summary>
        /// 调试器窗口数量
        /// </summary>
        int DebuggerWindowCount { get; }

        /// <summary>
        /// 当前选中的调试器窗口索引
        /// </summary>
        int SelectedIndex { get; set; }

        /// <summary>
        /// 当前选中的调试器窗口
        /// </summary>
        IDebuggerWindow SelectedWindow { get; }

        /// <summary>
        /// 获取调试器组的调试器窗口名称集合
        /// </summary>
        /// <returns></returns>
        string[] GetDebuggerWindowNames();

        /// <summary>
        /// 获取调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <returns></returns>
        IDebuggerWindow GetDebuggerWindow(string path);

        /// <summary>
        /// 注册调试器窗口
        /// </summary>
        /// <param name="path">调试器窗口路径</param>
        /// <param name="debuggerWindow">调试器窗口</param>
        void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow);
    }
}