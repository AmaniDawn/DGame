using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    public sealed class SignalHubComponent : Entity
    {
        private readonly Dictionary<Type, List<UnitSignalSubscription>> m_subscriptions = new();
        private readonly List<Type> m_publishingTypes = new();
        private readonly HashSet<Type> m_delayCleanupTypes = new();
        private bool m_disposed;

        public void Subscribe<T>(object owner, Action<T> handler) where T : struct, ISignal
        {
            if (m_disposed || handler == null || owner == null)
            {
                return;
            }
            var type = typeof(T);

            if (!m_subscriptions.TryGetValue(type, out var subscriptions))
            {
                subscriptions = new List<UnitSignalSubscription>();
                m_subscriptions.Add(type, subscriptions);
            }
            foreach (var subscription in subscriptions)
            {
                if (!subscription.Removed && subscription.Handler == (Delegate)handler)
                {
                    return;
                }
            }
            subscriptions.Add(new UnitSignalSubscription(owner, handler));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct, ISignal
        {
            if (m_disposed || handler == null)
            {
                return;
            }

            var type = typeof(T);
            if (!m_subscriptions.TryGetValue(type, out var subscriptions))
            {
                return;
            }

            var publishing = m_publishingTypes.Contains(type);

            for (int i = 0; i < subscriptions.Count; i++)
            {
                var subscription = subscriptions[i];

                if (subscription.Handler != (Delegate)handler)
                {
                    continue;
                }

                if (publishing)
                {
                    subscription.Removed = true;
                    m_delayCleanupTypes.Remove(type);
                }
                else
                {
                    subscriptions.RemoveAt(i);
                }

                break;
            }
        }

        public void RemoveAll(object owner)
        {
            if (m_disposed || owner == null)
            {
                return;
            }

            foreach (var pair in m_subscriptions)
            {
                var type = pair.Key;
                var subscriptions = pair.Value;
                var publishing = m_publishingTypes.Contains(type);

                for (int i = subscriptions.Count - 1; i >= 0; i--)
                {
                    var subscription = subscriptions[i];
                    if (!ReferenceEquals(subscription.Owner, owner))
                    {
                        continue;
                    }

                    if (publishing)
                    {
                        subscription.Removed = true;
                        m_delayCleanupTypes.Remove(type);
                    }
                    else
                    {
                        subscriptions.RemoveAt(i);
                    }
                }
            }
        }
        
        public void Publish<T>(T signal) where T : struct, ISignal
        {
            if (m_disposed)
            {
                return;
            }

            var type = typeof(T);
            if (!m_subscriptions.TryGetValue(type, out var subscriptions))
            {
                return;
            }

            m_publishingTypes.Add(type);
            foreach (var subscription in subscriptions)
            {
                if (subscription.Removed)
                {
                    continue;
                }

                if (subscription.Handler is Action<T> callback)
                {
                    callback(signal);
                }
            }

            m_publishingTypes.RemoveAt(m_publishingTypes.Count - 1);
            Cleanup(type);
        }

        private void Cleanup(Type type)
        {
            if (!m_delayCleanupTypes.Remove(type))
            {
                return;
            }

            if (!m_subscriptions.TryGetValue(type, out var subscriptions))
            {
                return;
            }

            for (var i = subscriptions.Count - 1; i >= 0; i--)
            {
                if (subscriptions[i].Removed)
                {
                    subscriptions.RemoveAt(i);
                }
            }
        }
        
        public void Clear()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            m_subscriptions.Clear();
            m_publishingTypes.Clear();
            m_delayCleanupTypes.Clear();
        }
    }
}