using System.Collections;
using System.Collections.Generic;

namespace DGame
{
    public class DGameDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        protected List<TKey> m_keys = new List<TKey>();
        protected Dictionary<TKey, TValue> m_dict = new Dictionary<TKey, TValue>();

        public List<TKey> Keys => new List<TKey>(m_keys);
        public int Count => m_keys.Count;
        public TValue GetValue(int index) => m_dict[Keys[index]];
        public void SetValue(int index, TValue value) => m_dict[Keys[index]] = value;

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var key in m_keys)
                {
                    yield return m_dict[key];
                }
            }
        }

        public TValue this[TKey key]
        {
            get => m_dict[key];
            set
            {
                if (!ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    m_dict[key] = value;
                }
            }
        }

        // public TValue this[int index]
        // {
        //     get
        //     {
        //         if (index < 0 || index >= m_keys.Count)
        //         {
        //             throw new ArgumentOutOfRangeException();
        //         }
        //         var key = m_keys[index];
        //         return m_dict[key];
        //     }
        //     set
        //     {
        //         if (index < 0 || index >= m_keys.Count)
        //         {
        //             throw new ArgumentOutOfRangeException();
        //         }
        //         var key = m_keys[index];
        //         m_dict[key] = value;
        //     }
        // }

        public void Clear()
        {
            m_keys.Clear();
            m_dict.Clear();
        }

        public virtual void Add(TKey key, TValue value)
        {
            if (m_dict.TryAdd(key, value))
            {
                m_keys.Add(key);
            }
        }

        private bool ContainsKey(TKey key) => m_dict.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => m_dict.TryGetValue(key, out value);

        public TKey GetKey(int index) => m_keys[index];

        public bool Remove(TKey key)
        {
            if (!ContainsKey(key))
            {
                return false;
            }

            return m_dict.Remove(key) && m_keys.Remove(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var key in m_keys)
            {
                yield return new KeyValuePair<TKey, TValue>(key, this[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}