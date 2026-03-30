using DGame;
using Launcher;

namespace Procedure
{
    public class DownloadOverProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private bool m_needClearCache;

        public override void OnEnter()
        {
            DLogger.Info("======== 7-下载热更补丁完成 ========");
            // 下载完成...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Download_Over_Tips);
            // 下载完成保存一下本地资源版本号
            Utility.PlayerPrefsUtil.SetString(UpdateUIDefine.Instance.Game_Version_Key, m_resourceModule.PackageVersion);
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (m_needClearCache)
            {
                SwitchState<ClearCacheProcedure>();
            }
            else
            {
                SwitchState<PreloadProcedure>();
            }
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