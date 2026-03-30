namespace Procedure
{
    /// <summary>
    /// 2 - 游戏闪屏
    /// </summary>
    public class SplashProcedure : ProcedureBase
    {

        public override bool UseNativeDialog => true;

        public override void OnEnter()
        {
            // DLogger.Info("======== 2-进入游戏闪屏流程 ========");
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            SwitchState<InitPackageProcedure>();
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