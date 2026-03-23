using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed partial class UIModule : Singleton<UIModule>, IUpdate
    {
        #region 常量定义

        private const int DEFAULT_LAYER_DEEP = 2000;
        public const int EACH_WINDOW_DEEP = 100;

        #endregion

        private bool m_enableErrorLog = true;
        private static Transform s_uiCanvas;
        private Camera m_uiCamera;
        private readonly List<UIWindow> m_uiStack = new List<UIWindow>(32);
        private readonly Dictionary<string, UIWindow> m_uiFullNameMap = new Dictionary<string, UIWindow>(32);
        private readonly Dictionary<uint, UIWindow> m_uiMap = new Dictionary<uint, UIWindow>(32);
        private readonly Queue<UIWindow> m_popWindowQueue = new Queue<UIWindow>(16);
        private bool m_isPoppingWindowQueue = false;
        private ErrorLogger m_errorLogger;

        #region UIController

        partial void RegisterAllController();

        #endregion

        /// <summary>
        /// 资源加载接口
        /// </summary>
        public static IUIResourceLoader ResourceLoader;

        /// <summary>
        /// UI根节点
        /// </summary>
        public static Transform UICanvas => s_uiCanvas;

        /// <summary>
        /// UI摄像机
        /// </summary>
        public Camera UICamera => m_uiCamera;

        protected override void OnInit()
        {
            var uiRoot = GameObject.Find("UIRoot");

            if (uiRoot != null)
            {
                s_uiCanvas = uiRoot.GetComponentInChildren<Canvas>()?.transform;
                m_uiCamera = uiRoot.GetComponentInChildren<Camera>();
            }
            else
            {
                DLogger.Fatal("不存在UIRoot，请检查场景资源");
                return;
            }

            ResourceLoader = new UIResourceLoader();
            UnityEngine.Object.DontDestroyOnLoad(uiRoot);

            if (s_uiCanvas != null)
            {
                s_uiCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            }

            RegisterAllController();

            if (DebuggerDriver.Instance != null)
            {
                switch (DebuggerDriver.Instance.ActiveWindowType)
                {
                    case DebuggerActiveWindowType.AlwaysOpen:
                        m_enableErrorLog = true;
                        break;

                    case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                        m_enableErrorLog = Debug.isDebugBuild;
                        break;

                    case DebuggerActiveWindowType.OnlyOpenInEditor:
                        m_enableErrorLog = Application.isEditor;
                        break;

                    default:
                        m_enableErrorLog = false;
                        break;
                }
                if (m_enableErrorLog)
                {
                    m_errorLogger = new ErrorLogger(this);
                }
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void OnUpdate()
        {
            if (m_uiStack == null || m_uiStack.Count <= 0)
            {
                return;
            }

            int count = m_uiStack.Count;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack.Count != count)
                {
                    return;
                }
                var window = m_uiStack[i];
                window.InternalUpdate();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GetAndCloseTopWindow((int)UILayer.System);
            }
        }

        /// <summary>
        /// 模块销毁释放
        /// </summary>
        protected override void OnDestroy()
        {
            if (m_errorLogger != null)
            {
                m_errorLogger.Dispose();
                m_errorLogger = null;
            }
            CloseAllWindows();
            if (s_uiCanvas != null && s_uiCanvas.parent != null)
            {
                UnityEngine.Object.Destroy(s_uiCanvas.parent.gameObject);
            }
            // RedDotModule.Instance.Destroy();
            ClearWindowQueue();
        }

        #region 设置安全区域

        /// <summary>
        /// 设置屏幕安全区域（异形屏支持）
        /// </summary>
        /// <param name="safeRect">安全区域举行（基于屏幕像素坐标）</param>
        public static void ApplyScreenSafeRect(Rect safeRect)
        {
            CanvasScaler scaler = UICanvas?.GetComponentInParent<CanvasScaler>();

            if (scaler == null)
            {
                DLogger.Error($"没有找到 {nameof(CanvasScaler)}");
                return;
            }

            float rateX = scaler.referenceResolution.x / Screen.width;
            float rateY = scaler.referenceResolution.y / Screen.height;
            float posX = (int)(safeRect.position.x * rateX);
            float posY = (int)(safeRect.position.y * rateY);
            float width = (int)(safeRect.size.x * rateX);
            float height = (int)(safeRect.size.y * rateY);

            float offsetX = scaler.referenceResolution.x - width - posX;
            float offsetY = scaler.referenceResolution.y - height - posY;

            // 注意：安全区域坐标系的原点为左下角
            var rectTrans = UICanvas.transform as RectTransform;

            if (rectTrans != null)
            {
                // 锚框状态下的屏幕左下角偏移向量
                rectTrans.offsetMin = new Vector2(posX, posY);
                // 锚框状态下的屏幕右上角角偏移向量
                rectTrans.offsetMax = new Vector2(-offsetX, -offsetY);
            }
        }

        /// <summary>
        /// 模拟iPhoneX刘海屏安全区域
        /// </summary>
        public static void SimulateIPhoneXNotchScreen()
        {
            Rect rect;

            if (Screen.height > Screen.width)
            {
                // 竖屏
                float deviceWidth = 1125;
                float deviceHeight = 2436;
                rect = new Rect(0 / deviceWidth, 102f / deviceHeight, 1125f / deviceWidth, 2202f / deviceHeight);
            }
            else
            {
                // 横屏
                float deviceWidth = 2436;
                float deviceHeight = 1125;
                rect = new Rect(132f / deviceWidth, 63f / deviceHeight, 2172f / deviceWidth, 1062f / deviceHeight);
            }

            Rect safeArea = new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width, Screen.height * rect.height);
            ApplyScreenSafeRect(safeArea);
        }

        #endregion

        #region Window队列系统

        /// <summary>
        /// 将窗口推入队列
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        public void PushWindowToQueue<T>() where T : UIWindow, new()
        {
            T window = new T();
            window.IsInQueue = true;
            m_popWindowQueue.Enqueue(window);
        }

        /// <summary>
        /// 开始弹出队列中的窗口
        /// </summary>
        public void StartPopWindowQueue()
        {
            if (m_isPoppingWindowQueue)
            {
                return;
            }
            m_isPoppingWindowQueue = true;
            PopWindowQueue().Forget();
        }

        private void PopQueueNextWindow(UIWindow window)
        {
            if (window != null && window.IsInQueue && m_isPoppingWindowQueue)
            {
                window.IsInQueue = false;
                PopWindowQueue().Forget();
            }
        }

        private async UniTaskVoid PopWindowQueue()
        {
            if (m_popWindowQueue.Count > 0)
            {
                var uiWindow =  m_popWindowQueue.Dequeue();
                await ShowWindowAsyncAwait(uiWindow, null);
            }
            else
            {
                m_isPoppingWindowQueue = false;
            }
        }

        /// <summary>
        /// 清空窗口队列
        /// </summary>
        public void ClearWindowQueue()
        {
            m_popWindowQueue.Clear();
        }

        #endregion

        #region GetTopWindow

        /// <summary>
        /// 获取所有层级下顶部的窗口名称
        /// </summary>
        /// <returns></returns>
        public string GetTopWindow()
        {
            if (m_uiStack.Count == 0)
            {
                return string.Empty;
            }
            // 队列最后一个一定是最顶部窗口
            UIWindow window = m_uiStack[^1];
            return window.WindowFullName;
        }

        /// <summary>
        /// 获取指定层级顶部的窗口名称
        /// </summary>
        /// <param name="layer">窗口层级</param>
        /// <returns>窗口名称</returns>
        public string GetTopWindow(int layer)
        {
            UIWindow lastOne = null;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack[i].WindowLayer == layer)
                {
                    lastOne = m_uiStack[i];
                }
            }

            if (lastOne == null)
            {
                return string.Empty;
            }
            return lastOne.WindowFullName;
        }

        /// <summary>
        /// 获取并关闭顶部窗口（排除指定层级）
        /// </summary>
        /// <param name="excludeLayer">排除的层级</param>
        /// <returns>是否成功关闭</returns>
        public bool GetAndCloseTopWindow(int excludeLayer)
        {
            UIWindow lastOne = null;
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack[i].WindowLayer != excludeLayer)
                {
                    lastOne = m_uiStack[i];
                }
            }

            if (lastOne == null)
            {
                return false;
            }

            if (lastOne.CanEscClose)
            {
                CloseWindow(lastOne);
                return true;
            }

            lastOne.OnEscCloseLastOneWindowCallback?.Invoke();
            return false;
        }

        /// <summary>
        /// 获取并关闭顶部窗口（排除多个层级）
        /// </summary>
        /// <param name="excludeLayers">排除的层级列表</param>
        /// <returns>是否成功关闭</returns>
        public bool GetAndCloseTopWindow(List<int> excludeLayers)
        {
            UIWindow lastOne = null;
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (!excludeLayers.Contains(m_uiStack[i].WindowLayer))
                {
                    lastOne = m_uiStack[i];
                }
            }

            if (lastOne == null)
            {
                return false;
            }

            if (lastOne.CanEscClose)
            {
                CloseWindow(lastOne);
                return true;
            }

            lastOne.OnEscCloseLastOneWindowCallback?.Invoke();
            return false;
        }

        /// <summary>
        /// 获取并关闭顶部窗口（排除指定层级和窗口）
        /// </summary>
        /// <param name="excludeLayers">排除的层级列表</param>
        /// <param name="excludeWindows">排除的窗口列表</param>
        /// <returns>是否成功关闭</returns>
        public bool GetAndCloseTopWindow(List<int> excludeLayers, List<UIWindow> excludeWindows)
        {
            UIWindow lastOne = null;
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                var window = m_uiStack[i];
                if (!excludeLayers.Contains(window.WindowLayer) && !excludeWindows.Contains(window))
                {
                    lastOne = window;
                }
            }

            if (lastOne == null)
            {
                return false;
            }

            if (lastOne.CanEscClose)
            {
                CloseWindow(lastOne);
                return true;
            }

            lastOne.OnEscCloseLastOneWindowCallback?.Invoke();
            return false;
        }

        #endregion

        #region ContainsWindow

        /// <summary>
        /// 检查是否包含指定窗口
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <returns>是否包含</returns>
        public bool ContainsWindow<T>() => ContainsWindow(typeof(T));

        /// <summary>
        /// 检查是否包含指定窗口
        /// </summary>
        /// <param name="type">窗口类型</param>
        /// <returns>是否包含</returns>
        public bool ContainsWindow(Type type) => IsContains(type.FullName);

        private bool IsContains(string windowFullName)
            => m_uiFullNameMap.TryGetValue(windowFullName, out UIWindow window);

        private bool IsContains(uint windowID) => m_uiMap.TryGetValue(windowID, out UIWindow window);

        #endregion

        #region ShowWindowAsync

        /// <summary>
        /// 异步显示窗口
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="userDatas">用户数据</param>
        public void ShowWindowAsync<T>(params System.Object[] userDatas) where T : UIWindow, new()
            => ShowWindowImp<T>(true, userDatas);

        /// <summary>
        /// 异步显示窗口（通过类型）
        /// </summary>
        /// <param name="type">窗口类型</param>
        /// <param name="userDatas">用户数据</param>
        public void ShowWindowAsync(Type type, params System.Object[] userDatas)
            => ShowWindowImp(type, true, userDatas);

        /// <summary>
        /// 同步显示窗口
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="userDatas">用户数据</param>
        public void ShowWindow<T>(params System.Object[] userDatas) where T : UIWindow, new()
            => ShowWindowImp<T>(false, userDatas);

        /// <summary>
        /// 同步显示窗口（通过类型）
        /// </summary>
        /// <param name="type">窗口类型</param>
        /// <param name="userDatas">用户数据</param>
        public void ShowWindow(Type type, params System.Object[] userDatas)
            => ShowWindowImp(type, false, userDatas);

        private void ShowWindowImp<T>(bool isAsync, params System.Object[] userDatas) where T : UIWindow, new()
        {
            Type type = typeof(T);
            string windowName = type.FullName;

            if (!TryGetWindow(windowName, out UIWindow window, userDatas))
            {
                window = InternalCreateWindow<T>();
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
            }
        }

        private void ShowWindowImp(Type type, bool isAsync, params System.Object[] userDatas)
        {
            string windowName = type.FullName;

            if (!TryGetWindow(windowName, out UIWindow window, userDatas))
            {
                window = InternalCreateWindow(type);
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
            }
        }

        private void ShowWindowImp(UIWindow uiWindow, bool isAsync, params System.Object[] userDatas)
        {
            if (!TryGetWindow(uiWindow.WindowID, out UIWindow window, userDatas))
            {
                window = InternalCreateWindow(uiWindow);
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
            }
        }

        /// <summary>
        /// 异步显示窗口并等待加载完成
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="userDatas">用户数据</param>
        /// <returns>窗口实例</returns>
        public async UniTask<T> ShowWindowAsyncAwait<T>(params System.Object[] userDatas) where T : UIWindow, new()
            => await ShowWindowAwaitImp<T>(true, userDatas);

        private async UniTask<T> ShowWindowAwaitImp<T>(bool isAsync, params System.Object[] userDatas) where T : UIWindow, new()
        {
            Type type = typeof(T);
            string windowName = type.FullName;

            if (TryGetWindow(windowName, out var window, userDatas))
            {
                return window as T;
            }
            else
            {
                window = InternalCreateWindow<T>();
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
                float time = 0f;

                while (!window.IsLoadDone) // 观察是否需要加上 && !window.IsDestroyed
                {
                    time += Time.deltaTime;

                    if (time > 60f)
                    {
                        DLogger.Warning($"窗口 {windowName} 加载超时(60s)");
                        break;
                    }
                    await UniTask.Yield();
                }
                return window as T;
            }
        }

        /// <summary>
        /// 异步显示窗口并等待加载完成（通过窗口实例）
        /// </summary>
        /// <param name="uiWindow">窗口实例</param>
        /// <param name="userDatas">用户数据</param>
        /// <returns>窗口实例</returns>
        public async UniTask<UIWindow> ShowWindowAsyncAwait(UIWindow uiWindow, params System.Object[] userDatas)
            => await ShowWindowAwaitImp(uiWindow, true, userDatas);

        private async UniTask<UIWindow> ShowWindowAwaitImp(UIWindow uiWindow, bool isAsync, params System.Object[] userDatas)
        {
            if (TryGetWindow(uiWindow.WindowID, out var window, userDatas))
            {
                return window;
            }
            else
            {
                window = InternalCreateWindow(uiWindow);
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
                float time = 0f;

                while (!window.IsLoadDone)
                {
                    time += Time.deltaTime;

                    if (time > 60f)
                    {
                        DLogger.Warning($"窗口 {window.WindowFullName} 加载超时(60s)");
                        break;
                    }
                    await UniTask.Yield();
                }
                return window;
            }
        }

        #endregion

        #region CreateWindow

        private UIWindow InternalCreateWindow<T>() where T : UIWindow, new()
        {
            Type type = typeof(T);
            UIWindow window = new T();
            if (window == null)
            {
                throw new DGameException($"窗口 {type.Name} 窗口实例化失败");
            }
            window.Initialize(type.FullName, type.Name);
            return window;
        }

        private UIWindow InternalCreateWindow(Type type)
        {
            UIWindow window = Activator.CreateInstance(type) as UIWindow;
            if (window == null)
            {
                throw new DGameException($"窗口 {type.Name} 窗口实例化失败");
            }
            window.Initialize(type.FullName, type.Name);
            return window;
        }

        private UIWindow InternalCreateWindow(UIWindow window)
        {
            if (window == null)
            {
                throw new DGameException($"窗口实例化失败");
            }
            window.Initialize(window.GetType().FullName, window.GetType().Name);
            return window;
        }

        #endregion

        #region GetWindow

        private bool TryGetWindow<T>(out UIWindow window, params System.Object[] userDatas) where T : UIWindow, new()
            => TryGetWindow(typeof(T).FullName, out window, userDatas);

        private bool TryGetWindow(string windowName, out UIWindow window, params System.Object[] userDatas)
        {
            window = null;
            if (IsContains(windowName))
            {
                window = GetWindow(windowName);
                Pop(window);
                Push(window);
                window.TryInvokePrepareCallback(OnWindowPrepare, userDatas);
                return true;
            }

            return false;
        }

        private bool TryGetWindow(uint windowID, out UIWindow window, params System.Object[] userDatas)
        {
            window = null;
            if (IsContains(windowID))
            {
                window = GetWindow(windowID);
                Pop(window);
                Push(window);
                window.TryInvokePrepareCallback(OnWindowPrepare, userDatas);
                return true;
            }

            return false;
        }

        public UIWindow GetWindow<T>() where T : UIWindow, new() => GetWindow(typeof(T).FullName);

        private UIWindow GetWindow(string windowName) => m_uiFullNameMap.GetValueOrDefault(windowName);

        private UIWindow GetWindow(uint windowID) => m_uiMap.GetValueOrDefault(windowID);

        /// <summary>
        /// 获取窗口并等待加载完成
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>窗口实例</returns>
        public async UniTask<T> GetWindowAsyncAwait<T>(CancellationToken cancellationToken = default) where T : UIWindow
        {
            string windowName = typeof(T).FullName;
            var window = GetWindow(windowName);

            if (window == null)
            {
                return null;
            }

            var result = window as T;
            if (result == null)
            {
                return null;
            }

            if (result.IsLoadDone)
            {
                return result;
            }

            float time = 0f;

            while (!result.IsLoadDone)
            {
                time += Time.deltaTime;

                if (time > 60f)
                {
                    DLogger.Warning($"窗口 {window.WindowFullName} 加载超时(60s)");
                    break;
                }
                await UniTask.Yield(cancellationToken);
            }
            return result;
        }

        /// <summary>
        /// 异步获取窗口并通过回调返回
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="callback">回调函数</param>
        public void GetWindowAsync<T>(Action<T> callback) where T : UIWindow
        {
            string windowName = typeof(T).FullName;
            var window = GetWindow(windowName);
            if (window == null)
            {
                return;
            }

            var result = window as T;
            if (result == null)
            {
                return;
            }

            GetWindowAsyncImp(callback).Forget();

            async UniTaskVoid GetWindowAsyncImp(Action<T> ctx)
            {
                float time = 0f;

                while (!result.IsLoadDone)
                {
                    time += Time.deltaTime;

                    if (time > 60f)
                    {
                        break;
                    }
                    await UniTask.Yield();
                }
                ctx?.Invoke(result);
            }
        }

        #endregion

        #region CloseWindow

        /// <summary>
        /// 关闭指定窗口
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        public void CloseWindow<T>() where T : UIWindow => CloseWindow(typeof(T));

        private void CloseWindow(Type type)
        {
            string windowName = type.FullName;
            UIWindow window = GetWindow(windowName);
            CloseWindow(window);
        }

        private void HideWindow<T>() where T : UIWindow => HideWindow(typeof(T));

        private void HideWindow(Type type)
        {
            string windowName = type.FullName;
            UIWindow window = GetWindow(windowName);
            HideWindow(window);
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        /// <param name="window">窗口实例</param>
        public void HideWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }

            PopQueueNextWindow(window);
            if (window.HideTimeToClose <= 0)
            {
                CloseWindow(window);
                return;
            }
            window.CancelHideToCloseTimer();
            window.Show(false);
            window.IsHide = true;
            window.HideTimer = GameModule.GameTimerModule.CreateOnceGameTimer(window.HideTimeToClose, _ =>
            {
                CloseWindow(window);
            });

            if (window.FullScreen)
            {
                OnSetWindowVisible();
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="window">窗口实例</param>
        public void CloseWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }
            PopQueueNextWindow(window);
            window.Destroy();
            Pop(window);
            OnSortWindowSortingOrder(window.WindowLayer);
            OnSetWindowVisible();
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public void CloseAllWindows()
        {
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                UIWindow window = m_uiStack[i];
                window?.Destroy();
            }
            m_uiStack.Clear();
            m_uiMap.Clear();
            m_uiFullNameMap.Clear();
        }

        /// <summary>
        /// 关闭除指定窗口外的所有窗口
        /// </summary>
        /// <param name="withOutWindows">保留的窗口列表</param>
        public void CloseAllWindowsWithOut(List<UIWindow> withOutWindows)
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (!withOutWindows.Contains(window))
                {
                    window.Destroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowID);
                    m_uiFullNameMap.Remove(window.WindowFullName);
                }
            }
        }

        /// <summary>
        /// 关闭除指定窗口外的所有窗口
        /// </summary>
        /// <param name="withOutWindows">保留的窗口</param>
        public void CloseAllWindowsWithOut(UIWindow withOutWindows)
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (window != withOutWindows)
                {
                    window.Destroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowID);
                    m_uiFullNameMap.Remove(window.WindowFullName);
                }
            }
        }

        /// <summary>
        /// 关闭除指定类型窗口外的所有窗口
        /// </summary>
        /// <typeparam name="T">保留的窗口类型</typeparam>
        public void CloseAllWindowsWithOut<T>() where T : UIWindow
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (window.GetType() != typeof(T))
                {
                    window.Destroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowID);
                    m_uiFullNameMap.Remove(window.WindowFullName);
                }
            }
        }

        #endregion

        /// <summary>
        /// 检查是否有窗口正在加载
        /// </summary>
        /// <returns>是否有窗口正在加载</returns>
        public bool IsAnyLoading()
        {
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                var window = m_uiStack[i];

                if (!window.IsLoadDone)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnWindowPrepare(UIWindow window)
        {
            window.InternalCreate();
            window.InternalRefresh();
            OnSortWindowSortingOrder(window.WindowLayer);
            OnSetWindowVisible();
        }

        private void OnSetWindowVisible()
        {
            bool isHideNext = false;

            // 隐藏所有层级低于最上级全屏显示窗口的窗口
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (!isHideNext)
                {
                    if (window.IsHide)
                    {
                        continue;
                    }

                    window.Show();

                    if (window.IsPrepared && window.FullScreen)
                    {
                        isHideNext = true;
                    }
                }
                else
                {
                    window.Show(false);
                }
            }
        }

        private void OnSortWindowSortingOrder(int layer)
        {
            // 重新排序所有的窗口层级
            int sortingOrder = layer * DEFAULT_LAYER_DEEP;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack[i].WindowLayer == layer)
                {
                    m_uiStack[i].SortingOrder = sortingOrder;
                    sortingOrder += EACH_WINDOW_DEEP;
                }
            }
        }

        private void Pop(UIWindow window)
        {
            m_uiStack.Remove(window);
            m_uiFullNameMap.Remove(window.WindowFullName);
            m_uiMap.Remove(window.WindowID);
        }

        private void Push(UIWindow window)
        {
            if (IsContains(window.WindowID))
            {
                throw new DGameException($"UI窗口 {window.WindowFullName} 已存在");
            }
            int insertIndex = -1;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (window.WindowLayer == m_uiStack[i].WindowLayer)
                {
                    insertIndex = i + 1;
                }
            }

            if (insertIndex == -1)
            {
                for (int i = 0; i < m_uiStack.Count; i++)
                {
                    if (window.WindowLayer > m_uiStack[i].WindowLayer)
                    {
                        insertIndex = i + 1;
                    }
                }
            }

            if (insertIndex == -1)
            {
                insertIndex = 0;
            }
            m_uiStack.Insert(insertIndex, window);
            m_uiFullNameMap[window.WindowFullName] = window;
            m_uiMap[window.WindowID] = window;
        }
    }
}