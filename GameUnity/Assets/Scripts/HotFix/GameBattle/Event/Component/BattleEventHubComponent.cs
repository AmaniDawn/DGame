using System;

namespace GameBattle
{
    public sealed class BattleEventHubComponent : EventHubCoreComponent
    {
        public void Subscribe<T>(object owner, Action<T> handler) where T : struct, IBattleEvent
            => InternalSubscribe(owner, handler);

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IBattleEvent => InternalUnsubscribe(handler);

        public void Publish<T>(T eventData) where T : struct, IBattleEvent => InternalPublish(eventData);
    }
}