using Fantasy.Entitas;

namespace GameBattle
{
    public sealed class UnitStateSyncComponent : Entity
    {
        public UnitStateSnapshot Snapshot { get; set; }
        public uint StateVersion { get; private set; }
        public uint AttrVersion { get; private set; }

        public void MarkDirty(UnitStateDirtyFlags flags)
        {
            if ((flags & UnitStateDirtyFlags.State) != 0)
            {
                StateVersion++;
            }
            
            if ((flags & UnitStateDirtyFlags.Attr) != 0)
            {
                AttrVersion++;
            }
        }
        
        public void Clear()
        {
            Snapshot = default;
            StateVersion = 0;
            AttrVersion = 0;
        }
    }
}