using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class SubscriptionScopeComponentDestroySystem : DestroySystem<SubscriptionScopeComponent>
    {
        protected override void Destroy(SubscriptionScopeComponent self)
        {
            self.Destroy();
        }
    }
    
    public static class SubscriptionScopeComponentSystem
    {
        public static void SubscribeScoped<T>(this LogicUnit self, object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, ISignal
        {
            self.Subscribe(owner, handler);
            scope.Add(() => self.Unsubscribe(handler));
        }
    }
}