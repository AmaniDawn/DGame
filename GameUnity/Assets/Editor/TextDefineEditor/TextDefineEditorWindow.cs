using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameLogic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 文本配置编辑器窗口 - 提供文本提取、生成、绑定等功能
    /// </summary>
    public class TextDefineEditorWindow : EditorWindow
    {
        // 视图状态
        private enum ToolTab
        {
            CodeExtract,    // 代码文本提取
            PrefabExtract,  // 预制体提取
        }

        private const string m_title = "文本处理工具";
        private const string m_menuPath = "DGame Tools/文本处理工具";

        private static readonly Color m_headerColor = new(0.4f, 0.6f, 0.8f);

        // 路径配置
        private const string m_defaultScriptPath = "Assets/Scripts/HotFix/GameLogic";
        private const string m_defaultPrefabPath = "Assets/BundleAssets/UI";
        private const string m_textDefinePath = "Assets/Scripts/HotFix/GameLogic/Module/TextModule/TextDefine.cs";
        private const string m_outputPath = "Assets/Editor/TextDefineEditor/Output/";
        private ToolTab m_currentTab = ToolTab.CodeExtract;

        // 代码提取选项
        private string m_scriptFolderPath = m_defaultScriptPath;
        private int m_startTextDefineId;
        private string m_tagPrefix = string.Empty;
        private bool m_useExistingText;
        private static int m_maxExistingId = 20000000;

        // 预制体提取选项
        private string m_prefabFolderPath = m_defaultPrefabPath;
        private int m_prefabStartId;
        private bool m_includeTextMeshPro = true;
        private bool m_useExistingBinders = false;
        private bool m_Include_m_Prefix_Node = false;
        private static int m_maxPrefabExistingId = 30000000;

        // 结果显示
        private Vector2 m_scrollPosition;
        private Vector2 m_prefabResultScrollPosition;

        [MenuItem(m_menuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<TextDefineEditorWindow>(m_title);
            window.minSize = new Vector2(600, 600);
            window.RefreshExistingData();
        }

        private void RefreshExistingData()
        {
            // m_existingTextConfigs = TextProcessorTool.LoadExistingTextConfigs();
            // m_maxExistingId = m_existingTextConfigs.Count > 0 ? m_existingTextConfigs.Keys.Max() : 0;
            var tempID = TextProcessorTool.GetLastTextDefineID(m_textDefinePath);
            m_maxExistingId = tempID > 0 ? tempID : m_maxExistingId;
            m_startTextDefineId = m_maxExistingId + 1;
            var tempPrefabID = TextProcessorTool.GetLastPrefabTextDefineID(m_textDefinePath);
            m_maxPrefabExistingId = tempPrefabID > 0 ? tempPrefabID : m_maxPrefabExistingId;
            m_prefabStartId = m_maxPrefabExistingId + 1;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabButtons();

            // 主内容区域 - 使用灵活高度
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));
            {
                switch (m_currentTab)
                {
                    case ToolTab.CodeExtract:
                        DrawCodeExtractTab();
                        break;
                    case ToolTab.PrefabExtract:
                        DrawPrefabExtractTab();
                        break;
                }
            }
            EditorGUILayout.EndScrollView();

            // 预制体扫描结果 - 独立滚动区域
            if (m_currentTab == ToolTab.PrefabExtract && TextProcessorTool.PrefabTextEntries.Count > 0)
            {
                DrawPrefabScanResults();
            }
        }

        #region UI绘制 - 头部和标签页

        private void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 50);
            var headerRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, 40);
            EditorGUI.DrawRect(headerRect, m_headerColor);

            GUI.Label(headerRect, m_title, new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });

            EditorGUILayout.Space(5);
        }

        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var tabs = Enum.GetValues(typeof(ToolTab)).Cast<ToolTab>().ToArray();
                for (int i = 0; i < tabs.Length; i++)
                {
                    var tab = tabs[i];
                    var tabName = GetTabDisplayName(tab);
                    var isSelected = m_currentTab == tab;

                    if (GUILayout.Toggle(isSelected, tabName, EditorStyles.toolbarButton, GUILayout.Width(120)))
                    {
                        if (!isSelected)
                        {
                            m_currentTab = tab;
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                // 刷新按钮
                if (GUILayout.Button("刷新数据", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RefreshExistingData();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string GetTabDisplayName(ToolTab tab) => tab switch
        {
            ToolTab.CodeExtract => "文本处理",
            ToolTab.PrefabExtract => "UITextIDBinder处理",
            _ => tab.ToString()
        };

        #endregion

        #region UI绘制 - 代码提取

        private void DrawCodeExtractTab()
        {
            DrawSectionHeader("从代码中提取文本并生成 TextDefine", "扫描C#代码中的 G.R(\"中文\") 模式，自动替换为 G.R(TextDefine.xxx) 枚举并生成配置表。");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // 文件夹选择
                DrawFolderSelector(ref m_scriptFolderPath, "代码文件夹", m_defaultScriptPath);

                EditorGUILayout.Space(5);

                // 起始ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("起始 ID", GUILayout.Width(100));
                m_startTextDefineId = EditorGUILayout.IntField(m_startTextDefineId);
                EditorGUILayout.EndHorizontal();

                // 标签前缀
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("标签前缀", GUILayout.Width(100));
                m_tagPrefix = EditorGUILayout.TextField(m_tagPrefix);
                EditorGUILayout.EndHorizontal();

                // 选项
                m_useExistingText = EditorGUILayout.ToggleLeft("使用现有文本配置（不重复创建）", m_useExistingText);

                EditorGUILayout.Space(10);

                // 统计信息
                DrawInfoBox($"当前最大ID: {m_maxExistingId}");

                EditorGUILayout.Space(10);

                // 执行按钮
                if (GUILayout.Button("开始提取并生成", GUILayout.Height(35)))
                {
                    if (m_startTextDefineId != 0)
                    {
                        if (!Directory.Exists(m_outputPath))
                        {
                            Directory.CreateDirectory(m_outputPath);
                        }
                        var outPath = Path.Combine(m_outputPath, $"CodeText_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        var options = new GRCodeExtractOptions()
                        {
                            ScriptFolderPath = m_scriptFolderPath,
                            StartId = m_startTextDefineId,
                            Tag = m_tagPrefix,
                            UseExistingText = m_useExistingText,
                            TextDefinePath = m_textDefinePath,
                            OutputPath = outPath
                        };
                        TextProcessorTool.ExtractGRFromCode(options);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            DrawUsageTips("提示：此工具会扫描代码中的 G.R(\"中文\") 模式，自动替换为 G.R(TextDefine.xxx) 并更新枚举文件。");
        }

        #endregion

        #region UI绘制 - 预制体提取

        private void DrawPrefabExtractTab()
        {
            DrawSectionHeader("从预制体提取文本，静态不太参数的文本", "扫描UI预制体中的文本组件，提取内容并自动绑定UITextIDBinder。");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawFolderSelector(ref m_prefabFolderPath, "预制体文件夹", m_defaultPrefabPath);

                EditorGUILayout.Space(5);

                // 起始ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("起始 ID", GUILayout.Width(100));
                m_prefabStartId = EditorGUILayout.IntField(m_prefabStartId);
                EditorGUILayout.EndHorizontal();

                // 选项
                m_includeTextMeshPro = EditorGUILayout.ToggleLeft("包含 TextMeshPro 组件", m_includeTextMeshPro);
                // m_useExistingBinders = EditorGUILayout.ToggleLeft("使用现有的文本配置（不重复创建）", m_useExistingBinders);
                m_Include_m_Prefix_Node = EditorGUILayout.ToggleLeft("是否包含(m_/_)开头的节点", m_Include_m_Prefix_Node);

                // 统计信息
                DrawInfoBox($"当前最大ID: {m_maxPrefabExistingId}");

                EditorGUILayout.Space(10);

                if (GUILayout.Button("扫描预制体文本", GUILayout.Height(35)))
                {
                    StartScanPrefabs();
                }

                EditorGUILayout.Space(5);

                // 如果有扫描数据，显示开始生成按钮
                if (TextProcessorTool.PrefabTextEntries.Count > 0)
                {
                    if (GUILayout.Button("开始生成", GUILayout.Height(30)))
                    {
                        GeneratePrefabTextBinders();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            DrawUsageTips("提示：提取后的文本会自动添加 UITextIDBinder 组件，并生成配置文件。");
        }

        public static void StartScanPrefabs()
        {
            TextProcessorTool.SkippedTextObjects.Clear();
            var window = GetWindow<TextDefineEditorWindow>(m_title);
            window.ScanPrefabs();
        }

        private void ScanPrefabs()
        {
            if (m_prefabStartId != 0)
            {
                if (!Directory.Exists(m_outputPath))
                {
                    Directory.CreateDirectory(m_outputPath);
                }
                var outPath = Path.Combine(m_outputPath, $"PrefabText_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var options = new PrefabExtractOptions()
                {
                    ScriptFolderPath = m_prefabFolderPath,
                    StartId = m_prefabStartId,
                    Tag = m_tagPrefix,
                    UseExistingText = m_useExistingBinders,
                    TextDefinePath = m_textDefinePath,
                    Include_m_Prefix_Node = m_Include_m_Prefix_Node,
                    IncludeTextMeshPro = m_includeTextMeshPro,
                    OutputPath = outPath
                };
                TextProcessorTool.ExtractFromPrefabs(options);
            }
        }

        #endregion

        #region 辅助UI方法

        private void DrawSectionHeader(string title, string subtitle)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
            }
        }

        private void DrawFolderSelector(ref string path, string label, string defaultPath)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择文件夹", GUILayout.Width(100)))
            {
                var selected = EditorUtility.OpenFolderPanel(label, path, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    path = PathHelper.MakeRelativePath(selected);
                }
            }
            EditorGUILayout.LabelField($"{label}: {path}");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button($"重置为默认 ({defaultPath})", GUILayout.Height(20)))
            {
                path = defaultPath;
            }
        }

        private void DrawInfoBox(string info1) // , string info2
        {
            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(info1, MessageType.Info);
            // EditorGUILayout.HelpBox(info2, MessageType.Info);
            // EditorGUILayout.EndHorizontal();
        }

        private void DrawUsageTips(string tips)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(tips, MessageType.None);
        }

        #endregion

        #region 预制体扫描结果

        /// <summary>
        /// 绘制预制体扫描结果列表
        /// </summary>
        private void DrawPrefabScanResults()
        {
            EditorGUILayout.Space(10);
            DrawSectionHeader($"扫描结果 ({TextProcessorTool.PrefabTextEntries.Count} 个预制体)", "找到的文本组件列表");

            // 固定高度的可滚动区域
            float listHeight = 180;
            m_prefabResultScrollPosition = EditorGUILayout.BeginScrollView(m_prefabResultScrollPosition, GUILayout.Height(listHeight));
            {
                // 绘制表头
                DrawPrefabResultHeader();
                // 绘制每个预制体的文本组件（使用for循环避免迭代时修改集合）
                for (int i = 0; i < TextProcessorTool.PrefabTextEntries.Count; i++)
                {
                    DrawPrefabEntry(TextProcessorTool.PrefabTextEntries[i]);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制结果表头
        /// </summary>
        private void DrawPrefabResultHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("预制体 -> 组件", GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("操作", GUILayout.Width(180));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制单个预制体条目及其文本组件
        /// </summary>
        private void DrawPrefabEntry(PrefabTextEntry prefabEntry)
        {
            if (prefabEntry == null || prefabEntry.NoBinderTextObjects == null)
                return;

            for (int i = 0; i < prefabEntry.NoBinderTextObjects.Count; i++)
            {
                var textObj = prefabEntry.NoBinderTextObjects[i];
                if (textObj == null)
                    continue;

                // 检查是否已跳过
                if (TextProcessorTool.SkippedTextObjects.Contains(textObj))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                {
                    // 左边
                    string displayText = $"{prefabEntry.PrefabName} -> {textObj.name} -> {prefabEntry.NoBinderTextContents[i]} -> {prefabEntry.BinderTextIDs[i]}";
                    EditorGUILayout.LabelField(displayText, GUILayout.Width(500));

                    GUILayout.FlexibleSpace();

                    // 右边：操作按钮
                    // 定位按钮 - 打开预制体预览并定位到组件
                    if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                    {
                        if (PrefabUtility.IsPartOfPrefabAsset(prefabEntry.prefab))
                        {
                            // 使用PrefabStage进入预制体编辑模式
                            PrefabStage prefabStage = PrefabStageUtility.OpenPrefab(prefabEntry.PrefabPath);
                            if (prefabStage != null)
                            {
                                var curTrans = FindChild(prefabStage.prefabContentsRoot.transform, prefabEntry.prefab.transform, textObj.transform);

                                EditorGUIUtility.PingObject(curTrans.gameObject);
                                Selection.activeGameObject = curTrans.gameObject;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("The selected object is not part of a prefab.");
                        }
                    }

                    // 保存预制体按钮
                    // if (GUILayout.Button("保存预制体", EditorStyles.miniButtonMid, GUILayout.Width(80)))
                    // {
                    //     SavePrefabWithBinder(prefabEntry, textObj, i);
                    // }

                    // 跳过按钮
                    if (GUILayout.Button("跳过", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        TextProcessorTool.SkippedTextObjects.Add(textObj);
                        ScanPrefabs();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 保存预制体并添加Binder
        /// </summary>
        private void SavePrefabWithBinder(PrefabTextEntry prefabEntry, GameObject textObj, int index)
        {
            if (prefabEntry.prefab == null || textObj == null)
            {
                Debug.LogWarning("预制体或文本对象无效");
                return;
            }

            // 添加UITextIDBinder组件
            var binder = textObj.GetComponent<UITextIDBinder>();
            if (binder == null)
            {
                binder = textObj.AddComponent<UITextIDBinder>();
            }

            binder.TextID = prefabEntry.BinderTextIDs[index];

            // 标记对象为脏
            EditorUtility.SetDirty(textObj);

            // 标记预制体为脏，保存更改
            EditorUtility.SetDirty(prefabEntry.prefab);
            AssetDatabase.SaveAssets();

            // 刷新当前打开的预制体阶段
            var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null)
            {
                EditorUtility.SetDirty(currentStage.prefabContentsRoot);
                currentStage.ClearDirtiness();
            }

            Debug.Log($"已保存预制体 {prefabEntry.PrefabName}，为 {textObj.name} 添加了 UITextIDBinder");
        }

        /// <summary>
        /// 批量生成预制体TextBinder
        /// </summary>
        private void GeneratePrefabTextBinders()
        {
            int successCount = 0;
            int skipCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < TextProcessorTool.PrefabTextEntries.Count; i++)
                {
                    var prefabEntry = TextProcessorTool.PrefabTextEntries[i];
                    if (prefabEntry?.prefab == null || prefabEntry.NoBinderTextObjects == null)
                        continue;

                    for (int j = 0; j < prefabEntry.NoBinderTextObjects.Count; j++)
                    {
                        var textObj = prefabEntry.NoBinderTextObjects[j];

                        // 跳过已标记跳过的对象
                        if (textObj == null || TextProcessorTool.SkippedTextObjects.Contains(textObj))
                        {
                            skipCount++;
                            continue;
                        }

                        // 添加UITextIDBinder组件
                        var binder = textObj.GetComponent<UITextIDBinder>();
                        if (binder == null)
                        {
                            binder = textObj.AddComponent<UITextIDBinder>();
                        }
                        binder.TextID = prefabEntry.BinderTextIDs[j];

                        EditorUtility.SetDirty(prefabEntry.prefab);
                        successCount++;
                    }
                }

                TextProcessorTool.ConfirmExtractFromPrefabs();
                AssetDatabase.SaveAssets();
                Debug.Log($"批量生成完成：成功添加 {successCount} 个Binder，跳过 {skipCount} 个");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // 清空数据
            TextProcessorTool.PrefabTextEntries.Clear();
            TextProcessorTool.SkippedTextObjects.Clear();
        }

        private Transform FindChild(Transform lhs, Transform rhs, Transform rhsChild)
        {
            List<Transform> list = new List<Transform>();
            List<int> indexList = new List<int>();
            var trans = rhsChild;
            while (trans != null && trans != rhs)
            {
                list.Add(trans);
                indexList.Add(trans.GetSiblingIndex());

                trans = trans.parent;
            }

            var curTrans = lhs.transform;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                curTrans = curTrans.GetChild(indexList[i]);
            }
            return curTrans;
        }

        #endregion
    }
}