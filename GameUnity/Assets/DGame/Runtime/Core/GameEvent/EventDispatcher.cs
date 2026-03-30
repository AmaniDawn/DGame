using System;
using System.Collections.Generic;

namespace DGame
{
    /// <summary>
    /// 事件分发和注册器
    /// </summary>
    public class EventDispatcher
    {
        private readonly Dictionary<int, EventDelegateData> m_eventTable = new Dictionary<int, EventDelegateData>();

        internal void Destroy()
        {
            m_eventTable.Clear();
        }

        #region AddEventListener/RemoveEventListener

        public bool AddEventListener(int eventId, Delegate callback)
        {
            if (!m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData = new EventDelegateData(eventId);
                m_eventTable.Add(eventId, eventData);
            }
            return eventData.AddHandler(callback);;
        }

        public void RemoveEventListener(int eventId, Delegate callback)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.RemoveHandler(callback);
            }
        }

        #endregion

        #region 事件分发接口

        public void Send(int eventId)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback();
            }
        }

        public void Send<T>(int eventId, T t)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t);
            }
        }

        public void Send<T1, T2>(int eventId, T1 t1, T2 t2)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t1, t2);
            }
        }

        public void Send<T1, T2, T3>(int eventId, T1 t1, T2 t2, T3 t3)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t1, t2, t3);
            }
        }

        public void Send<T1, T2, T3, T4>(int eventId, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t1, t2, t3, t4);
            }
        }

        public void Send<T1, T2, T3, T4, T5>(int eventId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t1, t2, t3, t4, t5);
            }
        }

        public void Send<T1, T2, T3, T4, T5, T6>(int eventId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            if (m_eventTable.TryGetValue(eventId, out var eventData))
            {
                eventData.Callback(t1, t2, t3, t4, t5, t6);
            }
        }

        #endregion
    }
}