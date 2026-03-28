using Fantasy.Entitas.Interface;

namespace GameLogic
{
    public sealed class CameraMgrComponentDestroySystem : DestroySystem<CameraMgrComponent>
    {
        protected override void Destroy(CameraMgrComponent self)
        {
            self.Clear();
        }
    }
    
    public static class CameraMgrComponentSystem
    {
        
    }
}