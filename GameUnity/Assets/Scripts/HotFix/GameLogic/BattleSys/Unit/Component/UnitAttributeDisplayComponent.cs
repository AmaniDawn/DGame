using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 单位属性表现快照。
    /// <remarks>用于承载渲染层当前关心的连续属性值。</remarks>
    /// </summary>
    public struct UnitAttributeSnapshot
    {
        /// <summary>
        /// 当前生命值。
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 最大生命值。
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 当前法力值。
        /// </summary>
        public int Mp { get; set; }

        /// <summary>
        /// 最大法力值。
        /// </summary>
        public int MaxMp { get; set; }
    }

    /// <summary>
    /// 单位属性表现同步组件。
    /// <remarks>负责缓存逻辑层属性快照，并在属性变化时向渲染层分发统一的属性更新事件。</remarks>
    /// </summary>
    public sealed class UnitAttributeDisplayComponent : Entity
    {
        /// <summary>
        /// 所属渲染单位。
        /// </summary>
        public RenderUnit OwnerUnit { get; private set; }

        /// <summary>
        /// 当前缓存的属性快照。
        /// </summary>
        public UnitAttributeSnapshot Snapshot { get; private set; }

        /// <summary>
        /// 当前属性组件是否已经完成首次同步。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 初始化属性表现同步组件。
        /// </summary>
        /// <param name="owner">所属渲染单位。</param>
        public void Init(RenderUnit owner)
        {
            OwnerUnit = owner;
            Snapshot = default;
            IsInitialized = false;
        }

        /// <summary>
        /// 根据逻辑层状态快照同步属性数据。
        /// </summary>
        /// <param name="stateSnapshot">逻辑层状态快照。</param>
        public void Sync(in UnitStateSnapshot stateSnapshot)
        {
            var previous = Snapshot;
            var current = new UnitAttributeSnapshot
            {
                Hp = stateSnapshot.Hp,
                MaxHp = stateSnapshot.MaxHp,
                Mp = stateSnapshot.Mp,
                MaxMp = stateSnapshot.MaxMp,
            };

            var changeFlags = GetChangeFlags(previous, current);
            Snapshot = current;

            if (!IsInitialized)
            {
                IsInitialized = true;
                changeFlags = UnitAttributeChangeFlags.All;
            }

            if (changeFlags == UnitAttributeChangeFlags.None)
            {
                return;
            }

            OwnerUnit?.PublishRender(new UnitAttributeChangedEvent(previous, current, changeFlags));
        }

        /// <summary>
        /// 清空当前缓存状态。
        /// </summary>
        public void Clear()
        {
            OwnerUnit = null;
            Snapshot = default;
            IsInitialized = false;
        }

        /// <summary>
        /// 比较两份属性快照并生成变化标记。
        /// </summary>
        /// <param name="previous">旧快照。</param>
        /// <param name="current">新快照。</param>
        /// <returns>属性变化标记。</returns>
        private static UnitAttributeChangeFlags GetChangeFlags(in UnitAttributeSnapshot previous, in UnitAttributeSnapshot current)
        {
            var flags = UnitAttributeChangeFlags.None;

            if (previous.Hp != current.Hp || previous.MaxHp != current.MaxHp)
            {
                flags |= UnitAttributeChangeFlags.Hp;
            }

            if (previous.Mp != current.Mp || previous.MaxMp != current.MaxMp)
            {
                flags |= UnitAttributeChangeFlags.Mp;
            }

            return flags;
        }
    }
}