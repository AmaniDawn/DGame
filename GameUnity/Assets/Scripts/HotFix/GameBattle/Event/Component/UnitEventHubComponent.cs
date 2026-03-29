/* -------------------------------------------------------------------------
 * 适合放这里的是“这个单位自己发生过一次的边沿事件”，只服务当前 LogicUnit。
 * 典型例子：UnitDamagedEvent、受击、死亡、复活、Buff 添加/移除、技能开始/结束、状态切换。
 * 
 * 不适合放这里的是连续状态，例如位置、旋转、HP 当前值、MP 当前值、移动朝向。
 * 这些应该进入 UnitStateSnapshot，由表现层按版本号主动拉取。
 *
 * 也不适合放 battle 范围内的广播事件，例如：
 * 单位创建后需要通知整个战场、某个单位死亡后触发波次/结算统计、战斗阶段切换。
 * 这类跨单位或跨系统通知应该走 BattleEventHubComponent。
 * -------------------------------------------------------------------------
 */

using System;

namespace GameBattle
{
    /// <summary>
    /// 单位级事件中心。
    /// </summary>
    public sealed class UnitEventHubComponent : EventHubCoreComponent
    {
        /// <summary>
        /// 注册单位级事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="owner">监听器所属者，用于后续批量移除。</param>
        /// <param name="handler">事件回调。</param>
        public void Subscribe<T>(object owner, Action<T> handler) where T : struct, IUnitEvent 
            => InternalSubscribe(owner, handler);

        /// <summary>
        /// 取消单位级事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="handler">待移除的事件回调。</param>
        public void Unsubscribe<T>(Action<T> handler) where T : struct, IUnitEvent => InternalUnsubscribe(handler);

        /// <summary>
        /// 发布单位级事件。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="eventData">事件数据。</param>
        public void Publish<T>(T eventData) where T : struct, IUnitEvent => InternalPublish(eventData);
    }
}