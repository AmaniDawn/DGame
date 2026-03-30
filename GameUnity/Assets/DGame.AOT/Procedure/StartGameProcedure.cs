using Launcher;

namespace Procedure
{
    /// <summary>
    /// 11-开始游戏
    /// </summary>
    public class StartGameProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        public override void OnEnter()
        {
            // DLogger.Info("======== 11-恭喜进入最后一步=>成功完成所有热更准备 进入游戏 ========");
            StartGame();
        }

        private void StartGame()
        {
            LauncherMgr.HideAllUI();
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