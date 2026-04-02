using System;
using DGame;
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
        {
            var logicUnit = self?.LogicUnitFactoryComponent?.Create(unitType);
            if (logicUnit == null)
            {
                return null;
            }
            var registered = false;
            var added = false;
            try
            {
                self.RegisterLogicUnit(logicUnit);
                registered = true;
                if (!self.AddLogicUnit(logicUnit))
                {
                    throw new InvalidOperationException($"Attach logic unit to battle failed: {logicUnit.GetType().Name}");
                }
                added = true;
                return logicUnit;
            }
            catch
            {
                if (added || registered)
                {
                    self.DetachLogicUnit(logicUnit);
                }
                if (!logicUnit.IsDisposed)
                {
                    logicUnit.Dispose();
                }
                return null;
            }
        }

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
        /// 将逻辑单位接入当前战斗的活跃生命周期管理。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待接入的逻辑单位。</param>
        /// <returns>成功接入时返回 true。</returns>
        public static bool AddLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
        {
            var added = self?.LogicUnitLifecycle?.AddUnit(logicUnit) ?? false;
            if (added)
            {
                self.PublishBattle(new LogicUnitSpawnedEvent(logicUnit));
            }

            return added;
        }

        /// <summary>
        /// 将逻辑单位从当前战斗的活跃生命周期管理中移除。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        /// <returns>成功移除任一生命周期记录时返回 true。</returns>
        public static bool RemoveLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
            => self?.LogicUnitLifecycle?.RemoveUnit(logicUnit) ?? false;

        /// <summary>
        /// 从当前战斗上下文索引中移除逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        public static void UnregisterLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
            => self?.LogicUnitRegistry?.Unregister(logicUnit);

        /// <summary>
        /// 将逻辑单位从当前战斗上下文中彻底摘除。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="logicUnit">待摘除的逻辑单位。</param>
        public static void DetachLogicUnit(this BattleContextComponent self, LogicUnit logicUnit)
        {
            self?.RemoveLogicUnit(logicUnit);
            self?.UnregisterLogicUnit(logicUnit);
        }

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

        /// <summary>
        /// 标记逻辑单位延迟销毁。
        /// </summary>
        public static bool MarkLogicUnitDelayDestroy(this BattleContextComponent self, LogicUnit logicUnit,
            FixedPoint64 destroyAt, LogicUnitDestroyReason reason)
        {
            if (self == null)
            {
                return false;
            }

            if (destroyAt <= FixedPoint64.Zero)
            {
                return self.DestroyLogicUnitImmediately(logicUnit, reason);
            }

            return self.LogicUnitLifecycle?.MarkDelayDestroy(logicUnit, destroyAt, reason) ?? false;
        }

        /// <summary>
        /// 立即销毁逻辑单位。
        /// </summary>
        public static bool DestroyLogicUnitImmediately(this BattleContextComponent self, LogicUnit logicUnit, LogicUnitDestroyReason reason)
        {
            if (self == null || logicUnit == null)
            {
                return false;
            }

            var removed = self.RemoveLogicUnit(logicUnit);
            if (removed)
            {
                self.PublishBattle(new LogicUnitDestroyingEvent(logicUnit, reason));
            }

            if (logicUnit.IsDisposed)
            {
                return false;
            }

            logicUnit.Dispose();
            return true;
        }

        /// <summary>
        /// 销毁当前战斗中的全部活跃逻辑单位。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="reason">统一使用的销毁原因。</param>
        public static void DestroyAllLogicUnits(this BattleContextComponent self, LogicUnitDestroyReason reason)
        {
            var snapshot = self?.LogicUnitLifecycle?.BuildActiveSnapshot();
            if (snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                var logicUnit = snapshot[i];
                if (logicUnit == null)
                {
                    continue;
                }

                self.DestroyLogicUnitImmediately(logicUnit, reason);
            }
        }

        /// <summary>
        /// 推进当前战斗中的全部活跃逻辑单位固定帧逻辑。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="deltaTime">当前固定逻辑帧时间增量。</param>
        public static void FixedUpdateBattleLogicUnits(this BattleContextComponent self, FixedPoint64 deltaTime)
            => self.FixedUpdateLogicUnits(deltaTime);

        /// <summary>
        /// 推进逻辑单位生命周期固定帧逻辑。
        /// </summary>
        public static void FixedUpdateLogicUnitLifecycle(this BattleContextComponent self, FixedPoint64 logicTime)
        {
            var expiredSnapshot = self?.LogicUnitLifecycle?.BuildExpiredDestroySnapshot(logicTime);
            if (expiredSnapshot == null)
            {
                return;
            }

            for (int i = 0; i < expiredSnapshot.Count; i++)
            {
                var delayData = expiredSnapshot[i];
                LogicUnit logicUnit = delayData.Unit;
                if (logicUnit == null)
                {
                    continue;
                }

                self.DestroyLogicUnitImmediately(logicUnit, delayData.Reason);
            }
        }
    }
}