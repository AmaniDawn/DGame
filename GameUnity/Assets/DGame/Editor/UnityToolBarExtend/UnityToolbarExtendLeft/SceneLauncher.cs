#if !UNITY_6000_3_OR_NEWER

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    public partial class UnityToolbarExtendLeft
    {
        private const string PREVIOUS_SCREEN_KEY = "DGame_PreviousScreenPath";
        private const string IS_LAUNCHER_BUTTON = "DGame_IsLauncher";

        private static readonly string SceneMain = "GameStart";
        private static readonly string ButtonStyleName = "Tab middle";
        private static GUIStyle m_buttonGUIStyle;

        private static void OnToolbarGUI_ScreenLauncher()
        {
            if (m_buttonGUIStyle == null)
            {
                m_buttonGUIStyle = new GUIStyle(ButtonStyleName)
                {
                    padding = new RectOffset(2, 8, 2, 2),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                };
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    new GUIContent("运行场景", EditorGUIUtility.FindTexture("PlayButton"), "启动场景"), m_buttonGUIStyle))
            {
                SceneHelper.StartScene(SceneMain);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // 从 EditorPrefs 读取之前的场景路径 并恢复之前的场景
                var previousScenePath = EditorPrefs.GetString(PREVIOUS_SCREEN_KEY, string.Empty);
                if (!string.IsNullOrEmpty(previousScenePath) && EditorPrefs.GetBool(IS_LAUNCHER_BUTTON))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(previousScenePath);
                        }
                    };
                }

                EditorPrefs.SetBool(IS_LAUNCHER_BUTTON, false);
            }
        }

        private static void OnEditorQuit()
        {
            EditorPrefs.SetString(PREVIOUS_SCREEN_KEY, "");
            EditorPrefs.SetBool(IS_LAUNCHER_BUTTON, false);
        }

        private static class SceneHelper
        {
            private static string m_sceneToOpen;

            public static void StartScene(string sceneName)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }

                var activeScene = SceneManager.GetActiveScene();

                // 缓存一下当前正在进行编辑的场景文件
                if (activeScene.isLoaded && activeScene.name != sceneName)
                {
                    EditorPrefs.SetString(PREVIOUS_SCREEN_KEY, activeScene.path);
                    EditorPrefs.SetBool(IS_LAUNCHER_BUTTON, true);
                }

                m_sceneToOpen = sceneName;
                EditorApplication.update += OnUpdate;
            }

            private static void OnUpdate()
            {
                if (string.IsNullOrEmpty(m_sceneToOpen) ||
                    EditorApplication.isPlaying || EditorApplication.isPaused ||
                    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }
                EditorApplication.update -= OnUpdate;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    string[] guids = AssetDatabase.FindAssets("t:scene " + m_sceneToOpen, null);

                    if (guids.Length <= 0)
                    {
                        Debug.LogWarning("找不到场景文件");
                    }
                    else
                    {
                        string scenePath = null;

                        for (int i = 0; i < guids.Length; i++)
                        {
                            scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);

                            if (scenePath.EndsWith("/" + m_sceneToOpen + ".unity"))
                            {
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(scenePath))
                        {
                            scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        }
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        EditorApplication.isPlaying = true;
                    }
                }

                m_sceneToOpen = null;
            }
        }
    }
}

#endif