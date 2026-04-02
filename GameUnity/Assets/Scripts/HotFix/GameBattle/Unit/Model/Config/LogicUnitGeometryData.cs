using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位运行时几何初始化数据。
    /// </summary>
    public sealed class LogicUnitGeometryData : MemoryObject
    {
        /// <summary>
        /// 碰撞形状类型。
        /// </summary>
        public UnitCollisionShapeType CollisionShapeType { get; set; }

        /// <summary>
        /// 胶囊体半径。
        /// </summary>
        public FixedPoint64 CapsuleRadius { get; set; }

        /// <summary>
        /// 胶囊体高度。
        /// </summary>
        public FixedPoint64 CapsuleHeight { get; set; }

        /// <summary>
        /// AABB 半尺寸。
        /// </summary>
        public FixedPointVector3 AabbHalfExtents { get; set; } = FixedPointVector3.zero;

        /// <summary>
        /// 基础模型缩放。
        /// </summary>
        public FixedPointVector3 ModelScale { get; set; } = FixedPointVector3.one;

        /// <summary>
        /// 发射点偏移。
        /// </summary>
        public FixedPointVector3 FireOffset { get; set; } = FixedPointVector3.zero;

        /// <summary>
        /// 是否启用碰撞。
        /// </summary>
        public bool EnableCollision { get; set; } = true;

        /// <summary>
        /// 是否使用 AABB 重叠判定。
        /// </summary>
        public bool UseAabbOverlap { get; set; }

        /// <summary>
        /// 从对象池创建一份单位配置数据。
        /// </summary>
        public static LogicUnitGeometryData Create() => Spawn<LogicUnitGeometryData>();

        /// <summary>
        /// 清空全部配置数据。
        /// </summary>
        public void Clear()
        {
            CollisionShapeType = UnitCollisionShapeType.AABB;
            CapsuleRadius = FixedPoint64.Zero;
            CapsuleHeight = FixedPoint64.Zero;
            AabbHalfExtents = FixedPointVector3.zero;
            ModelScale = FixedPointVector3.one;
            FireOffset = FixedPointVector3.zero;
            EnableCollision = true;
            UseAabbOverlap = false;
        }

        /// <summary>
        /// 复制另一份单位配置数据。
        /// </summary>
        public void CopyFrom(LogicUnitGeometryData other)
        {
            if (other == null)
            {
                Clear();
                return;
            }

            CollisionShapeType = other.CollisionShapeType;
            CapsuleRadius = other.CapsuleRadius;
            CapsuleHeight = other.CapsuleHeight;
            AabbHalfExtents = other.AabbHalfExtents;
            ModelScale = other.ModelScale;
            FireOffset = other.FireOffset;
            EnableCollision = other.EnableCollision;
            UseAabbOverlap = other.UseAabbOverlap;
        }

        public override void OnRelease()
        {
            Clear();
        }
    }
}