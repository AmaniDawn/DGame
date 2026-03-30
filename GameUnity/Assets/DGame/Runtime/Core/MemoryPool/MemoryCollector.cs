using System;
using System.Collections.Generic;

namespace DGame
{
    /// <summary>
    /// 内存池收集器
    /// </summary>
    internal sealed class MemoryCollector
    {
        private readonly Queue<IMemory> m_collector;
        public Type MemoryType { get; private set; }
        public int UsingCount { get; private set; }

        /// <summary>
        /// 当前未在使用中的容量
        /// </summary>
        public int UnusedCount => m_collector == null ? 0 : m_collector.Count;

        /// <summary>
        /// 对象池总容量 = 当前未在使用中的容量 + 正在使用中的数量
        /// </summary>
        public int Capacity => m_collector == null ? 0 : m_collector.Count + UsingCount;

        public int SpawnCount { get; private set; }

        public int ReleaseCount { get; private set; }

        public int AddCount { get; private set; }

        public int RemoveCount { get; private set; }

        #region Constructor

        public MemoryCollector(Type memoryType)
        {
            m_collector = new Queue<IMemory>();
            InitCollector(memoryType);
        }

        public MemoryCollector(Type memoryType, int count)
        {
            m_collector = new Queue<IMemory>(count);
            InitCollector(memoryType);
        }

        private void InitCollector(Type memoryType)
        {
            MemoryType = memoryType;
            UsingCount = 0;
            SpawnCount = 0;
            ReleaseCount = 0;
            AddCount = 0;
            RemoveCount = 0;
        }

        #endregion

        #region Spawn

        public T Spawn<T>() where T : class, IMemory, new()
        {
            if (!typeof(IMemory).IsAssignableFrom(MemoryType))
            {
                throw new DGameException($"内存池类型不匹配，无法取出对象：{MemoryType.Name}");
            }

            UsingCount++;
            SpawnCount++;

            lock (m_collector)
            {
                if (m_collector.Count > 0)
                {
                    return (T)m_collector.Dequeue();
                }
            }

            AddCount++;
            return new T();
        }

        public IMemory Spawn()
        {
            UsingCount++;
            SpawnCount++;
            lock (m_collector)
            {
                if (m_collector.Count > 0)
                {
                    return m_collector.Dequeue();
                }
            }
            AddCount++;
            return Activator.CreateInstance(MemoryType) as IMemory;
        }

        #endregion

        #region Release

        public void Release(IMemory memory)
        {
            memory.OnRelease();
            lock (m_collector)
            {
                if (MemoryPool.EnableStrictCheck && m_collector.Contains(memory))
                {
                    throw new DGameException("内存对象已被释放过");
                }

                m_collector.Enqueue(memory);
            }

            ReleaseCount--;
            UsingCount--;
        }

        #endregion

        #region Add

        public void Add<T>(int count) where T : class, IMemory, new()
        {
            if (!typeof(IMemory).IsAssignableFrom(MemoryType))
            {
                throw new DGameException($"类型不匹配：{typeof(T).Name} != {MemoryType.Name}");
            }

            lock (m_collector)
            {
                AddCount += count;
                while (count-- > 0)
                {
                    m_collector.Enqueue(new T());
                }
            }
        }

        public void Add(int count)
        {
            lock (m_collector)
            {
                AddCount += count;
                while (count-- > 0)
                {
                    m_collector.Enqueue(Activator.CreateInstance(MemoryType) as IMemory);
                }
            }
        }

        #endregion

        #region Remove

        public void Remove(int count)
        {
            lock (m_collector)
            {
                count = m_collector.Count > count ? count : m_collector.Count;
                RemoveCount += count;
                while (count-- > 0)
                {
                    m_collector.Dequeue();
                }
            }
        }

        #endregion

        public void Clear()
        {
            lock (m_collector)
            {
                RemoveCount += m_collector.Count;
                m_collector.Clear();
            }
        }
    }
}