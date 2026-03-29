/* ------------------------------------------------------------------------
 * 适合放这里的是 battle 作用域内的广播事件，也就是一个单位或系统发生变化后，
 * 需要让战斗中的多个系统共同感知的通知。
 * 典型例子：单位创建、单位死亡后触发战斗统计、波次切换、关卡阶段变化、战斗结算开始。
 * 不适合放这里的是某个单位自己的局部表现事件，例如单体受击、单体技能开始、单体 Buff 变化，
 * 这些更适合走 UnitEventHubComponent。
 * 同样不适合把持续状态塞到这里，例如位置、HP 当前值、朝向。
 * 连续状态仍然应该放在 UnitStateSnapshot。
 * -------------------------------------------------------------------------
 */

using System;

namespace GameBattle
{
    /// <summary>
    /// 战斗级事件中心。
    /// </summary>
    public sealed class BattleEventHubComponent : EventHubCoreComponent
    {
        /// <summary>
        /// 注册战斗级事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="owner">监听器所属者，用于后续批量移除。</param>
        /// <param name="handler">事件回调。</param>
        public void Subscribe<T>(object owner, Action<T> handler) where T : struct, IBattleEvent
            => InternalSubscribe(owner, handler);

        /// <summary>
        /// 取消战斗级事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="handler">待移除的事件回调。</param>
        public void Unsubscribe<T>(Action<T> handler) where T : struct, IBattleEvent => InternalUnsubscribe(handler);

        /// <summary>
        /// 发布战斗级事件。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="eventData">事件数据。</param>
        public void Publish<T>(T eventData) where T : struct, IBattleEvent => InternalPublish(eventData);
    }
}