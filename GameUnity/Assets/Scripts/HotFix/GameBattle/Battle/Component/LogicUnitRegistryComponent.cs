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
        private readonly Dictionary<long, LogicUnit> m_unitsById = new(); // 以实体 Id 建立的逻辑单位索引

        /// <summary>
        /// 当前已注册的逻辑单位数量。
        /// </summary>
        public int Count => m_unitsById.Count;

        /// <summary>
        /// 注册逻辑单位。
        /// 使用 <see cref="Entity.Id"/> 作为内部索引主键。
        /// </summary>
        /// <param name="logicUnit">待注册的逻辑单位。</param>
        public void Register(LogicUnit logicUnit)
        {
            if (logicUnit == null)
            {
                return;
            }

            m_unitsById[logicUnit.Id] = logicUnit;
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

            m_unitsById.Remove(logicUnit.Id);
        }

        /// <summary>
        /// 按实体 ID 查询逻辑单位。
        /// </summary>
        /// <param name="entityId">逻辑单位实体 ID。</param>
        /// <param name="logicUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public bool TryGet(long entityId, out LogicUnit logicUnit)
            => m_unitsById.TryGetValue(entityId, out logicUnit);

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

            foreach (var logicUnit in m_unitsById.Values)
            {
                visitor(logicUnit);
            }
        }

        /// <summary>
        /// 清空全部注册数据。
        /// </summary>
        public void Clear()
            => m_unitsById.Clear();
    }
}