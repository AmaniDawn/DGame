using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 渲染层单位销毁触发系统。
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        protected override void Destroy(RenderUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 渲染层单位扩展系统入口。
    /// </summary>
    public static class RenderUnitSystem
    {
    }
}