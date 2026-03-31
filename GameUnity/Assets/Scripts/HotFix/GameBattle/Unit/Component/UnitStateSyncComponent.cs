/* -------------------------------------------------------------------------
 * 这里处理的是连续状态，不负责广播一次性事件。
 *
 * 版本号适合跟随状态分组推进，例如：
 * Transform、Attr、State、Target、Buff、Skill。
 * 每个消费者只需要记录自己上次同步到的版本号，即可判断某一组状态是否需要刷新。
 *
 * 不适合用版本号驱动的是一次性边沿通知，例如：
 * 受伤飘字、暴击触发、死亡播特效、Buff 新增提示、技能开始/结束音效。
 * 这些应该走事件中心，而不是塞进状态版本。
 * -------------------------------------------------------------------------
 */

using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位的状态同步组件。
    /// <remarks>
    /// 负责保存 <see cref="UnitStateSnapshot"/> 与版本号。
    
    /// </remarks>
    /// </summary>
    public sealed class UnitStateSyncComponent : Entity
    {
        /// <summary>
        /// 当前单位状态快照。
        /// </summary>
        public UnitStateSnapshot Snapshot { get; set; }

        /// <summary>
        /// 状态类字段版本号。
        /// </summary>
        public uint StateVersion { get; private set; }

        /// <summary>
        /// 属性类字段版本号。
        /// </summary>
        public uint AttrVersion { get; private set; }

        /// <summary>
        /// 根据脏标记推进对应版本号。
        /// </summary>
        /// <param name="flags">状态脏标记。</param>
        public void MarkDirty(UnitStateDirtyFlags flags)
        {
            if ((flags & UnitStateDirtyFlags.State) != 0)
            {
                StateVersion++;
            }
            
            if ((flags & UnitStateDirtyFlags.Attr) != 0)
            {
                AttrVersion++;
            }
        }
        
        /// <summary>
        /// 清空状态快照与版本号。
        /// </summary>
        public void Clear()
        {
            Snapshot = default;
            StateVersion = 0;
            AttrVersion = 0;
        }
    }
}