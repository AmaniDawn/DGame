using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fantasy.Entitas;
using GameBattle;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameLogic
{
    /// <summary>
    /// 渲染显示初始化状态。
    /// </summary>
    public enum RenderDisplayInitState
    {
        /// <summary>
        /// 尚未开始初始化。
        /// </summary>
        None = 0,

        /// <summary>
        /// 显示初始化中。
        /// </summary>
        Loading = 1,

        /// <summary>
        /// 显示已初始化完成。
        /// </summary>
        Ready = 2,

        /// <summary>
        /// 显示初始化失败。
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 显示初始化被取消。
        /// </summary>
        Canceled = 4,
    }

    /// <summary>
    /// 渲染层单位基类。
    /// </summary>
    public abstract class RenderUnit : Entity, IRenderUnit
    {
        #region Component

        public UnitEventHubComponent UnitEventHub { get; private set; }

        public SubscriptionScopeComponent Subscriptions { get; private set; }

        public UnitStateSyncVersionComponent StateSyncVersion { get; private set; }

        public UnitDisplayComponent UnitDisplay { get; private set; }

        public UnitTransformSyncComponent TransformSync { get; private set; }

        public UnitAttributeDisplayComponent AttributeDisplay { get; private set; }

        #endregion

        #region Property

        /// <summary>
        /// 单位名称。
        /// </summary>
        public string UnitName { get; private set; }

        /// <summary>
        /// 单位唯一标识。
        /// </summary>
        public ulong UnitID { get; private set; }

        /// <summary>
        /// 单位类型。
        /// </summary>
        public UnitType UnitType { get; private set; }

        /// <summary>
        /// 绑定的逻辑层单位。
        /// </summary>
        public LogicUnit LogicUnit { get; private set; }

        /// <summary>
        /// 渲染层单位是否已销毁。
        /// </summary>
        public bool IsDestroyed { get; protected set; }

        /// <summary>
        /// 延迟销毁计时。
        /// </summary>
        public float WaitDestroyTime { get; set; } = 0f;

        /// <summary>
        /// 渲染对象根节点。
        /// </summary>
        public GameObject UnitRoot { get; protected set; }

        /// <summary>
        /// 渲染对象根变换。
        /// </summary>
        public Transform UnitRootTransform { get; protected set; }

        /// <summary>
        /// 当前世界坐标位置。
        /// </summary>
        public Vector3 Position => UnitRootTransform != null ? UnitRootTransform.position : Vector3.zero;

        /// <summary>
        /// 当前世界朝向。
        /// </summary>
        public Vector3 Forward => UnitRootTransform != null ? UnitRootTransform.forward : Vector3.forward;

        /// <summary>
        /// 当前世界旋转。
        /// </summary>
        public Quaternion Rotation => UnitRootTransform != null ? UnitRootTransform.rotation : Quaternion.identity;

        /// <summary>
        /// 渲染对象当前是否可见。
        /// </summary>
        public bool Visible { get; protected set; } = true;

        /// <summary>
        /// 指示当前渲染单位是否需要绑定逻辑层单位。
        /// </summary>
        /// <returns>默认返回 <see langword="true"/>。</returns>
        public virtual bool NeedBindLogicUnit() => true;

        /// <summary>
        /// 指示当前单位是否属于 Boss。
        /// </summary>
        /// <returns>默认返回 <see langword="false"/>。</returns>
        public virtual bool IsBoss() => false;

        /// <summary>
        /// 当前显示初始化状态。
        /// </summary>
        public RenderDisplayInitState DisplayInitState { get; private set; }

        /// <summary>
        /// 当前显示是否已经初始化完成。
        /// </summary>
        public bool IsDisplayReady => DisplayInitState == RenderDisplayInitState.Ready;

        private CancellationTokenSource m_initModelCancelTokenSource;

        #endregion

        #region 初始化相关

        /// <summary>
        /// 初始化渲染层单位并绑定对应的逻辑层单位。
        /// </summary>
        /// <param name="logicUnit">要绑定的逻辑层单位。</param>
        /// <returns>初始化成功时返回 <see langword="true"/>。</returns>
        public bool Init(LogicUnit logicUnit)
        {
            CancelInitModel();
            m_initModelCancelTokenSource = new CancellationTokenSource();
            UnitID = logicUnit.UnitID;
            LogicUnit = logicUnit;
            UnitType = logicUnit.UnitType;
            UnitName = logicUnit.UnitName;
            Visible = true;
            IsDestroyed = false;
            DisplayInitState = RenderDisplayInitState.None;
            InitModel(CreateGameObject());
            UnitEventHub = AddComponent<UnitEventHubComponent>();
            StateSyncVersion = AddComponent<UnitStateSyncVersionComponent>();
            Subscriptions = AddComponent<SubscriptionScopeComponent>();
            TransformSync = AddComponent<UnitTransformSyncComponent>();
            TransformSync.Init(this);
            TransformSync.SyncInit();
            AttributeDisplay = AddComponent<UnitAttributeDisplayComponent>();
            AttributeDisplay.Init(this);
            UnitDisplay = AddComponent<UnitDisplayComponent>();
            if (!OnInit(logicUnit))
            {
                return false;
            }

            if (!AfterInit())
            {
                return false;
            }

            RegisterRuntimeEvents();
            BattleSystem.Instance.RenderUnits.Register(this);
            StartDisplayInit();
            return true;
        }

        private bool AfterInit()
        {
            if (UnitRoot != null)
            {
                UnitRoot.name = GetGameObjectName();
            }

            return true;
        }

        protected virtual bool OnInit(LogicUnit logicUnit)
        {
            Visible = true;
            return true;
        }

        protected virtual GameObject CreateGameObject()
        {
            var go = new GameObject();
            var parent = BattleSystem.IsValid ? BattleSystem.Instance.ViewRoots?.UnitRoot() : null;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }
        
        public void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake() { }

        /// <summary>
        /// 注册渲染单位运行时事件。
        /// </summary>
        protected virtual void RegisterRuntimeEvents() { }

        /// <summary>
        /// 显示初始化完成后的异步扩展点。
        /// </summary>
        /// <param name="ct">初始化取消令牌。</param>
        protected virtual UniTask OnDisplayReadyAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>
        /// 显示初始化失败后的扩展点。
        /// </summary>
        protected virtual void OnDisplayInitFailed() { }

        /// <summary>
        /// 显示初始化被取消后的扩展点。
        /// </summary>
        protected virtual void OnDisplayInitCanceled() { }

        /// <summary>
        /// 启动显示初始化流程。
        /// </summary>
        private void StartDisplayInit()
        {
            var ct = m_initModelCancelTokenSource != null
                ? m_initModelCancelTokenSource.Token
                : CancellationToken.None;
            InitDisplayAsync(ct).Forget();
        }

        /// <summary>
        /// 异步初始化显示层资源。
        /// </summary>
        /// <param name="ct">初始化取消令牌。</param>
        private async UniTaskVoid InitDisplayAsync(CancellationToken ct)
        {
            if (UnitDisplay == null)
            {
                DisplayInitState = RenderDisplayInitState.Failed;
                OnDisplayInitFailed();
                return;
            }

            DisplayInitState = RenderDisplayInitState.Loading;

            try
            {
                if (!await UnitDisplay.InitAsync(this, ct))
                {
                    DisplayInitState = RenderDisplayInitState.Failed;
                    OnDisplayInitFailed();
                    return;
                }

                await OnDisplayReadyAsync(ct);

                if (IsDisposed || IsDestroyed)
                {
                    return;
                }

                DisplayInitState = RenderDisplayInitState.Ready;
            }
            catch (OperationCanceledException)
            {
                if (IsDisposed || IsDestroyed)
                {
                    return;
                }

                DisplayInitState = RenderDisplayInitState.Canceled;
                UnitDisplay?.Clear();
                OnDisplayInitCanceled();
            }
            catch (Exception e)
            {
                if (IsDisposed || IsDestroyed)
                {
                    return;
                }

                Debug.LogError($"RenderUnit display init failed: {e}");
                DisplayInitState = RenderDisplayInitState.Failed;
                UnitDisplay?.Clear();
                OnDisplayInitFailed();
            }
        }

        #endregion

        #region Destroy 相关

        protected virtual void OnDestroy() { }

        private void DestroyAllGameTimer() { }

        /// <summary>
        /// 销毁渲染层单位及其关联对象。
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (BattleSystem.IsValid)
            {
                BattleSystem.Instance.RenderUnits.Unregister(this);
            }
            
            OnDestroy();
            CancelInitModel();
            DestroyGameObject();
            DestroyAllGameTimer();
            TransformSync?.Clear();
            AttributeDisplay?.Clear();
            IsDestroyed = true;
            UnitID = 0;
            LogicUnit = null;
            UnitType = UnitType.None;
            Visible = true;
            WaitDestroyTime = 0;
            DisplayInitState = RenderDisplayInitState.None;
        }

        private void CancelInitModel()
        {
            m_initModelCancelTokenSource?.Cancel();
            m_initModelCancelTokenSource?.Dispose();
            m_initModelCancelTokenSource = null;
        }

        protected virtual void DestroyGameObject()
        {
            if (UnitRoot != null)
            {
                UnityEngine.Object.Destroy(UnitRoot);
                UnitRoot = null;
                UnitRootTransform = null;
            }
        }

        #endregion

        #region Sync 相关

        public void UpdateUnit()
        {
            if (LogicUnit == null || LogicUnit.StateSync == null)
            {
                return;
            }

            var stateSync = LogicUnit.StateSync;

            #region Transform

            if (StateSyncVersion.LastTransformVersion != stateSync.TransformVersion)
            {
                StateSyncVersion.LastTransformVersion = stateSync.TransformVersion;
                TransformSync?.NotifyLogicTransformChanged();
            }
            TransformSync?.Sync(Time.deltaTime);

            #endregion

            #region State

            if (StateSyncVersion.LastStateVersion != stateSync.StateVersion)
            {
                StateSyncVersion.LastStateVersion = stateSync.StateVersion;
                SyncState();
            }

            #endregion
            
            #region Attr

            if (StateSyncVersion.LastAttrVersion != stateSync.AttrVersion)
            {
                StateSyncVersion.LastAttrVersion = stateSync.AttrVersion;
                AttributeDisplay?.Sync(LogicUnit.StateSync.Snapshot);
                SyncAttr();
            }

            #endregion

            OnUpdateUnit();
        }

        protected virtual void SyncState() { }
        
        protected virtual void SyncAttr() { }

        protected virtual void OnUpdateUnit() { }

        #endregion

        #region 渲染相关

        public abstract int GetModelID();

        public uint GetConfigId() => LogicUnit?.ConfigID ?? 0;

        public ulong GetOwnerUnitId() => LogicUnit?.OwnerUnitID ?? 0;

        protected void InitModel(GameObject root)
        {
            UnitRoot = root;
            UnitRootTransform = UnitRoot.transform;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取在编辑器中展示的游戏对象名称。
        /// </summary>
        /// <returns>编辑器下返回包含单位信息的名称，运行时返回通用名称。</returns>
        public string GetGameObjectName()
        {
            if (DGame.Utility.PlatformUtil.IsEditorPlatform())
            {
                return $"[{UnitID}][{LogicUnit.UnitType}][{UnitName}]";
            }

            return "RenderUnit";
        }

        #endregion
    }
}