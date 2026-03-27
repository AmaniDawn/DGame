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
        /// <summary>
        /// 单位名称。
        /// </summary>
        public string UnitName { get; protected set; } = string.Empty;

        internal FixedPoint64 m_waitDestroyTime = FixedPoint64.Zero;

        /// <summary>
        /// 单位唯一标识。
        /// </summary>
        public ulong UnitID { get; internal set; }

        /// <summary>
        /// 单位类型。
        /// </summary>
        public UnitType UnitType { get; set; }

        /// <summary>
        /// 当前单位状态。
        /// </summary>
        public UnitState State { get; private set; }

        /// <summary>
        /// 单位配置 ID。
        /// </summary>
        public abstract uint ConfigID { get; }

        /// <summary>
        /// 绑定的渲染层单位。
        /// </summary>
        public IRenderUnit RenderUnit { get; set; }

        /// <summary>
        /// 单位使用的定点变换组件。
        /// </summary>
        public FPTransform transform { get; set; }

        /// <summary>
        /// 单位当前位置。
        /// </summary>
        public FixedPointVector3 position { get => transform.position; set => transform.position = value; }

        /// <summary>
        /// 单位面朝方向。
        /// </summary>
        public FixedPointVector3 Forward
        {
            get => transform.forward;
            set
            {
                var dir = value;
                dir.y = 0;
                if (dir.IsNearlyZero())
                {
                    return;
                }

                transform.rotation = FixedPointQuaternion.LookRotation(dir.normalized);
            }
        }

        /// <summary>
        /// 当前移动方向。
        /// </summary>
        public FixedPointVector3 MoveForward { get; internal set; } = FixedPointVector3.forward;

        /// <summary>
        /// 逻辑位移偏移量。
        /// </summary>
        public FixedPointVector3 TranslatePos { get; internal set; }

        /// <summary>
        /// 单位是否已销毁。
        /// </summary>
        public bool IsDestroyed { get; set; }

        internal void SetUnitState(UnitState state)
        {
            if (State == state)
            {
                return;
            }

            var curState = State;
            State = state;
            OnUnitStateChange(curState, state);
        }

        private void OnUnitStateChange(UnitState oldState, UnitState newState)
        {
        }

        /// <summary>
        /// 设置单位本地缩放。
        /// </summary>
        /// <param name="scale">目标缩放值。</param>
        public void SetScale(FixedPointVector3 scale) => transform.localScale = scale;

        /// <summary>
        /// 以统一倍率设置单位本地缩放。
        /// </summary>
        /// <param name="scale">统一缩放倍率。</param>
        public void SetScale(FixedPoint64 scale) => transform.localScale = new FixedPointVector3(scale, scale, scale);

        /// <summary>
        /// 立即朝向目标位置。
        /// </summary>
        /// <param name="targetPos">目标位置。</param>
        public void LookAt(FixedPointVector3 targetPos)
        {
            var dir = targetPos - transform.position;
            dir.y = 0;
            if (dir.IsNearlyZero())
            {
                return;
            }

            transform.rotation = FixedPointQuaternion.LookRotation(dir.normalized);
        }

        /// <summary>
        /// 以固定角速度平滑转向目标位置。
        /// </summary>
        /// <param name="targetPos">目标位置。</param>
        /// <param name="maxDegreesPerTick">每个逻辑帧允许旋转的最大角度。</param>
        public void TurnTowards(FixedPointVector3 targetPos, FixedPoint64 maxDegreesPerTick)
        {
            var dir = targetPos - transform.position;
            dir.y = 0;
            if (dir.IsNearlyZero())
            {
                return;
            }

            var targetRotation = FixedPointQuaternion.LookRotation(dir.normalized);
            transform.rotation = FixedPointQuaternion.RotateTowards
            (
                transform.rotation,
                targetRotation,
                maxDegreesPerTick
            );
        }

        /// <summary>
        /// 判断两个逻辑单位是否表示同一个运行时实例。
        /// </summary>
        /// <param name="other">待比较的逻辑单位。</param>
        /// <returns>是同一个有效运行时实例时返回 <see langword="true"/>。</returns>
        public bool IsSameUnit(LogicUnit other)
            => other != null && RuntimeId != 0 && other.RuntimeId != 0 && RuntimeId == other.RuntimeId;

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
        }

        protected void DestroyRenderUnit()
        {
            if (RenderUnit is Entity renderEntity && !renderEntity.IsDisposed)
            {
                renderEntity.Dispose();
            }
        }
    }
}
