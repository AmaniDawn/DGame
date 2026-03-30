using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace DGame
{
    internal sealed class SceneModule : Module, ISceneModule
    {
        private SceneHandle m_currentMainScene;

        private readonly Dictionary<string, SceneHandle> m_subScenes = new Dictionary<string, SceneHandle>(8);

        private readonly HashSet<string> m_handlingScene = new HashSet<string>(4);

        /// <summary>
        /// 当前主场景名称。
        /// </summary>
        public string CurrentMainSceneName { get; private set; } = string.Empty;

        public override void OnCreate()
        {
            m_currentMainScene = null;
            CurrentMainSceneName = SceneManager.GetSceneByBuildIndex(0).name;
        }

        public override void OnDestroy()
        {
            foreach (var sceneHandle in m_subScenes.Values)
            {
                sceneHandle?.UnloadAsync();
            }

            m_subScenes.Clear();
            m_handlingScene.Clear();
            CurrentMainSceneName = string.Empty;
        }

        public async UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single,
            bool suspendLoad = false, uint priority = 100, bool gcCollect = true,
            Action<float> progressCallBack = null)
        {
            if (!m_handlingScene.Add(location))
            {
                DLogger.Error($"场景正在加载中... Scene: {location}");
                return default;
            }

            try
            {
                if (sceneMode == LoadSceneMode.Additive)
                {
                    if (m_subScenes.TryGetValue(location, out var subScene))
                    {
                        throw new DGameException($"子场景已经加载过了 Scene: {location}");
                    }

                    subScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad,
                        priority);

                    m_subScenes.Add(location, subScene);

                    try
                    {
                        if (progressCallBack != null)
                        {
                            while (!subScene.IsDone && subScene.IsValid)
                            {
                                progressCallBack.Invoke(subScene.Progress);
                                await UniTask.Yield();
                            }
                        }
                        else
                        {
                            await subScene.ToUniTask();
                        }
                    }
                    catch (Exception e)
                    {
                        m_subScenes.Remove(location);
                        throw new DGameException($"子场景加载异常: {e}");
                    }

                    return subScene.SceneObject;
                }
                else
                {
                    if (m_currentMainScene != null && !m_currentMainScene.IsDone)
                    {
                        throw new DGameException($"当前场景正在加载中... CurrentMainScene: {CurrentMainSceneName}.");
                    }

                    CurrentMainSceneName = location;

                    m_currentMainScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None,
                        suspendLoad, priority);

                    if (progressCallBack != null)
                    {
                        while (!m_currentMainScene.IsDone && m_currentMainScene.IsValid)
                        {
                            progressCallBack.Invoke(m_currentMainScene.Progress);
                            await UniTask.Yield();
                        }
                    }
                    else
                    {
                        await m_currentMainScene.ToUniTask();
                    }

                    ModuleSystem.GetModule<IResourceModule>().ForceUnloadUnusedAssets(gcCollect);

                    return m_currentMainScene.SceneObject;
                }
            }
            finally
            {
                m_handlingScene.Remove(location);
            }
        }

        public void LoadScene(string location, LoadSceneMode sceneMode = LoadSceneMode.Single,
            bool suspendLoad = false, uint priority = 100, Action<Scene> callBack = null,
            bool gcCollect = true, Action<float> progressCallBack = null)
        {
            if (!m_handlingScene.Add(location))
            {
                DLogger.Error($"场景正在加载中... Scene: {location}");
                return;
            }

            if (sceneMode == LoadSceneMode.Additive)
            {
                if (m_subScenes.TryGetValue(location, out SceneHandle subScene))
                {
                    DLogger.Warning($"子场景已经加载过了... Scene: {location}");
                    return;
                }

                subScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

                subScene.Completed += handle =>
                {
                    m_handlingScene.Remove(location);
                    callBack?.Invoke(handle.SceneObject);
                };

                if (progressCallBack != null)
                {
                    InvokeProgress(subScene, progressCallBack).Forget();
                }

                m_subScenes.Add(location, subScene);
            }
            else
            {
                if (m_currentMainScene != null && !m_currentMainScene.IsDone)
                {
                    DLogger.Warning($"当前场景正在加载中... CurrentMainScene: {CurrentMainSceneName}.");
                    return;
                }

                CurrentMainSceneName = location;

                m_currentMainScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

                m_currentMainScene.Completed += handle =>
                {
                    m_handlingScene.Remove(location);
                    callBack?.Invoke(handle.SceneObject);
                };

                if (progressCallBack != null)
                {
                    InvokeProgress(m_currentMainScene, progressCallBack).Forget();
                }

                ModuleSystem.GetModule<IResourceModule>().ForceUnloadUnusedAssets(gcCollect);
            }
        }

        private async UniTaskVoid InvokeProgress(SceneHandle sceneHandle, Action<float> progress)
        {
            if (sceneHandle == null)
            {
                return;
            }

            while (!sceneHandle.IsDone && sceneHandle.IsValid)
            {
                await UniTask.Yield();
                progress?.Invoke(sceneHandle.Progress);
            }
        }

        public bool ActivateScene(string location)
        {
            if (CurrentMainSceneName.Equals(location))
            {
                if (m_currentMainScene != null)
                {
                    return m_currentMainScene.ActivateScene();
                }

                return false;
            }

            m_subScenes.TryGetValue(location, out var subScene);
            if (subScene != null)
            {
                return subScene.ActivateScene();
            }

            DLogger.Warning($"场景无效资源路径:{location}");
            return false;
        }

        public bool UnSuspend(string location)
        {
            if (CurrentMainSceneName.Equals(location))
            {
                if (m_currentMainScene != null)
                {
                    return m_currentMainScene.UnSuspend();
                }

                return false;
            }

            m_subScenes.TryGetValue(location, out var subScene);
            if (subScene != null)
            {
                return subScene.UnSuspend();
            }

            DLogger.Warning($"主场景无效资源路径:{location}");
            return false;
        }

        public bool IsMainScene(string location)
        {
            if (m_currentMainScene == null)
            {
                DLogger.Warning($"主场景未设置: {location}");
                return false;
            }

            if (!CurrentMainSceneName.Equals(location))
            {
                return false;
            }

            // 获取当前激活的场景
            Scene currentScene = SceneManager.GetActiveScene();
            return currentScene.name == m_currentMainScene.SceneName;
        }

        public async UniTask<bool> UnloadAsync(string location, Action<float> progressCallBack = null)
        {
            if (m_subScenes.TryGetValue(location, out SceneHandle subScene))
            {
                if (subScene.SceneObject == default)
                {
                    DLogger.Error($"无法卸载没有加载的场景对象. Scene: {location}");
                    return false;
                }

                if (!m_handlingScene.Add(location))
                {
                    DLogger.Warning($"无法卸载正在加载的场景对象. Scene: {location}");
                    return false;
                }

                var unloadOperation = subScene.UnloadAsync();

                if (progressCallBack != null)
                {
                    while (!unloadOperation.IsDone && unloadOperation.Status != EOperationStatus.Failed)
                    {
                        progressCallBack.Invoke(unloadOperation.Progress);
                        await UniTask.Yield();
                    }
                }
                else
                {
                    await unloadOperation.ToUniTask();
                }

                m_subScenes.Remove(location);
                m_handlingScene.Remove(location);

                return true;
            }
            DLogger.Warning($"无效的资源地址，卸载场景失败:{location}");
            return false;
        }

        public void Unload(string location, Action callBack = null, Action<float> progressCallBack = null)
        {
            if (m_subScenes.TryGetValue(location, out SceneHandle subScene))
            {
                if (subScene.SceneObject == default)
                {
                    DLogger.Error($"无法卸载没有加载的场景对象. Scene: {location}");
                    return;
                }

                if (!m_handlingScene.Add(location))
                {
                    DLogger.Warning($"无法卸载正在加载的场景对象. Scene: {location}");
                    return;
                }

                subScene.UnloadAsync();
                subScene.UnloadAsync().Completed += _ =>
                {
                    m_subScenes.Remove(location);
                    m_handlingScene.Remove(location);
                    callBack?.Invoke();
                };

                if (progressCallBack != null)
                {
                    InvokeProgress(subScene, progressCallBack).Forget();
                }

                return;
            }

            DLogger.Warning($"无效的资源地址，卸载场景失败:{location}");
        }

        public bool ContainsScene(string location)
        {
            if (CurrentMainSceneName.Equals(location))
            {
                return true;
            }

            return m_subScenes.TryGetValue(location, out _);
        }
    }
}