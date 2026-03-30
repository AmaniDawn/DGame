#if !UNITY_6000_3_OR_NEWER

using UnityEditor;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace DGame
{
    public partial class UnityToolbarExtendRight
    {
        private const string BUTTON_STYLE_NAME = "Tab middle";

        private static readonly string[] m_resourcesModeNames =
        {
            "EditorMode (编辑器下的模拟模式)",
            "OfflinePlayMode (单机模式)",
            "HostPlayMode (联机运行模式)",
            "WebPlayMode (WebGL运行模式)"
        };

        private static int m_resourcesModeIndex = 0;
        public static int ResourcesModeIndex => m_resourcesModeIndex;

        private static void OnToolbarGUI_EditorPlayMode()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Space(8);
                // 计算文本最长宽度
                float maxWidth = 0;
                foreach (var mode in m_resourcesModeNames)
                {
                    Vector2 size = ToolbarStyles.PopupStyle.CalcSize(new GUIContent(mode));

                    if (size.x > maxWidth)
                    {
                        maxWidth = size.x;
                    }
                }

                float popupWidth = Mathf.Clamp(maxWidth + 20, 100, 220);
                GUILayout.BeginHorizontal();

                int selectedIndex = EditorGUILayout.Popup(m_resourcesModeIndex, m_resourcesModeNames,
                    ToolbarStyles.PopupStyle, GUILayout.Width(popupWidth));
                if (selectedIndex != m_resourcesModeIndex)
                {
                    Debug.Log($"更改编辑器资源运行模式：{m_resourcesModeNames[m_resourcesModeIndex]}");
                    m_resourcesModeIndex = selectedIndex;
                    EditorPrefs.SetInt("EditorPlayMode", m_resourcesModeIndex);
                    OnResourcesModeIndexChanged(m_resourcesModeIndex);
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void OnResourcesModeIndexChanged(int resourcesModeIndex)
        {
            // 将索引转换为 EPlayMode 枚举
            EPlayMode newPlayMode = IndexToPlayMode(resourcesModeIndex);
            // 查找场景中所有的 ResourceModuleDriver
            ResourceModuleDriver driver = Object.FindObjectOfType<ResourceModuleDriver>();
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
                    Debug.Log($"已保存预制体: {prefabAssetPath} 的资源运行模式: {m_resourcesModeNames[m_resourcesModeIndex]}");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
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

        private static class ToolbarStyles
        {
            public static readonly GUIStyle PopupStyle;

            static ToolbarStyles()
            {
                PopupStyle = new GUIStyle(EditorStyles.popup)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 0, 0)
                };
            }
        }
    }
}

#endif