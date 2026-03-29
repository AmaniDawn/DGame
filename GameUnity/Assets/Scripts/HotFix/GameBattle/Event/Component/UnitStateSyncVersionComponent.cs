using Fantasy.Entitas;

namespace GameBattle
{
    public class UnitStateSyncVersionComponent : Entity
    {
        public uint LastStateVersion { get; set; }

        public void Clear()
        {
            LastStateVersion = 0;
        }
    }
}