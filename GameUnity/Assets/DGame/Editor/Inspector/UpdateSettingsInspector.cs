using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using YooAsset.Editor;

namespace DGame
{
    [CustomEditor(typeof(UpdateSettings), true)]
    public class UpdateSettingsInspector : UnityEditor.Editor
    {
        private SerializedProperty m_projectName;
        private SerializedProperty m_hotUpdateAssemblies;
        private SerializedProperty m_aotMetaAssemblies;
        private SerializedProperty m_logicMainDllName;
        private SerializedProperty m_assemblyTextAssetExtension;
        private SerializedProperty m_assemblyTextAssetPath;
        private SerializedProperty m_updateStyle;
        private SerializedProperty m_updateNotice;
        private SerializedProperty m_resDownloadPath;
        private SerializedProperty m_fallbackResDownloadPath;
        private SerializedProperty m_loadResWayWebGL;
        private SerializedProperty m_isAutoAssetCopyToBuildAddress;
        private SerializedProperty m_buildAddress;
        private SerializedProperty m_replaceAssetPathWithAddress;
        private SerializedProperty m_forceGenerateAtlas;
        private SerializedProperty m_packageVersionMode;
        private SerializedProperty m_manualBuildVersion;
        private SerializedProperty m_updateUIDefineConfigPath;
        private SerializedProperty m_enableAddressable;
        private SerializedProperty m_packageName;

        private int m_packageNameIndex;
        private string[] m_packageNames;
        public static List<string> HotUpdateAssembliesList;
        public static List<string> AotMetaAssembliesList;
        private int m_logicMainDllNameIndex;

        // UI状态
        private Vector2 m_scrollPosition;

        // 颜色定义
        private Color m_warningColor = new Color(1f, 0.6f, 0.2f, 1f);
        private Color m_successColor = new Color(0.2f, 0.8f, 0.3f, 1f);

        private void OnDestroy()
        {
            HotUpdateAssembliesList?.Clear();
            AotMetaAssembliesList?.Clear();
            HotUpdateAssembliesList = null;
            AotMetaAssembliesList = null;
        }

        private void OnEnable()
        {
            ForceUpdateAssemblies2();
            m_projectName = serializedObject.FindProperty("projectName");
            m_hotUpdateAssemblies = serializedObject.FindProperty("HotUpdateAssemblies");
            m_aotMetaAssemblies = serializedObject.FindProperty("AOTMetaAssemblies");
            m_logicMainDllName = serializedObject.FindProperty("LogicMainDllName");
            m_assemblyTextAssetExtension = serializedObject.FindProperty("AssemblyTextAssetExtension");
            m_assemblyTextAssetPath = serializedObject.FindProperty("AssemblyTextAssetPath");
            m_updateStyle = serializedObject.FindProperty("UpdateStyle");
            m_updateNotice = serializedObject.FindProperty("UpdateNotice");
            m_resDownloadPath = serializedObject.FindProperty("m_resDownloadPath");
            m_fallbackResDownloadPath = serializedObject.FindProperty("m_fallbackResDownloadPath");
            m_loadResWayWebGL = serializedObject.FindProperty("m_loadResWayWebGL");
            m_isAutoAssetCopyToBuildAddress = serializedObject.FindProperty("m_isAutoAssetCopyToBuildAddress");
            m_buildAddress = serializedObject.FindProperty("m_buildAddress");
            m_replaceAssetPathWithAddress = serializedObject.FindProperty("m_replaceAssetPathWithAddress");
            m_forceGenerateAtlas = serializedObject.FindProperty("m_forceGenerateAtlas");
            m_packageVersionMode = serializedObject.FindProperty("m_packageVersionMode");
            m_manualBuildVersion = serializedObject.FindProperty("m_manualBuildVersion");
            m_updateUIDefineConfigPath = serializedObject.FindProperty("updateUIDefineConfigPath");
            m_enableAddressable = serializedObject.FindProperty("m_enableAddressable");
            m_packageName = serializedObject.FindProperty("packageName");

            UpdateSettings updateSettings = (UpdateSettings)target;

            if (updateSettings != null)
            {
                HotUpdateAssembliesList = new List<string>(updateSettings.HotUpdateAssemblies);
                AotMetaAssembliesList = new List<string>(updateSettings.AOTMetaAssemblies);
            }
        }

