using System;
using System.Collections.Generic;

namespace DGame
{
    public static partial class MemoryPool
    {
        private static readonly Dictionary<Type, MemoryCollector> m_memoryCollectorPool = new Dictionary<Type, MemoryCollector>();

        /// <summary>
        /// 强制检查开关
        /// </summary>
        public static bool EnableStrictCheck { get; set; } = false;

        /// <summary>
        /// 获取内存收集对象数量
        /// </summary>
        public static int Capacity => m_memoryCollectorPool != null ? m_memoryCollectorPool.Count : 0;

        /// <summary>
        /// 获取所有的内存收集器信息
        /// </summary>
        /// <returns></returns>
        public static MemoryCollectorInfo[] GetAllMemoryCollectorInfos()
        {
            int index = 0;
            MemoryCollectorInfo[] results;

            lock (m_memoryCollectorPool)
            {
                results = new MemoryCollectorInfo[m_memoryCollectorPool.Count];

                foreach (var memoryCollector in m_memoryCollectorPool.Values)
                {
                    results[index++] = new MemoryCollectorInfo(memoryCollector.MemoryType, memoryCollector.UnusedCount,
                        memoryCollector.UsingCount, memoryCollector.SpawnCount, memoryCollector.ReleaseCount, memoryCollector.AddCount,
                        memoryCollector.RemoveCount, memoryCollector.Capacity);
                }
            }
            return results;
        }

        /// <summary>
        /// 清空所有的内存池
        /// </summary>
        public static void ClearAll()
        {
            lock (m_memoryCollectorPool)
            {
                foreach (var memoryCollector in m_memoryCollectorPool.Values)
                {
                    memoryCollector.Clear();
                }
                m_memoryCollectorPool.Clear();
            }
        }

        #region Spawn

        /// <summary>
        /// 从内存池中获取内存对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Spawn<T>() where T : class, IMemory, new()
            => GetMemoryCollector(typeof(T))?.Spawn<T>();

        /// <summary>
        /// 从内存池中获取内存对象
        /// </summary>
        /// <param name="memoryType"></param>
        /// <returns></returns>
        public static IMemory Spawn(Type memoryType)
        {
            InternalCheckMemoryTypeIsValid(memoryType);
            return GetMemoryCollector(memoryType)?.Spawn();
        }

        #endregion

        #region Recycle

        /// <summary>
        /// 将内存对象归还内存池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="DGameException"></exception>
        public static void Release<T>(List<T> memories) where T : class, IMemory
        {
            if (memories == null || memories.Count <= 0)
            {
                return;
            }
            var memoryType = typeof(T);
            var memoryCollector = GetMemoryCollector(memoryType);
            if (memoryCollector != null)
            {
                for (int i = 0; i < memories.Count; i++)
                {
                    memoryCollector.Release(memories[i]);
                }
            }
        }

        /// <summary>
        /// 将内存对象归还内存池
        /// </summary>
        /// <param name="memory"></param>
        /// <exception cref="DGameException"></exception>
        public static void Release(IMemory memory)
        {
            if (memory == null)
            {
                throw new DGameException("内存对象类型无效");
            }

            Type memoryType = memory.GetType();
            InternalCheckMemoryTypeIsValid(memoryType);
            GetMemoryCollector(memoryType)?.Release(memory);
        }

        #endregion

        #region Add MemoryCollector Obj Count

        /// <summary>
        /// 添加内存收集器的对象数量
        /// </summary>
        /// <param name="memoryType"></param>
        /// <param name="count"></param>
        public static void Add(Type memoryType, int count)
        {
            InternalCheckMemoryTypeIsValid(memoryType);
            GetMemoryCollector(memoryType)?.Add(count);
        }

        /// <summary>
        /// 添加内存收集器的对象数量
        /// </summary>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        public static void Add<T>(int count) where T : class, IMemory, new()
            => GetMemoryCollector(typeof(T))?.Add<T>(count);

        #endregion

        #region Remove MemoryCollector Obj Count

        /// <summary>
        /// 删除内存收集器的对象数量
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="count"></param>
        public static void Remove(Type classType, int count)
        {
            InternalCheckMemoryTypeIsValid(classType);
            GetMemoryCollector(classType)?.Remove(count);
        }

        /// <summary>
        /// 删除内存收集器的对象数量
        /// </summary>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        public static void Remove<T>(int count) where T : class, IMemory
            => GetMemoryCollector(typeof(T))?.Remove(count);

        #endregion

        #region Clear MemoryCollector

        /// <summary>
        /// 清空内存收集的内存对象
        /// </summary>
        /// <param name="memoryType">对象类型</param>
        public static void ClearMemoryCollector(Type memoryType)
        {
            InternalCheckMemoryTypeIsValid(memoryType);
            GetMemoryCollector(memoryType)?.Clear();
        }

        /// <summary>
        /// 清空内存收集的内存对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        public static void ClearMemoryCollector<T>() where T : class, IMemory
            => GetMemoryCollector(typeof(T))?.Clear();

        #endregion

        /// <summary>
        /// 内部检查内存池对象类型是否有效
        /// </summary>
        /// <param name="memoryType"></param>
        /// <exception cref="DGameException"></exception>
        private static void InternalCheckMemoryTypeIsValid(Type memoryType)
        {
            if (!EnableStrictCheck)
            {
                return;
            }

            if (memoryType == null)
            {
                throw new DGameException("内存池对象类型无效");
            }

            if (!memoryType.IsClass || memoryType.IsAbstract)
            {
                throw new DGameException("传入的内存池对象类型不能是抽象的或不是class类型");
            }

            if (!typeof(IMemory).IsAssignableFrom(memoryType))
            {
                throw new DGameException($"内存池对象类型{memoryType.FullName}无效");
            }
        }

        /// <summary>
        /// 从内存池中获取内存收集器
        /// </summary>
        /// <param name="memoryType">内存对象类型</param>
        /// <returns>内存收集器</returns>
        /// <exception cref="DGameException"></exception>
        private static MemoryCollector GetMemoryCollector(Type memoryType)
        {
            if (memoryType == null)
            {
                throw new DGameException("传入的内存池对象类型为空");
            }

            MemoryCollector memoryCollector = null;

            lock (m_memoryCollectorPool)
            {
                if (!m_memoryCollectorPool.TryGetValue(memoryType, out memoryCollector))
                {
                    memoryCollector = new MemoryCollector(memoryType);
                    m_memoryCollectorPool.Add(memoryType, memoryCollector);
                }
            }

            return memoryCollector;
        }
    }
}