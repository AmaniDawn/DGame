using System;
using System.Collections.Generic;

namespace DGame
{
    public class GameEventDriver : MemoryObject
    {
        private class EventRecord
        {
            public int EventID;
            public Delegate Handler;

            public EventRecord(int eventID, Delegate handler)
            {
                EventID = eventID;
                Handler = handler;
            }
        }

        private readonly List<EventRecord> m_eventRecords = new List<EventRecord>();
        private readonly bool m_isInitialized = false;

        public GameEventDriver()
        {
            if (m_isInitialized)
            {
                return;
            }

            m_isInitialized = true;
            m_eventRecords = new List<EventRecord>();
        }

        public override void OnRelease()
        {
            if (!m_isInitialized)
            {
                return;
            }

            for (int i = 0; i < m_eventRecords.Count; i++)
            {
                var record = m_eventRecords[i];
                GameEvent.RemoveEventListener(record.EventID, record.Handler);
            }

            m_eventRecords.Clear();
        }

        #region UIEvent

        public void AddUIEventImp(int eventID, Delegate handler)
        {
            EventRecord record = new EventRecord(eventID, handler);
            m_eventRecords.Add(record);
        }

        public void AddUIEvent(int eventID, Action handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T>(int eventID, Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4, T5, T6>(int eventID, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        #endregion
    }
}