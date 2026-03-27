using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑层对象销毁触发事件
    /// </summary>
    public sealed class LogicUnitDestroySystem : DestroySystem<LogicUnit>
    {
        protected override void Destroy(LogicUnit self)
        {
            self.Destroy();
        }
    }
    
    public sealed class LogicUnitSystem
    {
        
    }
}