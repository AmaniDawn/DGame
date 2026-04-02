using DGame;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位运行时几何组件。
    /// <remarks>
    /// 负责承载碰撞体、体型、缩放和挂点偏移等几何配置，
    /// 不承载战斗属性与运行时血量状态。
    /// </remarks>
    /// </summary>
    public sealed class LogicUnitGeometryComponent : Entity
    {
        /// <summary>
        /// 当前配置组件所属的逻辑单位。
        /// </summary>
        internal LogicUnit Owner { get; set; } // 当前配置组件所属的逻辑单位

        /// <summary>
        /// 碰撞形状类型。
        /// </summary>
        public UnitCollisionShapeType CollisionShapeType { get; internal set; }

        /// <summary>
        /// 基础胶囊体半径。
        /// </summary>
        public FixedPoint64 CapsuleRadius { get; internal set; }

        /// <summary>
        /// 基础胶囊体高度。
        /// </summary>
        public FixedPoint64 CapsuleHeight { get; internal set; }

        /// <summary>
        /// 基础 AABB 半尺寸。
        /// </summary>
        public FixedPointVector3 AabbHalfExtents { get; internal set; } = FixedPointVector3.zero;

        /// <summary>
        /// 基础模型缩放。
        /// </summary>
        public FixedPointVector3 BaseModelScale { get; internal set; } = FixedPointVector3.one;

        /// <summary>
        /// 出生缩放。
        /// </summary>
        public FixedPointVector3 SpawnScale { get; internal set; } = FixedPointVector3.one;

        /// <summary>
        /// 当前生效的模型缩放。
        /// </summary>
        public FixedPointVector3 RuntimeModelScale { get; internal set; } = FixedPointVector3.one;

        /// <summary>
        /// 基础发射点偏移。
        /// </summary>
        public FixedPointVector3 FireOffset { get; internal set; } = FixedPointVector3.zero;

        /// <summary>
        /// 是否启用碰撞。
        /// </summary>
        public bool EnableCollision { get; internal set; } = true;

        /// <summary>
        /// 是否使用 AABB 重叠判定。
        /// </summary>
        public bool UseAabbOverlap { get; internal set; }

        /// <summary>
        /// 当前生效的胶囊体半径。
        /// </summary>
        public FixedPoint64 RuntimeCapsuleRadius { get; internal set; }

        /// <summary>
        /// 当前生效的胶囊体高度。
        /// </summary>
        public FixedPoint64 RuntimeCapsuleHeight { get; internal set; }

        /// <summary>
        /// 当前生效的 AABB 半尺寸。
        /// </summary>
        public FixedPointVector3 RuntimeAabbHalfExtents { get; internal set; } = FixedPointVector3.zero;

        /// <summary>
        /// 当前生效的发射点偏移。
        /// </summary>
        public FixedPointVector3 RuntimeFireOffset { get; internal set; } = FixedPointVector3.zero;
    }
}