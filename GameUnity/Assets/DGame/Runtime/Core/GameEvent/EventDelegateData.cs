using System;
using System.Collections.Generic;

namespace DGame
{
    internal class EventDelegateData
    {
        private readonly int m_eventID = 0;
        private readonly List<Delegate> m_existList = new List<Delegate>();
        private readonly List<Delegate> m_removeList = new List<Delegate>();
        private readonly List<Delegate> m_addList = new List<Delegate>();
        private bool m_isExecute = false;
        private bool m_isDirty = false;

        internal EventDelegateData(int eventID)
        {
            m_eventID = eventID;
        }

        internal bool AddHandler(Delegate handler)
        {
            if (m_existList.Contains(handler))
            {
                DLogger.Fatal("重复添加事件");
                return false;
            }

            if (m_isExecute)
            {
                m_isDirty = true;
                m_addList.Add(handler);
            }
            else
            {
                m_existList.Add(handler);
            }
            return true;
        }

        internal bool RemoveHandler(Delegate handler)
        {
            if (m_isExecute)
            {
                m_isDirty = true;
                m_removeList.Add(handler);
            }
            else
            {
                if (!m_existList.Contains(handler))
                {
                    DLogger.Fatal("删除事件失败，事件不存在，请检查事件ID：{0}", StringId.HashToString(m_eventID));
                    return false;
                }
                m_existList.Remove(handler);
            }
            return true;
        }

        private void CheckDataDirty()
        {
            m_isExecute = false;

            if (m_isDirty)
            {
                for (int i = 0; i < m_addList.Count; i++)
                {
                    m_existList.Add(m_addList[i]);
                }
                m_addList.Clear();

                for (int i = 0; i < m_removeList.Count; i++)
                {
                    m_existList.Remove(m_removeList[i]);
                }
                m_removeList.Clear();
                m_isDirty = false;
            }
        }

        #region Callback

        public void Callback()
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action action)
                {
                    action();
                }
            }

            CheckDataDirty();
        }

        public void Callback<T>(T t)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T> action)
                {
                    action(t);
                }
            }

            CheckDataDirty();
        }

        public void Callback<T1, T2>(T1 t1, T2 t2)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T1, T2> action)
                {
                    action(t1, t2);
                }
            }

            CheckDataDirty();
        }

        public void Callback<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T1, T2, T3> action)
                {
                    action(t1, t2, t3);
                }
            }

            CheckDataDirty();
        }

        public void Callback<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T1, T2, T3, T4> action)
                {
                    action(t1, t2, t3, t4);
                }
            }

            CheckDataDirty();
        }

        public void Callback<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T1, T2, T3, T4, T5> action)
                {
                    action(t1, t2, t3, t4, t5);
                }
            }

            CheckDataDirty();
        }

        public void Callback<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            m_isExecute = true;

            for (int i = 0; i < m_existList.Count; i++)
            {
                var handler = m_existList[i];

                if (handler is Action<T1, T2, T3, T4, T5, T6> action)
                {
                    action(t1, t2, t3, t4, t5, t6);
                }
            }

            CheckDataDirty();
        }

        #endregion
    }
}