using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class UnitStateSyncVersionComponentDestroySystem : DestroySystem<UnitStateSyncVersionComponent>
    {
        protected override void Destroy(UnitStateSyncVersionComponent self)
        {
            self.Clear();
        }
    }
    
    public static class UnitStateSyncVersionComponentSystem
    {
        
    }
}