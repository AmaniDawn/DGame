using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位可重算属性数据。
    /// </summary>
    public sealed class LogicUnitAttrData : MemoryObject
    {
        /// <summary>
        /// 攻击力。
        /// </summary>
        public int Atk { get; set; }

        /// <summary>
        /// 最大生命值。
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 移动速度。
        /// </summary>
        public FixedPoint64 MoveSpeed { get; set; }

        /// <summary>
        /// 从内存池创建一份属性数据。
        /// </summary>
        public static LogicUnitAttrData Create() => Spawn<LogicUnitAttrData>();

        /// <summary>
        /// 清空全部属性值。
        /// </summary>
        public void Clear()
        {
            Atk = 0;
            MaxHp = 0;
            MoveSpeed = FixedPoint64.Zero;
        }

        /// <summary>
        /// 复制另一份属性数据。
        /// </summary>
        public void CopyFrom(LogicUnitAttrData other)
        {
            if (other == null)
            {
                Clear();
                return;
            }

            Atk = other.Atk;
            MaxHp = other.MaxHp;
            MoveSpeed = other.MoveSpeed;
        }

        public override void OnRelease()
        {
            Clear();
        }
    }
}