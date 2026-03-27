using DGame;
using Fantasy.Entitas;
// ReSharper disable InconsistentNaming

namespace GameBattle
{
    /// <summary>
    /// 逻辑层对象
    /// </summary>
    public abstract class LogicUnit : Entity
    {
        public string UnitName { get; protected set; } = string.Empty;
        
        internal FixedPoint64 m_waitDestroyTime = FixedPoint64.Zero;
        
        public ulong UnitID { get; internal set; }
        
        public UnitType UnitType { get; set; }

        public UnitState State { get; private set; }
        
        public abstract uint ConfigID { get; }
        
        /// <summary>
        /// 渲染层对象
        /// </summary>
        public IRenderUnit RenderUnit { get; set; }

        public FPTransform transform { get; set; }
        
        public FixedPointVector3 position { get => transform.position; set => transform.position = value; }

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
        /// 移动方向
        /// </summary>
        public FixedPointVector3 MoveForward { get; internal set; } = FixedPointVector3.zero;
        
        public FixedPointVector3 TranslatePos { get; internal set; }
        
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

        public void SetScale(FixedPointVector3 scale) => transform.localScale = scale;
        
        public void SetScale(FixedPoint64 scale) => transform.localScale = new FixedPointVector3(scale, scale, scale);
        
        /// <summary>
        /// 瞬时朝向
        /// </summary>
        /// <param name="targetPos">目标位置</param>
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
        /// 平滑转向
        /// </summary>
        /// <param name="targetPos">目标位置</param>
        /// <param name="maxDegreesPerTick">一帧最多旋转多少度</param>
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
