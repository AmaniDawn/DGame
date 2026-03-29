using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 事件中心共享内核组件。
    /// </summary>
    public abstract class EventHubCoreComponent : Entity
    {
        private readonly Dictionary<Type, List<EventSubscription>> m_subscriptions = new();
        private readonly Dictionary<Type, int> m_publishDepths = new();
        private readonly HashSet<Type> m_delayCleanupTypes = new();
        private bool m_disposed;

        /// <summary>
        /// 判断指定事件类型当前是否处于发布过程中。
        /// </summary>
        /// <param name="type">事件类型。</param>
        /// <returns>如果正在发布则返回 <see langword="true"/>。</returns>
        private bool IsPublishing(Type type) => m_publishDepths.TryGetValue(type, out var depth) && depth > 0;

        /// <summary>
        /// 进入指定事件类型的发布上下文。
        /// </summary>
        /// <param name="type">事件类型。</param>
        private void EnterPublish(Type type)
        {
            if (m_publishDepths.TryGetValue(type, out var depth))
            {
                m_publishDepths[type] = depth + 1;
                return;
            }

            m_publishDepths.Add(type, 1);
        }

        /// <summary>
        /// 退出指定事件类型的发布上下文。
        /// </summary>
        /// <param name="type">事件类型。</param>
        /// <returns>如果当前已退出最外层发布，则返回 <see langword="true"/>。</returns>
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

        /// <summary>
        /// 注册事件监听。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        protected void InternalSubscribe<T>(object owner, Action<T> handler) where T : struct
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

        /// <summary>
        /// 取消事件监听。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="handler">待移除的事件回调。</param>
        protected void InternalUnsubscribe<T>(Action<T> handler) where T : struct
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

        /// <summary>
        /// 移除指定所属者注册的全部监听。
        /// </summary>
        /// <param name="owner">监听所属者。</param>
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
        
        /// <summary>
        /// 发布事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="signal">事件数据。</param>
        protected void InternalPublish<T>(T signal) where T : struct
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
                    DelayCleanup(type);
                }
            }
        }

        /// <summary>
        /// 对指定事件类型执行延迟清理。
        /// </summary>
        /// <param name="type">事件类型。</param>
        private void DelayCleanup(Type type)
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
        
        /// <summary>
        /// 清空当前事件中心的全部状态。
        /// </summary>
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