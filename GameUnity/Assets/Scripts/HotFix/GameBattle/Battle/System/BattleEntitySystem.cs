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
        public static IRenderUnit CreateRenderUnit(this BattleEntity self, LogicUnit logicUnit)
            => self.GetRenderUnitFactory().Create(logicUnit);
        
        public static void SetRenderUnitFactory(this BattleEntity self, IRenderUnitFactory renderUnitFactory)
            => self.RenderUnitFactory = renderUnitFactory;

        public static IRenderUnitFactory GetRenderUnitFactory(this BattleEntity self)
            => self.RenderUnitFactory ?? (self.RenderUnitFactory = self.AddComponent<NullRenderUnitFactoryComponent>());
    }
}