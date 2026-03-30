using System;
using System.Collections.Generic;
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
            bool dontDestroy = false, bool allowMultiSpawn = false)
            => await CreateGameObjectPoolAsyncInternal(PoolRoot.transform, location, initCapacity, maxCapacity,
                autoDestroyTime, dontDestroy, allowMultiSpawn);

        private async UniTask<GameObjectPool> CreateGameObjectPoolAsyncInternal(Transform poolRoot, string location,
            int initCapacity, int maxCapacity, float autoDestroyTime,
            bool dontDestroy, bool allowMultiSpawn)
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
                await pool.CreatePoolAsync();
                m_poolDict[location] = pool;
            }
            else
            {
                DLogger.Warning($"对象池重复创建: {location}");
            }

            return pool;
        }

        public GameObjectPool CreateGameObjectPool(string location, int initCapacity = 0,
            int maxCapacity = Int32.MaxValue, float autoDestroyTime = -1, bool dontDestroy = false,
            bool allowMultiSpawn = false)
            => CreateGameObjectPoolInternal(PoolRoot.transform, location, initCapacity,
                maxCapacity, autoDestroyTime, dontDestroy, allowMultiSpawn);

        private GameObjectPool CreateGameObjectPoolInternal(Transform poolRoot, string location,
            int initCapacity, int maxCapacity, float autoDestroyTime, bool dontDestroy,
            bool allowMultiSpawn)
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
                pool.CreatePool().Forget();
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
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public async UniTask<GameObject> SpawnAsync(string location, bool forceClone = false)
            => await SpawnInternalAsync(location, null, Vector3.zero, Quaternion.identity,
                forceClone);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public async UniTask<GameObject> SpawnAsync(string location, Transform parent, bool forceClone = false)
            => await SpawnInternalAsync(location, parent, Vector3.zero, Quaternion.identity,
                forceClone);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">世界角度</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public async UniTask<GameObject> SpawnAsync(string location, Transform parent, Vector3 position,
            Quaternion rotation, bool forceClone = false)
            => await SpawnInternalAsync(location, parent, position, rotation, forceClone);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public GameObject SpawnSync(string location, bool forceClone = false)
            => SpawnInternal(location, null, Vector3.zero, Quaternion.identity, forceClone);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public GameObject SpawnSync(string location, Transform parent, bool forceClone = false)
            => SpawnInternal(location, parent, Vector3.zero, Quaternion.identity, forceClone);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">世界角度</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        public GameObject SpawnSync(string location, Transform parent, Vector3 position,
            Quaternion rotation, bool forceClone = false)
            => SpawnInternal(location, parent, position, rotation, forceClone);

        public void Recycle(GameObject gameObject)
        {
            if (TryGetGameObjectPool(gameObject.name, out var pool))
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
            if (TryGetGameObjectPool(gameObject.name, out var pool))
            {
                pool.Remove(gameObject);
            }
            else
            {
                DLogger.Warning($"没有找到该对象的对象池: {gameObject.name}");
            }
        }

        private GameObject SpawnInternal(string location, Transform parent,
            Vector3 position, Quaternion rotation, bool forceClone)
        {
            var pool = GetGameObjectPool(location);

            if (pool == null)
            {
                pool = GameObjectPool.Create(PoolRoot.transform, location);
                pool.CreatePool().Forget();
                m_poolDict[location] = pool;
            }
            return pool.SpawnSync(parent, position, rotation, forceClone);
        }

        private async UniTask<GameObject> SpawnInternalAsync(string location, Transform parent,
            Vector3 position, Quaternion rotation, bool forceClone)
        {
            var pool = GetGameObjectPool(location);

            if (pool == null)
            {
                pool = GameObjectPool.Create(PoolRoot.transform, location);
                await pool.CreatePoolAsync();
                m_poolDict[location] = pool;
            }

            return await pool.SpawnAsync(parent, position, rotation, forceClone);
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