using DGame;
using Launcher;
using YooAsset;

namespace Procedure
{
    public class ClearCacheProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        public override void OnEnter()
        {
            DLogger.Info("======== 8-清理未使用的缓存文件 ========");
            // 清理未使用的缓存文件...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Clear_Cache_Begin_Tips);
            var operation = m_resourceModule.ClearCacheFilesAsync();
            operation.Completed += Operation_Complete;
        }

        private void Operation_Complete(AsyncOperationBase obj)
        {
            // 清理完成 即将进入游戏...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Clear_Cache_Over_Tips);
            SwitchState<PreloadProcedure>();
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
    }
}