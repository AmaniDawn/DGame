using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件销毁系统。
    /// </summary>
    public sealed class UnitDisplayComponentDestroySystem : DestroySystem<UnitDisplayComponent>
    {
        /// <summary>
        /// 销毁单位显示组件。
        /// </summary>
        /// <param name="self">单位显示组件实例。</param>
        protected override void Destroy(UnitDisplayComponent self)
        {
            self.Destroy();
        }
    }
}