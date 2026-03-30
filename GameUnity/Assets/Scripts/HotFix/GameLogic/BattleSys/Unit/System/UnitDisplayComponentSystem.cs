using Fantasy.Entitas.Interface;

namespace GameLogic
{
    public sealed class UnitDisplayComponentDestroySystem : DestroySystem<UnitDisplayComponent>
    {
        protected override void Destroy(UnitDisplayComponent self)
        {
            self.Destroy();
        }
    }
}