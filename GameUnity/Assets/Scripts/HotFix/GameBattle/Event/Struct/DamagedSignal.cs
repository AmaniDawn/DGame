namespace GameBattle
{
    public readonly struct DamagedSignal : ISignal
    {
        public readonly ulong UnitID;
        public readonly ulong CasterUnitId;
        public readonly int DamageValue;
        
        public DamagedSignal(ulong unitId, ulong casterUnitId, int damageValue)
        {
            UnitID = unitId;
            CasterUnitId = casterUnitId;
            DamageValue = damageValue;
        }
    }
}