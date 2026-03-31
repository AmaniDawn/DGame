using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 战斗事件中心销毁系统。
    /// </summary>
    public sealed class BattleEventHubComponentDestroySystem : DestroySystem<BattleEventHubComponent>
    {
        /// <summary>
        /// 销毁战斗事件中心。
        /// </summary>
        /// <param name="self">战斗事件中心实例。</param>
        protected override void Destroy(BattleEventHubComponent self)
        {
            self.Clear();
        }
    }

    /// <summary>
    /// 战斗事件中心扩展方法。
    /// </summary>
    public static class BattleEventHubComponentSystem
    {
        /// <summary>
        /// 注册战斗事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="self">战斗上下文。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeBattle<T>(this BattleContextComponent self, object owner, Action<T> handler)
            where T : struct, IBattleEvent
            => self.BattleEvents.Subscribe(owner, handler);

        /// <summary>
        /// 取消战斗事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="self">战斗上下文。</param>
        /// <param name="handler">事件回调。</param>
        public static void UnsubscribeBattle<T>(this BattleContextComponent self, Action<T> handler)
            where T : struct, IBattleEvent
            => self.BattleEvents.Unsubscribe(handler);

        /// <summary>
        /// 移除指定所属者的全部战斗事件监听。
        /// </summary>
        /// <param name="self">战斗上下文。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAllBattleSubscriptions(this BattleContextComponent self, object owner)
            => self.BattleEvents.RemoveAll(owner);

        /// <summary>
        /// 发布战斗事件。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="self">战斗上下文。</param>
        /// <param name="eventData">事件数据。</param>
        public static void PublishBattle<T>(this BattleContextComponent self, T eventData)
            where T : struct, IBattleEvent
            => self.BattleEvents.Publish(eventData);
    }
}