using System.Collections.Generic;

namespace DGame
{
    internal sealed partial class DebuggerModule
    {
        private sealed class DebuggerWindowGroup : IDebuggerWindowGroup
        {
            private readonly List<KeyValuePair<string, IDebuggerWindow>> m_debuggerWindows = new List<KeyValuePair<string, IDebuggerWindow>>();
            private int m_selectedIndex = 0;
            private string[] m_debuggerWindowNames = null;

            public int DebuggerWindowCount => m_debuggerWindows.Count;
            public int SelectedIndex { get => m_selectedIndex; set => m_selectedIndex = value; }
            public IDebuggerWindow SelectedWindow
            {
                get
                {
                    if (m_selectedIndex >= m_debuggerWindows.Count)
                    {
                        return null;
                    }
                    return m_debuggerWindows[m_selectedIndex].Value;
                }
            }

            /// <summary>
            /// 初始化调试器组
            /// </summary>
            /// <param name="args"></param>
            public void Initialize(params object[] args)
            {

            }

            public void OnEnter()
            {
                SelectedWindow?.OnEnter();
            }

            public string[] GetDebuggerWindowNames()
            {
                return m_debuggerWindowNames;
            }

            public IDebuggerWindow GetDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                // 判断路径是调试器窗口还是调试器组里的窗口 TextGroup/TextWindow
                int pos = path.IndexOf('/');

                if (pos < 0 || pos >= path.Length - 1)
                {
                    return InternalGetDebuggerWindow(path);
                }
                // 获取调试器组名称 TextGroup
                string groupWindowName = path.Substring(0, pos);
                // 获取调试器窗口名称 TextWindow
                string debuggerWindowName = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup = (DebuggerWindowGroup)InternalGetDebuggerWindow(groupWindowName);

                if (debuggerWindowGroup == null)
                {
                    return null;
                }

                return debuggerWindowGroup.GetDebuggerWindow(debuggerWindowName);
            }

            private IDebuggerWindow InternalGetDebuggerWindow(string path)
            {
                foreach (var item in m_debuggerWindows)
                {
                    if (item.Key == path)
                    {
                        return item.Value;
                    }
                }
                return null;
            }

            private void RefreshDebuggerWindowNames()
            {
                int index = 0;
                m_debuggerWindowNames = new string[m_debuggerWindows.Count];

                foreach (var item in m_debuggerWindows)
                {
                    m_debuggerWindowNames[index++] = item.Key;
                }
            }

            public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new DGameException("路径无效");
                }

                int pos = path.IndexOf('/');

                if (pos < 0 || pos >= path.Length - 1)
                {
                    if (InternalGetDebuggerWindow(path) != null)
                    {
                        throw new DGameException("调试器窗口已经注册过");
                    }
                    m_debuggerWindows.Add(new KeyValuePair<string, IDebuggerWindow>(path, debuggerWindow));
                    RefreshDebuggerWindowNames();
                }
                else
                {
                    string groupWindowName = path.Substring(0, pos);
                    string debuggerWindowName = path.Substring(pos + 1);
                    DebuggerWindowGroup debuggerWindowGroup = (DebuggerWindowGroup)InternalGetDebuggerWindow(groupWindowName);

                    if (debuggerWindowGroup == null)
                    {
                        if (InternalGetDebuggerWindow(groupWindowName) != null)
                        {
                            throw new DGameException("调试器组已经注册过，无法再创建调试器组");
                        }
                        debuggerWindowGroup = new DebuggerWindowGroup();
                        m_debuggerWindows.Add(new KeyValuePair<string, IDebuggerWindow>(groupWindowName, debuggerWindowGroup));
                        RefreshDebuggerWindowNames();
                    }
                    debuggerWindowGroup?.RegisterDebuggerWindow(debuggerWindowName, debuggerWindow);
                }
            }

            /// <summary>
            /// 解除注册调试器窗口
            /// </summary>
            /// <param name="path">调试器窗口路径</param>
            public bool UnRegisterDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                int pos = path.IndexOf('/');

                if (pos < 0 || pos >= path.Length - 1)
                {
                    IDebuggerWindow debuggerWindow = InternalGetDebuggerWindow(path);
                    bool result = m_debuggerWindows.Remove(new KeyValuePair<string, IDebuggerWindow>(path, debuggerWindow));
                    debuggerWindow?.OnDestroy();
                    RefreshDebuggerWindowNames();
                    return result;
                }

                string groupWindowName = path.Substring(0, pos);
                string debuggerWindowName = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup = (DebuggerWindowGroup)InternalGetDebuggerWindow(groupWindowName);

                if (debuggerWindowGroup == null)
                {
                    return false;
                }
                return debuggerWindowGroup.UnRegisterDebuggerWindow(debuggerWindowName);
            }

            public bool SelectDebuggerWindow(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                int pos = path.IndexOf('/');

                if (pos < 0 || pos >= path.Length - 1)
                {
                    return InternalSelectDebuggerWindow(path);
                }
                string groupWindowName = path.Substring(0, pos);
                string debuggerWindowName = path.Substring(pos + 1);
                DebuggerWindowGroup debuggerWindowGroup = (DebuggerWindowGroup)InternalGetDebuggerWindow(groupWindowName);

                if (debuggerWindowGroup == null)
                {
                    return false;
                }

                return debuggerWindowGroup.SelectDebuggerWindow(debuggerWindowName);
            }

            private bool InternalSelectDebuggerWindow(string path)
            {
                for (int i = 0; i < m_debuggerWindows.Count; i++)
                {
                    if (m_debuggerWindows[i].Key == path)
                    {
                        m_selectedIndex = i;
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// 窗口轮询
            /// </summary>
            /// <param name="elapseSeconds">逻辑时间（秒）</param>
            /// <param name="realElapseSeconds">真实时间（秒）</param>
            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                SelectedWindow?.OnUpdate(elapseSeconds, realElapseSeconds);
            }

            public void OnDraw()
            {
            }

            public void OnExit()
            {
                SelectedWindow?.OnExit();
            }

            public void OnDestroy()
            {
                foreach (var item in m_debuggerWindows)
                {
                    item.Value?.OnDestroy();
                }
                m_debuggerWindows.Clear();
            }
        }
    }
}