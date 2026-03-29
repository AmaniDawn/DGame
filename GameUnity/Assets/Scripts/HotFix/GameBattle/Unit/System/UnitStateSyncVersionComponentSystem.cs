using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 单位状态同步版本组件销毁系统。
    /// </summary>
    public sealed class UnitStateSyncVersionComponentDestroySystem : DestroySystem<UnitStateSyncVersionComponent>
    {
        /// <summary>
        /// 销毁单位状态同步版本组件。
        /// </summary>
        /// <param name="self">单位状态同步版本组件实例。</param>
        protected override void Destroy(UnitStateSyncVersionComponent self)
        {
            self.Clear();
        }
    }
    
    /// <summary>
    /// 单位状态同步版本扩展方法。
    /// </summary>
    public static class UnitStateSyncVersionComponentSystem
    {
    }
}