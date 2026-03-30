using System;
using System.Collections;
using System.Collections.Generic;
using DGame;
using HybridCLR.Editor.Settings;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

public static class DGameHotfixSettingsProvider
{
    private static SerializedProperty m_projectName;
    private static SerializedProperty m_hotUpdateAssemblies;
    private static SerializedProperty m_aotMetaAssemblies;
    private static SerializedProperty m_logicMainDllName;
    private static SerializedProperty m_assemblyTextAssetExtension;
    private static SerializedProperty m_assemblyTextAssetPath;
    private static SerializedProperty m_updateStyle;
    private static SerializedProperty m_updateNotice;
    private static SerializedProperty m_resDownloadPath;
    private static SerializedProperty m_fallbackResDownloadPath;
    private static SerializedProperty m_loadResWayWebGL;
    private static SerializedProperty m_isAutoAssetCopyToBuildAddress;
    private static SerializedProperty m_buildAddress;
    private static SerializedProperty m_replaceAssetPathWithAddress;
    private static SerializedProperty m_forceGenerateAtlas;
    private static SerializedProperty m_updateUIDefineConfigPath;
    private static SerializedProperty m_enableAddressable;
    private static SerializedProperty m_packageName;

    private static int m_logicMainDllNameIndex;
    private static SerializedObject m_serializedObject;
    private static int m_packageNameIndex;
    private static string[] m_packageNames;

    // UI状态
    private static Vector2 m_scrollPosition;
    private static bool m_showBasicSettings = true;
    private static bool m_showUpdateSettings = true;
    private static bool m_showResourceSettings = true;
    private static bool m_showAdvancedSettings = true;

    private const string HOTFIX_SETTINGS_PATH = "Project/DGame/HotfixSettings";

    [MenuItem("DGame Tools/Settings/Hotfix Settings", priority = 0)]
    public static void OpenHotfixSettings() => SettingsService.OpenProjectSettings(HOTFIX_SETTINGS_PATH);

