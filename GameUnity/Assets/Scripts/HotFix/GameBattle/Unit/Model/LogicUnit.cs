using DGame;
using Fantasy.Entitas;
// ReSharper disable InconsistentNaming

namespace GameBattle
{
    /// <summary>
    /// 逻辑层单位基类。
    /// </summary>
    public abstract class LogicUnit : Entity
    {
        public BattleContextComponent BattleContext { get; private set; }
        
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

        #region 初始化

        public void Init(BattleContextComponent battleContextComponent)
        {
            BattleContext = battleContextComponent;
            CreateRenderUnit(this, battleContextComponent);
        }

        public void CreateRenderUnit(LogicUnit logicUnit, BattleContextComponent battleContextComponent)
        {
            RenderUnit = battleContextComponent.CreateRenderUnit(logicUnit);
        }

        #endregion

        internal virtual void OnUnitStateChange(UnitState oldState, UnitState newState)
        {
        }

        protected virtual void OnDestroy() { }

        internal void Destroy()
        {
            if (IsDisposed)
            {
                return;
            }

            OnDestroy();
            IsDestroyed = true;
            DestroyRenderUnit();

            UnitName = string.Empty;
            UnitID = 0;
            UnitType = UnitType.None;
            UnitState = UnitState.None;
            ConfigID = 0;
            transform = null;
            MoveForward = FixedPointVector3.forward;
            TranslatePos = FixedPointVector3.zero;
            m_waitDestroyTime = FixedPoint64.Zero;
            BattleContext = null;
        }

        protected virtual void DestroyRenderUnit()
        {
            if (RenderUnit is Entity { IsDisposed: false } renderEntity)
            {
                renderEntity.Dispose();
                RenderUnit = null;
            }
        }
    }
}