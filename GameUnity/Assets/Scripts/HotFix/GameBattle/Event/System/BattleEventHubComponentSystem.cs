using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class BattleEventHubComponentDestroySystem : DestroySystem<BattleEventHubComponent>
    {
        protected override void Destroy(BattleEventHubComponent self)
        {
            self.Clear();
        }
    }

    public static class BattleEventHubComponentSystem
    {
        public static void Subscribe<T>(this BattleContextComponent self, object owner, Action<T> handler)
            where T : struct, IBattleEvent
            => self.BattleEvents.Subscribe(owner, handler);

        public static void Unsubscribe<T>(this BattleContextComponent self, Action<T> handler)
            where T : struct, IBattleEvent
            => self.BattleEvents.Unsubscribe(handler);

        public static void RemoveAll(this BattleContextComponent self, object owner)
            => self.BattleEvents.RemoveAll(owner);

        public static void Publish<T>(this BattleContextComponent self, T eventData)
            where T : struct, IBattleEvent
            => self.BattleEvents.Publish(eventData);
    }
}