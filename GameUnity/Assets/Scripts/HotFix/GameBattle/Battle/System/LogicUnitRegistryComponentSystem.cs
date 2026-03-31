using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位注册表组件销毁系统。
    /// </summary>
    public sealed class LogicUnitRegistryComponentDestroySystem : DestroySystem<LogicUnitRegistryComponent>
    {
        protected override void Destroy(LogicUnitRegistryComponent self)
        {
            self.Clear();
        }
    }
}