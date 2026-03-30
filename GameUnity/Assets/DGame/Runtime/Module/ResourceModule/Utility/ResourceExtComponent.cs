#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    [DisallowMultipleComponent]
    internal sealed partial class ResourceExtComponent : MonoBehaviour
    {
        public static ResourceExtComponent Instance { get; private set; }
        private readonly TimeoutController m_timeoutController = new TimeoutController();

        /// <summary>
        /// 正在加载的资源列表
        /// </summary>
        private readonly HashSet<string> m_loadingAssetList = new HashSet<string>();

        /// <summary>
        /// 检查是否可以释放资源时间间隔
        /// </summary>
        [SerializeField]
        private float checkCanReleaseInternal = 30f;

        private float m_checkCanReleaseTime = 0.0f;

        /// <summary>
        /// 对象池自动释放时间间隔
        /// </summary>
        [SerializeField]
        private float autoReleaseInternal = 60.0f;

        /// <summary>
        /// 每帧最大处理资源数量，用于分帧处理避免卡顿。
        /// </summary>
        [SerializeField]
        private int m_maxProcessPerFrame = 50;

        /// <summary>
        /// 当前正在处理的节点，用于分帧处理。
        /// </summary>
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        private LinkedListNode<LoadedAssetObject> m_currentProcessNode;

        /// <summary>
        /// 保存加载的图片对象
        /// </summary>
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector, LabelText("保存加载的图片对象"), DisableInPlayMode]
#endif
        private LinkedList<LoadedAssetObject> m_loadedAssetObjectsLinkedList;

        /// <summary>
        /// 散图集合对象池
        /// </summary>
        private IObjectPool<AssetItemObject> m_assetItemPool;


#if UNITY_EDITOR
        public LinkedList<LoadedAssetObject> LoadAssetObjectsLinkedList
        {
            get => m_loadedAssetObjectsLinkedList;
            set => m_loadedAssetObjectsLinkedList = value;
        }
#endif

        private async void Start()
        {
            Instance = this;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            IObjectPoolModule poolModule = ModuleSystem.GetModule<IObjectPoolModule>();
            m_assetItemPool = poolModule.CreateMultiSpawnObjectPool<AssetItemObject>("SetAssetPool",
                autoReleaseInternal, 16, 60, 0);
            m_loadedAssetObjectsLinkedList = new LinkedList<LoadedAssetObject>();
            InitializedResources();
        }

        private void Update()
        {
            m_checkCanReleaseTime += Time.unscaledDeltaTime;

            if (m_checkCanReleaseTime < (double)checkCanReleaseInternal)
            {
                return;
            }
            ReleaseUnused();
        }

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [Button("释放无引用资源", ButtonHeight = 30)]
#endif
        public void ReleaseUnused()
        {
            if (m_loadedAssetObjectsLinkedList == null || m_loadedAssetObjectsLinkedList.Count == 0)
            {
                m_currentProcessNode = null;
                m_checkCanReleaseTime = 0f;
                return;
            }

            // 如果当前没有正在处理的节点，从头开始
            if (m_currentProcessNode == null)
            {
                m_currentProcessNode = m_loadedAssetObjectsLinkedList.First;
            }

            int processedCount = 0;
            LinkedListNode<LoadedAssetObject> current = m_currentProcessNode;

            // 分帧处理：每帧最多处理 maxProcessPerFrame 个资源
            while (current != null && processedCount < m_maxProcessPerFrame)
            {
                var next = current.Next;
                if(current.Value.assetObject.IsCanRelease())
                {
                    m_assetItemPool.Recycle(current.Value.assetTarget);
                    MemoryPool.Release(current.Value.assetObject);
                    m_loadedAssetObjectsLinkedList.Remove(current);
                }
                current = next;
                processedCount++;
            }
            m_currentProcessNode = current;

            if (m_currentProcessNode == null)
            {
                m_checkCanReleaseTime = 0.0f;
            }
            // Debugger.Info("======== ResourceExtComponent.释放无引用资源 ========");
        }

        private void SetAsset(ISetAssetObject setAssetObject, UnityEngine.Object assetObject)
        {
            m_loadedAssetObjectsLinkedList.AddLast(new LoadedAssetObject(setAssetObject, assetObject));
            setAssetObject.SetAsset(assetObject);
        }

        private async UniTask TryWaitingLoading(string assetObjectKey)
        {
            if (m_loadingAssetList.Contains(assetObjectKey))
            {
                try
                {
                    await UniTask.WaitUntil(
                            () => !m_loadingAssetList.Contains(assetObjectKey))
#if UNITY_EDITOR
                        .AttachExternalCancellation(m_timeoutController.Timeout(TimeSpan.FromSeconds(60)));
                    m_timeoutController.Reset();
#else
                    ;
#endif
                }
                catch (OperationCanceledException ex)
                {
                    if (m_timeoutController.IsTimeout())
                    {
                        DLogger.Error($"等待加载资源超时：{assetObjectKey}. 原因：{ex.Message}");
                    }
                }
            }
        }
    }
}