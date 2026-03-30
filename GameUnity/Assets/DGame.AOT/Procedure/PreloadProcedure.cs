using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    public class PreloadProcedure : ProcedureBase
    {
        public override bool UseNativeDialog => true;
        private float m_progress;
        private readonly Dictionary<string, bool> m_loadFlag = new Dictionary<string, bool>();
        private readonly bool m_needPreloadConfig = true;

        /// <summary>
        /// 预加载资源回调
        /// </summary>
        private LoadAssetCallbacks m_preloadAssetCallbacks;

        public override void OnCreate(IFsm<IProcedureModule> fsm)
        {
            base.OnCreate(fsm);
            m_preloadAssetCallbacks = new LoadAssetCallbacks(OnPreloadAssetSuccess, OnPreLoadAssetFailure);
        }

        public override void OnEnter()
        {
            // DLogger.Info("======== 9-预加载流程 ========");
            m_progress = 0;
            m_loadFlag.Clear();
            // 正在载入...{0}%
            LauncherMgr.ShowUI<LoadUpdateUI>(Utility.StringUtil.Format(UpdateUIDefine.Instance.Preload_Loading_Tips, 0));
            // GameEvent.Send("UILoadUpdate.RefreshVersion");
            PreloadResources();
        }

        private void PreloadResources()
        {
            if (m_needPreloadConfig)
            {
                LoadAllConfig();
            }
        }

        private void LoadAllConfig()
        {
            if (m_resourceModule.PlayMode == EPlayMode.EditorSimulateMode)
            {
                return;
            }

            // 获取预载标签资源的资源信息
            AssetInfo[] assetInfos = m_resourceModule.GetAssetInfos("PRELOAD");
            foreach (var assetInfo in assetInfos)
            {
                PreLoad(assetInfo.Address);
            }
#if UNITY_WEBGL
            AssetInfo[] webAssetInfos = _resourceModule.GetAssetInfos("WEBGL_PRELOAD");
            foreach (var assetInfo in webAssetInfos)
            {
                PreLoad(assetInfo.Address);
            }
#endif
            if (m_loadFlag.Count <= 0)
            {
                // SmoothValue(1, 1f, SwitchState<LoadAssemblyProcedure>).Forget();
                return;
            }
        }

        private void PreLoad(string location)
        {
            m_loadFlag.Add(location, false);
            m_resourceModule.LoadAssetAsync(location, 100, m_preloadAssetCallbacks, null);
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            var totalCnt = m_loadFlag.Count <= 0 ? 1 : m_loadFlag.Count;
            var loadCnt = m_loadFlag.Count <= 0 ? 1 : 0;

            foreach (var loadedFlag in m_loadFlag)
            {
                if (!loadedFlag.Value)
                {
                    break;
                }
                loadCnt++;
            }

            if (m_loadFlag.Count != 0)
            {
                // 正在载入...{0}%
                m_progress = (float)loadCnt / totalCnt;
                LauncherMgr.ShowUI<LoadUpdateUI>(Utility.StringUtil.Format(UpdateUIDefine.Instance.Preload_Loading_Tips,
                    m_progress * 100));
            }
            else
            {
                string progressStr = $"{m_progress * 100:f1}";

                if (Mathf.Abs(m_progress - 1f) < 0.001f)
                {
                    // 载入完成
                    LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Preload_Loading_Success_Tips);
                }
                else
                {
                    // 正在载入...{0}%
                    LauncherMgr.ShowUI<LoadUpdateUI>(Utility.StringUtil.Format(UpdateUIDefine.Instance.Preload_Loading_Tips, progressStr));
                }
            }

            LauncherMgr.RefreshProgress(m_progress);

            if (loadCnt < totalCnt)
            {
                return;
            }

            SwitchState<LoadAssemblyProcedure>();
        }

        private async UniTaskVoid SmoothValue(float value, float duration, Action callback = null)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var result = Mathf.Lerp(0, value, time / duration);
                m_progress = result;
                await UniTask.Yield();
            }

            m_progress = value;
            callback?.Invoke();
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnDestroy()
        {
        }

        private void OnPreLoadAssetFailure(string assetName, LoadResourceStatus status, string errormessage, object userdata)
        {
            DLogger.Warning("无法预加载资源文件 '{0}' 错误信息: '{1}'.", assetName, errormessage);
            m_loadFlag[assetName] = true;
        }

        private void OnPreloadAssetSuccess(string assetName, object asset, float duration, object userdata)
        {
            DLogger.Log("成功预加载资源文件 '{0}' 持续时间: '{1}'.", assetName, duration);
            m_loadFlag[assetName] = true;
        }
    }
}