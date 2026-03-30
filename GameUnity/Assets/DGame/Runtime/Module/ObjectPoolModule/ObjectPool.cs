using System;
using System.Collections.Generic;

namespace DGame
{
    internal sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 内部泛型对象池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class ObjectPool<T> : BaseObjectPool, IObjectPool<T> where T : BasePoolObject
        {
            private static string EMPTY_NAME = string.Empty;
            /// <summary>
            /// key -> 对象名称 value -> 内部对象池对象
            /// </summary>
            private readonly DGameMultiDictionary<string, PoolObject<T>> m_poolObjects;

            /// <summary>
            /// key -> 资源对象 value -> 内部对象池对象
            /// </summary>
            private readonly Dictionary<object, PoolObject<T>> m_poolObjectsMap;

            private readonly ReleaseObjectFilterCallback<T> m_defaultReleaseObjectFilterCallback;
            private readonly List<T> m_cachedCanReleaseObjects;
            private readonly List<T> m_cachedToReleaseObjects;
            private readonly bool m_allowMultiSpawn;
            private int m_capacity;
            private float m_expireTime;
            private float m_autoReleaseTime;

            /// <summary>
            /// 对象池对象类型
            /// </summary>
            public override Type ObjectType => typeof(T);

            /// <summary>
            /// 对象池中对象数量
            /// </summary>
            public override int Count => m_poolObjectsMap.Count;

            public override int CanReleaseCount
            {
                get
                {
                    GetCanReleaseObjects(m_cachedCanReleaseObjects);
                    return m_cachedCanReleaseObjects.Count;
                }
            }

            public override bool AllowMultiSpawn => m_allowMultiSpawn;
            public override float AutoReleaseInterval { get; set; }

            public override int Capacity
            {
                get => m_capacity;
                set
                {
                    if (value < 0)
                    {
                        throw new DGameException("对象池容量异常：小于0");
                    }

                    if (value == m_capacity)
                    {
                        return;
                    }

                    m_capacity = value;
                    Release();
                }
            }

            public override float ExpireTime
            {
                get => m_expireTime;
                set
                {
                    if (value < 0f)
                    {
                        throw new DGameException("无效的过期时间：小于0");
                    }

                    if (Math.Abs(ExpireTime - value) < 0.01f)
                    {
                        return;
                    }

                    m_expireTime = value;
                    Release();
                }
            }

            public override int Priority { get; set; }

            public ObjectPool(string name, bool allowMultiSpawn, float autoReleaseInterval, int capacity,
                float expireTime, int priority) : base(name)
            {
                m_poolObjects = new DGameMultiDictionary<string, PoolObject<T>>();
                m_poolObjectsMap = new Dictionary<object, PoolObject<T>>();
                m_defaultReleaseObjectFilterCallback = DefaultReleaseObjectFilterCallback;
                m_cachedCanReleaseObjects = new List<T>();
                m_cachedToReleaseObjects = new List<T>();
                m_allowMultiSpawn = allowMultiSpawn;
                AutoReleaseInterval = autoReleaseInterval;
                Capacity = capacity;
                ExpireTime = expireTime;
                Priority = priority;
                m_autoReleaseTime = 0f;
            }

            /// <summary>
            /// 注册对象
            /// </summary>
            /// <param name="obj">对象</param>
            /// <param name="spawned">是否已在使用</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new DGameException("注册的对象无效");
                }

                PoolObject<T> poolObject = PoolObject<T>.Create(obj, spawned);
                m_poolObjects.Add(obj.Name, poolObject);
                m_poolObjectsMap.Add(obj.Target, poolObject);

