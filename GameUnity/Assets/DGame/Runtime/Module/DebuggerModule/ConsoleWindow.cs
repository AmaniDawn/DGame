using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        [Serializable]
        public class ConsoleWindow : IDebuggerWindow
        {
            private readonly Queue<LogNode> m_logNodeQueue = new Queue<LogNode>();
            private readonly List<PrintLogNode> m_logNodes = new List<PrintLogNode>();

            private Vector2 m_logScrollPosition = Vector2.zero;
            private Vector2 m_stackTraceScrollPosition = Vector2.zero;
            private int m_infoCount = 0;
            private int m_warningCount = 0;
            private int m_errorCount = 0;
            private int m_fatalCount = 0;
            private LogNode m_selectedLogNode = null;
            private bool m_lastLockScroll = true;
            private bool m_lastInfoFilter = true;
            private bool m_lastWarningFilter = true;
            private bool m_lastErrorFilter = true;
            private bool m_lastFatalFilter = true;

            [SerializeField] private bool needPrintLog = false;
            [SerializeField] private bool lockScroll = true;
            [SerializeField] private int maxLine = 100;
            [SerializeField] private bool infoFilter = true;
            [SerializeField] private bool warningFilter = true;
            [SerializeField] private bool errorFilter = true;
            [SerializeField] private bool fatalFilter = true;
            [SerializeField] private Color infoColor = Color.white;
            [SerializeField] private Color warningColor = Color.yellow;
            [SerializeField] private Color errorColor = Color.red;
            [SerializeField] private Color fatalColor = new Color(0.7f, 0.2f, 0.2f);

            public bool LockScroll { get => lockScroll; set => lockScroll = value; }
            public int MaxLine { get => maxLine; set => maxLine = value; }
            public bool InfoFilter { get => infoFilter; set => infoFilter = value; }
            public bool WarningFilter { get => warningFilter; set => warningFilter = value; }
            public bool ErrorFilter { get => errorFilter; set => errorFilter = value; }
            public bool FatalFilter { get => fatalFilter; set => fatalFilter = value; }

            public int InfoCount => m_infoCount;
            public int WarningCount => m_warningCount;
            public int ErrorCount => m_errorCount;
            public int FatalCount => m_fatalCount;

            public Color InfoColor { get => infoColor; set => infoColor = value; }
            public Color WarningColor { get => warningColor; set => warningColor = value; }
            public Color ErrorColor { get => errorColor; set => errorColor = value; }
            public Color FatalColor { get => fatalColor; set => fatalColor = value; }

            // 样式缓存
            private GUIStyle _logToggleStyle;
            private GUIStyle _filterToggleStyle;
            private GUIStyle _detailsStyle;

            // 日志行样式缓存（避免每帧创建新对象导致内存泄漏）
            private GUIStyle _logRowStyleNormal;
            private GUIStyle _logRowStyleAlt;
            private GUIStyle _logRowStyleSelected;
            private Texture2D _bgTextureNormal;
            private Texture2D _bgTextureAlt;
            private Texture2D _bgTextureSelected;

            public void Initialize(params object[] args)
            {
                Application.logMessageReceived += OnLogMessageReceive;
                lockScroll = m_lastLockScroll = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_LOCK_SCROLL);
                infoFilter = m_lastInfoFilter = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_INFO_FILTER);
                warningFilter = m_lastWarningFilter = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_WARNING_FILTER);
                fatalFilter = m_lastFatalFilter = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_FATAL_FILTER);
                errorFilter = m_lastErrorFilter = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_ERROR_FILTER);
            }

            private void OnLogMessageReceive(string logMessage, string stacktrace, LogType type)
            {
                if (type == LogType.Assert)
                {
                    type = LogType.Error;
                }

                if (needPrintLog)
                {
                    PrintLogNode logNode = PrintLogNode.Create(type, logMessage, stacktrace);
                    m_logNodes.Add(logNode);
                }

                m_logNodeQueue.Enqueue(LogNode.Create(type, logMessage, stacktrace));
                while (m_logNodeQueue.Count > maxLine)
                {
                    MemoryObject.Release(m_logNodeQueue.Dequeue());
                }
            }

            public void OnEnter()
            {
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (m_lastLockScroll != lockScroll)
                {
                    m_lastLockScroll = lockScroll;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_LOCK_SCROLL, m_lastLockScroll);
                }

                if (m_lastInfoFilter != infoFilter)
                {
                    m_lastInfoFilter = infoFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_INFO_FILTER, m_lastInfoFilter);
                }

                if (m_lastWarningFilter != warningFilter)
                {
                    m_lastWarningFilter = warningFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_WARNING_FILTER, m_lastWarningFilter);
                }

                if (m_lastErrorFilter != errorFilter)
                {
                    m_lastErrorFilter = errorFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_ERROR_FILTER, m_lastErrorFilter);
                }

                if (m_lastFatalFilter != fatalFilter)
                {
                    m_lastFatalFilter = fatalFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_FATAL_FILTER, m_lastFatalFilter);
                }
            }

            public void OnDraw()
            {
                DebuggerStyles.Initialize();
                InitializeStyles();
                RefreshCount();

                // 工具栏区域
                DrawToolbar();

                GUILayout.Space(4);

                // 日志列表区域
                DrawLogList();

                GUILayout.Space(4);

                // 日志详情区域
                DrawLogDetails();
            }

            private void InitializeStyles()
            {
                if (_logToggleStyle == null)
                {
                    _logToggleStyle = new GUIStyle(GUI.skin.toggle)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(20, 4, 4, 4),
                        margin = new RectOffset(0, 0, 1, 1),
                        richText = true,
                        wordWrap = false,
                        fixedHeight = 22f
                    };
                    // 使用白色以确保富文本 <color> 标签的颜色能正确显示
                    _logToggleStyle.normal.textColor = Color.white;
                    _logToggleStyle.onNormal.textColor = Color.white;
                }

                if (_filterToggleStyle == null)
                {
                    _filterToggleStyle = new GUIStyle(GUI.skin.toggle)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(18, 6, 4, 4),
                        margin = new RectOffset(4, 4, 2, 2),
                        richText = true
                    };
                }

                if (_detailsStyle == null)
                {
                    _detailsStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.UpperLeft,
                        padding = new RectOffset(8, 8, 8, 8),
                        richText = true,
                        wordWrap = true
                    };
                    _detailsStyle.normal.textColor = DebuggerStyles.TextColor;
                }

                // 初始化日志行样式缓存
                InitializeLogRowStyles();
            }

            private void InitializeLogRowStyles()
            {
                if (_bgTextureNormal == null)
                {
                    _bgTextureNormal = CreateTexture(2, 2, new Color(0.18f, 0.18f, 0.22f, 0.6f));
                    _bgTextureAlt = CreateTexture(2, 2, new Color(0.22f, 0.22f, 0.26f, 0.6f));
                    _bgTextureSelected = CreateTexture(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.8f));
                }

                if (_logRowStyleNormal == null)
                {
                    _logRowStyleNormal = CreateLogRowStyle(_bgTextureNormal);
                    _logRowStyleAlt = CreateLogRowStyle(_bgTextureAlt);
                    _logRowStyleSelected = CreateLogRowStyle(_bgTextureSelected);
                }
            }

            private GUIStyle CreateLogRowStyle(Texture2D bgTexture)
            {
                GUIStyle style = new GUIStyle(_logToggleStyle);
                style.normal.background = bgTexture;
                style.onNormal.background = _bgTextureSelected;
                style.richText = true;
                style.normal.textColor = Color.white;
                style.onNormal.textColor = Color.white;
                return style;
            }

            private void DrawToolbar()
            {
                GUILayout.BeginHorizontal(DebuggerStyles.HeaderBoxStyle);
                {
                    // 清除按钮
                    if (GUILayout.Button("Clear", DebuggerStyles.ButtonStyle, GUILayout.Width(70f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                    {
                        Clear();
                    }

                    // 保存日志按钮
                    if (needPrintLog)
                    {
                        if (GUILayout.Button("Save", DebuggerStyles.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                        {
                            SaveLogsToDevice();
                        }
                    }

                    GUILayout.Space(8);

                    // 锁定滚动开关
                    lockScroll = GUILayout.Toggle(lockScroll, "Lock", _filterToggleStyle, GUILayout.Width(60f));

                    GUILayout.FlexibleSpace();

                    // 过滤器
                    DrawFilterToggle(ref infoFilter, "Info", m_infoCount, infoColor);
                    DrawFilterToggle(ref warningFilter, "Warn", m_warningCount, warningColor);
                    DrawFilterToggle(ref errorFilter, "Error", m_errorCount, errorColor);
                    DrawFilterToggle(ref fatalFilter, "Fatal", m_fatalCount, fatalColor);
                }
                GUILayout.EndHorizontal();
            }

            private void DrawFilterToggle(ref bool filter, string label, int count, Color color)
            {
                Color oldColor = GUI.contentColor;
                GUI.contentColor = color;
                filter = GUILayout.Toggle(filter, $"{label}({count})", _filterToggleStyle, GUILayout.Width(80f));
                GUI.contentColor = oldColor;
            }

            private void DrawLogList()
            {
                GUILayout.BeginVertical(DebuggerStyles.BoxStyle, GUILayout.ExpandHeight(true));
                {
                    if (lockScroll)
                    {
                        m_logScrollPosition.y = float.MaxValue;
                    }

                    m_logScrollPosition = GUILayout.BeginScrollView(m_logScrollPosition);
                    {
                        bool selected = false;
                        int index = 0;

                        foreach (var logNode in m_logNodeQueue)
                        {
                            // 过滤检查
                            if (!ShouldShowLog(logNode.LogType))
                            {
                                continue;
                            }

                            // 设置交替背景色
                            bool isAlt = index % 2 == 1;
                            bool isSelected = m_selectedLogNode == logNode;

                            GUIStyle rowStyle = GetLogRowStyle(logNode.LogType, isAlt, isSelected);

                            if (GUILayout.Toggle(isSelected, GetLogString(logNode), rowStyle))
                            {
                                selected = true;
                                if (m_selectedLogNode != logNode)
                                {
                                    m_selectedLogNode = logNode;
                                    m_stackTraceScrollPosition = Vector2.zero;
                                }
                            }

                            index++;
                        }

                        if (!selected)
                        {
                            m_selectedLogNode = null;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            private bool ShouldShowLog(LogType logType)
            {
                switch (logType)
                {
                    case LogType.Error:
                        return errorFilter;
                    case LogType.Warning:
                        return warningFilter;
                    case LogType.Log:
                        return infoFilter;
                    case LogType.Exception:
                        return fatalFilter;
                    default:
                        return true;
                }
            }

            private GUIStyle GetLogRowStyle(LogType logType, bool isAlt, bool isSelected)
            {
                // 使用缓存的样式，避免每帧创建新对象导致内存泄漏
                if (isSelected)
                {
                    return _logRowStyleSelected;
                }
                else if (isAlt)
                {
                    return _logRowStyleAlt;
                }
                else
                {
                    return _logRowStyleNormal;
                }
            }

            private static Texture2D CreateTexture(int width, int height, Color color)
            {
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }

            private void DrawLogDetails()
            {
                GUILayout.BeginVertical(DebuggerStyles.BoxStyle, GUILayout.Height(120f));
                {
                    // 详情标题
                    GUILayout.BeginHorizontal(DebuggerStyles.HeaderBoxStyle);
                    {
                        GUILayout.Label("<b>Details</b>", DebuggerStyles.LabelStyle);
                        GUILayout.FlexibleSpace();
                        if (m_selectedLogNode != null)
                        {
                            if (GUILayout.Button("Copy", DebuggerStyles.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                            {
                                CopyToClipboard(Utility.StringUtil.Format("{0}{2}{2}{1}",
                                    m_selectedLogNode.LogMessage, m_selectedLogNode.StackTrace, Environment.NewLine));
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    m_stackTraceScrollPosition = GUILayout.BeginScrollView(m_stackTraceScrollPosition);
                    {
                        if (m_selectedLogNode != null)
                        {
                            Color32 color = GetLogStringColor(m_selectedLogNode.LogType);
                            string detailsText = Utility.StringUtil.Format(
                                "<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>{4}</b></color>\n\n<color=#888888>{5}</color>",
                                color.r, color.g, color.b, color.a,
                                m_selectedLogNode.LogMessage,
                                m_selectedLogNode.StackTrace);

                            GUILayout.Label(detailsText, _detailsStyle);
                        }
                        else
                        {
                            GUILayout.Label("<color=#666666>Select a log entry to view details...</color>",
                                DebuggerStyles.CenteredLabelStyle);
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            public void OnExit()
            {
            }

            public void OnDestroy()
            {
                Application.logMessageReceived -= OnLogMessageReceive;
                Clear();

                // 释放缓存的纹理资源
                if (_bgTextureNormal != null)
                {
                    UnityEngine.Object.Destroy(_bgTextureNormal);
                    _bgTextureNormal = null;
                }
                if (_bgTextureAlt != null)
                {
                    UnityEngine.Object.Destroy(_bgTextureAlt);
                    _bgTextureAlt = null;
                }
                if (_bgTextureSelected != null)
                {
                    UnityEngine.Object.Destroy(_bgTextureSelected);
                    _bgTextureSelected = null;
                }

                _logRowStyleNormal = null;
                _logRowStyleAlt = null;
                _logRowStyleSelected = null;
            }

            private void Clear()
            {
                foreach (LogNode logNode in m_logNodeQueue)
                {
                    MemoryObject.Release(logNode);
                }
                m_logNodeQueue.Clear();
                ClearLogNodes();
                m_selectedLogNode = null;
            }

            public void RefreshCount()
            {
                m_infoCount = 0;
                m_warningCount = 0;
                m_errorCount = 0;
                m_fatalCount = 0;
                foreach (LogNode logNode in m_logNodeQueue)
                {
                    switch (logNode.LogType)
                    {
                        case LogType.Log:
                            m_infoCount++;
                            break;

                        case LogType.Warning:
                            m_warningCount++;
                            break;

                        case LogType.Error:
                            m_errorCount++;
                            break;

                        case LogType.Exception:
                            m_fatalCount++;
                            break;
                    }
                }
            }

            public void GetRecentLogs(List<LogNode> result)
            {
                if (result == null)
                {
                    DLogger.Error("result无效");
                    return;
                }
                result.Clear();

                foreach (var logNode in m_logNodeQueue)
                {
                    result.Add(logNode);
                }
            }

            public void GetRecentLogs(List<LogNode> results, int count)
            {
                if (results == null)
                {
                    DLogger.Error("result无效");
                    return;
                }

                if (count <= 0)
                {
                    DLogger.Error("数量无效");
                    return;
                }

                int position = m_logNodeQueue.Count - count;
                if (position < 0)
                {
                    position = 0;
                }

                int index = 0;
                results.Clear();
                foreach (LogNode logNode in m_logNodeQueue)
                {
                    if (index++ < position)
                    {
                        continue;
                    }

                    results.Add(logNode);
                }
            }

            private string GetLogString(LogNode logNode)
            {
                Color32 color = GetLogStringColor(logNode.LogType);
                // 对于Exception类型，Unity传递的logMessage已包含堆栈，只显示第一行
                string displayMessage = logNode.LogMessage;
                if (logNode.LogType == LogType.Exception)
                {
                    displayMessage = TruncateString(displayMessage, 500);
                }
                return Utility.StringUtil.Format(Constant.CONSOLE_WINDOW_LOG_SINGLE_MESSAGE_STRING, color.r, color.g,
                    color.b, color.a, logNode.LogTime.ToLocalTime(), logNode.LogFrameCount, displayMessage);
            }

            private string TruncateString(string str, int maxLength)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return str;
                }

                // 移除Exception错误消息开头的"Exception:"前缀
                if (str.StartsWith("Exception: "))
                {
                    str = str.Substring("Exception: ".Length);
                }

                // 先移除富文本标签，避免截断时破坏标签导致富文本解析失败
                string plainText = RemoveRichTextTags(str);

                if (plainText.Length <= maxLength)
                {
                    return plainText;
                }

                // 截取第一行
                int newlineIndex = plainText.IndexOf('\n');
                if (newlineIndex >= 0 && newlineIndex < maxLength)
                {
                    return plainText.Substring(0, newlineIndex) + "...";
                }

                return plainText.Substring(0, maxLength) + "...";
            }

            private string RemoveRichTextTags(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return str;
                }

                // 移除常见的富文本标签: <color>, <b>, <i>, <size>, </color>, </b>, </i>, </size> 等
                var result = System.Text.RegularExpressions.Regex.Replace(str, @"<[^>]+>", string.Empty);
                return result;
            }

            internal Color32 GetLogStringColor(LogType logType)
            {
                switch (logType)
                {
                    case LogType.Log:
                        return infoColor;

                    case LogType.Warning:
                        return warningColor;

                    case LogType.Error:
                        return errorColor;

                    case LogType.Exception:
                        return fatalColor;
                }

                return Color.white;
            }

            private void SaveLogsToDevice()
            {
                if (m_logNodes == null || m_logNodes.Count == 0)
                {
                    return;
                }
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Application.persistentDataPath + $"/game_logs_{timestamp}.txt";
                List<string> fileContentsList = new List<string>();
                DLogger.Info("Saving logs to " + filePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                for (int i = 0; i < m_logNodes.Count; i++)
                {
                    fileContentsList.Add(m_logNodes[i].LogType + "\n" + m_logNodes[i].LogMessage + "\n" + m_logNodes[i].StackTrace);
                }
                File.WriteAllLines(filePath, fileContentsList.ToArray());
                ClearLogNodes();
            }

            private void ClearLogNodes()
            {
                if (m_logNodes == null || m_logNodes.Count == 0)
                {
                    return;
                }

                foreach (var node in m_logNodes)
                {
                    MemoryObject.Release(node);
                }
                m_logNodes.Clear();
            }
        }
    }
}