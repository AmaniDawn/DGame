using Fantasy;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class BattleEntityDestroySystem : DestroySystem<BattleEntity>
    {
        protected override void Destroy(BattleEntity self)
        {
            self.Destroy();
        }
    }
    
    public static class BattleEntitySystem
    {
    }
}