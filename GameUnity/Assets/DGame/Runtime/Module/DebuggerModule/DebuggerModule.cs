namespace DGame
{
    internal sealed partial class DebuggerModule : Module, IDebuggerModule, IUpdateModule
    {
        private DebuggerWindowGroup m_debuggerWindowRoot;
        private bool m_activeWindow;

        public bool ActiveWindow { get => m_activeWindow; set=> m_activeWindow = value; }
        public IDebuggerWindowGroup DebuggerWindowRoot => m_debuggerWindowRoot;

        public override int Priority => -1;

        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new DGameException("路径无效");
            }

            if (debuggerWindow == null)
            {
                throw new DGameException("调试器窗口无效");
            }

            m_debuggerWindowRoot?.RegisterDebuggerWindow(path, debuggerWindow);
            debuggerWindow.Initialize(args);
        }

        public bool UnRegisterDebuggerWindow(string path)
        {
            return m_debuggerWindowRoot != null && m_debuggerWindowRoot.UnRegisterDebuggerWindow(path);
        }

        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return m_debuggerWindowRoot?.GetDebuggerWindow(path);
        }

        public bool SelectDebuggerWindow(string path)
        {
            return m_debuggerWindowRoot != null && m_debuggerWindowRoot.SelectDebuggerWindow(path);
        }

        public override void OnCreate()
        {
            m_debuggerWindowRoot = new DebuggerWindowGroup();
            m_activeWindow = false;
        }

        public override void OnDestroy()
        {
            m_activeWindow = false;
            m_debuggerWindowRoot?.OnDestroy();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!m_activeWindow)
            {
                return;
            }

            m_debuggerWindowRoot.OnUpdate(elapseSeconds, realElapseSeconds);
        }
    }
}