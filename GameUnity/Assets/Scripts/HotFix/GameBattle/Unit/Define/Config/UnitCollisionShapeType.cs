namespace GameBattle
{
    /// <summary>
    /// 逻辑单位碰撞形状类型。
    /// </summary>
    public enum UnitCollisionShapeType
    {
        /// <summary>
        /// 球形碰撞体。
        /// </summary>
        Sphere = 0,

        /// <summary>
        /// 轴对齐包围盒碰撞体。
        /// </summary>
        AABB = 1,

        /// <summary>
        /// 有向包围盒碰撞体。
        /// </summary>
        OBB = 2,

        /// <summary>
        /// 胶囊体碰撞体。
        /// </summary>
        Capsule = 3,

        /// <summary>
        /// 圆柱体碰撞体。
        /// </summary>
        Cylinder = 4,

        /// <summary>
        /// 轴对齐胶囊体碰撞体。
        /// </summary>
        AACapsule = 5,

        /// <summary>
        /// 网格碰撞体。
        /// </summary>
        Mesh = 6,

        /// <summary>
        /// 角色控制器碰撞体。
        /// </summary>
        CharacterController = 7,
    }
}