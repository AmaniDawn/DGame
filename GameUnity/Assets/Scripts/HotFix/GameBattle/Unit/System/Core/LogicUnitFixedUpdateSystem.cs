using System.Collections.Generic;
using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位固定帧更新系统。
    /// </summary>
    public static class LogicUnitFixedUpdateSystem
    {
        /// <summary>
        /// 推进当前战斗内全部活跃逻辑单位的固定帧逻辑。
        /// </summary>
        /// <param name="self">战斗上下文组件。</param>
        /// <param name="deltaTime">当前固定逻辑帧时间增量。</param>
        public static void FixedUpdateLogicUnits(this BattleContextComponent self, FixedPoint64 deltaTime)
        {
            if (self?.LogicUnitLifecycle == null)
            {
                return;
            }

            List<LogicUnit> snapshot = self.LogicUnitLifecycle.BuildActiveSnapshot();
            if (snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                LogicUnit logicUnit = snapshot[i];
                if (logicUnit == null || logicUnit.IsDisposed)
                {
                    continue;
                }

                logicUnit.FixedUpdate(deltaTime);
            }
        }
    }
}