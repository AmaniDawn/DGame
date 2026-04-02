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
        /// 将逻辑单位加入生命周期管理。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待接入的逻辑单位。</param>
        /// <returns>成功加入时返回 true。</returns>
        public static bool AddUnit(this LogicUnitLifecycleComponent self, LogicUnit logicUnit)
        {
            if (self == null || logicUnit == null)
            {
                return false;
            }

            var activeUnits = self.ActiveUnits;
            for (int i = 0; i < activeUnits.Count; i++)
            {
                if (activeUnits[i].IsSameUnitId(logicUnit))
                {
                    return false;
                }
            }

            activeUnits.Add(logicUnit);
            return true;
        }

        /// <summary>
        /// 将逻辑单位从生命周期管理中移除。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        /// <returns>成功移除任一生命周期记录时返回 true。</returns>
        public static bool RemoveUnit(this LogicUnitLifecycleComponent self, LogicUnit logicUnit)
        {
            if (self == null || logicUnit == null)
            {
                return false;
            }

            var removedActive = RemoveActiveUnit(self.ActiveUnits, logicUnit);
            var removedDelay = RemoveDelayDestroyUnit(self.DelayDestroyUnits, logicUnit);
            return removedActive || removedDelay;
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

            var delayDestroyUnits = self.DelayDestroyUnits;
            for (int i = delayDestroyUnits.Count - 1; i >= 0; i--)
            {
                LogicUnit delayUnit = delayDestroyUnits[i].Unit;
                if (delayUnit == null)
                {
                    delayDestroyUnits.RemoveAt(i);
                    continue;
                }

                if (!delayUnit.IsSameUnitId(logicUnit))
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
        /// 收集当前固定帧内到期的延迟销毁单位快照。
        /// </summary>
        /// <param name="self">逻辑单位生命周期组件。</param>
        /// <param name="logicTime">当前战斗时间。</param>
        /// <returns>到期待销毁单位快照；组件为空时返回 null。</returns>
        public static List<DelayDestroyLogicUnit> BuildExpiredDestroySnapshot(this LogicUnitLifecycleComponent self, FixedPoint64 logicTime)
        {
            if (self == null)
            {
                return null;
            }

            var expiredSnapshot = self.ExpiredDestroySnapshot;
            expiredSnapshot.Clear();
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
                expiredSnapshot.Add(delayData);
            }

            return expiredSnapshot;
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
            self.ExpiredDestroySnapshot.Clear();
        }

        /// <summary>
        /// 从活跃列表中移除指定逻辑单位。
        /// </summary>
        /// <param name="activeUnits">活跃逻辑单位列表。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        private static bool RemoveActiveUnit(List<LogicUnit> activeUnits, LogicUnit logicUnit)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                var activeUnit = activeUnits[i];
                if (activeUnit != null && activeUnit.IsSameUnitId(logicUnit))
                {
                    activeUnits.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从待销毁列表中移除指定逻辑单位。
        /// </summary>
        /// <param name="delayDestroyUnits">待销毁逻辑单位列表。</param>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        private static bool RemoveDelayDestroyUnit(List<DelayDestroyLogicUnit> delayDestroyUnits, LogicUnit logicUnit)
        {
            for (int i = 0; i < delayDestroyUnits.Count; i++)
            {
                var delayUnit = delayDestroyUnits[i].Unit.Value;
                if (delayUnit != null && delayUnit.IsSameUnitId(logicUnit))
                {
                    delayDestroyUnits.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}