using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 3 - 初始化 YooAsset Package
    /// </summary>
    public class InitPackageProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private readonly string INIT_PACKAGE_ERROR_TIPS =
            "PackageManifest_DefaultPackage.version Error : HTTP/1.1 404 Not Found";

        public override void OnEnter()
        {
            // DLogger.Info("======== 3-进入游戏初始化 Yooasset Package 流程 ========");
            InitPackage().Forget();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
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

        private async UniTaskVoid InitPackage()
        {
            // DLogger.Info("======== InitPackage ========");
            try
            {
                var initOperation = await m_resourceModule.InitPackage(m_resourceModule.DefaultPackageName);

                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    var updateUIDefineTextAsset = Resources.Load<TextAsset>(Settings.UpdateSettings.UpdateUIDefineConfigPath);
                    // 热更新文本初始化
                    UpdateUIDefine.Instance.InitConfigData(updateUIDefineTextAsset);

                    var playMode = m_resourceModule.PlayMode;
                    switch (playMode)
                    {
                        case EPlayMode.EditorSimulateMode:
                            DLogger.Info("======== 当前处于编辑器资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;

                        case EPlayMode.OfflinePlayMode:
                            DLogger.Info("======== 当前处于单机资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;

                        case EPlayMode.HostPlayMode:
                        case EPlayMode.WebPlayMode:
                            LauncherMgr.ShowUI<LoadUpdateUI>();
                            DLogger.Info("======== 当前处于联机/WebGL资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;
                        default:
                            DLogger.Error("======== 未知的资源模式 请检查 ResourcesModuleDriver 中的设置 ========");
                            break;
                    }
                }
                else
                {
                    DLogger.Error($"======== InitPackage 失败 ========> {initOperation.Error}");
                    // 资源初始化失败！
                    LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Package_Failed_Tips);
                    // 资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {0}</color>
                    LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(UpdateUIDefine.Instance.Init_Package_Failed_Try_Again_Tips, initOperation.Error),
                        Retry, Application.Quit);
                }
            }
            catch (Exception e)
            {
                OnInitPackageFailed(e.Message);
                throw;
            }
        }

        private void OnInitPackageFailed(string message)
        {
            DLogger.Error($"======== OnInitPackageFailed ========> {message}");
            // 资源初始化失败！
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Package_Failed_Tips);
            // PackageManifest_DefaultPackage.version Error : HTTP/1.1 404 Not Found
            if (message.Contains(INIT_PACKAGE_ERROR_TIPS))
            {
                // 请检查StreamingAssets/package/DefaultPackage/PackageManifest_DefaultPackage.version是否存在
                message = UpdateUIDefine.Instance.Init_Package_Error_Tips;
            }
            // 资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {0}</color>
            LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(UpdateUIDefine.Instance.Init_Package_Failed_Try_Again_Tips, message),
                Retry, Application.Quit);
        }

        /// <summary>
        /// 重试初始化包
        /// </summary>
        private void Retry()
        {
            DLogger.Error($"======== 重新尝试 InitPackage ========");
            // 重新初始化资源中...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Package_Retry_Tips);
            InitPackage().Forget();
        }
    }
}