#if UNITY_6000_3_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using YooAsset;

namespace DGame
{
    [InitializeOnLoad]
    public class MainToolbarInitializeOnLoad
    {
        static MainToolbarInitializeOnLoad()
        {
            MainToolbarExtender.MainToolbarSceneLauncherButton.Init();
            MainToolbarExtender.MainToolbarDropdownSceneSelector.Init();
            // MainToolbarExtender.MainToolbarDropdownPlayMode.Init();
        }
    }

    public class MainToolbarExtender
    {
        #region 快速启动入口场景按钮

        public class MainToolbarSceneLauncherButton
        {
            private const string kElementPath = "DGame/Open Scene Launcher Btn";

            private const string PREVIOUS_SCREEN_KEY = "DGame_PreviousScreenPath";
            private const string IS_LAUNCHER_BUTTON = "DGame_IsLauncher";

            private static readonly string SceneMain = "GameStart";

            [MainToolbarElement(kElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -100)]
            private static MainToolbarElement OpenSceneSwitcherButton()
            {
                var onIcon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
                var offIcon = EditorGUIUtility.IconContent("StopButton").image as Texture2D;
                var icon = !EditorApplication.isPlaying ? onIcon : offIcon;
                var content = new MainToolbarContent("Launcher", icon, "");
                var launcherBtn = new MainToolbarButton(content, () => { SceneHelper.StartScene(SceneMain); })
                {
                    displayed = true
                };
                return launcherBtn;
            }

            public static void Init()
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.quitting -= OnEditorQuit;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.quitting += OnEditorQuit;
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                // OpenSceneSwitcherButton();
                MainToolbar.Refresh(kElementPath);
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

        #endregion

        #region 场景选择器

        public class MainToolbarDropdownSceneSelector
        {
            private const string kElementPath = "DGame/Open Scene Dropdown Switcher";

            private static List<(string sceneName, string scenePath)> m_initScenes;
            private static List<(string sceneName, string scenePath)> m_abScenes;
            private static List<(string sceneName, string scenePath)> m_otherScenes;

            // private static string initScenePath = "Assets/Scenes";
            private static string abScenePath = "Assets/BundleAssets/Scenes";

            [MainToolbarElement(kElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 50)]
            public static MainToolbarElement OpenSceneSelectorDropdownButton()
            {
                var activeSceneName = Application.isPlaying ? SceneManager.GetActiveScene().name : EditorSceneManager.GetActiveScene().name;
                activeSceneName = string.IsNullOrEmpty(activeSceneName) ? "未保存的场景" : activeSceneName;
                var icon = EditorGUIUtility.IconContent("UnityLogo").image as Texture2D;
                var content = new MainToolbarContent(activeSceneName, icon, "Select active scene");
                return new MainToolbarDropdown(content, ShowDropdownMenu);
            }

            public static void Init()
            {
                EditorApplication.projectChanged += UpdateScenes;
                UpdateScenes();
                SceneManager.activeSceneChanged += SceneSwitched;
                EditorSceneManager.activeSceneChangedInEditMode += SceneSwitched;
            }

