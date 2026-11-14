using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DGame
{
    internal partial class ResourceExtComponent
    {
        private class LoadingState : MemoryObject
        {
            public CancellationTokenSource Cts { get; set; }
            public string Location { get; set; }

            public void Cancel()
            {
                if (Cts != null && !Cts.IsCancellationRequested)
                {
                    Cts.Cancel();
                }
            }

            public override void OnRelease()
            {
                var cts = Cts;
                Cts = null;
                if (cts != null)
                {
                    cts.Dispose();
                }
                Location = string.Empty;
            }
        }

        private static IResourceModule m_resourceModule;

        public static IResourceModule ResourceModule => m_resourceModule;

        private static readonly Dictionary<UnityEngine.Object, LoadingState> m_loadingStates = new Dictionary<UnityEngine.Object, LoadingState>();

        private bool m_isDestroyed;

        private void InitializedResources()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
        }

        /// <summary>
        /// 通过资源系统设置资源
        /// </summary>
        /// <param name="setAssetObject">需要设置的对象</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public async UniTaskVoid SetAssetByResources<T>(ISetAssetObject setAssetObject,
            CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            var target = setAssetObject.TargetObject;
            var location = setAssetObject.Location;
            if (target == null || m_isDestroyed)
            {
                MemoryPool.Release(setAssetObject);
                return;
            }
            var loadingState = CreateLoadingState(target, location, cancellationToken);
            var linkedTokenSource = loadingState.Cts;

            var hasLoadingMarker = false;
            var setAssetObjectTransferred = false;
            T loadedResource = null;
            var resourceRegistered = false;

            try
            {
                // 等待其他可能正在进行的加载
                await TryWaitingLoading(location).AttachExternalCancellation(linkedTokenSource.Token);

                // 再次检查是否被新请求替换
                if (!IsCurrentRequest(target, loadingState))
                {
                    return;
                }

                // 检查缓存
                if (m_assetItemPool.CanSpawn(location))
                {
                    var assetObject = (T)m_assetItemPool.Spawn(location).Target;
                    DetachCurrentRequest(target, loadingState);
                    setAssetObjectTransferred = true;
                    SetAsset(setAssetObject, assetObject);
                }
                else
                {
                    // 防止重复加载同一资源。
                    if (!m_loadingAssetList.Add(location))
                    {
                        // 已经在加载中，等待回调处理。
                        DLogger.Warning($"资源仍在加载中，跳过重复请求: {location}");
                        return;
                    }

                    hasLoadingMarker = true;
                    
                    loadedResource = await m_resourceModule.LoadAssetAsync<T>(location, linkedTokenSource.Token);
                    if (loadedResource == null)
                    {
                        if (linkedTokenSource.IsCancellationRequested)
                        {
                            return;
                        }
                        DLogger.Error($"加载资源失败，资源为空: {location}");
                        return;
                    }

                    if (!IsCurrentRequest(target, loadingState))
                    {
                        return;
                    }

                    m_assetItemPool.Register(AssetItemObject.Create(location, loadedResource), true);
                    resourceRegistered = true;
                    DetachCurrentRequest(target, loadingState);
                    setAssetObjectTransferred = true;
                    SetAsset(setAssetObject, loadedResource);
                }
            }
            catch (OperationCanceledException) when (linkedTokenSource.IsCancellationRequested)
            {
                // 请求被替换或目标销毁属于正常取消流程
            }
            catch (Exception e)
            {
                DLogger.Error($"Failed to load asset '{location}': {e}");
            }
            finally
            {
                DetachCurrentRequest(target, loadingState);

                if (hasLoadingMarker)
                {
                    m_loadingAssetList.Remove(location);
                }

                if (loadedResource != null && !resourceRegistered)
                {
                    m_resourceModule.UnloadAsset(loadedResource);
                }

                if (!setAssetObjectTransferred)
                {
                    MemoryPool.Release(setAssetObject);
                }
                MemoryObject.Release(loadingState);
            }
        }

        private LoadingState CreateLoadingState(UnityEngine.Object target, string location, CancellationToken cancellationToken)
        {
            var loadingState = MemoryObject.Spawn<LoadingState>();
            loadingState.Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            loadingState.Location = location;
            ReplaceLoadingState(target, loadingState);
            return loadingState;
        }

        private void DetachCurrentRequest(UnityEngine.Object target, LoadingState expectedState)
        {
            if(m_loadingStates.TryGetValue(target, out var curState)
               && ReferenceEquals(curState, expectedState))
            {
                m_loadingStates.Remove(target);   
            }
        }

        private void ReplaceLoadingState(UnityEngine.Object target, LoadingState newState)
        {
            if (m_loadingStates.Remove(target, out var oldState))
            {
                oldState.Cancel();
            }
            m_loadingStates[target] = newState;
        }
        
        private bool IsCurrentRequest(UnityEngine.Object target, LoadingState expectedState)
        {
            if (target == null || m_isDestroyed || expectedState.Cts.IsCancellationRequested)
            {
                return false;
            }
            return m_loadingStates.TryGetValue(target, out var curState) 
                   && ReferenceEquals(curState, expectedState);
        }

        /// <summary>
        /// 组件销毁时清理所有资源。
        /// </summary>
        private void OnDestroy()
        {
            m_isDestroyed = true;
            if (Instance == this)
            {
                Instance = null;
            }
            var loadingStates = new LoadingState[m_loadingStates.Count];
            m_loadingStates.Values.CopyTo(loadingStates, 0);
            m_loadingStates.Clear();
            foreach (var state in loadingStates)
            {
                state.Cancel();
            }
            if (m_loadedAssetObjectsLinkedList != null)
            {
                foreach (var loadedAssetObject in m_loadedAssetObjectsLinkedList)
                {
                    ReleaseAsset(loadedAssetObject, true);
                }
                m_loadedAssetObjectsLinkedList.Clear();
            }
            m_currentProcessNode = null;
            ReleaseSubSpriteAssets();
        }
    }
}
