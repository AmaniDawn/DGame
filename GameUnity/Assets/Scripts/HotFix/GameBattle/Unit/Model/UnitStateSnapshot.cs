using DGame;

namespace GameBattle
{
    public struct UnitStateSnapshot
    {
        public ulong UnitID { get; set; }
        public UnitType UnitType { get; set; }
        public UnitState UnitState { get; set; }

        public FixedPointVector3 Position { get; set; }
        public FixedPointQuaternion Rotation { get; set; }
        public FixedPointVector3 MoveForward { get; set; }

        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Mp { get; set; }
        public int MaxMp { get; set; }
    }
}