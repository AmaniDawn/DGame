using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DGame
{
    /// <summary>
    /// 多值字典类
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public sealed class
        DGameMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, DGameLinkedListRange<TValue>>>, IEnumerable
    {
        private readonly DGameLinkedList<TValue> m_linkedList = new DGameLinkedList<TValue>();

        private readonly Dictionary<TKey, DGameLinkedListRange<TValue>> m_dictionary =
            new Dictionary<TKey, DGameLinkedListRange<TValue>>();

        public int Count => m_dictionary.Count;

        public DGameLinkedListRange<TValue> this[TKey key]
        {
            get
            {
                m_dictionary.TryGetValue(key, out var range);
                return range;
            }
        }

        public void Clear()
        {
            m_dictionary.Clear();
            m_linkedList.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return m_dictionary.ContainsKey(key);
        }

        public bool ContainsValue(TKey key, TValue value)
        {
            if (m_dictionary.TryGetValue(key, out var range))
            {
                return range.Contains(value);
            }

            return false;
        }

        public bool TryGetValue(TKey key, out DGameLinkedListRange<TValue> value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            if (m_dictionary.TryGetValue(key, out var range))
            {
                m_linkedList.AddBefore(range.Last, value);
            }
            else
            {
                LinkedListNode<TValue> first = m_linkedList.AddLast(value);
                LinkedListNode<TValue> last = m_linkedList.AddLast(default(TValue));
                m_dictionary.Add(key, new DGameLinkedListRange<TValue>(first, last));
            }
        }

        public bool Remove(TKey key, TValue value)
        {
            if (m_dictionary.TryGetValue(key, out var range))
            {
                LinkedListNode<TValue> current = range.First;

                while (current != null && current != range.Last)
                {
                    if (current.Value.Equals(value))
                    {
                        if (current == range.First)
                        {
                            LinkedListNode<TValue> next = current.Next;

                            if (next == range.Last)
                            {
                                m_linkedList.Remove(next);
                                m_dictionary.Remove(key);
                            }
                            else
                            {
                                m_dictionary[key] = new DGameLinkedListRange<TValue>(next, range.Last);
                            }
                        }

                        m_linkedList.Remove(current);
                        return true;
                    }

                    current = current.Next;
                }
            }

            return false;
        }

        public void RemoveAll(TKey key)
        {
            if (m_dictionary.TryGetValue(key, out var range))
            {
                m_dictionary.Remove(key);
                LinkedListNode<TValue> current = range.First;

                while (current != null)
                {
                    LinkedListNode<TValue> next = current != range.Last ? current.Next : null;
                    m_linkedList.Remove(current);
                    current = next;
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_dictionary);
        }

        IEnumerator<KeyValuePair<TKey, DGameLinkedListRange<TValue>>>
            IEnumerable<KeyValuePair<TKey, DGameLinkedListRange<TValue>>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, DGameLinkedListRange<TValue>>>, IEnumerator
        {
            private Dictionary<TKey, DGameLinkedListRange<TValue>>.Enumerator m_enumerator;

            internal Enumerator(Dictionary<TKey, DGameLinkedListRange<TValue>> dictionary)
            {
                if (dictionary == null)
                {
                    throw new DGameException("字典无效");
                }

                m_enumerator = dictionary.GetEnumerator();
            }

            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }

            public void Reset()
            {
                ((IEnumerator<KeyValuePair<TKey, DGameLinkedListRange<TValue>>>)m_enumerator).Reset();
            }

            public KeyValuePair<TKey, DGameLinkedListRange<TValue>> Current => m_enumerator.Current;

            object IEnumerator.Current => m_enumerator.Current;

            public void Dispose()
            {
                m_enumerator.Dispose();
            }
        }
    }
}