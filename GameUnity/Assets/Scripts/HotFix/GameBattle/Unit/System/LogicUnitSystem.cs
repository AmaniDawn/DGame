using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑层单位销毁触发系统。
    /// </summary>
    public sealed class LogicUnitDestroySystem : DestroySystem<LogicUnit>
    {
        protected override void Destroy(LogicUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 逻辑层单位扩展系统入口。
    /// </summary>
    public static class LogicUnitSystem
    {
    }
}
