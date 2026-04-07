using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    internal sealed class GameObjectPoolModule : Module, IGameObjectPoolModule, IUpdateModule
    {
        private readonly Dictionary<string, GameObjectPool> m_poolDict
            = new Dictionary<string, GameObjectPool>(100);
        private readonly List<string> m_removeList = new List<string>(100);

        public GameObject PoolRoot { get; private set; }

        public override void OnCreate()
        {
            if (PoolRoot == null)
            {
                PoolRoot = new GameObject("[GAME_OBJECT_POOL_ROOT]");
            }
        }

        public override void OnDestroy() => DestroyAllPool(true);

        public async UniTask<GameObjectPool> CreateGameObjectPoolAsync(string location,
            int initCapacity = 0, int maxCapacity = Int32.MaxValue, float autoDestroyTime = -1,
            bool dontDestroy = false, bool allowMultiSpawn = false, CancellationToken ct = default)
            => await CreateGameObjectPoolAsyncInternal(location, initCapacity, maxCapacity,
                autoDestroyTime, dontDestroy, allowMultiSpawn, ct);

        private async UniTask<GameObjectPool> CreateGameObjectPoolAsyncInternal(string location,
            int initCapacity, int maxCapacity, float autoDestroyTime,
            bool dontDestroy, bool allowMultiSpawn, CancellationToken ct)
        {
            if (maxCapacity < initCapacity)
            {
                throw new DGameException("The max capacity value must be greater the init capacity value.");
            }

            GameObjectPool pool = GetGameObjectPool(location);

            if (pool == null)
            {
                pool = GameObjectPool.Create(PoolRoot.transform, location, initCapacity,
                    maxCapacity, autoDestroyTime, dontDestroy, allowMultiSpawn);
                await pool.CreatePoolAsync(ct);
                if (ct.IsCancellationRequested || pool.IsDestroyed)
                {
                    pool.Destroy();
                    return null;
                }

                m_poolDict[location] = pool;
            }
            else
            {
                DLogger.Warning($"对象池重复创建: {location}");
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
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">世界角度</param>
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
                DLogger.Warning($"没有找到该对象的对象池: {gameObject.name}");
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
                DLogger.Warning($"没有找到该对象的对象池: {gameObject.name}");
            }
        }

        private async UniTask<GameObject> SpawnInternalAsync(string location, Transform parent,
            Vector3 position, Quaternion rotation, CancellationToken ct)
        {
            var pool = GetGameObjectPool(location);

            if (pool == null)
            {
                pool = GameObjectPool.Create(PoolRoot.transform, location, allowMultiSpawn: false);
                await pool.CreatePoolAsync(ct);
                if (ct.IsCancellationRequested || pool.IsDestroyed)
                {
                    pool.Destroy();
                    return null;
                }

                m_poolDict[location] = pool;
            }

            return await pool.SpawnAsync(parent, position, rotation, ct);
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

            return TryGetGameObjectPool(gameObject.name, out pool);
        }

        public GameObjectPool GetGameObjectPool(string location)
            => m_poolDict.GetValueOrDefault(location);

        public bool TryGetGameObjectPool(string location, out GameObjectPool pool)
        {
            if (m_poolDict.TryGetValue(location, out pool))
            {
                return true;
            }
            return false;
        }

        public void DestroyPool(string location)
        {
            if (m_poolDict.TryGetValue(location, out var pool))
            {
                pool.ManualDestroy = true;
            }
        }

        public void DestroyAllPool(bool includeAll)
        {
            if (includeAll)
            {
                foreach (var pool in m_poolDict.Values)
                {
                    pool.Destroy();
                }
                m_poolDict.Clear();
            }
            else
            {
                foreach (var item in m_poolDict)
                {
                    if (!item.Value.DontDestroy)
                    {
                        m_removeList.Add(item.Key);
                    }
                }

                foreach (var poolKey in m_removeList)
                {
                    var pool = m_poolDict[poolKey];
                    pool.Destroy();
                    m_poolDict.Remove(poolKey);
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
                var pool = m_poolDict[poolKey];
                pool.Destroy();
                m_poolDict.Remove(poolKey);
            }
        }
    }
}
