using System;

namespace GameBattle
{
    internal sealed class EventSubscription
    {
        public object Owner { get; }
        public Delegate Handler { get; }
        public bool Removed { get; set; }

        public EventSubscription(object owner, Delegate handler)
        {
            Owner = owner;
            Handler = handler;
        }
    }
}