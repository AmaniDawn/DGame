using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 渲染单位注册表。
    /// <remarks>负责维护当前战斗中的渲染单位索引、遍历与安全快照，不承担具体业务逻辑。</remarks>
    /// </summary>
    public sealed class RenderUnitRegistry
    {
        private readonly List<RenderUnit> m_units = new(); // 当前活跃渲染单位列表
        private readonly List<RenderUnit> m_snapshotBuffer = new(); // 遍历时使用的安全快照缓冲区
        private readonly Dictionary<long, RenderUnit> m_unitsById = new(); // 以实体 Id 建立的渲染单位索引

        /// <summary>
        /// 当前已注册的渲染单位数量。
        /// </summary>
        public int Count => m_units.Count;

        /// <summary>
        /// 注册渲染单位。
        /// 使用 <see cref="RenderUnit.Id"/> 作为内部索引主键。
        /// </summary>
        /// <param name="renderUnit">待注册的渲染单位。</param>
        public void Register(RenderUnit renderUnit)
        {
            if (renderUnit == null)
            {
                return;
            }

            if (m_unitsById.TryGetValue(renderUnit.Id, out var oldUnit) && !oldUnit.IsSameUnitId(renderUnit))
            {
                m_units.Remove(oldUnit);
            }

            m_unitsById[renderUnit.Id] = renderUnit;
            if (!m_units.Contains(renderUnit))
            {
                m_units.Add(renderUnit);
            }
        }

        /// <summary>
        /// 反注册渲染单位。
        /// </summary>
        /// <param name="renderUnit">待移除的渲染单位。</param>
        public void Unregister(RenderUnit renderUnit)
        {
            if (renderUnit == null)
            {
                return;
            }

            if (m_unitsById.TryGetValue(renderUnit.Id, out var currentUnit) && currentUnit.IsSameUnitId(renderUnit))
            {
                m_unitsById.Remove(renderUnit.Id);
            }
            m_units.Remove(renderUnit);
        }

        /// <summary>
        /// 按实体 ID 查询渲染单位。
        /// </summary>
        /// <param name="entityId">渲染单位实体 ID。</param>
        /// <param name="renderUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public bool TryGet(long entityId, out RenderUnit renderUnit)
            => m_unitsById.TryGetValue(entityId, out renderUnit);

        /// <summary>
        /// 遍历当前战斗中的全部渲染单位。
        /// </summary>
        /// <param name="visitor">遍历回调。</param>
        public void ForEach(Action<RenderUnit> visitor)
        {
            if (visitor == null)
            {
                return;
            }

            foreach (var renderUnit in m_units)
            {
                visitor(renderUnit);
            }
        }

        /// <summary>
        /// 构建一个安全的快照列表，用于逐帧更新时避免遍历过程被修改。
        /// </summary>
        /// <returns>当前帧的渲染单位快照。</returns>
        public List<RenderUnit> BuildSnapshot()
        {
            m_snapshotBuffer.Clear();
            m_snapshotBuffer.AddRange(m_units);
            return m_snapshotBuffer;
        }

        /// <summary>
        /// 清空全部注册数据。
        /// </summary>
        public void Clear()
        {
            m_units.Clear();
            m_snapshotBuffer.Clear();
            m_unitsById.Clear();
        }
    }
}