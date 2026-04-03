using DGame;
using Fantasy.Entitas;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameBattle
{
    /// <summary>
    /// 逻辑层单位基类。
    /// </summary>
    public abstract class LogicUnit : Entity
    {
        #region Components

        public BattleContextComponent BattleContext { get; private set; }

        public UnitEventHubComponent UnitEventHub { get; private set; }

        public UnitStateSyncComponent StateSync { get; private set; }

        public LogicUnitAttrComponent Attr { get; private set; }

        /// <summary>
        /// 单位运行时几何配置组件。
        /// </summary>
        public LogicUnitGeometryComponent Geometry { get; private set; }

        #endregion

        /// <summary>
        /// 单位名称。
        /// </summary>
        public string UnitName { get; internal set; } = string.Empty;

        internal FixedPoint64 m_waitDestroyTime = FixedPoint64.Zero;

        /// <summary>
        /// 单位唯一标识。
        /// </summary>
        public ulong UnitID { get; internal set; }

        /// <summary>
        /// 所属单位唯一标识。
        /// </summary>
        public ulong OwnerUnitID { get; internal set; }

        /// <summary>
        /// 是否存在出生位姿数据。
        /// </summary>
        public bool HasBornPose { get; internal set; }

        /// <summary>
        /// 出生位置。
        /// </summary>
        public FixedPointVector3 BornPosition { get; internal set; } = FixedPointVector3.zero;

        /// <summary>
        /// 出生朝向。
        /// </summary>
        public FixedPointVector3 BornForward { get; internal set; } = FixedPointVector3.forward;

        /// <summary>
        /// 出生缩放。
        /// </summary>
        public FixedPointVector3 BornScale { get; internal set; } = FixedPointVector3.one;

        /// <summary>
        /// 单位类型。
        /// </summary>
        public UnitType UnitType { get; internal set; }

        /// <summary>
        /// 当前单位状态。
        /// </summary>
        public UnitState UnitState { get; internal set; }

        /// <summary>
        /// 单位配置 ID。
        /// </summary>
        public virtual uint ConfigID { get; protected set; }

        /// <summary>
        /// 绑定的渲染层单位。
        /// </summary>
        public IRenderUnit RenderUnit { get; internal set; }

        /// <summary>
        /// 单位使用的定点变换组件。
        /// </summary>
        public FPTransform transform { get; internal set; }

        /// <summary>
        /// 单位当前位置。
        /// </summary>
        public FixedPointVector3 position { get => transform.position; internal set => transform.position = value; }

        /// <summary>
        /// 单位面朝方向。
        /// </summary>
        public FixedPointVector3 Forward => transform.forward;

        /// <summary>
        /// 当前移动方向。
        /// </summary>
        public FixedPointVector3 MoveForward { get; internal set; } = FixedPointVector3.forward;

        /// <summary>
        /// 逻辑位移偏移量。
        /// </summary>
        public FixedPointVector3 TranslatePos { get; internal set; } = FixedPointVector3.zero;

        /// <summary>
        /// 单位是否已销毁。
        /// </summary>
        public bool IsDestroyed { get; internal set; }

        /// <summary>
        /// 单位是否已死亡。
        /// </summary>
        public bool IsDied => IsDestroyed || UnitState == UnitState.Die;

        public LogicUnitCreateData CreateData { get; private set; }

        public LogicUnitGeometryData GeometryData { get; private set; }

        #region 初始化

        internal bool ApplyCreateData(LogicUnitCreateData createData, LogicUnitGeometryData geometryData)
        {
            if (createData == null || geometryData == null || createData.UnitType == UnitType.None)
            {
                return false;
            }
            CreateData = createData;
            GeometryData = geometryData;
            UnitType = createData.UnitType;
            UnitName = createData.UnitName;
            OwnerUnitID = createData.OwnerUnitId;
            ConfigID = createData.ConfigId;
            HasBornPose = createData.HasBornPose;
            BornPosition = createData.BornPosition;
            BornForward = createData.BornForward;
            BornScale = createData.BornScale;

            Attr ??= AddComponent<LogicUnitAttrComponent>();
            Attr.Owner = this;
            Attr.InitBaseAttr(createData.BaseAttr);

            Geometry ??= AddComponent<LogicUnitGeometryComponent>();
            Geometry.Init(this, geometryData, createData.BornScale);

            if (HasBornPose && !BornForward.IsNearlyZero())
            {
                MoveForward = BornForward.normalized;
            }

            return OnApplyCreateData(createData);
        }

        internal bool Init(BattleContextComponent battleContextComponent)
        {
            BattleContext = battleContextComponent;
            StateSync = AddComponent<UnitStateSyncComponent>();
            UnitEventHub = AddComponent<UnitEventHubComponent>();
            if (Attr != null)
            {
                Attr.Owner = this;
            }
            if (Geometry != null)
            {
                Geometry.Owner = this;
            }

            if (!OnInit())
            {
                return false;
            }

            var initSucceeded = false;
            try
            {
                ApplyBornPoseToTransform();
                SyncInitialSnapshot();
                Attr?.SyncSnapshot();
                CreateRenderUnit(battleContextComponent);
                if (!AfterInit())
                {
                    return false;
                }

                initSucceeded = true;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (!initSucceeded)
                {
                    DestroyRenderUnit();
                }
            }
        }

        protected virtual bool AfterInit()
        {
            return true;
        }

        protected virtual bool OnApplyCreateData(LogicUnitCreateData createData)
        {
            return true;
        }

        protected virtual bool OnInit()
        {
            return true;
        }

        private void CreateRenderUnit(BattleContextComponent battleContextComponent)
        {
            RenderUnit = battleContextComponent.CreateRenderUnit(this);
        }

        private void SyncInitialSnapshot()
        {
            if (StateSync == null)
            {
                return;
            }

            var snapshot = StateSync.Snapshot;
            snapshot.UnitID = UnitID;
            snapshot.UnitType = UnitType;
            snapshot.UnitState = UnitState;
            snapshot.MoveForward = MoveForward;
            if (transform != null)
            {
                snapshot.Position = transform.position;
                snapshot.Rotation = transform.rotation;
            }

            StateSync.Snapshot = snapshot;
        }

        private void ApplyBornPoseToTransform()
        {
            if (transform == null)
            {
                return;
            }

            transform.localScale = Geometry?.RuntimeModelScale ?? BornScale;

            if (!HasBornPose)
            {
                return;
            }

            transform.position = BornPosition;

            var forward = BornForward;
            forward.y = 0;
            if (forward.IsNearlyZero())
            {
                return;
            }

            var normalized = forward.normalized;
            transform.rotation = FixedPointQuaternion.LookRotation(normalized);
            MoveForward = normalized;
        }

        #endregion

        /// <summary>
        /// 推进单位固定帧逻辑。
        /// </summary>
        /// <param name="deltaTime">当前固定逻辑帧时间增量。</param>
        internal void FixedUpdate(FixedPoint64 deltaTime)
        {
            if (IsDisposed || IsDestroyed)
            {
                return;
            }
            OnFixedUpdate(deltaTime);
        }

        /// <summary>
        /// 子类固定帧逻辑扩展点。
        /// </summary>
        /// <param name="deltaTime">当前固定逻辑帧时间增量。</param>
        protected virtual void OnFixedUpdate(FixedPoint64 deltaTime) { }

        internal virtual void OnUnitStateChange(UnitState oldState, UnitState newState) { }

        protected virtual void OnDestroy() { }

        internal void Destroy()
        {
            if (IsDisposed)
            {
                return;
            }

            var battleContext = BattleContext;
            battleContext?.DetachLogicUnit(this);
            OnDestroy();
            IsDestroyed = true;
            DestroyRenderUnit();

            UnitName = string.Empty;
            UnitID = 0;
            OwnerUnitID = 0;
            HasBornPose = false;
            BornPosition = FixedPointVector3.zero;
            BornForward = FixedPointVector3.forward;
            BornScale = FixedPointVector3.one;
            UnitType = UnitType.None;
            UnitState = UnitState.None;
            ConfigID = 0;
            transform = null;
            MoveForward = FixedPointVector3.forward;
            TranslatePos = FixedPointVector3.zero;
            m_waitDestroyTime = FixedPoint64.Zero;
            Attr = null;
            Geometry = null;
            BattleContext = null;
        }

        protected virtual void DestroyRenderUnit()
        {
            if (RenderUnit is Entity { IsDisposed: false } renderEntity)
            {
                renderEntity.Dispose();
            }
            RenderUnit = null;
        }
    }
}