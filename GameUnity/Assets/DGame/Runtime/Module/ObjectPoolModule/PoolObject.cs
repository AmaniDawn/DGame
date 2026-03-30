using System;

namespace DGame
{
    internal sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 对象池内部泛型对象 对象池使用
        /// </summary>
        /// <typeparam name="T">对象</typeparam>
        private sealed class PoolObject<T> : IMemory where T : BasePoolObject
        {
            #region 内存池方法

            /// <summary>
            /// 回收到内存池 初始化信息
            /// </summary>
            public void OnRelease()
            {
                m_object = null;
                SpawnCount = 0;
            }

            #endregion

            private T m_object = null;

            /// <summary>
            /// 对象名称
            /// </summary>
            public string Name => m_object.Name;

            /// <summary>
            /// 对象是否加锁
            /// </summary>
            public bool Locked
            {
                get => m_object.Locked; internal set => m_object.Locked = value;
            }

            /// <summary>
            /// 对象优先级
            /// </summary>
            public int Priority
            {
                get => m_object.Priority; internal set => m_object.Priority = value;
            }

            /// <summary>
            /// 自定义释放检查标记
            /// </summary>
            public bool CustomCanReleaseFlag => m_object.CustomCanReleaseFlag;

            public DateTime LastUseTime => m_object.LastUseTime;

            public bool IsUsing => SpawnCount > 0;

            public int SpawnCount { get; private set; } = 0;

            /// <summary>
            /// 从内存池中创建对象
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="spawned"></param>
            /// <returns></returns>
            public static PoolObject<T> Create(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                PoolObject<T> internalObj = MemoryPool.Spawn<PoolObject<T>>();
                internalObj.m_object = obj;
                internalObj.SpawnCount = spawned ? 1 : 0;

                if (spawned)
                {
                    obj.OnSpawn();
                }

                return internalObj;
            }

            /// <summary>
            /// 查看对象
            /// </summary>
            /// <returns></returns>
            public T Peek() => m_object;

            /// <summary>
            /// 从对象池中获取对象
            /// </summary>
            /// <returns></returns>
            public T OnSpawn()
            {
                SpawnCount++;
                m_object.LastUseTime = DateTime.UtcNow;
                m_object.OnSpawn();
                return m_object;
            }

            /// <summary>
            /// 回收对象到对象池
            /// </summary>
            /// <exception cref="DGameException"></exception>
            public void OnRecycle()
            {
                m_object.OnRecycle();
                m_object.LastUseTime = DateTime.UtcNow;
                SpawnCount--;

                if (SpawnCount < 0)
                {
                    throw new DGameException(Utility.StringUtil.Format("对象'{0}'正在使用的计数小于0", Name));
                }
            }

            /// <summary>
            /// 从对象池中释放都对象（回收到内存池）
            /// </summary>
            /// <param name="isDestroy">是否在对象池销毁时触发</param>
            public void ReleaseObj(bool isDestroy)
            {
                m_object.ReleaseObj(isDestroy);
                MemoryPool.Release(m_object);
            }
        }
    }
}