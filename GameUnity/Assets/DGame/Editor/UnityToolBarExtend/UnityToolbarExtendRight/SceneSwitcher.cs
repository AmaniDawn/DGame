#if !UNITY_6000_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    public partial class UnityToolbarExtendRight
    {
        private static List<(string sceneName, string scenePath)> m_initScenes;
        private static List<(string sceneName, string scenePath)> m_abScenes;
        private static List<(string sceneName, string scenePath)> m_otherScenes;

        // private static string initScenePath = "Assets/Scenes";
        private static string abScenePath = "Assets/BundleAssets/Scenes";

        static void UpdateScenes()
        {
            m_initScenes = SceneSwitcher.GetScenesInBuild();
            m_abScenes = SceneSwitcher.GetScenesInPath(abScenePath);

            List<(string sceneName, string scenePath)> allScenes = GetScenesInPath();
            m_otherScenes = new List<(string sceneName, string scenePath)>(allScenes);
            m_otherScenes.RemoveAll(scene =>
                m_initScenes.Exists(init => init.scenePath == scene.scenePath) ||
                m_abScenes.Exists(abScene => abScene.scenePath == scene.scenePath));
        }

        private static void OnToolbarGUI_SceneSwitcher()
        {
            if (m_initScenes.Count <= 0 && m_abScenes.Count <= 0 && m_otherScenes.Count <= 0)
            {
                return;
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            currentSceneName = string.IsNullOrEmpty(currentSceneName) ? "未保存的场景" : currentSceneName;
            GUIContent sceneLabel = new GUIContent(
                " " + "当前场景:",
                EditorGUIUtility.IconContent("SceneAsset Icon").image,
                "Current Selected Scene"
            );
            EditorGUILayout.LabelField(sceneLabel, GUILayout.Width(80));
            GUIContent content = new GUIContent(currentSceneName);
            Vector2 textSize = GUI.skin.button.CalcSize(content);
            float buttonWidth = textSize.x;
            var menu = new GenericMenu();
            AddScenesToMenu(m_initScenes, "UnityScenes", menu);
            AddScenesToMenu(m_abScenes, "ABScenes", menu);
            AddScenesToMenu(m_otherScenes, "OtherScenes", menu);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
            };
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                if (GUILayout.Button(currentSceneName, buttonStyle, GUILayout.Width(buttonWidth)))
                {
                    menu.ShowAsContext();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void AddScenesToMenu(List<(string sceneName, string scenePath)> scenes, string category, GenericMenu menu)
        {
            if (scenes != null && scenes.Count > 0)
            {
                foreach (var scene in scenes)
                {
                    menu.AddItem(new GUIContent($"{category}/{scene.sceneName}"), false, () =>
                    {
                        SwitchScene(scene.scenePath);
                    });
                }
            }
        }

        private static void SwitchScene(string scenePath)
        {
            if (SceneSwitcher.PromptSaveCurrentScene())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        public static List<(string sceneName, string scenePath)> GetScenesInPath()
        {
            var allScenes = new List<(string sceneName, string scenePath)>();

            // 查找项目中所有场景文件
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in guids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                allScenes.Add((sceneName, scenePath));
            }

            return allScenes;
        }

        private static class SceneSwitcher
        {
            public static List<(string sceneName, string scenePath)> GetScenesInBuild()
            {
                var scenes = new List<(string sceneName, string scenePath)>();
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (!scene.enabled || string.IsNullOrEmpty(scene.path))
                    {
                        continue;
                    }

                    var sceneName = Path.GetFileNameWithoutExtension(scene.path);
                    scenes.Add((sceneName, scene.path));
                }

                return scenes;
            }

            public static List<(string sceneName, string scenePath)> GetScenesInPath(string path)
            {
                var scenes = new List<(string sceneName, string scenePath)>();
                var guids = AssetDatabase.FindAssets("t:Scene", new string[] { path });

                foreach (var guid in guids)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    scenes.Add((sceneName, scenePath));
                }
                return scenes;
            }

            public static bool PromptSaveCurrentScene()
            {
                if (SceneManager.GetActiveScene().isDirty)
                {
                    bool saveScene = EditorUtility.DisplayDialog(
                        "是否保存当前场景",
                        "当前场景有未保存的更改，是否想保存？",
                        "保存",
                        "取消");

                    if (saveScene)
                    {
                        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}

#endif