    [SettingsProvider]
    public static SettingsProvider CreateHotfixSettingsProvider()
    {
        return new SettingsProvider(HOTFIX_SETTINGS_PATH, SettingsScope.Project)
        {
            label = "[DGame] 热更新设置",
            deactivateHandler = () =>
            {
                UpdateSettingsInspector.HotUpdateAssembliesList?.Clear();
                UpdateSettingsInspector.AotMetaAssembliesList?.Clear();
                UpdateSettingsInspector.HotUpdateAssembliesList = null;
                UpdateSettingsInspector.AotMetaAssembliesList = null;
            },
            activateHandler = (searchContext, rootElement) =>
            {
                UpdateSettingsInspector.ForceUpdateAssemblies2();
                var settings = Settings.UpdateSettings;
                if (settings == null)
                {
                    return;
                }
                UpdateSettingsInspector.HotUpdateAssembliesList = new List<string>(settings.HotUpdateAssemblies);
                UpdateSettingsInspector.AotMetaAssembliesList = new List<string>(settings.AOTMetaAssemblies);
                m_serializedObject = new SerializedObject(settings);
                if (m_serializedObject == null)
                {
                    return;
                }
                InitializeProperties();
            },
            guiHandler = (searchContext) =>
            {
                if (m_serializedObject == null)
                {
                    EditorGUILayout.HelpBox("未找到热更新设置文件", MessageType.Error);
                    return;
                }

                var updateSettings = Settings.UpdateSettings;
                if (updateSettings == null)
                {
                    EditorGUILayout.HelpBox("热更新设置文件无效", MessageType.Error);
                    return;
                }

                m_serializedObject.Update();

                // 绘制标题区域
                DrawHeader();
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
                {
                    DrawHybridCLRSettings();
                    DrawBasicSettings(updateSettings);
                    DrawAssemblySettings(updateSettings);
                    DrawUpdateSettings();
                    DrawResourceSettings();
                    DrawAdvancedSettings();
                    DrawUpdateProcedureSettings();
                    DrawStatistics(updateSettings);
                }
                EditorGUI.EndDisabledGroup();
                m_serializedObject.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSettingsInspector.HandleSettingsChange(updateSettings);
                }
                DrawActionButtons(updateSettings);
            },
            keywords = new HashSet<string>(new[] { "DGame", "Settings", "Hotfix", "HybridCLR", "热更新", "程序集", "资源更新" })
        };
    }

    private static void InitializeProperties()
    {
        m_projectName = m_serializedObject.FindProperty("projectName");
        m_hotUpdateAssemblies = m_serializedObject.FindProperty("HotUpdateAssemblies");
        m_aotMetaAssemblies = m_serializedObject.FindProperty("AOTMetaAssemblies");
        m_logicMainDllName = m_serializedObject.FindProperty("LogicMainDllName");
        m_assemblyTextAssetExtension = m_serializedObject.FindProperty("AssemblyTextAssetExtension");
        m_assemblyTextAssetPath = m_serializedObject.FindProperty("AssemblyTextAssetPath");
        m_updateStyle = m_serializedObject.FindProperty("UpdateStyle");
        m_updateNotice = m_serializedObject.FindProperty("UpdateNotice");
        m_resDownloadPath = m_serializedObject.FindProperty("m_resDownloadPath");
        m_fallbackResDownloadPath = m_serializedObject.FindProperty("m_fallbackResDownloadPath");
        m_loadResWayWebGL = m_serializedObject.FindProperty("m_loadResWayWebGL");
        m_isAutoAssetCopyToBuildAddress = m_serializedObject.FindProperty("m_isAutoAssetCopyToBuildAddress");
        m_buildAddress = m_serializedObject.FindProperty("m_buildAddress");
        m_replaceAssetPathWithAddress = m_serializedObject.FindProperty("m_replaceAssetPathWithAddress");
        m_forceGenerateAtlas = m_serializedObject.FindProperty("m_forceGenerateAtlas");
        m_updateUIDefineConfigPath = m_serializedObject.FindProperty("updateUIDefineConfigPath");
        m_enableAddressable = m_serializedObject.FindProperty("m_enableAddressable");
        m_packageName = m_serializedObject.FindProperty("packageName");
    }

    private static void DrawHeader()
    {
        // 标题区域
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        var titleStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField(new GUIContent("DGame 热更新配置系统", "HybridCLR Hotfix Configuration System"),
            titleStyle, GUILayout.Height(30));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 副标题
        var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) }
        };

        EditorGUILayout.LabelField("配置 HybridCLR 热更新和资源管理系统", subtitleStyle);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);
    }

    private static void DrawHybridCLRSettings()
    {
        EditorGUILayout.BeginVertical("HelpBox");
        {
            EditorGUILayout.LabelField(new GUIContent("HybridCLR 设置", "启用或禁用热更新功能"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                var originalColor = GUI.color;

                // 启用按钮
                GUI.color = new Color(0.2f, 0.8f, 0.3f, 1f);
                if (GUILayout.Button(new GUIContent("启用 HybridCLR", "启用热更新功能"),
                    GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("启用 HybridCLR", "确定要启用 HybridCLR 热更新功能吗？", "确定", "取消"))
                    {
                        HybridCLRDefineSymbols.EnableHybridCLR();
                        Debug.Log("HybridCLR 已启用");
                    }
                }

                // 禁用按钮
                GUI.color = new Color(1f, 0.4f, 0.3f, 1f);
                if (GUILayout.Button(new GUIContent("禁用 HybridCLR", "禁用热更新功能"),
                    GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("禁用 HybridCLR", "确定要禁用 HybridCLR 热更新功能吗？", "确定", "取消"))
                    {
                        HybridCLRDefineSymbols.DisableHybridCLR();
                        Debug.Log("HybridCLR 已禁用");
                    }
                }

                GUI.color = originalColor;
            }
            EditorGUILayout.EndHorizontal();

            // 状态显示
            bool isHybridCLREnabled =
#if ENABLE_HYBRIDCLR
                true;
#else
                false;
#endif
            string statusText = isHybridCLREnabled ? "HybridCLR 已启用" : "HybridCLR 已禁用";
            MessageType statusType = isHybridCLREnabled ? MessageType.Info : MessageType.Warning;

            EditorGUILayout.HelpBox(statusText, statusType);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private static void DrawBasicSettings(UpdateSettings updateSettings)
    {
        m_showBasicSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showBasicSettings,
            new GUIContent("基础设置", "项目基础信息配置"));

        if (m_showBasicSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.PropertyField(m_projectName,
                    new GUIContent("项目名称", "项目标识名称，用于版本管理和资源识别"));

                EditorGUILayout.Space(5);

                // 主业务逻辑DLL选择
                EditorGUILayout.LabelField("主业务逻辑配置", EditorStyles.boldLabel);

                if (updateSettings.HotUpdateAssemblies.Count > 0)
                {
                    m_logicMainDllNameIndex = updateSettings.HotUpdateAssemblies.IndexOf(m_logicMainDllName.stringValue);
                    if (m_logicMainDllNameIndex < 0)
                    {
                        m_logicMainDllNameIndex = 0;
                        m_logicMainDllName.stringValue = updateSettings.HotUpdateAssemblies[0];
                    }

                    m_logicMainDllNameIndex = EditorGUILayout.Popup(
                        new GUIContent("主业务逻辑DLL", "游戏主要逻辑所在的DLL文件"),
                        m_logicMainDllNameIndex, updateSettings.HotUpdateAssemblies.ToArray());

                    if (m_logicMainDllName.stringValue != updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex])
                    {
                        m_logicMainDllName.stringValue = updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex];
                    }

                    // 显示当前选择的DLL
                    EditorGUILayout.HelpBox($"当前主逻辑: {updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex]}",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("请先配置热更程序集", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUILayout.Space(8);
    }

    private static void DrawAssemblySettings(UpdateSettings updateSettings)
    {
        EditorGUILayout.BeginVertical("HelpBox");
        {
            // 热更程序集
            EditorGUILayout.LabelField("热更程序集", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_hotUpdateAssemblies,
                new GUIContent("热更DLL列表", "需要热更新的程序集文件列表"));

            EditorGUILayout.Space(5);

            // AOT程序集
            EditorGUILayout.LabelField("AOT程序集", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_aotMetaAssemblies,
                new GUIContent("AOT元数据DLL", "AOT泛型补充元数据程序集列表"));

            EditorGUILayout.Space(5);

            // DLL资产设置
            EditorGUILayout.LabelField("DLL资产设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_assemblyTextAssetExtension,
                new GUIContent("DLL打包后缀", "DLL转换为TextAsset的后缀名"));
            EditorGUILayout.PropertyField(m_assemblyTextAssetPath,
                new GUIContent("DLL资产路径", "DLL TextAsset资源的存放路径"));

            EditorGUILayout.Space(3);

            // 统计信息
            int hotUpdateCount = updateSettings.HotUpdateAssemblies?.Count ?? 0;
            int aotMetaCount = updateSettings.AOTMetaAssemblies?.Count ?? 0;
            string statsText = $"热更程序集: {hotUpdateCount} 个, AOT程序集: {aotMetaCount} 个";
            EditorGUILayout.HelpBox(statsText, MessageType.Info);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(8);
    }

    private static void DrawUpdateSettings()
    {
        m_showUpdateSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showUpdateSettings,
            new GUIContent("更新设置", "更新策略和提示设置"));

        if (m_showUpdateSettings)
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
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUILayout.Space(8);
    }

    private static void DrawResourceSettings()
    {
        m_showResourceSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showResourceSettings,
            new GUIContent("资源服务器", "资源下载和加载设置"));

        if (m_showResourceSettings)
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

                string serverStatus = hasPrimaryServer ?
                    (hasFallbackServer ? "主备服务器已配置" : "仅配置了主服务器") :
                    "未配置资源服务器";

                EditorGUILayout.HelpBox(serverStatus,
                    hasPrimaryServer ? MessageType.Info : MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUILayout.Space(8);
    }

    private static void DrawAdvancedSettings()
    {
        m_showAdvancedSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAdvancedSettings,
            new GUIContent("打包设置", "构建和部署相关设置"));

        if (m_showAdvancedSettings)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
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

                EditorGUILayout.HelpBox(tips, MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUILayout.Space(8);
    }

    private static void DrawUpdateProcedureSettings()
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
                string path = Application.dataPath + "/Resources/" + m_updateUIDefineConfigPath.stringValue + ".json";
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

            MessageType type = isEmptyPath || !isExit || !m_enableAddressable.boolValue
                ? MessageType.Warning
                : MessageType.Info;
            EditorGUILayout.HelpBox(tips, type);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(8);
    }

    private static List<string> GetBuildPackageNames()
    {
        List<string> packageNames = new List<string>();

        foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
        {
            packageNames.Add(package.PackageName);
        }
        return packageNames;
    }


    private static void DrawStatistics(UpdateSettings updateSettings)
    {
        EditorGUILayout.BeginVertical("Box");
        {
            EditorGUILayout.LabelField("配置概览", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("项目名称:", GUILayout.Width(80));
                EditorGUILayout.LabelField(m_projectName.stringValue, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("热更程序集:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{updateSettings.HotUpdateAssemblies?.Count ?? 0} 个", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("AOT程序集:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{updateSettings.AOTMetaAssemblies?.Count ?? 0} 个", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("主逻辑DLL:", GUILayout.Width(80));
                string mainDll = string.IsNullOrEmpty(m_logicMainDllName.stringValue) ?
                    "未设置" : m_logicMainDllName.stringValue;
                EditorGUILayout.LabelField(mainDll, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("资源服务器:", GUILayout.Width(80));
                string serverStatus = string.IsNullOrEmpty(m_resDownloadPath.stringValue) ?
                    "未配置" : "已配置";
                EditorGUILayout.LabelField(serverStatus, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private static void DrawActionButtons(UpdateSettings updateSettings)
    {
        GUILayout.Space(15);

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();

            // 同步到HybridCLR按钮
            var originalColor = GUI.color;
            GUI.color = new Color(1f, 0.8f, 0.2f, 1f);
            if (GUILayout.Button(new GUIContent("同步到HybridCLR", "强制同步程序集配置到HybridCLR"),
                GUILayout.Width(140), GUILayout.Height(35)))
            {
                UpdateSettingsInspector.ForceUpdateAssemblies();
                Debug.Log("程序集配置已同步到HybridCLR");
            }

            GUILayout.Space(10);

            // 保存配置按钮
            GUI.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            if (GUILayout.Button(new GUIContent("保存配置", "保存当前所有设置"),
                GUILayout.Width(100), GUILayout.Height(35)))
            {
                // EditorUtility.SetDirty(updateSettings);
                m_serializedObject.ApplyModifiedProperties();
                UpdateSettingsInspector.ForceUpdateAssemblies();
                // AssetDatabase.SaveAssets();
                Debug.Log("热更新配置已保存");
            }

            GUI.color = originalColor;

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private static string GetUpdateStyleDescription(UpdateNotice style)
    {
        switch (style)
        {
            case UpdateNotice.Notice: return "更新时会显示提示对话框";
            case UpdateNotice.NoNotice: return "静默更新，不显示提示";
            default: return "未知类型";
        }
    }

    private static string GetUpdateStyleDescription(UpdateStyle style)
    {
        switch (style)
        {
            case UpdateStyle.Optional: return "非强制";
            case UpdateStyle.Force: return "强制更新";
            default: return "未知类型";
        }
    }
}