using Fantasy.Entitas.Interface;

namespace GameLogic
{
    public sealed class UnitDisplayComponentAwakeSystem : AwakeSystem<UnitDisplayComponent>
    {
        protected override void Awake(UnitDisplayComponent self)
        {
            self.Awake();
        }
    }
    
    public sealed class UnitDisplayComponentDestroySystem : DestroySystem<UnitDisplayComponent>
    {
        protected override void Destroy(UnitDisplayComponent self)
        {
            self.Destroy();
        }
    }
}