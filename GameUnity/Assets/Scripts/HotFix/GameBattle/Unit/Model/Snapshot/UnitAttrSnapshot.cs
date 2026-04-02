using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位的属性同步快照。
    /// <remarks>
    /// 这里承载的是对外同步/表现直接需要读取的属性投影，
    /// 而不是内部用于结算的完整属性模型。
    /// </remarks>
    /// </summary>
    public struct UnitAttrSnapshot
    {
        /// <summary>
        /// 当前攻击力。
        /// </summary>
        public int Atk { get; set; }

        /// <summary>
        /// 当前生命值。
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 当前最大生命值。
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 当前移动速度。
        /// </summary>
        public FixedPoint64 MoveSpeed { get; set; }
    }
}