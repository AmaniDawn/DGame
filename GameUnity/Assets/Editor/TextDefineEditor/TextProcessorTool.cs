using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if TextMeshPro
using TMPro;
#endif

namespace DGame
{
    /// <summary>
    /// 文本处理核心类 - 负责代码分析、文本提取、预制体处理等功能
    /// </summary>
    public static class TextProcessorTool
    {
        #region 常量定义

        /// <summary>
        /// 匹配 G.R("xxx") 模式，捕获引号内的内容
        /// </summary>
        private static readonly Regex m_G_R_Pattern = new(@"G\s*\.\s*R\s*\(\s*""([^""]*)""", RegexOptions.Compiled);

        /// <summary>
        /// 匹配字符串格式参数 {0}, {1} 等
        /// </summary>
        private static readonly Regex m_formatParamRegex = new(@"\{[\d}]+\}", RegexOptions.Compiled);

        /// <summary>
        /// 匹配中文字符
        /// </summary>
        private static readonly Regex m_chineseRegex = new(@"[\u4e00-\u9fa5]", RegexOptions.Compiled);

        private static StringBuilder m_sb = new StringBuilder();

        #endregion

        #region 代码文本提取

        public static void ExtractGRFromCode(GRCodeExtractOptions options)
        {
            var strMap = new Dictionary<string, TextEntry>();
            var writeList = new List<TextEntry>();
            var paramList = new List<int>();
            var codeFiles = Directory.GetFiles(options.ScriptFolderPath, "*.cs", SearchOption.AllDirectories);
            int listCount = codeFiles.Length;
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < listCount; i++)
                {
                    writeList.Clear();
                    var file = codeFiles[i];
                    EditorUtility.DisplayProgressBar("提取代码G.R文本", $"正在处理: {Path.GetFileName(file)}", (float)i / listCount);

                    try
                    {
                        string text = File.ReadAllText(file);
                        m_G_R_Pattern.Replace(text, match =>
                        {
                            string content = match.Groups[1].Value;
                            if (!strMap.TryGetValue(content, out var textEntry))
                            {
                                paramList.Clear();
                                var collect = m_formatParamRegex.Matches(content);
                                foreach (Match item in collect)
                                {
                                    // 匹配{0} {1} 上的数字0 1
                                    if(match.Success && int.TryParse(item.Value.Trim('{', '}'), out int paramIndex))
                                    {
                                        paramList.Add(paramIndex);
                                    }
                                }

                                int maxParamIndex = -1;

                                foreach (var paramIndex in paramList)
                                {
                                    if (paramIndex > maxParamIndex)
                                    {
                                        maxParamIndex = paramIndex;
                                    }
                                }
                                var paramCnt = maxParamIndex + 1;

                                if (options.UseExistingText &&
                                    EditorConfigLoader.TryGetTextDefineStr(content, out var defineText))
                                {
                                    textEntry = new TextEntry(content, defineText, paramCnt);
                                }
                                else
                                {
                                    textEntry = new TextEntry(content, options.StartId++, paramCnt);
                                    strMap[content] = textEntry;
                                }
                            }
                            writeList.Add(textEntry);
                            return match.Value;
                        });

                        if (writeList.Count > 0)
                        {
                            foreach (var textEntry in writeList)
                            {
                                var entry = $"\"{textEntry.Content}\"";
                                text = text.Replace(entry, $"TextDefine.{textEntry.TextDefineIdName}");
                            }

                            using (var stream = new FileStream(file, FileMode.Create))
                            {
                                var bytes = Encoding.UTF8.GetBytes(text);
                                stream.Write(bytes, 0, bytes.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理文件 {file} 时出错: {ex.Message}");
                    }
                }

                string textDefine = File.ReadAllText(options.TextDefinePath);
                bool connect = false;
                int startIndex = textDefine.IndexOf("// AutoBuildStart", StringComparison.Ordinal);
                int endIndex = textDefine.IndexOf("\t\t// AutoBuildEnd", StringComparison.Ordinal);

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    var source = textDefine.Substring(startIndex, endIndex - startIndex);
                    string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    // 如果前一个ID已经定义了，就插在前一个id后面
                    if (EditorConfigLoader.TryGetTextDefineStr(options.StartId - 1, out var defineText))
                    {
                        foreach (var textItem in allText)
                        {
                            if (textItem.Contains(defineText))
                            {
                                var oldIndex = endIndex;
                                var textIndex = textDefine.IndexOf(textItem, StringComparison.Ordinal);
                                endIndex = textIndex + textItem.Length + "\r\n".Length;
                                endIndex = endIndex > oldIndex ? endIndex : oldIndex;
                                connect = true;
                                break;
                            }
                        }
                    }
                }

                // 注册到TextDefine
                string space = "\t\t";

                if (endIndex >= 0)
                {
                    m_sb.Clear();

                    // 不是连接上一行的情况 空一行
                    if (!connect)
                    {
                        m_sb.Append("\n");
                    }
                    // 块标签
                    if (!connect && !string.IsNullOrEmpty(options.Tag))
                    {
                        m_sb.Append($"{space}// {options.Tag}\n");
                    }

                    int cnt = 0;
                    foreach (var textEntry in strMap.Values)
                    {
                        var ret = $"{space}{textEntry.TextDefineIdName}{(cnt == 0 ? $" = {textEntry.TextDefineId}" : "")},   // {textEntry.Content}\r\n";
                        m_sb.Append(ret);
                        cnt++;
                    }
                    textDefine = textDefine.Insert(endIndex, m_sb.ToString());

                    using (var stream = new FileStream(options.TextDefinePath, FileMode.Create))
                    {
                        var bytes = Encoding.UTF8.GetBytes(textDefine);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                // 写表
                m_sb.Clear();
                var valueList = strMap.Values.ToList();
                valueList.Sort((a, b) => a.TextDefineId - b.TextDefineId);
                for (int i = 0; i < valueList.Count; i++)
                {
                    var value = valueList[i];

                    if (value == null)
                    {
                        continue;
                    }

                    m_sb.Append(
                        $"\t{value.TextDefineId}\t{(value.ParamCount > 0 ? value.ParamCount.ToString() : string.Empty)}\t{value.Content}\t{(string.IsNullOrEmpty(options.Tag) ? string.Empty : options.Tag)}\r\n");
                }
                var excelContent = Encoding.UTF8.GetBytes(m_sb.ToString());
                using (var stream = new FileStream(options.OutputPath, FileMode.Create))
                {
                    stream.Write(excelContent, 0, excelContent.Length);
                    System.Diagnostics.Process.Start("notepad.exe", options.OutputPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
        }

        #endregion

        #region 预制体提取

        private static readonly List<string> m_excludePrefabNames = new List<string>()
        {
            "GMPanel", "SwitchTabItem", "LogUI", "TipsUI", "RedDotItem"
        };

        public static List<TextEntry> PrefabWriteList = new List<TextEntry>();
        public static List<PrefabTextEntry> PrefabTextEntries = new List<PrefabTextEntry>();
        private static PrefabExtractOptions m_prefabExtractOptions;
        // 跟踪跳过的text对象
        public static HashSet<GameObject> SkippedTextObjects = new HashSet<GameObject>();

        public static void ExtractFromPrefabs(PrefabExtractOptions options)
        {
            m_prefabExtractOptions = options;
            PrefabTextEntries.Clear();
            PrefabWriteList.Clear();
            var prefabPaths = Directory.GetFiles(options.ScriptFolderPath, "*.prefab", SearchOption.AllDirectories);
            int listCount = prefabPaths.Length;
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < listCount; i++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                    EditorUtility.DisplayProgressBar("扫描UI预制体", $"正在处理: {Path.GetFileName(prefabPaths[i])}", (float)i / listCount);
                    if (prefab == null)
                    {
                        continue;
                    }
                    var prefabName = prefab.name;
                    if (m_excludePrefabNames.Contains(prefabName))
                    {
                        continue;
                    }
                    PrefabTextEntry prefabTextEntry = null;
                    TextEntry textEntry = null;
                    try
                    {
                        var textComponents = prefab.GetComponentsInChildren<Text>();
                        foreach (var textComponent in textComponents)
                        {
                            if(SkippedTextObjects.Contains(textComponent.gameObject))
                            {
                                continue;
                            }
                            // 获取源预制体资产
                            var source = PrefabUtility.GetCorrespondingObjectFromSource(textComponent.gameObject);
                            if (source != null)
                            {
                                var textParent = textComponent.transform.parent;
                                if (textParent != null)
                                {
                                    // 做筛选
                                }

                                var sourceParent = source.transform.parent;

                                if (sourceParent == null)
                                {
                                    continue;
                                }
                            }

                            var textComponentName = textComponent.name;

                            if ((!options.Include_m_Prefix_Node &&
                                 (textComponentName.StartsWith("m_") || textComponentName.StartsWith("_")))
                                || textComponentName == "Placeholder")
                            {
                                continue;
                            }

                            string result = Regex.Replace(textComponent.text, @"\s+", "");

                            if (string.IsNullOrEmpty(result))
                            {
                                continue;
                            }

                            textEntry = new TextEntry(result, options.StartId++, 0);
                            PrefabWriteList.Add(textEntry);

                            if (textComponent.GetComponent<UITextIDBinder>() == null)
                            {
                                if (prefabTextEntry == null)
                                {
                                    prefabTextEntry = new PrefabTextEntry()
                                    {
                                        prefab = prefab,
                                        PrefabName = prefabName,
                                        PrefabPath = prefabPaths[i],
                                    };
                                    PrefabTextEntries.Add(prefabTextEntry);
                                }
                                prefabTextEntry.NoBinderTextObjects.Add(textComponent.gameObject);
                                prefabTextEntry.NoBinderTextContents.Add(result);
                                prefabTextEntry.BinderTextIDs.Add(textEntry.TextDefineId);
                            }
                        }
#if TextMeshPro

                        if (options.IncludeTextMeshPro)
                        {
                            var textProComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>();

                            foreach (var textPro in textProComponents)
                            {
                                if(SkippedTextObjects.Contains(textPro.gameObject))
                                {
                                    continue;
                                }
                                // 获取源预制体资产
                                var source = PrefabUtility.GetCorrespondingObjectFromSource(textPro.gameObject);

                                if (source != null)
                                {
                                    var textParent = textPro.transform.parent;

                                    if (textParent != null)
                                    {
                                        // 做筛选
                                    }

                                    var sourceParent = source.transform.parent;

                                    if (sourceParent == null)
                                    {
                                        continue;
                                    }
                                }

                                var textComponentName = textPro.name;

                                if ((!options.Include_m_Prefix_Node &&
                                     (textComponentName.StartsWith("m_") || textComponentName.StartsWith("_")))
                                    || textComponentName == "Placeholder")
                                {
                                    continue;
                                }

                                string result = Regex.Replace(textPro.text, @"\s+", "");

                                if (string.IsNullOrEmpty(result))
                                {
                                    continue;
                                }

                                textEntry = new TextEntry(result, options.StartId++, 0);
                                PrefabWriteList.Add(textEntry);

                                if (textPro.GetComponent<UITextIDBinder>() == null)
                                {
                                    if (prefabTextEntry == null)
                                    {
                                        prefabTextEntry = new PrefabTextEntry()
                                        {
                                            prefab = prefab,
                                            PrefabName = prefabName,
                                            PrefabPath = prefabPaths[i],
                                        };
                                        PrefabTextEntries.Add(prefabTextEntry);
                                    }

                                    prefabTextEntry.NoBinderTextObjects.Add(textPro.gameObject);
                                    prefabTextEntry.NoBinderTextContents.Add(result);
                                    prefabTextEntry.BinderTextIDs.Add(textEntry.TextDefineId);
                                }
                            }
                        }

#endif
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理文件 {prefabPaths[i]} 时出错: {ex.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
        }

        public static void ConfirmExtractFromPrefabs()
        {
            if (m_prefabExtractOptions == null)
            {
                Debug.LogError("m_prefabExtractOptions 参数无效，请检查或重新扫描。");
                return;
            }
            string textDefine = File.ReadAllText(m_prefabExtractOptions.TextDefinePath);
            bool connect = false;
            int startIndex = textDefine.IndexOf("// PrefabAutoBuildStart", StringComparison.Ordinal);
            int endIndex = textDefine.IndexOf("\t\t// PrefabAutoBuildEnd", StringComparison.Ordinal);

            if (startIndex > 0 && endIndex >= startIndex)
            {
                var source = textDefine.Substring(startIndex, endIndex - startIndex);
                string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                // 如果前一个ID已经定义了，就插在前一个id后面
                if (EditorConfigLoader.TryGetTextDefineStr(m_prefabExtractOptions.StartId - 1, out var defineText))
                {
                    foreach (var textItem in allText)
                    {
                        if (textItem.Contains(defineText))
                        {
                            var oldIndex = endIndex;
                            var textIndex = textDefine.IndexOf(textItem, StringComparison.Ordinal);
                            endIndex = textIndex + textItem.Length + "\r\n".Length;
                            endIndex = endIndex > oldIndex ? endIndex : oldIndex;
                            connect = true;
                            break;
                        }
                    }
                }
            }

            // 注册到TextDefine
            string space = "\t\t";

            if (endIndex >= 0)
            {
                m_sb.Clear();

                // 不是连接上一行的情况 空一行
                if (!connect)
                {
                    m_sb.Append("\n");
                }
                // 块标签
                if (!connect && !string.IsNullOrEmpty(m_prefabExtractOptions.Tag))
                {
                    m_sb.Append($"{space}// {m_prefabExtractOptions.Tag}\n");
                }

                int cnt = 0;
                foreach (var textEntry in PrefabWriteList)
                {
                    var ret = $"{space}{textEntry.TextDefineIdName}{(cnt == 0 ? $" = {textEntry.TextDefineId}" : "")},   // {textEntry.Content}\r\n";
                    m_sb.Append(ret);
                    cnt++;
                }
                textDefine = textDefine.Insert(endIndex, m_sb.ToString());

                using (var stream = new FileStream(m_prefabExtractOptions.TextDefinePath, FileMode.Create))
                {
                    var bytes = Encoding.UTF8.GetBytes(textDefine);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            // 写表
            m_sb.Clear();
            var valueList = PrefabWriteList;
            valueList.Sort((a, b) => a.TextDefineId - b.TextDefineId);
            for (int i = 0; i < valueList.Count; i++)
            {
                var value = valueList[i];

                if (value == null)
                {
                    continue;
                }

                m_sb.Append(
                    $"\t{value.TextDefineId}\t{(value.ParamCount > 0 ? value.ParamCount.ToString() : string.Empty)}\t{value.Content}\t{(string.IsNullOrEmpty(m_prefabExtractOptions.Tag) ? string.Empty : m_prefabExtractOptions.Tag)}\r\n");
            }
            var excelContent = Encoding.UTF8.GetBytes(m_sb.ToString());
            using (var stream = new FileStream(m_prefabExtractOptions.OutputPath, FileMode.Create))
            {
                stream.Write(excelContent, 0, excelContent.Length);
                System.Diagnostics.Process.Start("notepad.exe", m_prefabExtractOptions.OutputPath);
            }
        }

        #endregion

        #region 工具方法

        public static int GetLastTextDefineID(string textDefinePath)
        {
            int id = 0;
            string textDefine = File.ReadAllText(textDefinePath);
            int startIndex = textDefine.IndexOf("// AutoBuildStart", StringComparison.Ordinal);
            int endIndex = textDefine.IndexOf("// AutoBuildEnd", StringComparison.Ordinal);

            if (startIndex > 0 && endIndex >= startIndex)
            {
                var source = textDefine.Substring(startIndex, endIndex - startIndex);
                string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (allText.Length > 0)
                {
                    var matchText = "LabelID";

                    for (int i = allText.Length - 1; i >= 0; i--)
                    {
                        var text = allText[i];

                        if (text.Contains(matchText))
                        {
                            var index1 = text.IndexOf(matchText, StringComparison.Ordinal) + matchText.Length;
                            var index2 = text.IndexOf(",", StringComparison.Ordinal);

                            if (index2 > index1)
                            {
                                var label = text.Substring(index1, index2 - index1);
                                int.TryParse(label, out id);
                            }
                            break;
                        }
                    }
                }
            }
            return id;
        }

        public static int GetLastPrefabTextDefineID(string textDefinePath)
        {
            int id = 0;
            string textDefine = File.ReadAllText(textDefinePath);
            int startIndex = textDefine.IndexOf("// PrefabAutoBuildStart", StringComparison.Ordinal);
            int endIndex = textDefine.IndexOf("// PrefabAutoBuildEnd", StringComparison.Ordinal);

            if (startIndex > 0 && endIndex >= startIndex)
            {
                var source = textDefine.Substring(startIndex, endIndex - startIndex);
                string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (allText.Length > 0)
                {
                    var matchText = "LabelID";

                    for (int i = allText.Length - 1; i >= 0; i--)
                    {
                        var text = allText[i];

                        if (text.Contains(matchText))
                        {
                            var index1 = text.IndexOf(matchText, StringComparison.Ordinal) + matchText.Length;
                            var index2 = text.IndexOf(",", StringComparison.Ordinal);

                            if (index2 > index1)
                            {
                                var label = text.Substring(index1, index2 - index1);
                                int.TryParse(label, out id);
                            }
                            break;
                        }
                    }
                }
            }
            return id;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 文本条目
    /// </summary>
    public class TextEntry
    {
        public string Content { get; set; }
        public int TextDefineId { get; set; }
        public int ParamCount { get; set; }
        public string TextDefineIdName { get; set; }
        public string ExtraParam { get; set; }
        public string ItemName { get; set; }
        public string Component { get; set; }
        public string TextType { get; set; }

        public TextEntry(string content, int textDefineId, int paramCnt, string extraParam, string itemName,
            string component, string textType)
        {
            Content = content;
            TextDefineId = textDefineId;
            ParamCount = paramCnt;
            TextDefineIdName = GetDefineIdName(textDefineId);
            ExtraParam = extraParam;
            ItemName = itemName;
            Component = component;
            TextType = textType;
        }

        public TextEntry(string content, string defineText, int paramCnt, string extraParam = "")
        {
            Content = content;
            TextDefineId = 0;
            ParamCount = paramCnt;
            TextDefineIdName = defineText;
            ExtraParam = extraParam;
        }
        public TextEntry(string content, int textDefineId, int paramCnt, string extraParam = "")
        {
            Content = content;
            TextDefineId = textDefineId;
            TextDefineIdName = GetDefineIdName(textDefineId);
            ExtraParam = extraParam;
            ParamCount = paramCnt;
        }
        private string GetDefineIdName(int id)
        {
            return "LabelID" + id;
        }
    }

    /// <summary>
    /// 预制体文本条目
    /// </summary>
    public class PrefabTextEntry
    {
        public GameObject prefab { get; set; }
        public string PrefabName { get; set; }
        public string PrefabPath { get; set; }
        public List<GameObject> NoBinderTextObjects { get; set; } = new List<GameObject>();
        public List<string> NoBinderTextContents { get; set; } = new List<string>();
        public List<int> BinderTextIDs { get; set; } = new List<int>();
    }

    /// <summary>
    /// 代码提取选项
    /// </summary>
    public class GRCodeExtractOptions
    {
        public string ScriptFolderPath { get; set; }
        public int StartId { get; set; }
        public string Tag { get; set; }
        public bool UseExistingText { get; set; }
        public string TextDefinePath { get; set; }
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// 预制体提取选项
    /// </summary>
    public class PrefabExtractOptions
    {
        public string ScriptFolderPath { get; set; }
        public int StartId { get; set; }
        public string Tag { get; set; }
        public bool UseExistingText { get; set; }
        public bool Include_m_Prefix_Node { get; set; }
        public bool IncludeTextMeshPro { get; set; }
        public string TextDefinePath { get; set; }
        public string OutputPath { get; set; }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 路径辅助类
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// 将绝对路径转换为相对于Assets的相对路径
        /// </summary>
        public static string MakeRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            var assetsIndex = absolutePath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return absolutePath.Substring(assetsIndex).Replace('\\', '/');
            }

            return absolutePath.Replace('\\', '/');
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 将绝对路径转换为相对于Assets的相对路径
        /// </summary>
        public static string MakeRelativePath(this string absolutePath)
        {
            return PathHelper.MakeRelativePath(absolutePath);
        }
    }

    #endregion
}