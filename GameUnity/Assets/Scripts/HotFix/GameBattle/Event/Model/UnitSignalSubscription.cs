using System;

namespace GameBattle
{
    internal sealed class UnitSignalSubscription
    {
        public object Owner { get; }
        public Delegate Handler { get; }
        public bool Removed { get; set; }

        public UnitSignalSubscription(object owner, Delegate handler)
        {
            Owner = owner;
            Handler = handler;
        }
    }
}