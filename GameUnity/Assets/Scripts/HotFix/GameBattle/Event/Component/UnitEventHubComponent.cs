using System;

namespace GameBattle
{
    public sealed class UnitEventHubComponent : EventHubCoreComponent
    {
        public void Subscribe<T>(object owner, Action<T> handler) where T : struct, IUnitEvent 
            => InternalSubscribe(owner, handler);

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IUnitEvent => InternalUnsubscribe(handler);

        public void Publish<T>(T eventData) where T : struct, IUnitEvent => InternalPublish(eventData);
    }
}