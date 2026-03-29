using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class UnitStateSyncComponentDestroySystem : DestroySystem<UnitStateSyncComponent>
    {
        protected override void Destroy(UnitStateSyncComponent self)
        {
            self.Clear();
        }
    }
    
    public static class UnitStateSyncComponentSystem
    {
        public static void MarkPosition(this LogicUnit self, FixedPointVector3 pos)
        {
            self.transform.position = pos;
            var snapshot = self.StateSync.Snapshot;
            snapshot.Position = pos;
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Transform);
        }

        public static void MarkHp(this LogicUnit self, int hp)
        {
            var snapshot = self.StateSync.Snapshot;
            snapshot.Hp = hp;
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Attr);
        }
    }
}