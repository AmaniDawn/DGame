using System.Collections.Generic;
using DGame;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位生命周期组件。
    /// <remarks>负责维护当前战斗中逻辑单位的活跃列表、待销毁列表与安全遍历快照。</remarks>
    /// </summary>
    public sealed class LogicUnitLifecycleComponent : Entity
    {
        /// <summary>
        /// 当前活跃逻辑单位列表
        /// </summary>
        internal readonly List<LogicUnit> ActiveUnits = new();

        /// <summary>
        /// 遍历活跃单位时使用的安全快照
        /// </summary>
        internal readonly List<LogicUnit> ActiveSnapshot = new();

        /// <summary>
        /// 已标记待销毁的逻辑单位列表
        /// </summary>
        internal readonly List<DelayDestroyLogicUnit> DelayDestroyUnits = new();

        /// <summary>
        /// 固定帧内已到期的待销毁单位快照
        /// </summary>
        internal readonly List<DelayDestroyLogicUnit> ExpiredDestroySnapshot = new();
    }

    /// <summary>
    /// 待销毁逻辑单位记录。
    /// </summary>
    public struct DelayDestroyLogicUnit
    {
        /// <summary>
        /// 待销毁逻辑单位引用
        /// </summary>
        public EntityReference<LogicUnit> Unit;

        /// <summary>
        /// 真正执行销毁的时间点
        /// </summary>
        public FixedPoint64 DestroyTime;

        /// <summary>
        /// 待销毁原因
        /// </summary>
        public LogicUnitDestroyReason Reason;
    }

    /// <summary>
    /// 逻辑单位销毁原因。
    /// </summary>
    public enum LogicUnitDestroyReason
    {
        None = 0, // 未指定销毁原因
        Dead = 1, // 单位死亡后销毁
        SummonExpired = 2, // 召唤物到期销毁
        LeaveBattle = 3, // 单位离开战场
        Replace = 4, // 单位被替换时销毁
        ScriptRequest = 5, // 外部脚本主动请求销毁
        BattleCleanup = 6, // 战斗结束批量清理
    }
}