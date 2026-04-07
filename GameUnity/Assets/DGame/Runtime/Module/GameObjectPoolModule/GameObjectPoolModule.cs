using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    internal sealed class GameObjectPoolModule : Module, IGameObjectPoolModule, IUpdateModule
    {
        private sealed class PoolCreateLock
        {
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public int RefCount;

            public void Dispose() => Semaphore.Dispose();
        }

        private readonly Dictionary<string, GameObjectPool> m_poolDict
            = new Dictionary<string, GameObjectPool>(100);
        private readonly Dictionary<string, PoolCreateLock> m_poolCreateLocks
            = new Dictionary<string, PoolCreateLock>(100);
        private readonly List<string> m_removeList = new List<string>(100);
        private bool m_isShuttingDown;

        public GameObject PoolRoot { get; private set; }

        public override void OnCreate()
        {
            m_isShuttingDown = false;
            if (PoolRoot == null)
            {
                PoolRoot = new GameObject("[GAME_OBJECT_POOL_ROOT]");
            }
        }

        public override void OnDestroy()
        {
            m_isShuttingDown = true;
            DestroyAllPool(true);

            if (PoolRoot != null)
            {
                UnityEngine.Object.Destroy(PoolRoot);
                PoolRoot = null;
            }
        }

        public async UniTask<GameObjectPool> CreateGameObjectPoolAsync(string location,
            int initCapacity = 0, int maxCapacity = Int32.MaxValue, float autoDestroyTime = -1,
            bool dontDestroy = false, bool allowMultiSpawn = false, CancellationToken ct = default)
        {
            var pool = await GetOrCreatePoolAsync(location, initCapacity, maxCapacity, autoDestroyTime,
                dontDestroy, allowMultiSpawn, ct, true);
            if (pool == null)
            {
                DLogger.Warning($"对象池创建失败或已被标记销毁: {location}");
            }

            return pool;
        }

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="ct">取消令牌</param>
        public async UniTask<GameObject> SpawnAsync(string location, CancellationToken ct = default)
            => await SpawnInternalAsync(location, null, Vector3.zero, Quaternion.identity, ct);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="ct">取消令牌</param>
        public async UniTask<GameObject> SpawnAsync(string location, Transform parent, CancellationToken ct = default)
            => await SpawnInternalAsync(location, parent, Vector3.zero, Quaternion.identity, ct);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="position">本地坐标</param>
        /// <param name="rotation">本地角度</param>
        /// <param name="ct">取消令牌</param>
        public async UniTask<GameObject> SpawnAsync(string location, Transform parent, Vector3 position,
            Quaternion rotation, CancellationToken ct = default)
            => await SpawnInternalAsync(location, parent, position, rotation, ct);

        public void Recycle(GameObject gameObject)
        {
            if (TryResolvePool(gameObject, out var pool))
            {
                pool.Recycle(gameObject);
            }
            else
            {
                DLogger.Warning($"对象不属于有效对象池，忽略回收: {gameObject?.name}");
            }
        }

        public void Remove(GameObject gameObject)
        {
            if (TryResolvePool(gameObject, out var pool))
            {
                pool.Remove(gameObject);
            }
            else
            {
                DLogger.Warning($"对象不属于有效对象池，忽略回收: {gameObject?.name}");
            }
        }

        private async UniTask<GameObject> SpawnInternalAsync(string location, Transform parent,
            Vector3 position, Quaternion rotation, CancellationToken ct)
        {
            if (m_isShuttingDown)
            {
                return null;
            }

            var pool = await GetOrCreatePoolAsync(location, 0, Int32.MaxValue,
                -1f, false, false, ct, false);
            if (pool == null || pool.MarkedForDestroy || pool.IsDestroyed)
            {
                return null;
            }

            return await pool.SpawnAsync(parent, position, rotation, ct);
        }

        private async UniTask<GameObjectPool> GetOrCreatePoolAsync(string location, int initCapacity,
            int maxCapacity, float autoDestroyTime, bool dontDestroy, bool allowMultiSpawn,
            CancellationToken ct, bool applyConfiguration)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("对象池 location 无效");
            }

            if (m_isShuttingDown)
            {
                return null;
            }

            if (!applyConfiguration && TryGetGameObjectPool(location, out var pool))
            {
                return pool.MarkedForDestroy || pool.IsDestroyed ? null : pool;
            }

            var poolCreateLock = AcquireCreateLock(location);
            var lockTaken = false;
            try
            {
                await poolCreateLock.Semaphore.WaitAsync(ct);
                lockTaken = true;

                if (m_isShuttingDown || PoolRoot == null)
                {
                    return null;
                }

                if (TryGetGameObjectPool(location, out pool))
                {
                    if (applyConfiguration && !pool.MarkedForDestroy && !pool.IsDestroyed)
                    {
                        var success = await pool.ConfigureAsync(initCapacity, maxCapacity, autoDestroyTime, dontDestroy,
                            allowMultiSpawn, ct);
                        if (!success)
                        {
                            DLogger.Warning($"对象池重配置失败，保留旧池配置: {location}");
                            return null;
                        }
                    }

                    return pool.MarkedForDestroy || pool.IsDestroyed ? null : pool;
                }

                pool = GameObjectPool.Create(PoolRoot.transform, location, initCapacity,
                    maxCapacity, autoDestroyTime, dontDestroy, allowMultiSpawn);

                try
                {
                    var success = await pool.CreatePoolAsync(ct);
                    if (!success)
                    {
                        DLogger.Warning($"对象池预热失败，创建对象池失败: {location}");
                        pool.Destroy();
                        return null;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        DLogger.Warning($"对象池创建被取消: {location}");
                        pool.Destroy();
                        return null;
                    }

                    if (pool.IsDestroyed || pool.MarkedForDestroy)
                    {
                        DLogger.Warning($"对象池创建过程中池已失效: {location}");
                        pool.Destroy();
                        return null;
                    }

                    m_poolDict[location] = pool;
                    return pool;
                }
                catch
                {
                    pool.Destroy();
                    return null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    poolCreateLock.Semaphore.Release();
                }

                ReleaseCreateLock(location);
            }
        }

        private PoolCreateLock AcquireCreateLock(string location)
        {
            lock (m_poolCreateLocks)
            {
                if (!m_poolCreateLocks.TryGetValue(location, out var poolCreateLock))
                {
                    poolCreateLock = new PoolCreateLock();
                    m_poolCreateLocks.Add(location, poolCreateLock);
                }

                poolCreateLock.RefCount++;
                return poolCreateLock;
            }
        }

        private void ReleaseCreateLock(string location)
        {
            lock (m_poolCreateLocks)
            {
                if (!m_poolCreateLocks.TryGetValue(location, out var poolCreateLock))
                {
                    return;
                }

                poolCreateLock.RefCount--;
                if (poolCreateLock.RefCount <= 0)
                {
                    m_poolCreateLocks.Remove(location);
                    poolCreateLock.Dispose();
                }
            }
        }

        private void ClearIdleCreateLocks()
        {
            List<string> removeKeys = new List<string>();
            lock (m_poolCreateLocks)
            {
                foreach (var item in m_poolCreateLocks)
                {
                    if (item.Value.RefCount > 0)
                    {
                        continue;
                    }

                    removeKeys.Add(item.Key);
                }

                foreach (var key in removeKeys)
                {
                    if (!m_poolCreateLocks.Remove(key, out var poolCreateLock))
                    {
                        continue;
                    }

                    poolCreateLock.Dispose();
                }
            }
        }

        private bool TryResolvePool(GameObject gameObject, out GameObjectPool pool)
        {
            pool = null;
            if (gameObject == null)
            {
                return false;
            }

            if (gameObject.TryGetComponent<GameObjectPoolIdentity>(out var identity)
                && !string.IsNullOrEmpty(identity.PoolKey))
            {
                return TryGetGameObjectPool(identity.PoolKey, out pool);
            }

            return false;
        }

        public GameObjectPool GetGameObjectPool(string location)
            => m_poolDict.GetValueOrDefault(location, null);

        public bool TryGetGameObjectPool(string location, out GameObjectPool pool)
            => m_poolDict.TryGetValue(location, out pool);

        public void DestroyPool(string location)
        {
            if (TryGetGameObjectPool(location, out var pool))
            {
                pool.MarkedForDestroy = true;
            }
        }

        public void DestroyAllPool(bool includeAll)
        {
            m_removeList.Clear();

            if (includeAll)
            {
                foreach (var pool in m_poolDict.Values)
                {
                    pool.Destroy();
                }
                m_poolDict.Clear();
                ClearIdleCreateLocks();
            }
            else
            {
                foreach (var item in m_poolDict)
                {
                    if (!item.Value.DontDestroy)
                    {
                        item.Value.MarkedForDestroy = true;
                        m_removeList.Add(item.Key);
                    }
                }

                foreach (var poolKey in m_removeList)
                {
                    if (!TryGetGameObjectPool(poolKey, out var pool))
                    {
                        continue;
                    }

                    if (pool.SpawnedCount <= 0)
                    {
                        pool.Destroy();
                        m_poolDict.Remove(poolKey);
                    }
                }
            }
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_removeList.Clear();

            foreach (var item in m_poolDict)
            {
                if (item.Value.CanAutoDestroy())
                {
                    m_removeList.Add(item.Key);
                }
            }

            foreach (var poolKey in m_removeList)
            {
                if (!TryGetGameObjectPool(poolKey, out var pool))
                {
                    continue;
                }

                pool.Destroy();
                m_poolDict.Remove(poolKey);
            }
        }
    }
}