        public override void OnInspectorGUI()
        {
            // 绘制标题区域
            DrawInspectorHeader();

            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            UpdateSettings updateSettings = (UpdateSettings)target;


            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawBasicSettings(updateSettings);
                DrawAssemblySettings(updateSettings);
                DrawUpdateSettings(updateSettings);
                DrawResourceSettings(updateSettings);
                DrawAdvancedSettings(updateSettings);
                DrawUpdateProcedureSettings(updateSettings);
                DrawStatistics(updateSettings);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                // 底部操作按钮
                DrawActionButtons(updateSettings);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                HandleSettingsChange(updateSettings);
            }
        }

        private void DrawInspectorHeader()
        {
            GUILayout.Space(5);
            // 主标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUILayout.LabelField(new GUIContent("DGame热更新配置系统", "Hot Update Configuration System"),
                titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 副标题
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            };

            EditorGUILayout.LabelField("配置 HybridCLR 热更新参数", subtitleStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawBasicSettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.PropertyField(m_projectName, new GUIContent("项目名称", "项目标识名称"));
                EditorGUILayout.Space(5);
                // 主业务逻辑DLL选择
                m_logicMainDllNameIndex =
                    updateSettings.HotUpdateAssemblies.IndexOf(m_logicMainDllName.stringValue);

                if (m_logicMainDllNameIndex < 0 && updateSettings.HotUpdateAssemblies.Count > 0)
                {
                    m_logicMainDllNameIndex = 0;
                    m_logicMainDllName.stringValue = updateSettings.HotUpdateAssemblies[0];
                }

                if (updateSettings.HotUpdateAssemblies.Count > 0)
                {
                    m_logicMainDllNameIndex = EditorGUILayout.Popup(
                        new GUIContent("主业务逻辑DLL", "游戏主要逻辑所在的DLL"),
                        m_logicMainDllNameIndex, updateSettings.HotUpdateAssemblies.ToArray());

                    if (m_logicMainDllName.stringValue !=
                        updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex])
                    {
                        m_logicMainDllName.stringValue =
                            updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex];
                    }

