using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class SceneInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Scene Statistics");
                BeginPanel();
                {
                    DrawItem("Scene Count", SceneManager.sceneCount.ToString(), "当前加载的场景总数");
                    DrawItem("Scene Count In Build Settings", SceneManager.sceneCountInBuildSettings.ToString(), "在构建设置中配置的场景数量");
                }
                EndPanel();

                Scene activeScene = SceneManager.GetActiveScene();

                DrawSectionTitle("Active Scene");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Name", string.IsNullOrEmpty(activeScene.name) ? "(unnamed)" : activeScene.name, "当前活动场景的名称");
                    DrawItem("Path", string.IsNullOrEmpty(activeScene.path) ? "(no path)" : activeScene.path, "当前活动场景文件的完整资源路径");
                    DrawItem("Build Index", activeScene.buildIndex.ToString(), "当前活动场景在构建设置中的索引号");
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Handle", activeScene.handle.ToString(), "活动场景的内部句柄标识符");
#endif
                }
                EndPanel();

                DrawSectionTitle("Scene Status");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 validColor = activeScene.IsValid() ? DebuggerStyles.SuccessColor : DebuggerStyles.ErrorColor;
                    DrawItemColored("Is Valid", activeScene.IsValid().ToString(), validColor);

                    Color32 loadedColor = activeScene.isLoaded ? DebuggerStyles.SuccessColor : DebuggerStyles.WarningColor;
                    DrawItemColored("Is Loaded", activeScene.isLoaded.ToString(), loadedColor);

                    Color32 dirtyColor = activeScene.isDirty ? DebuggerStyles.WarningColor : DebuggerStyles.TextColor;
                    DrawItemColored("Is Dirty", activeScene.isDirty.ToString(), dirtyColor);

#if UNITY_2019_1_OR_NEWER
                    DrawItem("Is Sub Scene", activeScene.isSubScene.ToString(), "当前活动场景是否为子场景");
#endif
                }
                EndPanel();

                DrawSectionTitle("Scene Contents");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Root Object Count", activeScene.rootCount.ToString(), "当前活动场景根层级中的游戏对象数量");
                }
                EndPanel();

                // 如果有多个场景加载，显示所有已加载场景
                if (SceneManager.sceneCount > 1)
                {
                    DrawSectionTitle("All Loaded Scenes");
                    BeginPanel();
                    {
                        // 表格头
                        DrawTableHeader(
                            ("#", 30f),
                            ("Name", 0),
                            ("Build Index", 80f),
                            ("Loaded", 60f)
                        );

                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            Scene scene = SceneManager.GetSceneAt(i);
                            bool isActive = scene == activeScene;

                            if (isActive)
                            {
                                // 高亮活动场景
                                GUILayout.BeginHorizontal(DebuggerStyles.PrimaryBoxStyle);
                                {
                                    GUILayout.Label(DebuggerStyles.ColorText(i.ToString(), DebuggerStyles.PrimaryColor),
                                        DebuggerStyles.RichLabelStyle, GUILayout.Width(30f));
                                    GUILayout.Label(DebuggerStyles.ColorText(
                                        string.IsNullOrEmpty(scene.name) ? "(unnamed)" : scene.name + " (Active)",
                                        DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle);
                                    GUILayout.Label(DebuggerStyles.ColorText(scene.buildIndex.ToString(), DebuggerStyles.PrimaryColor),
                                        DebuggerStyles.RichLabelStyle, GUILayout.Width(80f));
                                    GUILayout.Label(DebuggerStyles.ColorText(scene.isLoaded.ToString(), DebuggerStyles.PrimaryColor),
                                        DebuggerStyles.RichLabelStyle, GUILayout.Width(60f));
                                }
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                DrawTableRow(i % 2 == 1,
                                    (i.ToString(), 30f),
                                    (string.IsNullOrEmpty(scene.name) ? "(unnamed)" : scene.name, 0),
                                    (scene.buildIndex.ToString(), 80f),
                                    (scene.isLoaded.ToString(), 60f)
                                );
                            }
                        }
                    }
                    EndPanel();
                }
            }
        }
    }
}