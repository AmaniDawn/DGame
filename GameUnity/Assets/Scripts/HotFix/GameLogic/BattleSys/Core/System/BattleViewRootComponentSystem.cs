using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 战斗视图根节点组件销毁系统。
    /// </summary>
    public sealed class BattleViewRootComponentDestroySystem : DestroySystem<BattleViewRootComponent>
    {
        /// <summary>
        /// 销毁战斗视图根节点组件。
        /// </summary>
        /// <param name="self">战斗视图根节点组件实例。</param>
        protected override void Destroy(BattleViewRootComponent self)
        {
            self?.Clear();
        }
    }

    /// <summary>
    /// 战斗视图根节点组件扩展方法。
    /// </summary>
    public static class BattleViewRootComponentSystem
    {
    }
}
