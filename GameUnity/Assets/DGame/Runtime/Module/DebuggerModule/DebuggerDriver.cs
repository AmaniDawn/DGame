using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    [DisallowMultipleComponent]
    public sealed partial class DebuggerDriver : MonoBehaviour
    {
        private static DebuggerDriver m_instance;
        public static DebuggerDriver Instance => m_instance;

        /// <summary>
        /// 默认调试器漂浮框大小
        /// </summary>
        internal static readonly Rect DefaultIconRect = new Rect(10f, 10f, 120f, 56f);

        /// <summary>
        /// 默认调试器窗口大小
        /// </summary>
        internal static readonly Rect DefaultWindowRect = new Rect(10f, 10f, 640f, 640f);

        /// <summary>
        /// 默认调试器窗口缩放比例
        /// </summary>
        internal static readonly float DefaultWindowScale = 1.5f;

        private static TextEditor m_textEditor;
        private IDebuggerModule m_debuggerModule;
        private readonly Rect m_dragRect = new Rect(0f, 0f, float.MaxValue, float.MaxValue);
        private const float DragMargin = 15f; // 四边可拖动边距
        private Rect m_iconRect = DefaultIconRect;
        private Rect m_windowRect = DefaultWindowRect;
        private float m_windowScale = DefaultWindowScale;

        [SerializeField] private GUISkin reporterScrollerSkin;

        [SerializeField] [InspectorName("调试窗口开启模式")] private DebuggerActiveWindowType activeWindowType = DebuggerActiveWindowType.AlwaysOpen;

        public DebuggerActiveWindowType ActiveWindowType => activeWindowType;

        [SerializeField] private bool m_showFullWindow = false;
        [SerializeField] private ConsoleWindow m_consoleWindow = new ConsoleWindow();

        private SystemInformationWindow m_systemInformationWindow = new SystemInformationWindow();
        private EnvironmentInformationWindow m_environmentInformationWindow = new EnvironmentInformationWindow();
        private ScreenInformationWindow m_screenInformationWindow = new ScreenInformationWindow();
        private GraphicsInformationWindow m_graphicsInformationWindow = new GraphicsInformationWindow();
        private InputSummaryInformationWindow m_inputSummaryInformationWindow = new InputSummaryInformationWindow();
        private InputTouchInformationWindow m_inputTouchInformationWindow = new InputTouchInformationWindow();
        private InputLocationInformationWindow m_inputLocationInformationWindow = new InputLocationInformationWindow();
        private InputAccelerationInformationWindow m_inputAccelerationInformationWindow = new InputAccelerationInformationWindow();
        private InputGyroscopeInformationWindow m_inputGyroscopeInformationWindow = new InputGyroscopeInformationWindow();
        private InputCompassInformationWindow m_inputCompassInformationWindow = new InputCompassInformationWindow();
        private SceneInformationWindow m_sceneInformationWindow = new SceneInformationWindow();
        private PathInformationWindow m_pathInformationWindow = new PathInformationWindow();
        private TimeInformationWindow m_timeInformationWindow = new TimeInformationWindow();
        private QualityInformationWindow m_qualityInformationWindow = new QualityInformationWindow();
        private RuntimeMemorySummaryWindow m_runtimeMemorySummaryWindow = new RuntimeMemorySummaryWindow();
        private RuntimeMemoryInformationWindow<UnityEngine.Object> m_runtimeMemoryAllInformationWindow = new RuntimeMemoryInformationWindow<UnityEngine.Object>();
        private RuntimeMemoryInformationWindow<Texture> m_runtimeMemoryTextureInformationWindow = new RuntimeMemoryInformationWindow<Texture>();
        private RuntimeMemoryInformationWindow<Mesh> m_runtimeMemoryMeshInformationWindow = new RuntimeMemoryInformationWindow<Mesh>();
        private RuntimeMemoryInformationWindow<Material> m_runtimeMemoryMaterialInformationWindow = new RuntimeMemoryInformationWindow<Material>();
        private RuntimeMemoryInformationWindow<Shader> m_runtimeMemoryShaderInformationWindow = new RuntimeMemoryInformationWindow<Shader>();
        private RuntimeMemoryInformationWindow<AnimationClip> m_runtimeMemoryAnimationClipInformationWindow = new RuntimeMemoryInformationWindow<AnimationClip>();
        private RuntimeMemoryInformationWindow<AudioClip> m_runtimeMemoryAudioClipInformationWindow = new RuntimeMemoryInformationWindow<AudioClip>();
        private RuntimeMemoryInformationWindow<Font> m_runtimeMemoryFontInformationWindow = new RuntimeMemoryInformationWindow<Font>();
        private RuntimeMemoryInformationWindow<TextAsset> m_runtimeMemoryTextAssetInformationWindow = new RuntimeMemoryInformationWindow<TextAsset>();
        private RuntimeMemoryInformationWindow<ScriptableObject> m_runtimeMemoryScriptableObjectInformationWindow = new RuntimeMemoryInformationWindow<ScriptableObject>();
        private ObjectPoolInformationWindow m_objectPoolInformationWindow = new ObjectPoolInformationWindow();
        private MemoryPoolPoolInformationWindow m_memoryPoolPoolInformationWindow = new MemoryPoolPoolInformationWindow();
        private ProfilerInformationWindow m_profilerInformationWindow = new ProfilerInformationWindow();

        private SettingsWindow m_settingsWindow = new SettingsWindow();

        private FpsCounter m_fpsCounter;

        private GameObject m_eventSystem;

        private bool m_activeWindow;

        public bool ActiveWindow
        {
            get => m_debuggerModule.ActiveWindow;
            set=> enabled = m_debuggerModule.ActiveWindow = value;
        }

        public bool ShowFullWindow
        {
            get => m_showFullWindow;
            set
            {
                if (m_eventSystem != null)
                {
                    m_eventSystem.SetActive(!value);
                }
                m_showFullWindow = value;
            }
        }

        public Rect IconRect { get => m_iconRect; set => m_iconRect = value; }
        public Rect WindowRect { get => m_windowRect; set => m_windowRect = value; }
        public float WindowScale { get => m_windowScale; set => m_windowScale = value; }

        private void Awake()
        {
            switch (activeWindowType)
            {
                case DebuggerActiveWindowType.AlwaysOpen:
                    m_activeWindow = true;
                    break;

                case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                    m_activeWindow = Debug.isDebugBuild;
                    break;

                case DebuggerActiveWindowType.OnlyOpenInEditor:
                    m_activeWindow = Application.isEditor;
                    break;

                default:
                    m_activeWindow = false;
                    break;
            }

            if (!m_activeWindow)
            {
                Destroy(gameObject);
            }

            m_instance = this;
            m_textEditor = new TextEditor();
            m_instance.gameObject.name = $"[{nameof(DebuggerDriver)}]";
            m_eventSystem = GameObject.Find("EventSystem");
        }

        private void Start()
        {
            Initialize();

            RegisterDebuggerWindow("Console", m_consoleWindow);
            RegisterDebuggerWindow("Information/System", m_systemInformationWindow);
            RegisterDebuggerWindow("Information/Environment", m_environmentInformationWindow);
            RegisterDebuggerWindow("Information/Screen", m_screenInformationWindow);
            RegisterDebuggerWindow("Information/Graphics", m_graphicsInformationWindow);
            RegisterDebuggerWindow("Information/Input/Summary", m_inputSummaryInformationWindow);
            RegisterDebuggerWindow("Information/Input/Touch", m_inputTouchInformationWindow);
            RegisterDebuggerWindow("Information/Input/Location", m_inputLocationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Acceleration", m_inputAccelerationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Gyroscope", m_inputGyroscopeInformationWindow);
            RegisterDebuggerWindow("Information/Input/Compass", m_inputCompassInformationWindow);
            RegisterDebuggerWindow("Information/Other/Scene", m_sceneInformationWindow);
            RegisterDebuggerWindow("Information/Other/Path", m_pathInformationWindow);
            RegisterDebuggerWindow("Information/Other/Time", m_timeInformationWindow);
            RegisterDebuggerWindow("Information/Other/Quality", m_qualityInformationWindow);
            RegisterDebuggerWindow("Profiler/Summary", m_profilerInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Summary", m_runtimeMemorySummaryWindow);
            RegisterDebuggerWindow("Profiler/Memory/All", m_runtimeMemoryAllInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Texture", m_runtimeMemoryTextureInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Mesh", m_runtimeMemoryMeshInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Material", m_runtimeMemoryMaterialInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Shader", m_runtimeMemoryShaderInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AnimationClip", m_runtimeMemoryAnimationClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AudioClip", m_runtimeMemoryAudioClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Font", m_runtimeMemoryFontInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/TextAsset", m_runtimeMemoryTextAssetInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/ScriptableObject", m_runtimeMemoryScriptableObjectInformationWindow);
            RegisterDebuggerWindow("Profiler/Object Pool", m_objectPoolInformationWindow);
            RegisterDebuggerWindow("Profiler/Reference Pool", m_memoryPoolPoolInformationWindow);
            RegisterDebuggerWindow("Other/Settings", m_settingsWindow);

            ActiveWindow = m_activeWindow;
        }

        private void Update()
        {
            m_fpsCounter?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (m_debuggerModule == null || !m_debuggerModule.ActiveWindow)
            {
                return;
            }

            DebuggerStyles.Initialize();

            GUISkin cachedGuiSkin = GUI.skin;
            Matrix4x4 cachedMatrix = GUI.matrix;
            GUI.skin = reporterScrollerSkin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(m_windowScale, m_windowScale, 1f));

            if (m_showFullWindow)
            {
                m_windowRect = GUILayout.Window(0, m_windowRect, DrawWindow, "", DebuggerStyles.PanelStyle);
            }
            else
            {
                m_iconRect = GUILayout.Window(0, m_iconRect, DrawDebuggerWindowIcon, "", DebuggerStyles.PanelStyle);
            }

            GUI.matrix = cachedMatrix;
            GUI.skin = cachedGuiSkin;
        }

        private void DrawDebuggerWindowIcon(int windowID)
        {
            m_consoleWindow.RefreshCount();

            // 获取FPS颜色
            Color32 fpsColor = DebuggerStyles.GetFpsColor(m_fpsCounter.CurrentFps);

            // 获取日志状态颜色
            Color32 statusColor = Color.white;
            string statusIcon = "";
            if (m_consoleWindow.FatalCount > 0)
            {
                statusColor = m_consoleWindow.GetLogStringColor(LogType.Exception);
                statusIcon = " [!]";
            }
            else if (m_consoleWindow.ErrorCount > 0)
            {
                statusColor = m_consoleWindow.GetLogStringColor(LogType.Error);
                statusIcon = " [E]";
            }
            else if (m_consoleWindow.WarningCount > 0)
            {
                statusColor = m_consoleWindow.GetLogStringColor(LogType.Warning);
                statusIcon = " [W]";
            }

            // 构建显示文本
            string fpsText = DebuggerStyles.ColorBoldText($"FPS: {m_fpsCounter.CurrentFps:F1}", fpsColor);
            string statusText = statusIcon.Length > 0 ? DebuggerStyles.ColorText(statusIcon, statusColor) : "";

            if (GUILayout.Button(fpsText + statusText, DebuggerStyles.FpsButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(40f)))
            {
                ShowFullWindow = true;
            }

            // 拖动放在最后，让按钮优先处理点击事件
            GUI.DragWindow(m_dragRect);
        }

        public void GetRecentLogs(List<LogNode> results)
        {
            m_consoleWindow.GetRecentLogs(results);
        }

        public void GetRecentLogs(List<LogNode> results, int count)
        {
            m_consoleWindow.GetRecentLogs(results, count);
        }

        private void DrawDebuggerWindowGroup(IDebuggerWindowGroup debuggerWindowGroup)
        {
            if (debuggerWindowGroup == null)
            {
                return;
            }

            List<string> names = new List<string>();
            string[] debuggerWindowNames = debuggerWindowGroup.GetDebuggerWindowNames();

            for (int i = 0; i < debuggerWindowNames.Length; i++)
            {
                names.Add(Utility.StringUtil.Format("<b>{0}</b>", debuggerWindowNames[i]));
            }

            bool isRoot = debuggerWindowGroup == m_debuggerModule.DebuggerWindowRoot;
            if (isRoot)
            {
                names.Add("<b>Close</b>");
            }

            // 绘制选项卡
            GUILayout.BeginVertical();
            {
                int toolbarIndex = 0;
                int itemsPerRow = names.Count > 6 ? 5 : names.Count;
                int rowCount = Mathf.CeilToInt((float)names.Count / itemsPerRow);

                toolbarIndex = GUILayout.SelectionGrid(
                    debuggerWindowGroup.SelectedIndex,
                    names.ToArray(),
                    itemsPerRow,
                    DebuggerStyles.ToolbarButtonStyle,
                    GUILayout.Height(DebuggerStyles.TabHeight * rowCount)
                );

                if (toolbarIndex >= debuggerWindowGroup.DebuggerWindowCount)
                {
                    ShowFullWindow = false;
                    GUILayout.EndVertical();
                    return;
                }

                if (debuggerWindowGroup.SelectedWindow == null)
                {
                    GUILayout.EndVertical();
                    return;
                }

                if (debuggerWindowGroup.SelectedIndex != toolbarIndex)
                {
                    debuggerWindowGroup.SelectedWindow?.OnExit();
                    debuggerWindowGroup.SelectedIndex = toolbarIndex;
                    debuggerWindowGroup.SelectedWindow?.OnEnter();
                }

                // 绘制分隔线
                GUILayout.Space(4);

                // 绘制子窗口组或窗口内容
                if (debuggerWindowGroup.SelectedWindow is IDebuggerWindowGroup subDebuggerWindowGroup)
                {
                    DrawDebuggerWindowGroup(subDebuggerWindowGroup);
                }

                debuggerWindowGroup.SelectedWindow?.OnDraw();
            }
            GUILayout.EndVertical();
        }

        private void DrawWindow(int windowID)
        {
            // 外层边距（用于拖动）
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(DragMargin); // 左边距

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(DragMargin); // 上边距

                    // 窗口标题栏
                    GUILayout.BeginHorizontal(DebuggerStyles.HeaderBoxStyle);
                    {
                        GUILayout.Label("<b>DEBUGGER</b>", DebuggerStyles.TitleStyle);
                        GUILayout.FlexibleSpace();

                        // 显示FPS
                        Color32 fpsColor = DebuggerStyles.GetFpsColor(m_fpsCounter.CurrentFps);
                        string fpsText = DebuggerStyles.ColorBoldText($"FPS: {m_fpsCounter.CurrentFps:F1}", fpsColor);
                        GUILayout.Label(fpsText, DebuggerStyles.RichLabelStyle, GUILayout.Width(100f));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                    DrawDebuggerWindowGroup(m_debuggerModule.DebuggerWindowRoot);

                    GUILayout.Space(DragMargin); // 下边距
                }
                GUILayout.EndVertical();

                GUILayout.Space(DragMargin); // 右边距
            }
            GUILayout.EndHorizontal();

            // 绘制悬停提示
            DebuggerStyles.DrawTooltip();

            // 拖动放在最后，让按钮等控件优先处理点击事件
            GUI.DragWindow(m_dragRect);
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }

        private void Initialize()
        {
            m_debuggerModule = ModuleSystem.GetModule<IDebuggerModule>();

            if (m_debuggerModule == null)
            {
                DLogger.Fatal("DebuggerModule无效");
                return;
            }

            m_fpsCounter = new FpsCounter(Constant.DEFAULT_DEBUGGER_WINDOW_FPS_UPDATE_INTERVAL);

            var lastIconX = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, DefaultIconRect.x);
            var lastIconY = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, DefaultIconRect.y);
            var lastWindowX = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, DefaultWindowRect.x);
            var lastWindowY = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, DefaultWindowRect.y);
            var lastWindowWidth = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, DefaultWindowRect.width);
            var lastWindowHeight = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, DefaultWindowRect.height);
            m_windowScale = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, DefaultWindowScale);
            // m_windowRect = new Rect(lastIconX, lastIconY, DefaultIconRect.width, DefaultIconRect.height);
            m_windowRect = new Rect(lastWindowX, lastWindowY, lastWindowWidth, lastWindowHeight);
        }

        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            m_debuggerModule.RegisterDebuggerWindow(path, debuggerWindow, args);
        }

        public bool UnRegisterDebuggerWindow(string path)
        {
            return m_debuggerModule.UnRegisterDebuggerWindow(path);
        }

        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return m_debuggerModule.GetDebuggerWindow(path);
        }

        public bool SelectDebuggerWindow(string path)
        {
            return m_debuggerModule.SelectDebuggerWindow(path);
        }

        public void ResetWindowLayout()
        {
            IconRect = DefaultIconRect;
            WindowRect = DefaultWindowRect;
            WindowScale = DefaultWindowScale;
        }

        private static void CopyToClipboard(string content)
        {
            m_textEditor.text = content;
            m_textEditor.OnFocus();
            m_textEditor.Copy();
            m_textEditor.text = string.Empty;
        }
    }
}