using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 单位属性表现同步组件销毁系统。
    /// </summary>
    public sealed class UnitAttributeDisplayComponentDestroySystem : DestroySystem<UnitAttributeDisplayComponent>
    {
        /// <summary>
        /// 销毁单位属性表现同步组件。
        /// </summary>
        /// <param name="self">单位属性表现同步组件实例。</param>
        protected override void Destroy(UnitAttributeDisplayComponent self)
        {
            self?.Clear();
        }
    }
}