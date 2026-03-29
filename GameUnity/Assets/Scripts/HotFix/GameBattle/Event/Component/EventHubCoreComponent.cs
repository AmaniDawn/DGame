using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    public abstract class EventHubCoreComponent : Entity
    {
        private readonly Dictionary<Type, List<EventSubscription>> m_subscriptions = new();
        private readonly Dictionary<Type, int> m_publishDepths = new();
        private readonly HashSet<Type> m_delayCleanupTypes = new();
        private bool m_disposed;

        private bool IsPublishing(Type type) => m_publishDepths.TryGetValue(type, out var depth) && depth > 0;

        private void EnterPublish(Type type)
        {
            if (m_publishDepths.TryGetValue(type, out var depth))
            {
                m_publishDepths[type] = depth + 1;
                return;
            }

            m_publishDepths.Add(type, 1);
        }

        private bool ExitPublish(Type type)
        {
            if (!m_publishDepths.TryGetValue(type, out var depth))
            {
                return true;
            }

            if (depth <= 1)
            {
                m_publishDepths.Remove(type);
                return true;
            }

            m_publishDepths[type] = depth - 1;
            return false;
        }

        protected void SubscribeCore<T>(object owner, Action<T> handler) where T : struct
        {
            if (m_disposed || handler == null || owner == null)
            {
                return;
            }
            var type = typeof(T);

            if (!m_subscriptions.TryGetValue(type, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                m_subscriptions.Add(type, subscriptions);
            }
            foreach (var subscription in subscriptions)
            {
                if (!subscription.Removed && subscription.Handler == (Delegate)handler)
                {
                    return;
                }
            }
            subscriptions.Add(new EventSubscription(owner, handler));
        }

        protected void UnsubscribeCore<T>(Action<T> handler) where T : struct
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

            var publishing = IsPublishing(type);

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
                    m_delayCleanupTypes.Add(type);
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
                var publishing = IsPublishing(type);

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
                        m_delayCleanupTypes.Add(type);
                    }
                    else
                    {
                        subscriptions.RemoveAt(i);
                    }
                }
            }
        }
        
        protected void PublishCore<T>(T signal) where T : struct
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

            EnterPublish(type);

            try
            {
                var count = subscriptions.Count;
                for (int i = 0; i < count; i++)
                {
                    var subscription = subscriptions[i];
                    if (subscription.Removed)
                    {
                        continue;
                    }

                    if (subscription.Handler is Action<T> callback)
                    {
                        callback(signal);
                    }
                }
            }
            finally
            {
                if (ExitPublish(type))
                {
                    Cleanup(type);
                }
            }
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
            m_publishDepths.Clear();
            m_delayCleanupTypes.Clear();
        }
    }
}