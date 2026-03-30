using System.Threading;
using Fantasy.Entitas;
using GameBattle;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameLogic
{
    /// <summary>
    /// 渲染层单位基类。
    /// </summary>
    public abstract class RenderUnit : Entity, IRenderUnit
    {
        public UnitEventHubComponent UnitEventHub { get; private set; }

        public SubscriptionScopeComponent Subscriptions { get; private set; }
        
        public UnitStateSyncVersionComponent StateSyncVersion { get; private set; }
        
        public UnitDisplayComponent UnitDisplay { get; private set; }
        
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
        
        private CancellationTokenSource m_initModelCancelTokenSource;

        #region 初始化相关

        /// <summary>
        /// 初始化渲染层单位并绑定对应的逻辑层单位。
        /// </summary>
        /// <param name="logicUnit">要绑定的逻辑层单位。</param>
        /// <returns>初始化成功时返回 <see langword="true"/>。</returns>
        public bool Init(LogicUnit logicUnit)
        {
            m_initModelCancelTokenSource = new CancellationTokenSource();
            UnitID = logicUnit.UnitID;
            LogicUnit = logicUnit;
            UnitType = logicUnit.UnitType;
            UnitName = logicUnit.UnitName;
            Visible = true;
            InitModel(CreateGameObject());
            UnitEventHub = AddComponent<UnitEventHubComponent>();
            StateSyncVersion = AddComponent<UnitStateSyncVersionComponent>();
            Subscriptions = AddComponent<SubscriptionScopeComponent>();
            UnitDisplay = AddComponent<UnitDisplayComponent>();
            if (!OnInit(logicUnit))
            {
                return false;
            }

            UnitDisplay.InitAsync(m_initModelCancelTokenSource.Token).Forget();
            return AfterInit();
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
            return true;
        }

        protected virtual GameObject CreateGameObject()
        {
            var go = new GameObject();
            // go.transform.SetParent(BattleManager.Instance.TransHeroActorRoot);
            return go;
        }
        
        public void Awake()
        {
            OnAwake();
            RegisterEvent();
        }

        protected virtual void OnAwake() { }

        protected virtual void RegisterEvent() { }

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
            
            OnDestroy();
            CancelInitModel();
            DestroyGameObject();
            DestroyAllGameTimer();
            IsDestroyed = true;
            UnitID = 0;
            LogicUnit = null;
            UnitType = UnitType.None;
            Visible = true;
            WaitDestroyTime = 0;
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
                Object.Destroy(UnitRoot);
                UnitRoot = null;
                UnitRootTransform = null;
            }
        }

        #endregion

        #region Sync 相关

        public virtual void SyncFromLogic()
        {
            // 示例
            if (LogicUnit == null || LogicUnit.StateSync == null)
            {
                return;
            }
            var stateSync = LogicUnit.StateSync;
            
            if (StateSyncVersion.LastStateVersion != stateSync.StateVersion)
            {
                StateSyncVersion.LastStateVersion = stateSync.StateVersion;
                SyncState();
            }
            
            if (StateSyncVersion.LastAttrVersion != stateSync.AttrVersion)
            {
                StateSyncVersion.LastAttrVersion = stateSync.AttrVersion;
                SyncAttr();
            }
        }

        protected virtual void SyncState() { }
        
        protected virtual void SyncAttr() { }

        #endregion

        #region 渲染相关

        public abstract int GetModelID();

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