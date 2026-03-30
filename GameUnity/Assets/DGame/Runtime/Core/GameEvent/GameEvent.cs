using System;

namespace DGame
{
    public class GameEvent
    {
        public static EventMgr EventMgr { get; } = new EventMgr();

        #region AddEventListener

        public static bool AddEventListener(int eventID, Action handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T>(int eventID, Action<T> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4, T5, T6>(int eventID, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        #endregion

        #region RemoveEventListener

        public static void RemoveEventListener(int eventID, Delegate handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener(int eventID, Action handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T>(int eventID, Action<T> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4, T5, T6>(int eventID,
            Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        #endregion

        #region 分发消息接口

        public static T Get<T>()
        {
            return EventMgr.GetInterface<T>();
        }

        public static void Send(string eventType)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType));
        }

        public static void Send<T>(string eventType, T t)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), t);
        }

        public static void Send<T1, T2>(string eventType, T1 t1, T2 t2)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), t1, t2);
        }

        public static void Send<T1, T2, T3>(string eventType, T1 t1, T2 t2, T3 t3)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), t1, t2, t3);
        }

        public static void Send<T1, T2, T3, T4>(string eventType, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), t1, t2, t3, t4);
        }

        public static void Send<T1, T2, T3, T4, T5>(string eventType, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), t1, t2, t3, t4, t5);
        }

        public static void Send<T1, T2, T3, T4, T5, T6>(int eventType, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            EventMgr.Dispatcher.Send(eventType, t1, t2, t3, t4, t5, t6);
        }

        public static void Send(string eventType, Delegate handler)
        {
            EventMgr.Dispatcher.Send(StringId.StringToHash(eventType), handler);
        }

        #endregion

        public void Destroy()
        {
            EventMgr?.Destroy();
        }
    }
}