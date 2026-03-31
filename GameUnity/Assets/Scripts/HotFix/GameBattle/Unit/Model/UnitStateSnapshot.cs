/* -------------------------------------------------------------------------
 * 适合放入这里的是“某个时刻可直接读取的当前状态”，而不是一次性的边沿通知。
 * 典型例子：
 * Position、Rotation、MoveForward、Hp/MaxHp、Mp/MaxMp、当前 UnitState。
 *
 * 这些数据通常会被 RenderUnit、血条、名字板等表现层在同步阶段主动拉取，
 * 并配合版本号判断是否需要刷新。
 *
 * 适合做版本控制的字段有两类：
 * 一类是表现层需要按需拉取的连续状态，例如位置、旋转、朝向、血量、蓝量、当前状态；
 * 另一类是“当前值”语义明确、并且可能被多个表现消费者共享读取的状态，
 * 例如 TargetUnitId、MoveSpeed、CastSkillId、CastProgress、Buff 当前层数、控制状态、阵营标识。
 *
 * 判断标准是：
 * 这个值是否代表“现在是什么状态”，而不是“刚刚发生过什么”；
 * 它是否允许 RenderUnit 或 UI 在任意时刻直接读取；
 * 它是否适合通过版本号比较来决定是否刷新，而不是靠事件触发一次。
 *
 * 不适合放入这里的是“发生过一次”的事件，例如：
 * 受伤一次、暴击一次、死亡一次、Buff 添加一次、技能开始一次。
 * 这类边沿变化应该走 UnitEventHubComponent 或 BattleEventHubComponent。
 *
 * 同样不适合做版本控制的还有：
 * 纯临时中间量，例如某一帧的路径采样点、导航临时结果、内部计算缓存；
 * 纯派生数据，例如可以由其他快照字段直接算出的结果；
 * 只用于逻辑过程、不会被表现层直接消费的一次性流程标记；
 * 以及应该广播给多个系统处理的 battle 级流程事件。
 * -------------------------------------------------------------------------
 */

using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位的连续状态快照。
    /// </summary>
    public struct UnitStateSnapshot
    {
        /// <summary>
        /// 单位 ID。
        /// </summary>
        public ulong UnitID { get; set; }

        /// <summary>
        /// 单位类型。
        /// </summary>
        public UnitType UnitType { get; set; }

        /// <summary>
        /// 当前单位状态。
        /// </summary>
        public UnitState UnitState { get; set; }

        /// <summary>
        /// 当前逻辑位置。
        /// </summary>
        public FixedPointVector3 Position { get; set; }

        /// <summary>
        /// 当前逻辑旋转。
        /// </summary>
        public FixedPointQuaternion Rotation { get; set; }

        /// <summary>
        /// 当前移动朝向。
        /// </summary>
        public FixedPointVector3 MoveForward { get; set; }

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
}