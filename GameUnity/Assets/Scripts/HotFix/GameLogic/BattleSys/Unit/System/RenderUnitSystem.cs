using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 渲染层对象销毁触发事件
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        protected override void Destroy(RenderUnit self)
        {
            self.Destroy();
        }
    }
    
    public sealed class RenderUnitSystem
    {
        
    }
}