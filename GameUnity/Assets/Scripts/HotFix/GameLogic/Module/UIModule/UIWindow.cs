using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public abstract class UIWindow : UIBase
    {
        #region Propreties

        private static uint s_nextWindowId = 0;
        private readonly string m_modelSpritePath = "ModelSprite";
        private SetUISafeFitHelper m_setUISafeFitHelper;
        private System.Action<UIWindow> m_prepareCallback;
        private Canvas m_canvas;
        private Canvas[] m_childCanvas;
        private GraphicRaycaster m_graphicRaycaster;
        private GraphicRaycaster[] m_childGraphicRaycasters;
        private bool m_isChildCanvasDirty;
        private const float NORMAL_TWEEN_POP_TIME = 0.3f;
        private const float NORMAL_MODEL_ALPHA = 0.85f;
        private float m_curModelAlpha;
        private float m_manualAlpha;
        private Image m_modelSprite;
        private UIButton m_modelCloseBtn;
        private bool m_isCreated;

        public Canvas Canvas => m_canvas;

        public GraphicRaycaster GraphicRaycaster => m_graphicRaycaster;

        public override UIType Type => UIType.Window;

        public uint WindowID { get; set; }

        private CanvasGroup m_canvasGroup;

        public CanvasGroup CanvasGroup
            => m_canvasGroup == null
                ? m_canvasGroup = DGame.Utility.UnityUtil.AddMonoBehaviour<CanvasGroup>(gameObject)
                : m_canvasGroup;

        private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

        private Transform m_transform;

        /// <summary>
        /// 窗口位置组件
        /// </summary>
        public override Transform transform => m_transform;

        /// <summary>
        /// 窗口矩阵位置组件
        /// </summary>
        public override RectTransform rectTransform => m_transform as RectTransform;

        /// <summary>
        /// 窗口实例化资源对象
        /// </summary>
        public override GameObject gameObject { get; protected set; }

        /// <summary>
        /// 窗口名称
        /// </summary>
        public string WindowFullName { get; private set; }

        protected virtual UILayer windowLayer => UILayer.UI;

        /// <summary>
        /// 窗口层级
        /// </summary>
        public int WindowLayer => (int)windowLayer;

        /// <summary>
        /// 资源定位地址
        /// </summary>
        public virtual string AssetLocation { get; private set; }

        /// <summary>
        /// 是否全屏窗口
        /// </summary>
        public virtual bool FullScreen => false;

        /// <summary>
        /// 是否是Resources资源 无需AB包加载
        /// </summary>
        public virtual bool FromResources => false;

        /// <summary>
        /// 隐藏窗口关闭时间
        /// </summary>
        public virtual int HideTimeToClose => 10;

        /// <summary>
        /// 隐藏窗口关闭时间计时器句柄
        /// </summary>
        public GameTimer HideTimer { get; set; }

        public int SortingOrder
        {
            get=> m_canvas != null ? m_canvas.sortingOrder : 0;
            set
            {
                if (m_canvas == null || m_canvas.sortingOrder == value)
                {
                    return;
                }

                var oldOrder = m_canvas.sortingOrder;

                if (m_isChildCanvasDirty)
                {
                    m_childCanvas = gameObject.GetComponentsInChildren<Canvas>(true);
                    m_isChildCanvasDirty = false;
                }

                //设置子类
                if (m_childCanvas != null && m_childCanvas.Length > 0)
                {
                    for (int i = 0; i < m_childCanvas.Length; i++)
                    {
                        var childCanvas = m_childCanvas[i];

                        if (childCanvas != m_canvas)
                        {
                            childCanvas.sortingOrder = value + (childCanvas.sortingOrder - oldOrder);
                        }
                    }
                }
                m_canvas.sortingOrder = value;

                // 虚函数
                if (Visible)
                {
                    _OnSortingOrderChange();
                }
                else
                {
                    m_isSortingOrderDirty = true;
                }
            }
        }

        protected override bool Visible
        {
            get => m_canvas != null && CanvasGroup?.alpha >= 1;
            set
            {
                if (m_canvas == null || !IsPrepared || IsDestroyed)
                {
                    return;
                }

                if (m_canvasGroup == null)
                {
                    m_canvasGroup = DGame.Utility.UnityUtil.AddMonoBehaviour<CanvasGroup>(gameObject);
                }

                int alpha = value ? 1 : 0;
                if (m_canvasGroup.alpha == alpha)
                {
                    return;
                }
                m_canvasGroup.alpha = alpha;
                // 关闭CanvasGroup上的射线检测
                m_canvasGroup.blocksRaycasts = value;
                Interactable = value;

                if (m_isCreated)
                {
                    return;
                }

                if (m_isSortingOrderDirty)
                {
                    _OnSortingOrderChange();
                }

                if (value)
                {
                    OnVisible();
                }
                else
                {
                    OnHidden();
                }
            }
        }

        public bool Interactable
        {
            get => m_graphicRaycaster != null && m_graphicRaycaster.enabled;
            set
            {
                if (m_graphicRaycaster == null)
                {
                    return;
                }
                m_graphicRaycaster.enabled = value;

                if (m_childGraphicRaycasters != null)
                {
                    for (int i = 0; i < m_childGraphicRaycasters.Length; i++)
                    {
                        m_childGraphicRaycasters[i].enabled = value;
                    }
                }
            }
        }

        /// <summary>
        /// 是否加载完成
        /// </summary>
        internal bool IsLoadDone = false;

        /// <summary>
        /// UI是否隐藏
        /// </summary>
        public bool IsHide { get; internal set; } = false;

        protected virtual bool NeedTweenPop => true;

        private bool m_isTweenPopping = false;

        /// <summary>
        /// 是否队列弹窗
        /// </summary>
        public bool IsInQueue { get; set; } = false;

        /// <summary>
        /// 是否可以通过返回关闭
        /// </summary>
        public virtual bool CanEscClose { get; private set; } = true;

        public System.Action OnEscCloseLastOneWindowCallback { get; private set; }

        #endregion

        public void Initialize(string windowName, string assetLocation)
        {
            WindowFullName = windowName;
            AssetLocation = assetLocation;
            AllocWindowId();
        }

        #region 刘海屏适配

        /// <summary>
        /// 移动设备屏幕适配
        /// </summary>
        /// <param name="fitRect">适配的RectTransform对象</param>
        /// <param name="liuHaiFit">是否开启刘海屏顶部适配</param>
        /// <param name="topSpacing">刘海屏顶部适配偏移高度</param>
        /// <param name="bottomFit">是否开启刘海屏底部适配</param>
        /// <param name="bottomSpacing">刘海屏底部适配偏移高度</param>
        public void SetUIFit(RectTransform fitRect, bool liuHaiFit = true, float topSpacing = 0, bool bottomFit = true, float bottomSpacing = 0)
        {
            if (m_setUISafeFitHelper == null)
            {
                m_setUISafeFitHelper = new SetUISafeFitHelper(fitRect, liuHaiFit, topSpacing, bottomFit, bottomSpacing);
            }
            m_setUISafeFitHelper?.SetUIFit();
        }

        /// <summary>
        /// rectTransform不受m_curRect适配影响
        /// </summary>
        /// <param name="rect"></param>
        public void SetUINotFit(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            m_setUISafeFitHelper?.SetUINotFit(rect);
        }

        /// <summary>
        /// 设置某一个节点不受指定RectTransform的影响
        /// </summary>
        /// <param name="rect">设置的RectTransform</param>
        /// <param name="refRect">依赖的RectTransform</param>
        public void SetUINotFit(RectTransform rect, RectTransform refRect)
        {
            if (rect == null || refRect == null)
            {
                return;
            }
            if (m_setUISafeFitHelper == null)
            {
                m_setUISafeFitHelper = new SetUISafeFitHelper();
            }
            m_setUISafeFitHelper?.SetUINotFit(rect, refRect);
        }

        #endregion

        public void AllocWindowId()
        {
            if (s_nextWindowId == 0)
            {
                s_nextWindowId++;
            }

            WindowID = s_nextWindowId++;
        }

        public void SetModelAlphaManually(float alpha)
        {
            m_manualAlpha = alpha;
        }

        protected virtual ModelType GetModelType()
        {
            if (FullScreen || WindowLayer == (int)UILayer.Top)
            {
                return ModelType.TransparentType;
            }

            return ModelType.NormalType;
        }

        public void SetEscCloseLastOneWindowCallback(System.Action callback)
            => OnEscCloseLastOneWindowCallback = callback;

        internal void TryInvokePrepareCallback(System.Action<UIWindow> prepareCallback, System.Object[] userData)
        {
            CancelHideToCloseTimer();
            base.m_userDatas = userData;

            if (IsPrepared)
            {
                prepareCallback?.Invoke(this);
            }
            else
            {
                m_prepareCallback = prepareCallback;
            }
        }

        internal async UniTaskVoid InternalLoad(string location, System.Action<UIWindow> prepareCallback, bool isAsync, System.Object[] userData)
        {
            m_prepareCallback = prepareCallback;
            m_userDatas = userData;

            if (!FromResources)
            {
                if (isAsync)
                {
                    var uiInstance = await UIModule.ResourceLoader.LoadGameObjectAsync(location, UIModule.UICanvas);
                    Handle_Completed(uiInstance);
                }
                else
                {
                    var uiInstance = UIModule.ResourceLoader.LoadGameObject(location, UIModule.UICanvas);
                    Handle_Completed(uiInstance);
                }
            }
            else
            {
                var uiInstance = Object.Instantiate(Resources.Load<GameObject>(location), UIModule.UICanvas);
                Handle_Completed(uiInstance);
            }
        }

        internal void InternalRefresh()
        {
            OnRefresh();
        }

        internal void InternalCreate()
        {
            if (IsDestroyed || m_isCreated)
            {
                return;
            }

            m_isCreated = true;
            ScriptGenerator();
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            SetModelState(GetModelType());
            if (NeedTweenPop && !FullScreen)
            {
                TweenPop();
            }
        }

        private void SetModelState(ModelType modelType)
        {
            m_curModelAlpha = NORMAL_MODEL_ALPHA;
            var canClose = false;
            switch (modelType)
            {
                case ModelType.NormalType:
                    break;

                case ModelType.TransparentType:
                    m_curModelAlpha = 0.01f;
                    break;

                case ModelType.NormalType75:
                    m_curModelAlpha = 0.75f;
                    break;

                case ModelType.UndertintHaveClose:
                    m_curModelAlpha = 0.4f;
                    canClose = true;
                    break;

                case ModelType.NormalHaveClose:
                    canClose = true;
                    break;

                case ModelType.TransparentHaveClose:
                    m_curModelAlpha = 0.01f;
                    canClose = true;
                    break;

                default:
                    m_curModelAlpha = 0f;
                    break;
            }

            m_curModelAlpha = m_manualAlpha > 0 ? m_manualAlpha : m_curModelAlpha;

            if (m_curModelAlpha <= 0)
            {
                return;
            }
            GameObject modelObj = UIModule.ResourceLoader.LoadGameObject(m_modelSpritePath, transform);

            if (modelObj != null)
            {
                modelObj.transform.SetAsFirstSibling();
                modelObj.transform.localScale = Vector3.one;
                modelObj.transform.localPosition = Vector3.zero;
                modelObj.name = m_modelSpritePath;
                if (canClose)
                {
                    m_modelCloseBtn = DGame.Utility.UnityUtil.AddMonoBehaviour<UIButton>(modelObj);
                    m_modelCloseBtn.onClick.AddListener(Close);
                }
                m_modelSprite = DGame.Utility.UnityUtil.AddMonoBehaviour<UIImage>(modelObj);
                m_modelSprite.color = new Color(0, 0, 0, m_curModelAlpha);
            }
        }

        private void TweenPop()
        {
            if (m_isTweenPopping || gameObject == null)
            {
                return;
            }
            m_isTweenPopping = true;
            transform.localScale = Vector3.one * 0.8f;
            transform.DOScale(Vector3.one, NORMAL_TWEEN_POP_TIME)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetAutoKill(true)
                .onComplete += OnTweenPopComplete;
        }

        private void OnTweenPopComplete()
        {
            m_isTweenPopping = false;
        }

        internal bool InternalUpdate()
        {
            if (!IsPrepared || !Visible || IsDestroyed)
            {
                return false;
            }

            List<UIWidget> listNextUpdateChild = null;

            if (ChildList != null && ChildList.Count > 0)
            {
                listNextUpdateChild = m_updateChildList;
                var updateListDirty = m_updateListDirty;
                List<UIWidget> childList = null;
                if (updateListDirty)
                {
                    if (listNextUpdateChild == null)
                    {
                        listNextUpdateChild = new List<UIWidget>();
                        m_updateChildList = listNextUpdateChild;
                    }
                    else
                    {
                        listNextUpdateChild.Clear();
                    }
                    childList = ChildList;
                }
                else
                {
                    childList = listNextUpdateChild;
                }

                for (int i = 0; i < childList.Count; i++)
                {
                    var uiWidget = childList[i];

                    if (uiWidget == null)
                    {
                        continue;
                    }

                    var needValid = uiWidget.InternalUpdate();

                    if (updateListDirty && needValid)
                    {
                        listNextUpdateChild.Add(uiWidget);
                    }
                }

                if (updateListDirty)
                {
                    m_updateListDirty = false;
                }
            }

            bool needUpdate = false;

            if (listNextUpdateChild == null || listNextUpdateChild.Count <= 0)
            {
                m_hasOverrideUpdate = true;
                OnUpdate();
                needUpdate = m_hasOverrideUpdate;
            }
            else
            {
                OnUpdate();
                needUpdate = true;
            }
            return needUpdate;
        }

        protected internal void Destroy()
        {
            m_isCreated = false;
            OnEscCloseLastOneWindowCallback = null;
            if (IsDestroyed)
            {
                return;
            }

            transform?.DOKill();
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            RemoveAllUIEvents();

            for (int i = ChildList.Count - 1; i >= 0; i--)
            {
                var uiChild = ChildList[i];
                uiChild?.Destroy();
            }

            m_prepareCallback = null;
            OnDestroy();

            if (gameObject != null)
            {
                Object.Destroy(gameObject);
                gameObject = null;
            }

            IsDestroyed = true;

            CancelHideToCloseTimer();
        }

        private void Handle_Completed(GameObject windowGo)
        {
            if (windowGo == null)
            {
                return;
            }

            IsLoadDone = true;
            if (IsDestroyed)
            {
                Object.Destroy(windowGo);
                return;
            }

            windowGo.name = GetType().Name;
            gameObject = windowGo;
            m_transform = gameObject?.transform;
            windowGo.transform.localPosition = Vector3.zero;

            // UIDebugBehaviour.AddUIDebugBehaviour(windowGo);

            m_canvas = windowGo.GetComponent<Canvas>();
            if (m_canvas == null)
            {
                throw new DGameException($"在UI窗口 {WindowFullName} 没有找到 {nameof(Canvas)}");
            }
            m_canvas.overrideSorting = true;
            m_canvas.sortingOrder = 0;
            m_canvas.sortingLayerName = "Default";
            m_graphicRaycaster = windowGo.GetComponent<GraphicRaycaster>();
            m_childCanvas = windowGo.GetComponentsInChildren<Canvas>(true);
            m_childGraphicRaycasters = windowGo.GetComponentsInChildren<GraphicRaycaster>(true);

            m_isChildCanvasDirty = false;

            IsPrepared = true;
            IsDestroyed = false;
            m_prepareCallback?.Invoke(this);
        }

        internal void CancelHideToCloseTimer()
        {
            IsHide = false;

            if (!GameTimer.IsNull(HideTimer))
            {
                ModuleSystem.GetModule<IGameTimerModule>().DestroyGameTimer(HideTimer);
            }
            HideTimer = null;
        }

        public void Show(bool isVisible = true)
        {
            Visible = isVisible;
        }

        public void MakeChildCanvasDirty()
        {
            m_isChildCanvasDirty = true;
        }

        public virtual void Hide()
        {
            UIModule.Instance.HideWindow(this);
        }

        public virtual void Close()
        {
            UIModule.Instance.CloseWindow(this);
        }
    }
}