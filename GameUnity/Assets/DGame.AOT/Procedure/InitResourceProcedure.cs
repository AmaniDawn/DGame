using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 4 - 初始化资源
    /// </summary>
    public class InitResourceProcedure : ProcedureBase
    {
        public override bool UseNativeDialog => true;

        private bool m_initResourcesComplete;

        public override void OnEnter()
        {
            // DLogger.Info("======== 4-进入资源初始化 InitResourceProcedure 流程 ========");
            m_initResourcesComplete = false;
            // 初始化资源中...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Resource_Tips);

            // 使用单机资源模式时：需要在初始化资源前 先构建 AssetBundle 并复制到 StreamingAssets 中 否则会提示 HTTP 404 错误
            InitResources().Forget();
        }

        private async UniTaskVoid InitResources()
        {
            // DLogger.Info("======== 更新资源清单 ========");
            // 更新清单文件...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Resource_Update_Manifest_Tips);
            // 1.获取资源清单的版本信息
            var operation1 = m_resourceModule.RequestPackageVersionAsync();
            await operation1.ToUniTask();

            if (operation1.Status != EOperationStatus.Succeed)
            {
                OnInitResourcesError(operation1.Error);
                return;
            }

            var packageVersion = operation1.PackageVersion;
            m_resourceModule.PackageVersion = packageVersion;

            if (Utility.PlayerPrefsUtil.HasKey(UpdateUIDefine.Instance.Game_Version_Key))
            {
                Utility.PlayerPrefsUtil.SetString(UpdateUIDefine.Instance.Game_Version_Key, m_resourceModule.PackageVersion);
            }

            DLogger.Info($"======== 初始化资源版本 : {packageVersion} ========");
            // 2.传入版本信息更新资源清单
            var operation2 = m_resourceModule.UpdatePackageManifestAsync(packageVersion);
            await operation2.ToUniTask();
            if (operation2.Status != EOperationStatus.Succeed)
            {
                OnInitResourcesError(operation2.Error);
                return;
            }
            m_initResourcesComplete = true;
        }

        private void OnInitResourcesError(string message)
        {
            // 联机模式 检查设备网络链接状态
            if (m_resourceModule.PlayMode == EPlayMode.HostPlayMode)
            {
                // 检查是否需要更新
                if (IsNeedUpdate())
                {
                    // 需要强制更新才能进游戏
                    DLogger.Error(message);
                    // 获取远程版本失败！点击确认重试\n <color=#FF0000>{message}</color>
                    LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(UpdateUIDefine.Instance.Init_Resource_Error_Try_Again_Tips, message),
                        () =>
                        {
                            InitResources().Forget();
                        }, Application.Quit);
                    return;
                }
                else
                {
                    // 不强制更新或可边玩边下
                    return;
                }
            }

            // WebGL模式
            DLogger.Error(message);
            // 初始化资源失败！点击确认重试\n <color=#FF0000>原因: {0}</color>
            LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(UpdateUIDefine.Instance.Init_Resource_Error_Try_Again_WebGL_Tips, message),
                () =>
                {
                    InitResources().Forget();
                }, Application.Quit);
        }

        private bool IsNeedUpdate()
        {
            // 不能联网且非强制更新(不更新可以进游戏)
            if (Settings.UpdateSettings.UpdateStyle == UpdateStyle.Optional && !m_resourceModule.UpdatableWhilePlaying)
            {
                // 获取上次成功记录的版本 "GAME_VERSION"
                string packageVersion = Utility.PlayerPrefsUtil.GetString(UpdateUIDefine.Instance.Game_Version_Key, string.Empty);
                if (string.IsNullOrEmpty(packageVersion))
                {
                    // 之前没下载过游戏资源 第一次进入游戏需要下载资源
                    // 当前网络不可用，请检查本地网络设置后点击确认进行重试
                    LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Resource_Network_Error_Try_Again_WebGL_Tips);
                    // 没有找到本地版本记录，需要更新资源！
                    LauncherMgr.ShowMessageBox(UpdateUIDefine.Instance.Init_Resource_No_Find_Local_Record_Tips,
                        () =>
                        {
                            InitResources().Forget();
                        }, Application.Quit);
                    return false;
                }

                m_resourceModule.PackageVersion = packageVersion;
                if (Settings.UpdateSettings.UpdateNotice == UpdateNotice.Notice)
                {
                    // 有更新提示 但不强制更新
                    // 检测到可选资源更新，推荐完成更新提升游戏体验
                    LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Init_Resource_Have_Notice_No_Force_Update_Tips);
                    // 更新失败，检测到可选资源更新，推荐完成更新提升游戏体验！ \n \n 确定再试一次，取消进入游戏
                    LauncherMgr.ShowMessageBox(UpdateUIDefine.Instance.Init_Resource_Have_Notice_No_Force_Update_Suggestion_Tips,
                        () =>
                        {
                            InitResources().Forget();
                        }, SwitchState<PreloadProcedure>);
                }
                else
                {
                    // 没有更新提示则直接进入预载模式
                    SwitchState<PreloadProcedure>();
                }
                return false;
            }
            return true;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!m_initResourcesComplete)
            {
                return;
            }

            // 联机模式和WEBGL模式
            if (m_resourceModule.PlayMode == EPlayMode.HostPlayMode ||
                m_resourceModule.PlayMode == EPlayMode.WebPlayMode)
            {
                // 线上最新版本 operation1.PackageVersion
                DLogger.Log($"======== 更新资源版本: {m_resourceModule.GetPackageVersion()} => {m_resourceModule.PackageVersion}");
                // 注意：保存资源版本号作为下次默认启动的版本
                // 如果当前是WebGL或者边玩边下直接进入预加载阶段
                if (m_resourceModule.PlayMode == EPlayMode.WebPlayMode || m_resourceModule.UpdatableWhilePlaying)
                {
                    // 边玩边下载还可以拓展首包支持
                    SwitchState<PreloadProcedure>();
                    return;
                }

                SwitchState<CreateDownloaderProcedure>();
                return;
            }

            // 单机模式和编辑器模式直接预载资源
            SwitchState<PreloadProcedure>();
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
    }
}