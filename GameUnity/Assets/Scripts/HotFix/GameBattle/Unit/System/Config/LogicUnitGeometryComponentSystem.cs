using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位运行时几何组件销毁系统。
    /// </summary>
    public sealed class LogicUnitGeometryComponentDestroySystem : DestroySystem<LogicUnitGeometryComponent>
    {
        /// <summary>
        /// 销毁逻辑单位运行时几何组件。
        /// </summary>
        protected override void Destroy(LogicUnitGeometryComponent self)
        {
            self.Clear();
        }
    }

    /// <summary>
    /// 逻辑单位运行时几何组件扩展方法。
    /// </summary>
    public static class LogicUnitGeometryComponentSystem
    {
        /// <summary>
        /// 使用几何数据初始化单位运行时配置。
        /// </summary>
        public static void Init(this LogicUnitGeometryComponent self, LogicUnit owner, LogicUnitGeometryData geometryData,
            FixedPointVector3 spawnScale)
        {
            if (self == null)
            {
                return;
            }

            self.Owner = owner;
            self.ApplyBaseGeometry(geometryData);
            self.SetSpawnScale(spawnScale);
        }

        /// <summary>
        /// 应用一份基础几何数据。
        /// </summary>
        public static void ApplyBaseGeometry(this LogicUnitGeometryComponent self, LogicUnitGeometryData geometryData)
        {
            if (self == null)
            {
                return;
            }

            if (geometryData == null)
            {
                self.Clear();
                return;
            }

            self.CollisionShapeType = geometryData.CollisionShapeType;
            self.CapsuleRadius = geometryData.CapsuleRadius;
            self.CapsuleHeight = geometryData.CapsuleHeight;
            self.AabbHalfExtents = geometryData.AabbHalfExtents;
            self.BaseModelScale = NormalizeScaleVector(geometryData.ModelScale);
            self.FireOffset = geometryData.FireOffset;
            self.EnableCollision = geometryData.EnableCollision;
            self.UseAabbOverlap = geometryData.UseAabbOverlap;
            self.RefreshRuntimeConfig();
        }

        /// <summary>
        /// 设置出生缩放并刷新运行时派生配置。
        /// </summary>
        public static void SetSpawnScale(this LogicUnitGeometryComponent self, FixedPointVector3 spawnScale)
        {
            if (self == null)
            {
                return;
            }

            self.SpawnScale = NormalizeScaleVector(spawnScale);
            self.RefreshRuntimeConfig();
        }

        /// <summary>
        /// 刷新运行时派生配置。
        /// </summary>
        public static void RefreshRuntimeConfig(this LogicUnitGeometryComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.RuntimeModelScale = ScaleVector(self.BaseModelScale, self.SpawnScale);

            var horizontalScale = Max(self.RuntimeModelScale.x, self.RuntimeModelScale.z);
            self.RuntimeCapsuleRadius = self.CapsuleRadius * horizontalScale;
            self.RuntimeCapsuleHeight = self.CapsuleHeight * self.RuntimeModelScale.y;
            self.RuntimeAabbHalfExtents = ScaleVector(self.AabbHalfExtents, self.RuntimeModelScale);
            self.RuntimeFireOffset = ScaleVector(self.FireOffset, self.RuntimeModelScale);
        }

        /// <summary>
        /// 获取逻辑单位当前碰撞半径。
        /// </summary>
        public static FixedPoint64 GetCapsuleRadius(this LogicUnit self)
            => self?.Geometry?.RuntimeCapsuleRadius ?? FixedPoint64.Zero;

        /// <summary>
        /// 获取逻辑单位当前碰撞高度。
        /// </summary>
        public static FixedPoint64 GetCapsuleHeight(this LogicUnit self)
            => self?.Geometry?.RuntimeCapsuleHeight ?? FixedPoint64.Zero;

        /// <summary>
        /// 清空运行时配置组件。
        /// </summary>
        public static void Clear(this LogicUnitGeometryComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.Owner = null;
            self.CollisionShapeType = UnitCollisionShapeType.AABB;
            self.CapsuleRadius = FixedPoint64.Zero;
            self.CapsuleHeight = FixedPoint64.Zero;
            self.AabbHalfExtents = FixedPointVector3.zero;
            self.BaseModelScale = FixedPointVector3.one;
            self.SpawnScale = FixedPointVector3.one;
            self.RuntimeModelScale = FixedPointVector3.one;
            self.FireOffset = FixedPointVector3.zero;
            self.EnableCollision = true;
            self.UseAabbOverlap = false;
            self.RuntimeCapsuleRadius = FixedPoint64.Zero;
            self.RuntimeCapsuleHeight = FixedPoint64.Zero;
            self.RuntimeAabbHalfExtents = FixedPointVector3.zero;
            self.RuntimeFireOffset = FixedPointVector3.zero;
        }

        private static FixedPointVector3 NormalizeScaleVector(FixedPointVector3 scale)
        {
            if (scale.IsNearlyZero())
            {
                return FixedPointVector3.one;
            }

            if (scale.x <= FixedPoint64.Zero)
            {
                scale.x = FixedPoint64.One;
            }

            if (scale.y <= FixedPoint64.Zero)
            {
                scale.y = FixedPoint64.One;
            }

            if (scale.z <= FixedPoint64.Zero)
            {
                scale.z = FixedPoint64.One;
            }

            return scale;
        }

        private static FixedPointVector3 ScaleVector(FixedPointVector3 left, FixedPointVector3 right)
            => new(left.x * right.x, left.y * right.y, left.z * right.z);

        private static FixedPoint64 Max(FixedPoint64 left, FixedPoint64 right) => left >= right ? left : right;
    }
}
