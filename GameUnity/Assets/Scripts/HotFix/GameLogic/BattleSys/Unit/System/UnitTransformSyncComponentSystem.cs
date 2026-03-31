using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 单位变换同步组件销毁系统。
    /// </summary>
    public sealed class UnitTransformSyncComponentDestroySystem : DestroySystem<UnitTransformSyncComponent>
    {
        protected override void Destroy(UnitTransformSyncComponent self)
        {
            self?.Clear();
        }
    }
}