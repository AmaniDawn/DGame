using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class EnvironmentInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Application Identity");
                BeginPanel();
                {
                    DrawItem("Product Name", Application.productName, "游戏/应用名称");
                    DrawItem("Company Name", Application.companyName, "公司名称");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Identifier", Application.identifier, "应用标识符");
#else
                    DrawItem("Identifier", Application.bundleIdentifier, "应用标识符");
#endif
                    DrawItem("Version", Application.version, "应用版本号");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Build GUID", Application.buildGUID, "构建唯一标识符");
#endif
                    DrawItem("Cloud Project ID", string.IsNullOrEmpty(Application.cloudProjectId) ? "(none)" : Application.cloudProjectId, "Unity云项目ID");
                }
                EndPanel();

                DrawSectionTitle("Platform & Engine");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Unity Version", Application.unityVersion, "Unity引擎版本");
                    DrawItem("Platform", Application.platform.ToString(), "运行平台");
                    DrawItem("System Language", Application.systemLanguage.ToString(), "系统语言");

                    Color32 mobileColor = Application.isMobilePlatform ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Mobile Platform", Application.isMobilePlatform.ToString(), mobileColor);

                    Color32 consoleColor = Application.isConsolePlatform ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Console Platform", Application.isConsolePlatform.ToString(), consoleColor);
                }
                EndPanel();

                DrawSectionTitle("Runtime Status");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 playingColor = Application.isPlaying ? DebuggerStyles.SuccessColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Playing", Application.isPlaying.ToString(), playingColor);

                    Color32 editorColor = Application.isEditor ? DebuggerStyles.WarningColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Editor", Application.isEditor.ToString(), editorColor);

                    Color32 debugColor = Debug.isDebugBuild ? DebuggerStyles.WarningColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Debug Build", Debug.isDebugBuild.ToString(), debugColor);

#if UNITY_5_6_OR_NEWER
                    Color32 focusColor = Application.isFocused ? DebuggerStyles.SuccessColor : DebuggerStyles.SecondaryTextColor;
                    DrawItemColored("Is Focused", Application.isFocused.ToString(), focusColor);
#endif
#if UNITY_2018_2_OR_NEWER
                    DrawItem("Is Batch Mode", Application.isBatchMode.ToString(), "是否为批处理模式（无图形界面）");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Splash Screen Finished", SplashScreen.isFinished.ToString(), "启动画面是否已结束");
#else
                    DrawItem("Is Showing Splash Screen", Application.isShowingSplashScreen.ToString(), "启动画面是否已结束");
#endif
                }
                EndPanel();

                DrawSectionTitle("Performance Settings");
                BeginPanel();
                {
                    ResetRowIndex();
                    string frameRateText = Application.targetFrameRate == -1 ? "Unlimited" : Application.targetFrameRate.ToString();
                    DrawItem("Target Frame Rate", frameRateText, "目标帧率设置");
                    DrawItem("Background Loading Priority", Application.backgroundLoadingPriority.ToString(), "后台加载优先级");
                    DrawItem("Run In Background", Application.runInBackground.ToString(), "是否在后台运行");
                }
                EndPanel();

                DrawSectionTitle("Network & Installation");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 networkColor = GetNetworkColor(Application.internetReachability);
                    DrawItemColored("Internet Reachability", Application.internetReachability.ToString(), networkColor);
#if UNITY_5_5_OR_NEWER
                    DrawItem("Installer Name", string.IsNullOrEmpty(Application.installerName) ? "(unknown)" : Application.installerName, "安装程序名称");
#endif
                    DrawItem("Install Mode", Application.installMode.ToString(), "安装模式");
                    DrawItem("Sandbox Type", Application.sandboxType.ToString(), "沙盒类型");
                }
                EndPanel();
            }

            private Color32 GetNetworkColor(NetworkReachability reachability)
            {
                switch (reachability)
                {
                    case NetworkReachability.ReachableViaLocalAreaNetwork:
                        return DebuggerStyles.SuccessColor;
                    case NetworkReachability.ReachableViaCarrierDataNetwork:
                        return DebuggerStyles.WarningColor;
                    default:
                        return DebuggerStyles.ErrorColor;
                }
            }
        }
    }
}