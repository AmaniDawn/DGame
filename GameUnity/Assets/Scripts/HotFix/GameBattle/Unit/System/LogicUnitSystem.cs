using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑层单位销毁触发系统。
    /// </summary>
    public sealed class LogicUnitDestroySystem : DestroySystem<LogicUnit>
    {
        protected override void Destroy(LogicUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 逻辑层单位扩展系统入口。
    /// </summary>
    public static class LogicUnitSystem
    {
        public static void SetUnitState(this LogicUnit self, UnitState state)
        {
            if (self.UnitState == state)
            {
                return;
            }

            var curState = self.UnitState;
            self.UnitState = state;
            self.OnUnitStateChange(curState, state);
        }
        
        /// <summary>
        /// 设置单位本地缩放。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="scale">目标缩放值。</param>
        public static void SetScale(this LogicUnit self, FixedPointVector3 scale) 
            => self.transform.localScale = scale;

        /// <summary>
        /// 以统一倍率设置单位本地缩放。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="scale">统一缩放倍率。</param>
        public static void SetScale(this LogicUnit self, FixedPoint64 scale) 
            => self.transform.localScale = new FixedPointVector3(scale, scale, scale);
        
        /// <summary>
        /// 设置面朝方向。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value">目标位置</param>
        public static void SetForward(this LogicUnit self, FixedPointVector3 value)
        {
            var dir = value;
            dir.y = 0;
            if (dir.IsNearlyZero())
            {
                return;
            }
            var normalized = dir.normalized;
            self.transform.rotation = FixedPointQuaternion.LookRotation(normalized);
            self.MoveForward = normalized;
            self.MarkForward(normalized);
        }
        
        /// <summary>
        /// 立即朝向目标位置。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="targetPos">目标位置。</param>
        public static void LookAt(this LogicUnit self, FixedPointVector3 targetPos)
        {
            var dir = targetPos - self.transform.position;
            dir.y = 0;
            if (dir.IsNearlyZero())
            {
                return;
            }

            var normalized = dir.normalized;
            self.transform.rotation = FixedPointQuaternion.LookRotation(normalized);
            self.MoveForward = normalized;
            self.MarkForward(normalized);
        }

        /// <summary>
        /// 以固定角速度平滑转向目标位置。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="targetPos">目标位置。</param>
        /// <param name="maxDegreesPerTick">每个逻辑帧允许旋转的最大角度。</param>
        public static void TurnTowards(this LogicUnit self, FixedPointVector3 targetPos, FixedPoint64 maxDegreesPerTick)
        {
            var dir = targetPos - self.transform.position;
            dir.y = 0;
            if (dir.IsNearlyZero())
            {
                return;
            }

            var normalized = dir.normalized;
            var targetRotation = FixedPointQuaternion.LookRotation(normalized);
            self.transform.rotation = FixedPointQuaternion.RotateTowards
            (
                self.transform.rotation,
                targetRotation,
                maxDegreesPerTick
            );
            self.MoveForward = normalized;
            self.MarkForward(normalized);
        }
        
        /// <summary>
        /// 判断两个逻辑单位是否表示同一个运行时实例。
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other">待比较的逻辑单位。</param>
        /// <returns>是同一个有效运行时实例时返回 <see langword="true"/>。</returns>
        public static bool IsSameUnit(this LogicUnit self, LogicUnit other)
            => other != null && self.RuntimeId != 0 && other.RuntimeId != 0 && self.RuntimeId == other.RuntimeId;
    }
}
