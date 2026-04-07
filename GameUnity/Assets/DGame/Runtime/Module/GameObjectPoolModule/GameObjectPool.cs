using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    [DisallowMultipleComponent]
    internal sealed class GameObjectPoolIdentity : MonoBehaviour
    {
        public string PoolKey;
    }

    public sealed class GameObjectPool : MemoryObject
    {
        private Queue<GameObject> m_goPool;
        private readonly DGameLinkedList<GameObject> m_spawnedPool = new DGameLinkedList<GameObject>();
        private GameObject m_parent;
        private int m_initCapacity;
        private int m_maxCapacity;
        private float m_autoDestroyTime;
        private float m_lastRecycleTime = -1f;
        private bool m_allowMultiSpawn;
        private IResourceModule m_resourceModule;
        private CancellationTokenSource m_destroyCancellationTokenSource;

        /// <summary>
        /// 自动销毁时间
        /// </summary>
        public float AutoDestroyTime { get => m_autoDestroyTime; set => m_autoDestroyTime = value; }

        /// <summary>
        /// 资源定位地址
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// 对象池总容量
        /// </summary>
        public int Count => m_goPool != null && m_spawnedPool != null ? m_goPool.Count + m_spawnedPool.Count : 0;

        /// <summary>
        /// 正在被使用的游戏对象个数
        /// </summary>
        public int SpawnedCount => m_spawnedPool?.Count ?? 0;

        /// <summary>
        /// 没在使用的游戏对象个数
        /// </summary>
        public int NoSpawnCount => m_goPool?.Count ?? 0;

        /// <summary>
        /// 持久化对象池
        /// </summary>
        public bool DontDestroy { get; set; }

        /// <summary>
        /// 手动标记销毁
        /// </summary>
        public bool MarkedForDestroy { get; set; }

        /// <summary>
        /// 对象池最大容量
        /// </summary>
        public int MaxCapacity { get => m_maxCapacity; set => m_maxCapacity = value; }

        /// <summary>
        /// 对象池是否被销毁
        /// </summary>
        public bool IsDestroyed { get; private set; }

        private CancellationToken DestroyToken
            => m_destroyCancellationTokenSource != null ? m_destroyCancellationTokenSource.Token : CancellationToken.None;

        public static GameObjectPool Create(Transform poolRoot, string location, int initCapacity = 0,
            int maxCapacity = int.MaxValue, float autoDestroyTime = -1f, bool dontDestroy = false,
            bool allowMultiSpawn = false)
        {
            NormalizeCapacity(ref initCapacity, ref maxCapacity);
            GameObjectPool pool = MemoryObject.Spawn<GameObjectPool>();
            pool.IsDestroyed = false;
            pool.m_destroyCancellationTokenSource = new CancellationTokenSource();
            pool.m_parent = new GameObject($"{location}_Object_Pool");
            pool.m_parent.transform.SetParent(poolRoot, false);
            pool.Location = location;
            pool.m_initCapacity = initCapacity;
            pool.m_maxCapacity = maxCapacity;
            pool.m_autoDestroyTime = autoDestroyTime;
            pool.DontDestroy = dontDestroy;
            pool.MarkedForDestroy = false;
            pool.m_goPool = new Queue<GameObject>(initCapacity);
            pool.m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            pool.m_allowMultiSpawn = allowMultiSpawn;
            return pool;
        }

        public async UniTask CreatePoolAsync(CancellationToken ct = default)
            => await EnsureCapacityAsync(m_initCapacity, ct);

        public async UniTask ConfigureAsync(int initCapacity, int maxCapacity, float autoDestroyTime,
            bool dontDestroy, bool allowMultiSpawn, CancellationToken ct = default)
        {
            NormalizeCapacity(ref initCapacity, ref maxCapacity);
            m_initCapacity = initCapacity;
            m_maxCapacity = maxCapacity;
            m_autoDestroyTime = autoDestroyTime;
            DontDestroy = dontDestroy;
            m_allowMultiSpawn = allowMultiSpawn;
            TrimInactiveObjectsToCapacity();
            await EnsureCapacityAsync(m_initCapacity, ct);
        }

        private async UniTask EnsureCapacityAsync(int targetCapacity, CancellationToken ct)
        {
            if (IsDestroyed || MarkedForDestroy)
            {
                return;
            }

            targetCapacity = Mathf.Clamp(targetCapacity, 0, m_maxCapacity);

            while (Count < targetCapacity)
            {
                var go = await LoadPoolGameObjectAsync(ct);
                if (go == null)
                {
                    return;
                }

                go.SetActive(false);
                m_goPool.Enqueue(go);
            }
        }

        public async UniTask<GameObject> SpawnAsync(Transform parent, Vector3 position,
            Quaternion rotation, CancellationToken ct = default)
        {
            if (IsDestroyed || MarkedForDestroy)
            {
                return null;
            }

            GameObject go = null;

            if (m_goPool.Count > 0)
            {
                go = m_goPool.Dequeue();
            }
            else if (Count >= m_maxCapacity)
            {
                if (m_allowMultiSpawn && m_spawnedPool.Count > 0)
                {
                    go = m_spawnedPool.First.Value;
                    m_spawnedPool.RemoveFirst();
                    DLogger.Warning($"强制复用正在使用的对象: {go.name}");
                }
                else
                {
                    DLogger.Warning($"对象池容量已满，无法继续生成对象: {Location}");
                    return null;
                }
            }

            if (go == null)
            {
                go = await LoadPoolGameObjectAsync(ct);
            }

            if (go == null || IsDestroyed || MarkedForDestroy)
            {
                if (go != null)
                {
                    DestroyGameObject(go);
                }

                return null;
            }

            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(position, rotation);
            go.SetActive(true);
            m_spawnedPool.AddLast(go);
            return go;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="go"></param>
        public void Recycle(GameObject go)
        {
            if (go == null)
            {
                DLogger.Warning("尝试回收 null 对象");
                return;
            }
            if (IsDestroyed)
            {
                DestroyGameObject(go);
                return;
            }
            if (!m_spawnedPool.Remove(go))
            {
                DLogger.Warning($"对象不在已生成列表中，可能已回收: {go.name}");
                return;
            }
            if (SpawnedCount <= 0)
            {
                m_lastRecycleTime = Time.realtimeSinceStartup;
            }

            if (!MarkedForDestroy && Count < m_maxCapacity)
            {
                go.SetActive(false);
                go.transform.SetParent(m_parent.transform, false);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                m_goPool.Enqueue(go);
            }
            else
            {
                DestroyGameObject(go);
            }
        }

        /// <summary>
        /// 丢弃对象
        /// </summary>
        /// <param name="go"></param>
        public void Remove(GameObject go)
        {
            if (go == null)
            {
                DLogger.Warning("尝试丢弃 null 对象");
                return;
            }
            if (IsDestroyed)
            {
                DestroyGameObject(go);
                return;
            }

            if (!m_spawnedPool.Remove(go))
            {
                DLogger.Warning($"对象不在已生成列表中，可能已丢弃或回收: {go.name}");
                return;
            }

            if (SpawnedCount <= 0)
            {
                m_lastRecycleTime = Time.realtimeSinceStartup;
            }
            DestroyGameObject(go);
        }

        /// <summary>
        /// 是否可以进行自动销毁对象池
        /// </summary>
        /// <returns></returns>
        public bool CanAutoDestroy()
        {
            if (DontDestroy)
            {
                return false;
            }

            if (MarkedForDestroy)
            {
                return SpawnedCount <= 0;
            }

            if (m_autoDestroyTime <= 0f)
            {
                return false;
            }

            if (m_lastRecycleTime > 0 && SpawnedCount <= 0)
            {
                return Time.realtimeSinceStartup - m_lastRecycleTime > m_autoDestroyTime;
            }
            return false;
        }

        /// <summary>
        /// 销毁对象池
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;
            CancelDestroyToken();
            MemoryObject.Release(this);
        }

        private async UniTask<GameObject> LoadPoolGameObjectAsync(CancellationToken externalToken)
        {
            var operationToken = CreateOperationToken(externalToken, out var linkedTokenSource);
            try
            {
                var go = await m_resourceModule.LoadGameObjectAsync(Location, m_parent.transform, operationToken);
                if (go == null)
                {
                    if (operationToken.IsCancellationRequested || IsDestroyed)
                    {
                        return null;
                    }

                    DLogger.Error($"创建对象池失败: {Location}");
                    return null;
                }

                if (operationToken.IsCancellationRequested || IsDestroyed)
                {
                    DestroyGameObject(go);
                    return null;
                }

                MarkPooledObject(go);
                return go;
            }
            finally
            {
                linkedTokenSource?.Dispose();
            }
        }

        private CancellationToken CreateOperationToken(CancellationToken externalToken,
            out CancellationTokenSource linkedTokenSource)
        {
            linkedTokenSource = null;

            if (!externalToken.CanBeCanceled)
            {
                return DestroyToken;
            }

            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken, DestroyToken);
            return linkedTokenSource.Token;
        }

        private void CancelDestroyToken()
        {
            if (m_destroyCancellationTokenSource == null)
            {
                return;
            }

            if (!m_destroyCancellationTokenSource.IsCancellationRequested)
            {
                m_destroyCancellationTokenSource.Cancel();
            }
        }

        private void DestroyGameObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            UnityEngine.Object.Destroy(go);
        }

        private void DestroyAllGameObject()
        {
            if (m_goPool == null)
            {
                m_spawnedPool.Clear();
                return;
            }

            while (m_goPool.Count > 0)
            {
                var go = m_goPool.Dequeue();
                DestroyGameObject(go);
            }

            var curNode = m_spawnedPool.First;

            while (curNode != null)
            {
                var nextNode = curNode.Next;
                DestroyGameObject(curNode.Value);
                curNode = nextNode;
            }

            m_goPool.Clear();
            m_goPool = null;
            m_spawnedPool.Clear();
        }

        private void MarkPooledObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var identity = DGame.Utility.UnityUtil.AddMonoBehaviour<GameObjectPoolIdentity>(go);
            identity.PoolKey = Location;
        }

        private void TrimInactiveObjectsToCapacity()
        {
            if (m_goPool == null)
            {
                return;
            }

            int targetInactiveCount = Mathf.Max(0, m_maxCapacity - SpawnedCount);
            while (m_goPool.Count > targetInactiveCount)
            {
                DestroyGameObject(m_goPool.Dequeue());
            }
        }

        private static void NormalizeCapacity(ref int initCapacity, ref int maxCapacity)
        {
            initCapacity = Mathf.Max(0, initCapacity);
            maxCapacity = Mathf.Max(0, maxCapacity);

            if (initCapacity > maxCapacity)
            {
                initCapacity = maxCapacity;
            }
        }

        public override void OnRelease()
        {
            m_destroyCancellationTokenSource?.Dispose();
            m_destroyCancellationTokenSource = null;
            DestroyAllGameObject();
            UnityEngine.Object.Destroy(m_parent);
            m_parent = null;
            m_initCapacity = 0;
            m_autoDestroyTime = 0f;
            m_lastRecycleTime = -1f;
            m_maxCapacity = 0;
            m_resourceModule = null;
            m_allowMultiSpawn = false;
            Location = null;
            DontDestroy = false;
            MarkedForDestroy = false;
        }
    }
}