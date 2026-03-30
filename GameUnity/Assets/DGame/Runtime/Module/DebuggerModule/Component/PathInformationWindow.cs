using System;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class PathInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Application Paths");
                BeginPanel();
                {
                    DrawItem("Data Path", Utility.PathUtil.GetRegularPath(Application.dataPath), "数据资源路径（只读）");
                    DrawItem("Persistent Data Path", Utility.PathUtil.GetRegularPath(Application.persistentDataPath), "持久化数据路径（可读写）");
                    DrawItem("Streaming Assets Path", Utility.PathUtil.GetRegularPath(Application.streamingAssetsPath), "流式资源路径（只读）");
                    DrawItem("Temporary Cache Path", Utility.PathUtil.GetRegularPath(Application.temporaryCachePath), "临时缓存路径");
                }
                EndPanel();

                DrawSectionTitle("System Paths");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Current Directory", Utility.PathUtil.GetRegularPath(Environment.CurrentDirectory), "当前工作目录");
#if UNITY_2018_3_OR_NEWER
                    string logPath = Application.consoleLogPath;
                    DrawItem("Console Log Path", string.IsNullOrEmpty(logPath) ? "(not available)" : Utility.PathUtil.GetRegularPath(logPath), "控制台日志文件路径");
#endif
                }
                EndPanel();

                DrawSectionTitle("Path Notes");
                BeginPanel();
                {
                    GUILayout.Label(DebuggerStyles.ColorText(
                        "Data Path: Contains read-only application resources.\n" +
                        "Persistent Path: For user data that persists between sessions.\n" +
                        "Streaming Assets: Raw files bundled with the application.\n" +
                        "Temporary Cache: For temporary files that can be cleared.",
                        DebuggerStyles.SecondaryTextColor), DebuggerStyles.RichLabelStyle);
                }
                EndPanel();
            }
        }
    }
}