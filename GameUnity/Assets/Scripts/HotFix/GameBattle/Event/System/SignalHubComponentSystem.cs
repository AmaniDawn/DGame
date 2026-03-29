using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class SignalHubComponentDestroySystem : DestroySystem<SignalHubComponent>
    {
        protected override void Destroy(SignalHubComponent self)
        {
            self.Clear();
        }
    }
    
    public static class SignalHubComponentSystem
    {
        public static void Subscribe<T>(this LogicUnit self, object owner, Action<T> handler)
            where T : struct, ISignal
            => self.SignalHub.Subscribe(owner, handler);
        
        public static void Unsubscribe<T>(this LogicUnit self, Action<T> handler)
            where T : struct, ISignal
            => self.SignalHub.Unsubscribe(handler);

        public static void RemoveAll<T>(this LogicUnit self, object owner)
            where T : struct, ISignal
            => self.SignalHub.RemoveAll(owner);
        
        public static void Publish<T>(this LogicUnit self, T signal)
            where T : struct, ISignal
            => self.SignalHub.Publish(signal);
    }
}