using Fantasy;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 战斗上下文组件销毁系统。
    /// </summary>
    public sealed class BattleContextComponentDestroySystem : DestroySystem<BattleContextComponent>
    {
        /// <summary>
        /// 销毁战斗上下文组件。
        /// </summary>
        /// <param name="self">战斗上下文组件实例。</param>
        protected override void Destroy(BattleContextComponent self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 战斗上下文组件扩展方法系统。
    /// </summary>
    public static class BattleContextComponentSystem
    {
        /// <summary>
        /// 创建渲染单位。
        /// </summary>
        /// <param name="self">战斗上下文组件实例。</param>
        /// <param name="logicUnit">逻辑层单位。</param>
        /// <returns>创建的渲染层单位。</returns>
        public static IRenderUnit CreateRenderUnit(this BattleContextComponent self, LogicUnit logicUnit)
            => self.GetRenderUnitFactory().Create(logicUnit);

        /// <summary>
        /// 设置渲染单位工厂。
        /// </summary>
        /// <param name="self">战斗上下文组件实例。</param>
        /// <param name="renderUnitFactory">渲染单位工厂。</param>
        public static void SetRenderUnitFactory(this BattleContextComponent self, IRenderUnitFactory renderUnitFactory)
            => self.RenderUnitFactory = renderUnitFactory;

        /// <summary>
        /// 获取渲染单位工厂。
        /// <remarks>如果未设置则自动添加空渲染工厂组件作为兜底。</remarks>
        /// </summary>
        /// <param name="self">战斗上下文组件实例。</param>
        /// <returns>渲染单位工厂实例。</returns>
        public static IRenderUnitFactory GetRenderUnitFactory(this BattleContextComponent self)
            => self.RenderUnitFactory ?? (self.RenderUnitFactory = self.AddComponent<NullRenderUnitFactoryComponent>());
    }
}