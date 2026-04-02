using System.Collections.Generic;
using DGame;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位属性组件。
    /// <remarks>
    /// BaseAttr / RuntimeBaseAttr / FinalAttr 承载可重算属性，
    /// CurrentHp 承载运行时血量状态，避免属性重算覆盖当前生命值。
    /// </remarks>
    /// </summary>
    public sealed class LogicUnitAttrComponent : Entity
    {
        private LogicUnitAttrData m_baseAttr; // 当前单位的基础属性数据
        private LogicUnitAttrData m_runtimeBaseAttr; // 当前单位的运行时基础属性数据
        private LogicUnitAttrData m_finalAttr; // 当前单位的最终属性数据

        /// <summary>
        /// 当前单位持有的属性修正列表。
        /// </summary>
        internal readonly List<LogicUnitAttrModifier> Modifiers = new(); // 当前单位的属性修正列表

        /// <summary>
        /// 当前最终属性是否需要重新结算。
        /// </summary>
        internal bool Dirty = true; // 当前最终属性是否需要重新结算

        /// <summary>
        /// 当前单位不含 Hp 修正时的基础生命值。
        /// </summary>
        internal int BaseCurrentHp { get; set; } // 当前单位不含 Hp 修正时的基础生命值

        /// <summary>
        /// 当前属性组件所属的逻辑单位。
        /// </summary>
        internal LogicUnit Owner { get; set; } // 当前属性组件所属的逻辑单位

        /// <summary>
        /// 基础属性。
        /// </summary>
        public LogicUnitAttrData BaseAttr => m_baseAttr ??= LogicUnitAttrData.Create();

        /// <summary>
        /// 运行时基础属性。
        /// </summary>
        public LogicUnitAttrData RuntimeBaseAttr => m_runtimeBaseAttr ??= LogicUnitAttrData.Create();

        /// <summary>
        /// 最终属性。
        /// </summary>
        public LogicUnitAttrData FinalAttr => m_finalAttr ??= LogicUnitAttrData.Create();

        /// <summary>
        /// 当前生命值。
        /// </summary>
        public int CurrentHp { get; internal set; }

        /// <summary>
        /// 清空属性存储对象中的数据，但保留对象实例。
        /// </summary>
        internal void ClearAttrStorages()
        {
            m_baseAttr?.Clear();
            m_runtimeBaseAttr?.Clear();
            m_finalAttr?.Clear();
        }

        /// <summary>
        /// 释放属性存储对象及修正累加缓存。
        /// </summary>
        internal void ReleaseAttrStorages()
        {
            if (m_baseAttr != null)
            {
                MemoryObject.Release(m_baseAttr);
                m_baseAttr = null;
            }

            if (m_runtimeBaseAttr != null)
            {
                MemoryObject.Release(m_runtimeBaseAttr);
                m_runtimeBaseAttr = null;
            }

            if (m_finalAttr != null)
            {
                MemoryObject.Release(m_finalAttr);
                m_finalAttr = null;
            }
        }
    }
}
