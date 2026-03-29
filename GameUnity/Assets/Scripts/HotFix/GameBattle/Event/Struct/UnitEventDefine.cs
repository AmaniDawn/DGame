namespace GameBattle
{
    public readonly struct UnitDamagedEvent : IUnitEvent
    {
        public readonly ulong UnitID;
        public readonly ulong CasterUnitId;
        public readonly int DamageValue;
        
        public UnitDamagedEvent(ulong unitId, ulong casterUnitId, int damageValue)
        {
            UnitID = unitId;
            CasterUnitId = casterUnitId;
            DamageValue = damageValue;
        }
    }
}