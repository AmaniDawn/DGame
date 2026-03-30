#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class AtlasConfigWindow : EditorWindow
    {
        [MenuItem("DGame Tools/图集工具/图集配置窗口")]
        public static void ShowWindow()
        {
            var window = GetWindow<AtlasConfigWindow>();
            window.titleContent = new GUIContent(" 图集配置窗口", EditorGUIUtility.IconContent("Settings").image);
            window.minSize = new Vector2(450, 400);
        }

        private Vector2 m_scrollPos = Vector2.zero;
        private int[] m_paddingPopup = new int[] { 2, 4, 8 };
        private bool m_showExcludeKeyword = false;
        private bool m_showSingleAtlasPath = false;
        private bool m_showRootDirAtlasPath = false;
        private bool m_showSourceAtlasRootPath = false;
        private bool m_showExcludeAtlasPath = false;

        private void OnGUI()
        {
            var config = AtlasConfig.Instance;
            DrawHeader();
            using var scrollScope = new EditorGUILayout.ScrollViewScope(m_scrollPos);
            m_scrollPos = scrollScope.scrollPosition;
            EditorGUI.BeginChangeCheck();

            DrawFolderSettings(config);
            DrawPlatformSettings(config);
            DrawPackingSettings(config);
            DrawSpriteImportSettings(config);
            DrawAdvancedSettings(config);

            if (EditorGUI.EndChangeCheck())
            {
                AtlasConfig.Save();
                AssetDatabase.Refresh();
            }

            DrawBottomButtons();
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            // 标题区域背景
            // Rect headerRect = EditorGUILayout.GetControlRect(false, 60);
            // EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, position.width, 60),
            //     new Color(0.1f, 0.1f, 0.1f, 0.8f));

            GUILayout.Space(10);

            // 主标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUILayout.LabelField(new GUIContent("图集配置窗口", "DGame Atlas Configuration System"),
                titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 副标题
            // var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            // {
            //     alignment = TextAnchor.MiddleCenter,
            //     normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            // };

            // EditorGUILayout.LabelField("高效管理您的UI图集资源", subtitleStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawBottomButtons()
        {
            using var horizontalScope = new EditorGUILayout.HorizontalScope();
            Color originalColor = GUI.color;
            GUI.color = Color.yellow;
            if (GUILayout.Button(new GUIContent(" 立即重新生成", EditorGUIUtility.IconContent("Refresh").image), GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("确认删除", "此操作将会立即删除相关路径下的所有图集资源，并重新生成，确定继续吗？", "删除", "取消"))
                {
                    EditorSpriteSaveInfo.ForceGenerateAll(true);
                }
            }
            GUI.color = originalColor;

            if (GUILayout.Button(new GUIContent("重新生成有变动的图集数据", EditorGUIUtility.IconContent("Refresh").image), GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                EditorSpriteSaveInfo.ForceGenerateAll();
            }

            if (GUILayout.Button(new GUIContent(" 清空缓存", EditorGUIUtility.IconContent("TreeEditor.Trash").image), GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                EditorSpriteSaveInfo.ClearCache();
            }
        }

        private void DrawSpriteImportSettings(AtlasConfig config)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            var labelGUIContent = new GUIContent(" Sprite导入设置", EditorGUIUtility.IconContent("Sprite Icon").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            var checkMipmapsContent = new GUIContent(" 检查Mipmap导入设置", EditorGUIUtility.IconContent("LODGroup Icon").image);
            config.checkMipmaps = EditorGUILayout.Toggle(checkMipmapsContent, config.checkMipmaps);
            if (config.checkMipmaps)
            {
                var enableMipmapsContent = new GUIContent(" 允许Mipmap", EditorGUIUtility.IconContent("FilterByType").image);
                config.enableMipmaps = EditorGUILayout.Toggle(enableMipmapsContent, config.enableMipmaps);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawAdvancedSettings(AtlasConfig config)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            var labelGUIContent = new GUIContent(" 高级设置", EditorGUIUtility.IconContent("ToolHandleGlobal").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            var autoGenerateContent = new GUIContent(" 自动生成", EditorGUIUtility.IconContent("PlayButton").image);
            config.autoGenerate = EditorGUILayout.Toggle(autoGenerateContent, config.autoGenerate);
            var enableLoggingContent = new GUIContent(" 启用日志", EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image);
            config.enableLogging = EditorGUILayout.Toggle(enableLoggingContent, config.enableLogging);
            var enableV2Content = new GUIContent(" 启用V2打包", EditorGUIUtility.IconContent("CollabNew").image);
            config.enableV2 = EditorGUILayout.Toggle(enableV2Content, config.enableV2);
            EditorGUILayout.BeginHorizontal();
            m_showExcludeKeyword = EditorGUILayout.BeginFoldoutHeaderGroup(m_showExcludeKeyword, "排除关键词");
            // GUILayout.Label("", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            if (m_showExcludeKeyword)
            {
                GUILayout.Label("数量:", GUILayout.ExpandWidth(false));
                int newSize = EditorGUILayout.IntField(config.excludeKeywords.Length, GUILayout.Width(40));
                newSize = Mathf.Max(0, newSize);
                if (newSize != config.excludeKeywords.Length)
                {
                    Array.Resize(ref config.excludeKeywords, newSize);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(25), GUILayout.Height(20)))
                {
                    Array.Resize(ref config.excludeKeywords, config.excludeKeywords.Length + 1);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"), GUILayout.Width(25), GUILayout.Height(20)) && config.excludeKeywords.Length > 0)
                {
                    Array.Resize(ref config.excludeKeywords, config.excludeKeywords.Length - 1);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (m_showExcludeKeyword)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                for (int i = 0; i < config.excludeKeywords.Length; i++)
                {
                    var keywordsContent = new GUIContent($" 关键词 [{i}]", EditorGUIUtility.IconContent("FilterByLabel").image);
                    config.excludeKeywords[i] = EditorGUILayout.TextField(keywordsContent, config.excludeKeywords[i]);
                }
                GUILayout.Space(2);
                if (GUILayout.Button(new GUIContent(" 清空", EditorGUIUtility.IconContent("TreeEditor.Trash").image), GUILayout.Height(25)))
                {
                    config.excludeKeywords = Array.Empty<string>();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPackingSettings(AtlasConfig config)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            var labelGUIContent = new GUIContent(" 图集设置", EditorGUIUtility.IconContent("SpriteAtlas Icon").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            // var paddingContent = new GUIContent(" Padding", EditorGUIUtility.IconContent("RectTransformBlueprint").image);
            // config.padding = EditorGUILayout.IntPopup("Padding", config.padding, Array.ConvertAll(m_paddingPopup, x => x.ToString()), m_paddingPopup, GUILayout.Height(20));
            GUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("RectTransformBlueprint"), GUILayout.Width(16), GUILayout.Height(18));
            config.padding = EditorGUILayout.IntPopup("Padding", config.padding, Array.ConvertAll(m_paddingPopup, x => x.ToString()), m_paddingPopup, GUILayout.Height(20));
            GUILayout.EndHorizontal();
            var offsetContent = new GUIContent(" Block Offset", EditorGUIUtility.IconContent("MoveTool").image);
            config.blockOffset = EditorGUILayout.IntField(offsetContent, config.blockOffset);
            var rotationContent = new GUIContent(" Enable Rotation", EditorGUIUtility.IconContent("RotateTool").image);
            config.enableRotation = EditorGUILayout.Toggle(rotationContent, config.enableRotation);
            var tightPackingContent = new GUIContent(" 剔除透明区域", EditorGUIUtility.IconContent("ViewToolOrbit").image);
            config.tightPacking = EditorGUILayout.Toggle(tightPackingContent, config.tightPacking);
            var alphaDilationContent = new GUIContent(" Alpha扩张", EditorGUIUtility.IconContent("ViewToolOrbit").image);
            config.alphaDilation = EditorGUILayout.Toggle(alphaDilationContent, config.alphaDilation);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPlatformSettings(AtlasConfig config)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            var labelGUIContent = new GUIContent(" 平台设置", EditorGUIUtility.IconContent("BuildSettings.Standalone").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            var androidContent = new GUIContent(" Android 格式", EditorGUIUtility.IconContent("BuildSettings.Android.Small").image);
            config.androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(androidContent, config.androidFormat);
            var iosContent = new GUIContent(" iOS 格式", EditorGUIUtility.IconContent("BuildSettings.iPhone.Small").image);
            config.iosFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(iosContent, config.iosFormat);
            var webGLContent = new GUIContent(" WebGL 格式", EditorGUIUtility.IconContent("BuildSettings.WebGL.Small").image);
            config.webGLFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(webGLContent, config.webGLFormat);
            var compressionContent = new GUIContent(" 压缩质量", EditorGUIUtility.IconContent("MeshRenderer Icon").image);
            config.compressionQuality = EditorGUILayout.IntSlider(compressionContent, config.compressionQuality, 0, 100);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawFolderSettings(AtlasConfig config)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            var labelGUIContent = new GUIContent(" 目录设置", EditorGUIUtility.IconContent("Folder Icon").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            config.outputAtlasDir = DrawFolderField("输出目录", "FolderOpened Icon", config.outputAtlasDir);
            // config.excludeFolder = DrawFolderField("排除目录", "FolderEmpty On Icon", config.excludeFolder);

            // config.sourceAtlasRootDir = DrawFolderField("收集目录", "FolderOpened Icon", config.sourceAtlasRootDir);

            DrawPathArrItem("收集目录", "收集目录", "Collab.FolderAdded", ref config.sourceAtlasRootDir, ref m_showSourceAtlasRootPath);
            DrawPathArrItem("排除目录", "排除目录", "Collab.FolderIgnored", ref config.excludeFolder, ref m_showExcludeAtlasPath);
            DrawPathArrItem("以根目录的子级目录生成图集", "根目录", "Collab.FolderAdded", ref config.rootChildAtlasDir, ref m_showRootDirAtlasPath);
            DrawPathArrItem("每张图都单独生成图集的目录", "单张图集目录", "Collab.FolderAdded", ref config.singleAtlasDir, ref m_showSingleAtlasPath);

            // EditorGUILayout.Space(5);
            // EditorGUILayout.BeginHorizontal();
            // m_showSingleAtlasPath = EditorGUILayout.BeginFoldoutHeaderGroup(m_showSingleAtlasPath, "每张图都单独生成图集的路径");
            // // GUILayout.Label("", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            // if (m_showSingleAtlasPath)
            // {
            //     GUILayout.Label("数量:", GUILayout.ExpandWidth(false));
            //     int newSize = EditorGUILayout.IntField(config.singleAtlasDir.Length, GUILayout.Width(40));
            //     newSize = Mathf.Max(0, newSize);
            //     if (newSize != config.singleAtlasDir.Length)
            //     {
            //         Array.Resize(ref config.singleAtlasDir, newSize);
            //     }
            //     if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(25), GUILayout.Height(20)))
            //     {
            //         Array.Resize(ref config.singleAtlasDir, config.singleAtlasDir.Length + 1);
            //     }
            //     if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"), GUILayout.Width(25), GUILayout.Height(20)) && config.singleAtlasDir.Length > 0)
            //     {
            //         Array.Resize(ref config.singleAtlasDir, config.singleAtlasDir.Length - 1);
            //     }
            // }
            // EditorGUILayout.EndHorizontal();
            // if (m_showSingleAtlasPath)
            // {
            //     EditorGUILayout.BeginVertical("box");
            //     for (int i = 0; i < config.singleAtlasDir.Length; i++)
            //     {
            //         config.singleAtlasDir[i] = DrawFolderField($"单张图集路径[{i}]", "FolderOpened Icon", config.singleAtlasDir[i]);
            //         // var keywordsContent = new GUIContent($" 关键词 [{i}]", EditorGUIUtility.IconContent("FilterByLabel").image);
            //         // config.excludeKeywords[i] = EditorGUILayout.TextField(keywordsContent, config.excludeKeywords[i]);
            //     }
            //     GUILayout.Space(2);
            //     if (GUILayout.Button(new GUIContent(" 清空", EditorGUIUtility.IconContent("TreeEditor.Trash").image), GUILayout.Height(25)))
            //     {
            //         config.singleAtlasDir = Array.Empty<string>();
            //     }
            //     EditorGUILayout.EndVertical();
            // }
            // EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPathArrItem(string label, string itemLabel, string iconName,ref string[] paths,ref bool isShow)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            isShow = EditorGUILayout.BeginFoldoutHeaderGroup(isShow, label);
            // GUILayout.Label("", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            if (isShow)
            {
                GUILayout.Label("数量:", GUILayout.ExpandWidth(false));
                int newSize = EditorGUILayout.IntField(paths.Length, GUILayout.Width(40));
                newSize = Mathf.Max(0, newSize);
                if (newSize != paths.Length)
                {
                    Array.Resize(ref paths, newSize);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(25), GUILayout.Height(20)))
                {
                    Array.Resize(ref paths, paths.Length + 1);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"), GUILayout.Width(25), GUILayout.Height(20)) && paths.Length > 0)
                {
                    Array.Resize(ref paths, paths.Length - 1);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (isShow)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                for (int i = 0; i < paths.Length; i++)
                {
                    paths[i] = DrawFolderField($"{itemLabel}[{i}]", iconName, paths[i]);
                    // var keywordsContent = new GUIContent($" 关键词 [{i}]", EditorGUIUtility.IconContent("FilterByLabel").image);
                    // config.excludeKeywords[i] = EditorGUILayout.TextField(keywordsContent, config.excludeKeywords[i]);
                }
                GUILayout.Space(2);
                if (GUILayout.Button(new GUIContent(" 清空", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), GUILayout.Height(25)))
                {
                    paths = Array.Empty<string>();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private string DrawFolderField(string label, string labelIcon, string path)
        {
            using var horizontalScope = new EditorGUILayout.HorizontalScope();
            var buttonGUIContent = new GUIContent("选择", EditorGUIUtility.IconContent("Folder Icon").image);
            var labelGUIContent = new GUIContent(" " + label, EditorGUIUtility.IconContent(labelIcon).image);
            path = EditorGUILayout.TextField(labelGUIContent, path);

            if (GUILayout.Button(buttonGUIContent, GUILayout.Width(60), GUILayout.Height(20)))
            {
                var newPath = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);

                if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(Application.dataPath))
                {
                    path = "Assets" + newPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("路径不在Unity项目内: " + newPath);
                }
            }
            return path;
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal("Box");
            {
                var config = AtlasConfig.Instance;
                int totalPaths = config.sourceAtlasRootDir.Length + config.rootChildAtlasDir.Length +
                                 config.singleAtlasDir.Length + config.excludeFolder.Length;

                EditorGUILayout.LabelField($"监控目录: {totalPaths} 个", EditorStyles.miniLabel);
                // GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(config.autoGenerate ? "自动生成: 开启" : "自动生成: 关闭",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif