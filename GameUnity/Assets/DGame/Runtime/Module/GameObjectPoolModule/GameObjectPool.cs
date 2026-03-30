using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    public sealed class GameObjectPool : IMemory
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

        /// <summary>
        /// 每帧最大实例化对象数量，用于分帧处理避免卡顿
        /// </summary>
        private int m_maxProcessPerFrame = 50;

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
        public int Count => m_goPool.Count + m_spawnedPool.Count;

        /// <summary>
        /// 正在被使用的游戏对象个数
        /// </summary>
        public int SpawnedCount => m_spawnedPool.Count;

        /// <summary>
        /// 没在使用的游戏对象个数
        /// </summary>
        public int NoSpawnCount => m_goPool.Count;

        /// <summary>
        /// 持久化对象池
        /// </summary>
        public bool DontDestroy { get; set; }

        /// <summary>
        /// 手动标记销毁
        /// </summary>
        public bool ManualDestroy { get; set; }

        /// <summary>
        /// 对象池最大容量
        /// </summary>
        public int MaxCapacity { get => m_maxCapacity; set => m_maxCapacity = value; }

        /// <summary>
        /// 对象池是否被销毁
        /// </summary>
        public bool IsDestroyed { get; private set; }

        public static GameObjectPool Create(Transform poolRoot, string location, int initCapacity = 0,
            int maxCapacity = int.MaxValue, float autoDestroyTime = -1f, bool dontDestroy = false,
            bool allowMultiSpawn = true)
        {
            GameObjectPool pool = MemoryPool.Spawn<GameObjectPool>();
            pool.IsDestroyed = false;
            pool.m_parent = new GameObject($"{location}_Object_Pool");
            pool.m_parent.transform.SetParent(poolRoot, false);
            pool.Location = location;
            pool.m_initCapacity = initCapacity;
            pool.m_maxCapacity = maxCapacity;
            pool.m_autoDestroyTime = autoDestroyTime;
            pool.DontDestroy = dontDestroy;
            pool.m_goPool = new Queue<GameObject>(initCapacity);
            pool.m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            pool.m_allowMultiSpawn = allowMultiSpawn;
            return pool;
        }

        public async UniTaskVoid CreatePool(CancellationToken ct = default)
        {
            for (int i = 0; i < m_initCapacity; i++)
            {
                var go = m_resourceModule.LoadGameObject(Location, m_parent.transform);
                if (go == null)
                {
                    DLogger.Error($"创建对象池失败，加载资源失败: {Location}");
                    return;
                }
                go.SetActive(false);
                go.name = Location;
                m_goPool.Enqueue(go);
                // 每 N 个实例等待一帧，避免卡顿
                if ((i + 1) % m_maxProcessPerFrame == 0)
                {
                    await UniTask.Yield(cancellationToken: ct);
                }
            }
        }

        /// <summary>
        /// 异步分帧创建对象池
        /// </summary>
        /// <param name="ct"></param>
        public async UniTask CreatePoolAsync(CancellationToken ct = default)
        {
            for (int i = 0; i < m_initCapacity; i++)
            {
                var go = await m_resourceModule.LoadGameObjectAsync(Location, m_parent.transform, ct);

                if (go == null)
                {
                    DLogger.Error($"创建对象池失败: {Location}");
                    return;
                }
                go.SetActive(false);
                go.name = Location;
                m_goPool.Enqueue(go);
            }
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="forceNew">是否强制实例化</param>
        /// <returns></returns>
        public async UniTask<GameObject> SpawnAsync(Transform parent, Vector3 position,
            Quaternion rotation, bool forceNew = false, CancellationToken ct = default)
        {
            GameObject go = null;

            if (m_goPool.Count > 0)
            {
                if (!forceNew)
                {
                    go = m_goPool.Dequeue();
                }
            }
            else
            {
                if (Count >= m_maxCapacity)
                {
                    if (m_allowMultiSpawn && m_spawnedPool.Count > 0)
                    {
                        go = m_spawnedPool.First.Value;
                        m_spawnedPool.RemoveFirst();
                        DLogger.Warning($"强制复用正在使用的对象: {go.name}");
                    }
                }
            }
            if(go == null)
            {
                go = await m_resourceModule.LoadGameObjectAsync(Location, m_parent.transform, ct);
                go.name = Location;
            }
            if (go != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(position, rotation);
                go.SetActive(true);
                m_spawnedPool.AddLast(go);
            }

            return go;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="forceNew">是否强制实例化</param>
        /// <returns></returns>
        public GameObject SpawnSync(Transform parent, Vector3 position,
            Quaternion rotation, bool forceNew = false)
        {
            GameObject go = null;

            if (m_goPool.Count > 0)
            {
                if (!forceNew)
                {
                    go = m_goPool.Dequeue();
                }
            }
            else
            {
                if (Count >= m_maxCapacity)
                {
                    if (m_allowMultiSpawn && m_spawnedPool.Count > 0)
                    {
                        go = m_spawnedPool.First.Value;
                        m_spawnedPool.RemoveFirst();
                        DLogger.Warning($"强制复用正在使用的对象: {go.name}");
                    }
                }
            }
            if(go == null)
            {
                go = m_resourceModule.LoadGameObject(Location, m_parent.transform);
                go.name = Location;
            }
            if (go != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(position, rotation);
                go.SetActive(true);
                m_spawnedPool.AddLast(go);
            }
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
            if(SpawnedCount <= 0)
            {
                m_lastRecycleTime = Time.realtimeSinceStartup;
            }

            if (m_goPool.Count < m_maxCapacity)
            {
                if (go != null)
                {
                    go.SetActive(false);
                    go.transform.SetParent(m_parent.transform, false);
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
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

            if(SpawnedCount <= 0)
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

            if (m_autoDestroyTime <= 0f)
            {
                return false;
            }

            if (m_lastRecycleTime > 0 && SpawnedCount <= 0)
            {
                return Time.realtimeSinceStartup - m_lastRecycleTime > m_autoDestroyTime;
            }

            return ManualDestroy && SpawnedCount <= 0;
        }

        /// <summary>
        /// 销毁对象池
        /// </summary>
        public void Destroy()
        {
            IsDestroyed = true;
            MemoryPool.Release(this);
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
            for (int i = 0; i < m_goPool.Count; i++)
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

        public void OnRelease()
        {
            DestroyAllGameObject();
            UnityEngine.Object.Destroy(m_parent);
            m_parent = null;
            m_initCapacity = 0;
            m_autoDestroyTime = 0f;
            m_lastRecycleTime = -1f;
            m_maxCapacity = 0;
            m_resourceModule = null;
            m_allowMultiSpawn = false;
        }
    }
}