using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位注册表组件。
    /// <remarks>负责维护当前战斗内逻辑单位的索引、查询与遍历能力，不承载业务生命周期逻辑。</remarks>
    /// </summary>
    public sealed class LogicUnitRegistryComponent : Entity
    {
        private readonly Dictionary<ulong, LogicUnit> m_unitsByUnitId = new(); // 以逻辑 UnitID 建立的单位主索引
        private readonly Dictionary<long, LogicUnit> m_unitsByEntityId = new(); // 以实体 Id 建立的运行时实例索引

        /// <summary>
        /// 当前已注册的逻辑单位数量。
        /// </summary>
        public int Count => m_unitsByUnitId.Count;

        /// <summary>
        /// 注册逻辑单位。
        /// 同时写入逻辑 UnitID 主索引与实体 Id 运行时索引。
        /// </summary>
        /// <param name="logicUnit">待注册的逻辑单位。</param>
        public void Register(LogicUnit logicUnit)
        {
            if (logicUnit == null || logicUnit.UnitID == 0)
            {
                return;
            }

            m_unitsByUnitId[logicUnit.UnitID] = logicUnit;
            m_unitsByEntityId[logicUnit.Id] = logicUnit;
        }

        /// <summary>
        /// 反注册逻辑单位。
        /// </summary>
        /// <param name="logicUnit">待移除的逻辑单位。</param>
        public void Unregister(LogicUnit logicUnit)
        {
            if (logicUnit == null)
            {
                return;
            }

            if (logicUnit.UnitID != 0)
            {
                m_unitsByUnitId.Remove(logicUnit.UnitID);
            }

            m_unitsByEntityId.Remove(logicUnit.Id);
        }

        /// <summary>
        /// 按逻辑 UnitID 查询逻辑单位。
        /// </summary>
        /// <param name="unitId">逻辑单位 ID。</param>
        /// <param name="logicUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public bool TryGet(ulong unitId, out LogicUnit logicUnit) => m_unitsByUnitId.TryGetValue(unitId, out logicUnit);

        /// <summary>
        /// 按实体 ID 查询逻辑单位。
        /// </summary>
        /// <param name="entityId">逻辑单位实体 ID。</param>
        /// <param name="logicUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public bool TryGet(long entityId, out LogicUnit logicUnit) => m_unitsByEntityId.TryGetValue(entityId, out logicUnit);

        /// <summary>
        /// 遍历当前战斗中的全部逻辑单位。
        /// </summary>
        /// <param name="visitor">遍历回调。</param>
        public void ForEach(Action<LogicUnit> visitor)
        {
            if (visitor == null)
            {
                return;
            }

            foreach (var logicUnit in m_unitsByUnitId.Values)
            {
                visitor(logicUnit);
            }
        }

        /// <summary>
        /// 清空全部注册数据。
        /// </summary>
        public void Clear()
        {
            m_unitsByUnitId.Clear();
            m_unitsByEntityId.Clear();
        }
    }
}