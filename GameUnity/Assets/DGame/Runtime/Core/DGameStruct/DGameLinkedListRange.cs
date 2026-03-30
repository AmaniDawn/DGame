using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DGame
{
    /// <summary>
    /// 链表范围
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct DGameLinkedListRange<T> : IEnumerable<T>, IEnumerable
    {
        private readonly LinkedListNode<T> m_first;
        private readonly LinkedListNode<T> m_last;

        public DGameLinkedListRange(LinkedListNode<T> first, LinkedListNode<T> last)
        {
            if (first == null || last == null || first == last)
            {
                throw new DGameException("链表范围无效");
            }

            m_first = first;
            m_last = last;
        }

        /// <summary>
        /// 获取链表范围是否有效的
        /// </summary>
        public bool IsValid => m_first != null && m_last != null && m_first != m_last;

        public LinkedListNode<T> First => m_first;
        public LinkedListNode<T> Last => m_last;

        public int Count
        {
            get
            {
                if (!IsValid)
                {
                    return 0;
                }

                int count = 0;

                LinkedListNode<T> current = m_first;

                while (current != null && current != m_last)
                {
                    count++;
                    current = current.Next;
                }

                return count;
            }
        }

        public bool Contains(T value)
        {
            LinkedListNode<T> current = m_first;

            while (current != null && current != m_last)
            {
                if (current.Value.Equals(value))
                {
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 循环访问集合的枚举数。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly DGameLinkedListRange<T> m_listRange;
            private LinkedListNode<T> m_current;
            private T m_currentValue;

            internal Enumerator(DGameLinkedListRange<T> range)
            {
                if (!range.IsValid)
                {
                    throw new DGameException("链表范围无效");
                }

                m_listRange = range;
                m_current = range.First;
                m_currentValue = default(T);
            }

            /// <summary>
            /// 当前节点
            /// </summary>
            public T Current => m_currentValue;

            /// <summary>
            /// 当前节点枚举数
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// 清理枚举数
            /// </summary>
            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                if (m_current == null || m_current == m_listRange.Last)
                {
                    return false;
                }

                m_currentValue = m_current.Value;
                m_current = m_current.Next;
                return true;
            }

            void IEnumerator.Reset()
            {
                m_current = m_listRange.First;
                m_currentValue = default(T);
            }
        }
    }
}