                if (Count > m_capacity)
                {
                    Release();
                }
            }

            public bool CanSpawn()
            {
                return CanSpawn(EMPTY_NAME);
            }

            public bool CanSpawn(string name)
            {
                if (name == null)
                {
                    throw new DGameException("对象名称无效");
                }

                if (m_poolObjects.TryGetValue(name, out var objectRange))
                {
                    foreach (var poolObject in objectRange)
                    {
                        // 对象如果正在被使用则需要判断是否可以被多次获取
                        if (m_allowMultiSpawn || !poolObject.IsUsing)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public T Spawn()
            {
                return Spawn(EMPTY_NAME);
            }

            public T Spawn(string name)
            {
                if (name == null)
                {
                    throw new DGameException("对象名称无效");
                }

                if (m_poolObjects.TryGetValue(name, out var objectRange))
                {
                    foreach (var poolObject in objectRange)
                    {
                        // 对象如果正在被使用则需要判断是否可以被多次获取
                        if (m_allowMultiSpawn || !poolObject.IsUsing)
                        {
                            return poolObject.OnSpawn();
                        }
                    }
                }

                return null;
            }

            public void Recycle(T obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                Recycle(obj.Target);
            }

            public void Recycle(object obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                // 尝试从对象池中获取
                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.OnRecycle();

                    if (Count > m_capacity && poolObject.SpawnCount <= 0)
                    {
                        Release();
                    }
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public void SetLocked(T obj, bool locked)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                SetLocked(obj.Target, locked);
            }

            public void SetLocked(object obj, bool locked)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.Locked = locked;
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public void SetPriority(T obj, int priority)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                SetPriority(obj.Target, priority);
            }

            public void SetPriority(object obj, int priority)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.Priority = priority;
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public bool ReleaseObject(T obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                return ReleaseObject(obj.Target);
            }

            public bool ReleaseObject(object obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject == null)
                {
                    return false;
                }

                if (poolObject.IsUsing || poolObject.Locked || !poolObject.CustomCanReleaseFlag)
                {
                    return false;
                }

                m_poolObjects.Remove(poolObject.Name, poolObject);
                m_poolObjectsMap.Remove(poolObject.Peek().Target);
                poolObject.ReleaseObj(false);
                MemoryPool.Release(poolObject);
                return true;
            }

            public override void Release()
            {
                Release(Count - m_capacity, m_defaultReleaseObjectFilterCallback);
            }

            public override void Release(int releaseCnt)
            {
                Release(releaseCnt, m_defaultReleaseObjectFilterCallback);
            }

            public void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                Release(Count - m_capacity, releaseObjectFilterCallback);
            }

            public void Release(int releaseCnt,
                ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                if (releaseObjectFilterCallback == null)
                {
                    throw new DGameException("释放对象池对象的过滤回调函数无效");
                }

                if (releaseCnt < 0)
                {
                    releaseCnt = 0;
                }

                DateTime expireTime = DateTime.MinValue;

                if (m_expireTime < float.MaxValue)
                {
                    // 计算过期时间
                    expireTime = DateTime.UtcNow.AddSeconds(-m_expireTime);
                }

                m_autoReleaseTime = 0.0f;
                GetCanReleaseObjects(m_cachedCanReleaseObjects);
                List<T> toReleaseObjs = releaseObjectFilterCallback(m_cachedCanReleaseObjects, releaseCnt, expireTime);

                if (toReleaseObjs == null || toReleaseObjs.Count <= 0)
                {
                    return;
                }

                foreach (var releaseObj in toReleaseObjs)
                {
                    ReleaseObject(releaseObj);
                }
            }

            public override void ReleaseAllUnused()
            {
                m_autoReleaseTime = 0.0f;
                GetCanReleaseObjects(m_cachedCanReleaseObjects);

                foreach (var recycleObject in m_cachedCanReleaseObjects)
                {
                    ReleaseObject(recycleObject);
                }
            }

            internal override void Update(float elapsedSeconds, float realElapseSeconds)
            {
                m_autoReleaseTime += realElapseSeconds;

                if (m_autoReleaseTime < AutoReleaseInterval)
                {
                    return;
                }

                Release();
            }

            public override PoolObjectInfo[] GetAllPoolObjectInfos()
            {
                List<PoolObjectInfo> poolObjectInfos = new List<PoolObjectInfo>();

                foreach (var objRanges in m_poolObjects)
                {
                    foreach (var poolObject in objRanges.Value)
                    {
                        poolObjectInfos.Add(new PoolObjectInfo(poolObject.Name, poolObject.Locked,
                            poolObject.CustomCanReleaseFlag, poolObject.Priority, poolObject.LastUseTime,
                            poolObject.SpawnCount));
                    }
                }

                return poolObjectInfos.ToArray();
            }

            internal override void Destroy()
            {
                foreach (var poolObj in m_poolObjectsMap.Values)
                {
                    poolObj.ReleaseObj(true);
                    MemoryPool.Release(poolObj);
                }

                m_poolObjectsMap.Clear();
                m_poolObjects.Clear();
                m_cachedCanReleaseObjects.Clear();
                m_cachedToReleaseObjects.Clear();
            }

            private PoolObject<T> GetPoolObject(object target)
            {
                if (target == null)
                {
                    throw new DGameException("目标对象无效");
                }

                return m_poolObjectsMap.GetValueOrDefault(target);
            }

            private void GetCanReleaseObjects(List<T> result)
            {
                if (result == null)
                {
                    throw new DGameException("传入的列表参数无效");
                }

                result.Clear();

                foreach (var poolObject in m_poolObjectsMap.Values)
                {
                    if (poolObject.IsUsing || poolObject.Locked || !poolObject.CustomCanReleaseFlag)
                    {
                        continue;
                    }

                    result.Add(poolObject.Peek());
                }
            }

            private List<T> DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount,
                DateTime expireTime)
            {
                m_cachedToReleaseObjects.Clear();

                // 检查是否有设置过期时间
                if (expireTime > DateTime.MinValue)
                {
                    for (int i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        var poolObject = candidateObjects[i];

                        // 对象最后使用时间 <= 过期时间阈值
                        if (poolObject.LastUseTime <= expireTime)
                        {
                            m_cachedToReleaseObjects.Add(poolObject);
                            candidateObjects.RemoveAt(i);
                        }
                    }

                    toReleaseCount -= m_cachedToReleaseObjects.Count;
                }

                candidateObjects.Sort(SortCandidateObjects);

                for (int i = 0; i < candidateObjects.Count && toReleaseCount > 0; i++)
                {
                    m_cachedToReleaseObjects.Add(candidateObjects[i]);
                    toReleaseCount--;
                }

                return m_cachedToReleaseObjects;
            }

            private int SortCandidateObjects(T a, T b)
            {
                int priorityCompare = b.Priority.CompareTo(a.Priority);
                return priorityCompare != 0 ? priorityCompare : a.LastUseTime.CompareTo(b.LastUseTime);
            }
        }
    }
}