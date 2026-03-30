using DGame;
using Launcher;

namespace Procedure
{
    /// <summary>
    /// 1 - 游戏启动
    /// </summary>
    public class LaunchProcedure : ProcedureBase
    {
        public override bool UseNativeDialog => true;
        private IAudioModule m_audioModule;

        public override void OnCreate(IFsm<IProcedureModule> fsm)
        {
            base.OnCreate(fsm);
            m_audioModule = ModuleSystem.GetModule<IAudioModule>();
        }

        public override void OnEnter()
        {
            // DLogger.Info("======== 1-进入游戏启动流程 ========");
            LauncherMgr.Initialize();
            InitAudioModuleSettings();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            SwitchState<SplashProcedure>();
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

        private void InitAudioModuleSettings()
        {
            m_audioModule.MusicEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.MUSIC_MUTED, false);
            m_audioModule.MusicVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.MUSIC_VOLUME, 1f);
            m_audioModule.SoundEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.SOUND_MUTED, false);
            m_audioModule.SoundVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.SOUND_VOLUME, 1f);
            m_audioModule.UISoundEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.UI_SOUND_MUTED, false);
            m_audioModule.UISoundVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.UI_SOUND_VOLUME, 1f);
            // DLogger.Info("======== 初始化音频模块完成 ========");
        }
    }
}