                    // 显示当前选择的DLL
                    EditorGUILayout.HelpBox(
                        $"当前主逻辑: {updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex]}",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("请先配置热更程序集", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawAssemblySettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                // 热更程序集
                EditorGUILayout.LabelField("热更程序集", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_hotUpdateAssemblies,
                    new GUIContent("热更DLL列表", "需要热更新的程序集文件"));

                // AOT程序集
                EditorGUILayout.LabelField("AOT程序集", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_aotMetaAssemblies,
                    new GUIContent("AOT元数据DLL", "AOT泛型补充元数据程序集"));

                EditorGUILayout.Space(5);

                // DLL资产设置
                EditorGUILayout.LabelField("DLL资产设置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_assemblyTextAssetExtension,
                    new GUIContent("DLL打包后缀", "DLL转换为TextAsset的后缀名"));
                EditorGUILayout.PropertyField(m_assemblyTextAssetPath,
                    new GUIContent("DLL资产路径", "DLL TextAsset资源的存放路径"));

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("热更程序集: 包含需要热更新的业务逻辑\nAOT程序集: 为泛型提供元数据支持",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawUpdateSettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.PropertyField(m_updateStyle,
                    new GUIContent("强制更新类型", "资源更新策略"));
                EditorGUILayout.PropertyField(m_updateNotice,
                    new GUIContent("更新提示", "是否显示更新提示对话框"));

                EditorGUILayout.Space(3);

                // 更新策略说明
                string updateStyleDescription = GetUpdateStyleDescription((UpdateStyle)m_updateStyle.enumValueIndex);
                string noticeDescription = GetUpdateStyleDescription((UpdateNotice)m_updateNotice.enumValueIndex);

                EditorGUILayout.HelpBox($"更新策略: {updateStyleDescription}\n提示设置: {noticeDescription}",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawResourceSettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.PropertyField(m_resDownloadPath,
                    new GUIContent("主资源服务器", "资源下载主服务器地址"));
                EditorGUILayout.PropertyField(m_fallbackResDownloadPath,
                    new GUIContent("备用资源服务器", "主服务器不可用时的备用地址"));

                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(m_loadResWayWebGL,
                    new GUIContent("WebGL加载方式", "WebGL平台资源加载方式"));

                EditorGUILayout.Space(3);

                // 服务器状态检查
                bool hasPrimaryServer = !string.IsNullOrEmpty(m_resDownloadPath.stringValue);
                bool hasFallbackServer = !string.IsNullOrEmpty(m_fallbackResDownloadPath.stringValue);

                string serverStatus = hasPrimaryServer ? (hasFallbackServer ? "主备服务器已配置" : "仅配置了主服务器") : "未配置资源服务器";

                EditorGUILayout.HelpBox(serverStatus,
                    hasPrimaryServer ? MessageType.Info : MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawAdvancedSettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                GUILayout.Label(new GUIContent("打包设置", "构建和部署相关设置"));
                GUILayout.Space(5);
                // 自动复制设置
                bool isAutoAssetCopyToBuildAddress = EditorGUILayout.ToggleLeft(
                    new GUIContent("自动复制内嵌资源到存放路径", "构建时自动复制StreamingAssets资源文件"),
                    m_isAutoAssetCopyToBuildAddress.boolValue);

                if (isAutoAssetCopyToBuildAddress != m_isAutoAssetCopyToBuildAddress.boolValue)
                {
                    m_isAutoAssetCopyToBuildAddress.boolValue = isAutoAssetCopyToBuildAddress;
                }

                bool replaceAssetPathWithAddress = EditorGUILayout.ToggleLeft(
                    new GUIContent("是否使用可寻址资源代替资源路径", "说明：开启此项可以节省运行时清单占用的内存！"),
                    m_replaceAssetPathWithAddress.boolValue);

                if (replaceAssetPathWithAddress != m_replaceAssetPathWithAddress.boolValue)
                {
                    m_replaceAssetPathWithAddress.boolValue = replaceAssetPathWithAddress;
                }

                bool forceGenerateAtlas = EditorGUILayout.ToggleLeft(
                    new GUIContent("是否在打AB包前强制刷新图集"),
                    m_forceGenerateAtlas.boolValue);

                if (forceGenerateAtlas != m_forceGenerateAtlas.boolValue)
                {
                    m_forceGenerateAtlas.boolValue = forceGenerateAtlas;
                }

                EditorGUILayout.Space(3);

                EditorGUILayout.PropertyField(m_packageVersionMode,
                    new GUIContent("AB包版本模式", "自动使用时间戳或手动指定 YooAsset PackageVersion"));

                if ((PackageVersionMode)m_packageVersionMode.enumValueIndex == PackageVersionMode.Manual)
                {
                    EditorGUILayout.PropertyField(m_manualBuildVersion,
                        new GUIContent("手动AB包版本", "例如 1.1、1.2、1.2.3"));
                }

                EditorGUILayout.Space(3);

                // 构建地址
                EditorGUILayout.PropertyField(m_buildAddress,
                    new GUIContent("内嵌资源存放路径", "StreamingAssets资源文件复制目标地址"));

                EditorGUILayout.Space(3);

                string tips = string.Empty;

                if (m_isAutoAssetCopyToBuildAddress.boolValue)
                {
                    tips = "构建时将自动复制StreamingAssets资源到存放路径";
                }
                else
                {
                    tips = "需要手动处理StreamingAssets资源文件部署";
                }

                if (m_replaceAssetPathWithAddress.boolValue)
                {
                    tips += "\n可寻址资源代替资源路径：启用 (说明：开启此项可以节省运行时清单占用的内存！)";
                }
                else
                {
                    tips += "\n可寻址资源代替资源路径：禁用";
                }

                if (m_forceGenerateAtlas.boolValue)
                {
                    tips += "\n打AB包前强制刷新所有图集：启用";
                }
                else
                {
                    tips += "\n打AB包前强制刷新所有图集：禁用";
                }

                if ((PackageVersionMode)m_packageVersionMode.enumValueIndex == PackageVersionMode.Manual)
                {
                    string manualBuildVersion = m_manualBuildVersion.stringValue?.Trim();
                    if (string.IsNullOrEmpty(manualBuildVersion))
                    {
                        tips += "\nAB包版本模式：手动输入，但当前为空，打包时会回退到自动时间戳";
                    }
                    else
                    {
                        tips += $"\nAB包版本模式：手动输入，当前版本 = {manualBuildVersion}";
                    }
                }
                else
                {
                    tips += $"\nAB包版本模式：自动时间戳，本次预期版本 = {updateSettings.GetBuildPackageVersion()}";
                }

                EditorGUILayout.HelpBox(tips, MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawUpdateProcedureSettings(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                GUILayout.Label(new GUIContent("热更流程文本设置"));
                GUILayout.Space(5);

                // 资源包名
                EditorGUILayout.LabelField("资源包配置", EditorStyles.boldLabel);
                m_packageNames = GetBuildPackageNames().ToArray();
                m_packageNameIndex = Array.IndexOf(m_packageNames, m_packageName.stringValue);
                var setting = AssetBundleCollectorSettingData.Setting;
                if (m_packageNameIndex < 0)
                {
                    m_packageNameIndex = 0;
                }
                m_packageNameIndex = EditorGUILayout.Popup("资源包名", m_packageNameIndex, m_packageNames);
                if (m_packageName.stringValue != m_packageNames[m_packageNameIndex])
                {
                    m_packageName.stringValue = m_packageNames[m_packageNameIndex];

                    var package = setting.GetPackage(m_packageName.stringValue);

                    if (package != null)
                    {
                        m_enableAddressable.boolValue = package.EnableAddressable;
                    }
                }

                bool enableAddressable = EditorGUILayout.ToggleLeft(
                    new GUIContent("AB资源是否支持可寻址"), m_enableAddressable.boolValue);

                if (enableAddressable != m_enableAddressable.boolValue)
                {
                    m_enableAddressable.boolValue = enableAddressable;
                    var package = setting.GetPackage(m_packageName.stringValue);
                    if (package != null)
                    {
                        package.EnableAddressable = m_enableAddressable.boolValue;
                    }
                }

                EditorGUILayout.Space(3);

                // 构建地址
                EditorGUILayout.PropertyField(m_updateUIDefineConfigPath,
                    new GUIContent("热更新流程文本配置路径"));

                EditorGUILayout.Space(3);

                string tips = string.Empty;

                if (m_enableAddressable.boolValue)
                {
                    tips += $"{m_packageName.stringValue}资源包是否支持可寻址：启用";
                }
                else
                {
                    tips += $"{m_packageName.stringValue}资源包是否支持可寻址：禁用";
                }

                bool isEmptyPath = false;
                bool isExit = false;
                if (!string.IsNullOrEmpty(m_updateUIDefineConfigPath.stringValue))
                {
                    string path = Application.dataPath +  "/Resources/" + m_updateUIDefineConfigPath.stringValue + ".json";
                    isExit = System.IO.File.Exists(path);
                    if (!isExit)
                    {
                        tips += $"\n热更新流程文本配置路径: 没有找到相关的配置文件'Resources/{m_updateUIDefineConfigPath.stringValue}.json'";
                    }
                    else
                    {
                        tips += $"\n热更新流程文本配置路径: Resources/{m_updateUIDefineConfigPath.stringValue}.json";
                    }
                }
                else
                {
                    tips += $"\n热更新流程文本配置路径: 未配置";
                    isEmptyPath = true;
                }

                MessageType type = isEmptyPath || !isExit || !m_enableAddressable.boolValue ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(tips, type);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawStatistics(UpdateSettings updateSettings)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("配置统计", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("热更程序集:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(updateSettings.HotUpdateAssemblies.Count.ToString(),
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("AOT程序集:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(updateSettings.AOTMetaAssemblies.Count.ToString(),
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("主逻辑DLL:", GUILayout.Width(100));
                    string mainDll = string.IsNullOrEmpty(m_logicMainDllName.stringValue)
                        ? "未设置"
                        : m_logicMainDllName.stringValue;
                    EditorGUILayout.LabelField(mainDll, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("资源服务器:", GUILayout.Width(100));
                    string serverStatus = string.IsNullOrEmpty(m_resDownloadPath.stringValue) ? "未配置" : "已配置";
                    EditorGUILayout.LabelField(serverStatus, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawActionButtons(UpdateSettings updateSettings)
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                // 同步到HybridCLR按钮
                var originalColor = GUI.color;
                GUI.color = m_warningColor;

                if (GUILayout.Button(new GUIContent("同步到HybridCLR", "强制同步程序集配置到HybridCLR"),
                        GUILayout.Width(140), GUILayout.Height(30)))
                {
                    ForceUpdateAssemblies();
                    Debug.Log("程序集配置已同步到HybridCLR");
                }

                GUI.color = originalColor;

                GUILayout.Space(10);

                // 从HybridCLR同步按钮
                if (GUILayout.Button(new GUIContent("从HybridCLR同步", "从HybridCLR设置同步程序集"),
                        GUILayout.Width(140), GUILayout.Height(30)))
                {
                    ForceUpdateAssemblies2();
                    Debug.Log("已从HybridCLR同步程序集配置");
                }

                GUILayout.Space(10);

                // 保存配置按钮
                GUI.color = m_successColor;

                if (GUILayout.Button(new GUIContent("保存配置", "保存当前所有设置"),
                        GUILayout.Width(100), GUILayout.Height(30)))
                {
                    UpdateSettingsInspector.ForceUpdateAssemblies();
                    // AssetDatabase.SaveAssets();
                    Debug.Log("热更新配置已保存");
                }

                GUI.color = originalColor;

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private List<string> GetBuildPackageNames()
        {
            List<string> packageNames = new List<string>();

            foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
            {
                packageNames.Add(package.PackageName);
            }
            return packageNames;
        }

        private string GetUpdateStyleDescription(UpdateStyle style)
        {
            switch (style)
            {
                case UpdateStyle.Optional: return "非强制";
                case UpdateStyle.Force: return "强制更新";
                default: return "未知类型";
            }
        }

        private string GetUpdateStyleDescription(UpdateNotice style)
        {
            switch (style)
            {
                case UpdateNotice.Notice: return "更新时会显示提示对话框";
                case UpdateNotice.NoNotice: return "静默更新，不显示提示";
                default: return "未知类型";
            }
        }

        public static void HandleSettingsChange(UpdateSettings updateSettings)
        {
            EditorUtility.SetDirty(updateSettings);
            bool isHotChanged = !HotUpdateAssembliesList.SequenceEqual(updateSettings.HotUpdateAssemblies);
            bool isAOTChanged = !AotMetaAssembliesList.SequenceEqual(updateSettings.AOTMetaAssemblies);

            if (isHotChanged)
            {
                HotUpdateAssembliesList = new List<string>(updateSettings.HotUpdateAssemblies);
                SettingsUtil.HybridCLRSettings.hotUpdateAssemblies = updateSettings.HotUpdateAssemblies.ToArray();

                for (int i = 0; i < updateSettings.HotUpdateAssemblies.Count; i++)
                {
                    var assemblyName = updateSettings.HotUpdateAssemblies[i];

                    if (assemblyName.Length <= 0)
                    {
                        continue;
                    }
                    string assemblyNameWithoutExtension = assemblyName.Substring(0, assemblyName.LastIndexOf('.'));
                    SettingsUtil.HybridCLRSettings.hotUpdateAssemblies[i] = assemblyNameWithoutExtension;
                }

                DLogger.Info("======== HybridCLR => 热更程序集发生变化 ========");
            }

            if (isAOTChanged)
            {
                AotMetaAssembliesList = new List<string>(updateSettings.AOTMetaAssemblies);
                SettingsUtil.HybridCLRSettings.patchAOTAssemblies = updateSettings.AOTMetaAssemblies.ToArray();
                DLogger.Info("======== HybridCLR => AOT程序集发生变化 ========");
            }

            if (isAOTChanged || isHotChanged)
            {
                EditorUtility.SetDirty(SettingsUtil.HybridCLRSettings);
                HybridCLRSettings.Save();
            }
            AssetDatabase.SaveAssets();
        }

        public static void ForceUpdateAssemblies()
        {
            UpdateSettings updateSettings = null;
            string[] guids = AssetDatabase.FindAssets("t:UpdateSettings");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                updateSettings = AssetDatabase.LoadAssetAtPath<UpdateSettings>(path);
            }

            if (updateSettings == null)
            {
                DLogger.Error("======== 没有找到 updateSettings SO 文件 ========");
                return;
            }

            HybridCLRSettings.Instance.hotUpdateAssemblies = updateSettings.HotUpdateAssemblies.ToArray();

            for (int i = 0; i < updateSettings.HotUpdateAssemblies.Count; i++)
            {
                var assemblyName = updateSettings.HotUpdateAssemblies[i];
                if (assemblyName.Length <= 0)
                {
                    continue;
                }
                string assemblyNameWithoutExtension = assemblyName.Substring(0, assemblyName.LastIndexOf('.'));
                HybridCLRSettings.Instance.hotUpdateAssemblies[i] = assemblyNameWithoutExtension;
            }

            HybridCLRSettings.Instance.patchAOTAssemblies = updateSettings.AOTMetaAssemblies.ToArray();
            DLogger.Info("======== HybridCLR => AOT和热更程序集发生变化 ========");
            EditorUtility.SetDirty(HybridCLRSettings.Instance);
            EditorUtility.SetDirty(updateSettings);
            HybridCLRSettings.Save();
            AssetDatabase.SaveAssets();
        }

        public static void ForceUpdateAssemblies2()
        {
            UpdateSettings updateSettings = null;
            string[] guids = AssetDatabase.FindAssets("t:UpdateSettings");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                updateSettings = AssetDatabase.LoadAssetAtPath<UpdateSettings>(path);
            }

            if (updateSettings == null)
            {
                DLogger.Error("======== 没有找到 updateSettings SO 文件 ========");
                return;
            }

            updateSettings.HotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies.ToList();

            for (int i = 0; i < HybridCLRSettings.Instance.hotUpdateAssemblies.Length; i++)
            {
                var assemblyName = HybridCLRSettings.Instance.hotUpdateAssemblies[i];
                string assemblyNameWithoutExtension = assemblyName + ".dll";
                updateSettings.HotUpdateAssemblies[i] = assemblyNameWithoutExtension;
            }

            updateSettings.AOTMetaAssemblies = HybridCLRSettings.Instance.patchAOTAssemblies.ToList();
            DLogger.Info("======== 同步HybridCLR的设置 ========");
            EditorUtility.SetDirty(updateSettings);
            AssetDatabase.SaveAssets();
        }
    }

    // [InitializeOnLoad]
    // public static class HybridCLRSettingsSyncUpdateSettings
    // {
    //     private static List<string> m_lastHotUpdateAssemblies;
    //     private static List<string> m_lastAOTAssemblies;
    //
    //     static HybridCLRSettingsSyncUpdateSettings()
    //     {
    //         m_lastHotUpdateAssemblies = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);
    //         m_lastAOTAssemblies = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
    //         EditorApplication.update += OnEditorUpdate;
    //     }
    //
    //     private static void OnEditorUpdate()
    //     {
    //         if (HybridCLRSettings.Instance == null)
    //         {
    //             return;
    //         }
    //
    //         if (!m_lastHotUpdateAssemblies.SequenceEqual(HybridCLRSettings.Instance.hotUpdateAssemblies)
    //             || !m_lastAOTAssemblies.SequenceEqual(HybridCLRSettings.Instance.patchAOTAssemblies))
    //         {
    //             UpdateSettingsInspector.ForceUpdateAssemblies2();
    //             m_lastHotUpdateAssemblies = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);
    //             m_lastAOTAssemblies = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
    //         }
    //     }
    // }
}