            static void ShowDropdownMenu(Rect dropDownRect)
            {
                var menu = new GenericMenu();
                AddScenesToMenu(m_initScenes, "UnityScenes", menu);
                AddScenesToMenu(m_abScenes, "ABScenes", menu);
                AddScenesToMenu(m_otherScenes, "OtherScenes", menu);
                menu.DropDown(dropDownRect);
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

            private static void SceneSwitched(Scene oldScene, Scene newScene)
            {
                MainToolbar.Refresh(kElementPath);
            }

            private static void UpdateScenes()
            {
                m_initScenes = SceneSwitcher.GetScenesInBuild();
                m_abScenes = SceneSwitcher.GetScenesInPath(abScenePath);

                List<(string sceneName, string scenePath)> allScenes = GetScenesInPath();
                m_otherScenes = new List<(string sceneName, string scenePath)>(allScenes);
                m_otherScenes.RemoveAll(scene =>
                    m_initScenes.Exists(init => init.scenePath == scene.scenePath) ||
                    m_abScenes.Exists(abScene => abScene.scenePath == scene.scenePath));
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

        #endregion

        #region YooAsset资源模式选择器

        public class MainToolbarDropdownPlayMode
        {
            private const string kElementPath = "DGame/Open Play Mode Button";

            private static readonly string[] m_resourcesModeNames =
            {
                "EditorMode (编辑器下的模拟模式)",
                "OfflinePlayMode (单机模式)",
                "HostPlayMode (联机运行模式)",
                "WebPlayMode (WebGL运行模式)"
            };

            private static int m_resourceModeIndex = 0;
            public static int ResourceModeIndex => m_resourceModeIndex;

            [MainToolbarElement(kElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 101)]
            public static MainToolbarElement OpenPlayModeButton()
            {
                var content = new MainToolbarContent(m_resourcesModeNames[ResourceModeIndex]);
                var btn = new MainToolbarDropdown(content, ShowDropdownMenu)
                {
                    enabled = !EditorApplication.isPlaying
                };
                m_resourceModeIndex = EditorPrefs.GetInt("EditorPlayMode");
                return btn;
            }

            public static void Init()
            {
                // 监听播放模式变化
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                // EditorApplication.projectChanged += UpdateScenes;
                // SceneManager.activeSceneChanged += SceneSwitched;
                // EditorSceneManager.activeSceneChangedInEditMode += SceneSwitched;
            }

            private static void OnResourcesModeIndexChanged(int resourcesModeIndex)
            {
                // 将索引转换为 EPlayMode 枚举
                EPlayMode newPlayMode = IndexToPlayMode(resourcesModeIndex);
                // 查找场景中所有的 ResourceModuleDriver
                ResourceModuleDriver driver = DGame.Utility.UnityUtil.FindObjectOfType<ResourceModuleDriver>();
                GameObject go = null;
                if (driver.gameObject.scene.IsValid())
                {
                    driver.PlayMode = newPlayMode;
                    EditorUtility.SetDirty(driver);
                    go = driver.gameObject;
                    SaveParentPrefab(go);
                }

                // 如果有对象被修改，刷新Inspector
                if (go != null)
                {
                    Selection.activeObject = go;
                }
            }

            private static EPlayMode IndexToPlayMode(int index)
            {
                return index switch
                {
                    0 => EPlayMode.EditorSimulateMode,
                    1 => EPlayMode.OfflinePlayMode,
                    2 => EPlayMode.HostPlayMode,
                    3 => EPlayMode.WebPlayMode,
                    _ => EPlayMode.EditorSimulateMode
                };
            }

            private static void SaveParentPrefab(GameObject targetObject)
            {
                // 获取该GameObject对应的预制体根源
                GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject);

                if (prefabRoot != null)
                {
                    // 获取预制体资产路径
                    string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);

                    if (!string.IsNullOrEmpty(prefabAssetPath))
                    {
                        // 保存预制体修改
                        PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabAssetPath, InteractionMode.UserAction);
                        Debug.Log($"已保存预制体: {prefabAssetPath} 的资源运行模式: {m_resourcesModeNames[m_resourceModeIndex]}");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                MainToolbar.Refresh(kElementPath);
            }

            private static void ShowDropdownMenu(Rect dropDownRect)
            {
                var menu = new GenericMenu();

                for (var index = 0; index < m_resourcesModeNames.Length; index++)
                {
                    int i = index;
                    var resourceModeName = m_resourcesModeNames[index];
                    menu.AddItem(new GUIContent(resourceModeName), false, () =>
                    {
                        m_resourceModeIndex = i;
                        Debug.Log($"更改编辑器资源运行模式：{m_resourcesModeNames[m_resourceModeIndex]}");
                        EditorPrefs.SetInt("EditorPlayMode", m_resourceModeIndex);
                        OnResourcesModeIndexChanged(m_resourceModeIndex);
                        MainToolbar.Refresh(kElementPath);
                    });
                }

                menu.DropDown(dropDownRect);
            }
        }

        #endregion

    }
}

#endif