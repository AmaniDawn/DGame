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

            public override void OnRelease()
            {
                Cts?.Cancel();
                Cts?.Dispose();
                Cts = null;
                Location = string.Empty;
            }
        }

        private static IResourceModule m_resourceModule;
        private LoadAssetCallbacks m_loadAssetCallbacks;

        public static IResourceModule ResourceModule => m_resourceModule;

        private static readonly Dictionary<UnityEngine.Object, LoadingState> m_loadingStates = new Dictionary<UnityEngine.Object, LoadingState>();

        private void InitializedResources()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            m_loadAssetCallbacks = new LoadAssetCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure);
        }

        private void OnLoadAssetFailure(string assetName, LoadResourceStatus status, string errormessage, object userdata)
        {
            m_loadingAssetList.Remove(assetName);

            if (userdata is ISetAssetObject setAssetObject)
            {
                ClearLoadingState(setAssetObject.TargetObject);
            }
            DLogger.Error("加载资源失败 '{0}' 错误信息： '{1}'.", assetName, errormessage);
        }

        private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userdata)
        {
            m_loadingAssetList.Remove(assetName);
            if (asset is UnityEngine.Object assetObject)
            {
                if (userdata is ISetAssetObject setAssetObject)
                {
                    // 检查资源是否仍然是当前需要的
                    if (IsCurrentLocation(setAssetObject.TargetObject, setAssetObject.Location))
                    {
                        ClearLoadingState(setAssetObject.TargetObject);

                        m_assetItemPool.Register(AssetItemObject.Create(setAssetObject.Location, assetObject), true);
                        SetAsset(setAssetObject, assetObject);
                    }
                    else
                    {
                        // 资源已经过期，卸载
                        m_resourceModule.UnloadAsset(assetObject);
                    }
                }
            }
            else
            {
                DLogger.Error($"加载资源失败 资源类型： {asset.GetType()}.");
            }
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
            if (target == null)
            {
                return;
            }
            // 取消并清理旧的加载请求
            CancelAndCleanupOldRequest(target);
            // 创建新的加载状态
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var loadingState = MemoryObject.Spawn<LoadingState>();
            loadingState.Cts = linkedTokenSource;
            loadingState.Location = location;
            m_loadingStates[target] = loadingState;

            try
            {
                // 等待其他可能正在进行的加载
                await TryWaitingLoading(location).AttachExternalCancellation(linkedTokenSource.Token);
                // 再次检查是否被新请求替换
                if (!IsCurrentLocation(target, location))
                {
                    return;
                }

                // 检查缓存
                if (m_assetItemPool.CanSpawn(location))
                {
                    ClearLoadingState(target);

                    var assetObject = (T)m_assetItemPool.Spawn(location).Target;
                    SetAsset(setAssetObject, assetObject);
                }
                else
                {
                    // 最后一次检查是否被替换
                    if (!IsCurrentLocation(target, location))
                    {
                        return;
                    }

                    // 防止重复加载同一资源。
                    if (!m_loadingAssetList.Add(location))
                    {
                        // 已经在加载中，等待回调处理。
                        return;
                    }

                    T resource = await m_resourceModule.LoadAssetAsync<T>(location, linkedTokenSource.Token);
                    if (resource != null)
                    {
                        m_loadAssetCallbacks?.LoadAssetSuccessCallback?.Invoke(location,resource, 0f, setAssetObject);
                    }
                    m_loadingAssetList.Remove(location);
                }
            }
            catch (Exception e)
            {
                DLogger.Error($"Failed to load asset '{location}': {e}");
                ClearLoadingState(target);
            }
        }

        /// <summary>
        /// 取消并清理旧的加载请求
        /// <param name="target">Unity对象</param>
        /// </summary>
        private void CancelAndCleanupOldRequest(UnityEngine.Object target)
        {
            if (m_loadingStates.TryGetValue(target, out var oldState))
            {
                MemoryObject.Release(oldState);
                m_loadingStates.Remove(target);
            }
        }

        /// <summary>
        /// 清理加载状态
        /// <param name="target">Unity对象</param>
        /// </summary>
        private void ClearLoadingState(UnityEngine.Object target)
        {
            if (m_loadingStates.TryGetValue(target, out var state))
            {
                MemoryObject.Release(state);
                m_loadingStates.Remove(target);
            }
        }

        /// <summary>
        /// 检查指定位置是否仍是该目标的当前加载位置
        /// </summary>
        private bool IsCurrentLocation(UnityEngine.Object target, string location)
        {
            if (target == null)
            {
                return false;
            }
            return m_loadingStates.TryGetValue(target, out var state) && state.Location == location;
        }

        /// <summary>
        /// 组件销毁时清理所有资源。
        /// </summary>
        private void OnDestroy()
        {
            foreach (var state in m_loadingStates.Values)
            {
                MemoryObject.Release(state);
            }

            m_loadingStates.Clear();
        }
    }
}