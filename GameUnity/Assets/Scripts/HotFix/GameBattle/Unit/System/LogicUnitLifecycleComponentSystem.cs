using System.Collections.Generic;
using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位生命周期组件销毁系统。
    /// </summary>
    public sealed class LogicUnitLifecycleComponentDestroySystem : DestroySystem<LogicUnitLifecycleComponent>
    {
        protected override void Destroy(LogicUnitLifecycleComponent self)
        {
            self.Clear();
        }
    }

    /// <summary>
    /// 逻辑单位生命周期扩展方法集合。
    /// </summary>
    public static class LogicUnitLifecycleComponentSystem
    {
        /// <summary>
        /// 获取当前处于活跃状态的逻辑单位数量。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <returns>活跃逻辑单位数量。</returns>
        public static int ActiveCount(this LogicUnitLifecycleComponent self) => self?.ActiveUnits.Count ?? 0;

        /// <summary>
        /// 获取当前待销毁逻辑单位数量。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <returns>待销毁逻辑单位数量。</returns>
        public static int DelayDestroyCount(this LogicUnitLifecycleComponent self) => self?.DelayDestroyUnits.Count ?? 0;

        /// <summary>
        /// 创建并接入一个新的逻辑单位。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="unitType">要创建的逻辑单位类型。</param>
        /// <returns>创建成功时返回逻辑单位实例，否则返回 null。</returns>
        public static LogicUnit Spawn(this LogicUnitLifecycleComponent self, UnitType unitType)
        {
            var battleContext = self?.Parent as BattleContextComponent;
            return battleContext?.LogicUnitFactoryComponent?.Create(unitType);
        }

        /// <summary>
        /// 将逻辑单位加入生命周期管理。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待接入的逻辑单位。</param>
        public static void AddUnit(this LogicUnitLifecycleComponent self, LogicUnit logicUnit)
        {
            if (self == null || logicUnit == null)
            {
                return;
            }

            var activeUnits = self.ActiveUnits;
            for (int i = 0; i < activeUnits.Count; i++)
            {
                if (activeUnits[i].IsSameUnit(logicUnit))
                {
                    return;
                }
            }

            activeUnits.Add(logicUnit);
            var battleContext = self.Parent as BattleContextComponent;
            battleContext?.PublishBattle(new LogicUnitSpawnedEvent(logicUnit));
        }

        /// <summary>
        /// 将逻辑单位从生命周期管理中移除。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        public static void RemoveUnit(this LogicUnitLifecycleComponent self, LogicUnit logicUnit)
        {
            if (self == null || logicUnit == null)
            {
                return;
            }

            RemoveActiveUnit(self.ActiveUnits, logicUnit);
            RemoveDelayDestroyUnit(self.DelayDestroyUnits, logicUnit);
        }

        /// <summary>
        /// 标记逻辑单位为待销毁状态。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待销毁的逻辑单位。</param>
        /// <param name="destroyTime">真正执行销毁的时间点；小于等于零时立即销毁。</param>
        /// <param name="reason">销毁原因。</param>
        /// <returns>成功标记或立即销毁时返回 true。</returns>
        public static bool MarkDelayDestroy(this LogicUnitLifecycleComponent self, LogicUnit logicUnit,
            FixedPoint64 destroyTime, LogicUnitDestroyReason reason)
        {
            if (self == null || logicUnit == null || logicUnit.IsDisposed)
            {
                return false;
            }

            if (destroyTime <= FixedPoint64.Zero)
            {
                return self.DestroyImmediately(logicUnit, reason);
            }

            var delayDestroyUnits = self.DelayDestroyUnits;
            for (int i = delayDestroyUnits.Count - 1; i >= 0; i--)
            {
                LogicUnit delayUnit = delayDestroyUnits[i].Unit;
                if (delayUnit == null)
                {
                    delayDestroyUnits.RemoveAt(i);
                    continue;
                }

                if (!delayUnit.IsSameUnit(logicUnit))
                {
                    continue;
                }

                var delayData = delayDestroyUnits[i];
                delayData.DestroyTime = destroyTime;
                delayData.Reason = reason;
                delayDestroyUnits[i] = delayData;
                return true;
            }

            delayDestroyUnits.Add(new DelayDestroyLogicUnit
            {
                Unit = logicUnit,
                DestroyTime = destroyTime,
                Reason = reason
            });
            return true;
        }

        /// <summary>
        /// 立即销毁指定逻辑单位。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待销毁的逻辑单位。</param>
        /// <param name="reason">销毁原因。</param>
        /// <returns>执行销毁流程时返回 true；目标已无效时返回 false。</returns>
        public static bool DestroyImmediately(this LogicUnitLifecycleComponent self, LogicUnit logicUnit, LogicUnitDestroyReason reason)
        {
            if (self == null || logicUnit == null)
            {
                return false;
            }

            self.RemoveUnit(logicUnit);
            var battleContext = self.Parent as BattleContextComponent;
            battleContext?.PublishBattle(new LogicUnitDestroyingEvent(logicUnit, reason));

            if (logicUnit.IsDisposed)
            {
                return false;
            }

            logicUnit.Dispose();
            return true;
        }

        /// <summary>
        /// 推进待销毁逻辑单位列表的固定帧逻辑。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicTime">当前战斗时间。</param>
        public static void FixedUpdate(this LogicUnitLifecycleComponent self, FixedPoint64 logicTime)
        {
            if (self == null)
            {
                return;
            }

            FixedUpdateDelayDestroyUnits(self, logicTime);
        }

        /// <summary>
        /// 推进延迟销毁逻辑单位列表的固定帧逻辑。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicTime">当前战斗时间。</param>
        private static void FixedUpdateDelayDestroyUnits(LogicUnitLifecycleComponent self, FixedPoint64 logicTime)
        {
            var delayDestroyUnits = self.DelayDestroyUnits;
            for (int i = delayDestroyUnits.Count - 1; i >= 0; i--)
            {
                var delayData = delayDestroyUnits[i];
                LogicUnit logicUnit = delayData.Unit;
                if (logicUnit == null || logicUnit.IsDisposed)
                {
                    delayDestroyUnits.RemoveAt(i);
                    continue;
                }

                if (delayData.DestroyTime > logicTime)
                {
                    continue;
                }

                delayDestroyUnits.RemoveAt(i);
                self.DestroyImmediately(logicUnit, delayData.Reason);
            }
        }

        /// <summary>
        /// 构建当前活跃逻辑单位的安全快照。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <returns>活跃逻辑单位快照列表；组件为空时返回 null。</returns>
        public static List<LogicUnit> BuildActiveSnapshot(this LogicUnitLifecycleComponent self)
        {
            if (self == null)
            {
                return null;
            }

            var snapshot = self.ActiveSnapshot;
            snapshot.Clear();
            snapshot.AddRange(self.ActiveUnits);
            return snapshot;
        }

        /// <summary>
        /// 销毁当前全部活跃逻辑单位。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="reason">统一使用的销毁原因。</param>
        public static void DestroyAll(this LogicUnitLifecycleComponent self, LogicUnitDestroyReason reason)
        {
            if (self == null)
            {
                return;
            }

            var snapshot = self.BuildActiveSnapshot();
            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var logicUnit = snapshot[i];
                    if (logicUnit == null)
                    {
                        continue;
                    }

                    self.DestroyImmediately(logicUnit, reason);
                }
            }

            self.Clear();
        }

        /// <summary>
        /// 清空生命周期组件内部缓存。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        public static void Clear(this LogicUnitLifecycleComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.ActiveUnits.Clear();
            self.ActiveSnapshot.Clear();
            self.DelayDestroyUnits.Clear();
        }

        /// <summary>
        /// 从活跃列表中移除指定逻辑单位。
        /// </summary>
        /// <param name="activeUnits">活跃逻辑单位列表。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        private static void RemoveActiveUnit(List<LogicUnit> activeUnits, LogicUnit logicUnit)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                var activeUnit = activeUnits[i];
                if (activeUnit != null && activeUnit.IsSameUnit(logicUnit))
                {
                    activeUnits.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 从待销毁列表中移除指定逻辑单位。
        /// </summary>
        /// <param name="delayDestroyUnits">待销毁逻辑单位列表。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        private static void RemoveDelayDestroyUnit(List<DelayDestroyLogicUnit> delayDestroyUnits, LogicUnit logicUnit)
        {
            for (int i = 0; i < delayDestroyUnits.Count; i++)
            {
                var delayUnit = delayDestroyUnits[i].Unit.Value;
                if (delayUnit != null && delayUnit.IsSameUnit(logicUnit))
                {
                    delayDestroyUnits.RemoveAt(i);
                    return;
                }
            }
        }
    }
}