using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 摄像机管理组件销毁系统。
    /// </summary>
    public sealed class CameraMgrComponentDestroySystem : DestroySystem<CameraMgrComponent>
    {
        /// <summary>
        /// 销毁摄像机管理组件。
        /// </summary>
        /// <param name="self">摄像机管理组件实例。</param>
        protected override void Destroy(CameraMgrComponent self)
        {
            self.Clear();
        }
    }
    
    /// <summary>
    /// 摄像机管理组件扩展方法。
    /// </summary>
    public static class CameraMgrComponentSystem
    {
    }
}