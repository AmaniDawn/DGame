using System;
using UnityEngine;


namespace DGame
{

    [DisallowMultipleComponent]
    public sealed class RootModule : MonoBehaviour
    {
        private static RootModule m_instance = null;

        public static RootModule Instance => m_instance == null ? DGame.Utility.UnityUtil.FindObjectOfType<RootModule>() : m_instance;

        private const int DEFAULT_DPI = 96;

        private float m_gameSpeedBeforePause = 1f;

        // [SerializeField]
        // private Language editorLanguage = Language.CN;
        //
        // /// <summary>
        // /// 获取或设置编辑器语言（仅编辑器内有效）。
        // /// </summary>
        // public Language EditorLanguage
        // {
        //     get => editorLanguage;
        //     set => editorLanguage = value;
        // }

        [SerializeField]
        private string stringUtilHelperTypeName = "DGame.DGameStringUtilHelper";

        [SerializeField]
        private string logHelperTypeName = "DGame.DGameLogHelper";

        [SerializeField]
        private string jsonHelperTypeName = "DGame.DefaultJsonHelper";

        [SerializeField]
        private int frameRate = 120;

        [SerializeField]
        private float gameSpeed = 1f;

        [SerializeField]
        private bool runInBackground = true;

        [SerializeField]
        private bool neverSleep = true;

        public int FrameRate
        {
            get => frameRate;
            set => Application.targetFrameRate = frameRate = value;
        }

        public float GameSpeed
        {
            get => gameSpeed;
            set => Time.timeScale = gameSpeed = value >= 0f ? value : 0f;
        }

        public bool IsGamePaused => gameSpeed <= 0f;

        public bool IsNormalGameSpeed => Mathf.Abs(gameSpeed - 1f) < 0.01f;

        public bool RunInBackground
        {
            get => runInBackground;
            set => Application.runInBackground = runInBackground = value;
        }

        public bool NeverSleep
        {
            get => neverSleep;
            set
            {
                neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        [SerializeField]
        private MemoryStrictCheckType m_memoryStrictCheckType = MemoryStrictCheckType.OnlyEnableInEditor;

        public bool EnableStrictCheck
        {
            get => MemoryPool.EnableStrictCheck;
            set
            {
                MemoryPool.EnableStrictCheck = value;
                if (value)
                {
                    DLogger.Info("内存池已启用严格检查，这将极大程序影响性能。");
                }
            }
        }

        private void Awake()
        {
            InitMemoryPoolSetting();
            // DLogger.Info($"======== RootModule Awake() ========");
            m_instance = this;
            InitStringUtilHelper();
            InitLogHelper();
            InitJsonHelper();
            DLogger.Info($"======== Unity Version: {Application.unityVersion} ========");

            Utility.Converter.ScreenDpi = Screen.dpi;
            if (Utility.Converter.ScreenDpi <= 0)
            {
                Utility.Converter.ScreenDpi = DEFAULT_DPI;
            }

            Application.targetFrameRate = frameRate;
            Time.timeScale = gameSpeed;
            Application.runInBackground = runInBackground;
            Screen.sleepTimeout = neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            Application.lowMemory += OnLowMemory;
            GameTime.StartFrame();
        }

        private void InitMemoryPoolSetting()
        {
            switch (m_memoryStrictCheckType)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    EnableStrictCheck = true;
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    EnableStrictCheck = Application.isEditor;
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    EnableStrictCheck = false;
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    EnableStrictCheck = Debug.isDebugBuild;
                    break;
            }
        }

        private void OnLowMemory()
        {
            DLogger.Warning("======== 内存不足 自动清理缓存... ========");
            var objectPoolModule = ModuleSystem.GetModule<IObjectPoolModule>();
            objectPoolModule?.ReleaseAllUnused();
            var resourceModule = ModuleSystem.GetModule<IResourceModule>();
            resourceModule?.OnLowMemory();
        }

        private void InitLogHelper()
        {
            if (string.IsNullOrEmpty(logHelperTypeName))
            {
                return;
            }

            Type logHelperType = Utility.AssemblyUtil.GetType(logHelperTypeName);

            if (logHelperType == typeof(Nullable))
            {
                return;
            }

            if (logHelperType == null)
            {
                DLogger.Error("查找不到默认的ILogHelper类型：'{0}'", logHelperTypeName);
                return;
            }

            DGameLog.ILogHelper logHelper = Activator.CreateInstance(logHelperType) as DGameLog.ILogHelper;

            if (logHelper == null)
            {
                DLogger.Error("无法创建ILogHelper类型实例：'{0}'", logHelperTypeName);
                return;
            }

            DGameLog.SetLogHelper(logHelper);
        }

        private void InitStringUtilHelper()
        {
            if (string.IsNullOrEmpty(stringUtilHelperTypeName))
            {
                return;
            }
            Type type = Utility.AssemblyUtil.GetType(stringUtilHelperTypeName);

            if (type == typeof(Nullable))
            {
                return;
            }

            if (type == null)
            {
                DLogger.Error("查找不到默认的IStringUtilHelper类型：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.IStringUtilHelper stringUtilHelper = Activator.CreateInstance(type) as Utility.StringUtil.IStringUtilHelper;

            if (stringUtilHelper == null)
            {
                DLogger.Error("无法创建IStringUtilHelper类型实例：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.SetStringHelper(stringUtilHelper);
        }

        private void InitJsonHelper()
        {
            if (string.IsNullOrEmpty(jsonHelperTypeName))
            {
                return;
            }

            Type jsonHelperType = Utility.AssemblyUtil.GetType(jsonHelperTypeName);

            if (jsonHelperType == typeof(Nullable))
            {
                return;
            }

            if (jsonHelperType == null)
            {
                DLogger.Error("查找不到默认的IJsonHelper类型：'{0}'", jsonHelperTypeName);
                return;
            }

            Utility.IJsonHelper jsonHelper = Activator.CreateInstance(jsonHelperType) as Utility.IJsonHelper;

            if (jsonHelper == null)
            {
                DLogger.Error("无法创建IJsonHelper类型实例：'{0}'", jsonHelperTypeName);
                return;
            }

            Utility.JsonUtil.SetJsonHelper(jsonHelper);
        }

        private void Update()
        {
            GameTime.StartFrame();
            ModuleSystem.Update(GameTime.DeltaTime, GameTime.UnscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            GameTime.StartFrame();
        }

        private void LateUpdate()
        {
            GameTime.StartFrame();
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR

            ModuleSystem.Destroy();

#endif
        }

        private void OnApplicationQuit()
        {
            Application.lowMemory -= OnLowMemory;
            StopAllCoroutines();
        }

        internal void Destroy()
        {
            Destroy(gameObject);
        }

        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }
            m_gameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }
            GameSpeed = m_gameSpeedBeforePause;
        }

        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed)
            {
                return;
            }
            GameSpeed = 1f;
        }
    }
}