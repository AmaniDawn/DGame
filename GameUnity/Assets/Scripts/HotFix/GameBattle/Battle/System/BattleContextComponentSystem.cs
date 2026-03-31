using System;
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
            => self?.GetRenderUnitFactory()?.Create(logicUnit);

        /// <summary>
        /// 创建指定类型的逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="unitType">单位类型枚举。</param>
        /// <returns>创建的逻辑单位实例。</returns>
        public static LogicUnit CreateLogicUnit(this BattleContextComponent self, UnitType unitType)
            => self?.LogicUnitFactoryComponent?.Create(unitType);

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

        /// <summary>
        /// 注册逻辑单位到当前战斗上下文索引。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待注册的逻辑单位。</param>
        public static void RegisterLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
            => self?.LogicUnitRegistry?.Register(logicUnit);

        /// <summary>
        /// 从当前战斗上下文索引中移除逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        public static void UnregisterLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
            => self?.LogicUnitRegistry?.Unregister(logicUnit);

        /// <summary>
        /// 按 Entity.Id 查询逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="entityId">逻辑单位实体 ID。</param>
        /// <param name="logicUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public static bool TryGetLogicUnit(this BattleContextComponent self, long entityId, out LogicUnit logicUnit)
        {
            if (self == null || self.LogicUnitRegistry == null)
            {
                logicUnit = null;
                return false;
            }

            return self.LogicUnitRegistry.TryGet(entityId, out logicUnit);
        }

        /// <summary>
        /// 遍历当前战斗中的全部逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="visitor">遍历回调。</param>
        public static void ForEachLogicUnit(this BattleContextComponent self, Action<LogicUnit> visitor)
            => self?.LogicUnitRegistry?.ForEach(visitor);
    }
}