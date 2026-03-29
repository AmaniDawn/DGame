using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class UnitEventHubComponentDestroySystem : DestroySystem<UnitEventHubComponent>
    {
        protected override void Destroy(UnitEventHubComponent self)
        {
            self.Clear();
        }
    }
    
    public static class UnitEventHubComponentSystem
    {
        public static void Subscribe<T>(this LogicUnit self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEvents.Subscribe(owner, handler);
        
        public static void Unsubscribe<T>(this LogicUnit self, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEvents.Unsubscribe(handler);

        public static void RemoveAll(this LogicUnit self, object owner)
            => self.UnitEvents.RemoveAll(owner);
        
        public static void Publish<T>(this LogicUnit self, T eventData)
            where T : struct, IUnitEvent
            => self.UnitEvents.Publish(eventData);
    }
}