using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(RootModule))]
    internal sealed class RootModuleInspector : DGameInspector
    {
        private const string NONE_OPTION_NAME = "<None>";
        private static readonly float[] m_gameSpeedArr = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private static readonly string[] m_gameSpeedForDisplay = new string[] { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_stringUtilHelperTypeName = null;
        private SerializedProperty m_logHelperTypeName = null;
        private SerializedProperty m_jsonHelperTypeName = null;
        private SerializedProperty m_gameSpeed = null;
        private SerializedProperty m_frameRate = null;
        private SerializedProperty m_runInBackground = null;
        private SerializedProperty m_neverSleep = null;
        private SerializedProperty m_memoryStrictCheckType = null;
        private SerializedProperty m_editorLanguage = null;

        private string[] m_stringUtilHelperTypeNames = null;
        private int m_stringUtilHelperTypeNameIndex = 0;

        private string[] m_logHelperTypeNames = null;
        private int m_logHelperTypeNameIndex = 0;

        private string[] m_jsonHelperTypeNames = null;
        private int m_jsonHelperTypeNameIndex = 0;

        // UI状态
        private Vector2 m_scrollPosition;
        private bool m_showGlobalHelperSetting = true;
        private bool m_showMemorySetting = true;
        private bool m_showPerformanceSetting = true;
        private bool m_showSystemSetting = true;

        private string[] m_memoryStrictCheckTypeNames = null;
        private int m_memoryStrictCheckTypeIndex = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            RootModule rootModule = (RootModule)target;

            // 绘制标题区域
            DrawInspectorHeader();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                // DrawEditorLanguageSettings(rootModule);
                DrawGlobalHelperSettings(rootModule);
                DrawMemoryPoolSettings(rootModule);
                DrawPerformanceSettings(rootModule);
                DrawSystemSettings(rootModule);
                DrawStatistics(rootModule);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEditorLanguageSettings(RootModule rootModule)
        {
            // EditorGUILayout.BeginVertical("HelpBox");
            // {
            //     EditorGUILayout.LabelField("编辑器模式语言设置");
            //     EditorGUILayout.BeginHorizontal();
            //     EditorGUILayout.LabelField(new GUIContent("Editor Language"), GUILayout.Width(120));
            //     EditorGUILayout.PropertyField(m_editorLanguage, GUIContent.none);
            //
            //     EditorGUILayout.EndHorizontal();
            //
            //     EditorGUILayout.Space(3);
            //
            //     string helperStatus = "编辑器模式下运行的语言类型: " + LocalizationUtil.GetLanguage(rootModule.EditorLanguage);
            //     EditorGUILayout.HelpBox(helperStatus, MessageType.Info);
            // }
            // EditorGUILayout.EndVertical();
        }

        private void DrawMemoryPoolSettings(RootModule rootModule)
        {
            m_showMemorySetting = EditorGUILayout.BeginFoldoutHeaderGroup(m_showMemorySetting,
                new GUIContent("内存池设置"));

            if (m_showMemorySetting)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    EditorGUILayout.BeginHorizontal();
                    int memoryStrictCheckTypeIndex = EditorGUILayout.Popup("内存池强制检查开启模式", m_memoryStrictCheckTypeIndex, m_memoryStrictCheckTypeNames);
                    if (memoryStrictCheckTypeIndex != m_memoryStrictCheckTypeIndex)
                    {
                        m_memoryStrictCheckTypeIndex = memoryStrictCheckTypeIndex;
                        m_memoryStrictCheckType.enumValueIndex = memoryStrictCheckTypeIndex <= 0 ? 0 : memoryStrictCheckTypeIndex;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(3);

                    // 内存池检查状态
                    string helperStatus = GetMemorySettingCheckStatus((MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex);
                    EditorGUILayout.HelpBox(helperStatus,
                        IsMemorySettingWarning() ? MessageType.Warning : MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
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

            EditorGUILayout.LabelField(new GUIContent("游戏根模块配置", "Root Module Configuration"),
                titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 状态指示
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            };

            EditorGUILayout.LabelField("配置游戏核心系统和辅助器", statusStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawGlobalHelperSettings(RootModule rootModule)
        {
            m_showGlobalHelperSetting = EditorGUILayout.BeginFoldoutHeaderGroup(m_showGlobalHelperSetting,
                new GUIContent("全局辅助器设置", "配置各种工具辅助器"));

            if (m_showGlobalHelperSetting)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 字符串辅助器
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("字符串辅助器", "字符串处理工具"), GUILayout.Width(120));
                    int textHelperSelectedIndex = EditorGUILayout.Popup(m_stringUtilHelperTypeNameIndex, m_stringUtilHelperTypeNames);
                    if (textHelperSelectedIndex != m_stringUtilHelperTypeNameIndex)
                    {
                        m_stringUtilHelperTypeNameIndex = textHelperSelectedIndex;
                        m_stringUtilHelperTypeName.stringValue = textHelperSelectedIndex <= 0 ? null : m_stringUtilHelperTypeNames[textHelperSelectedIndex];
                    }
                    EditorGUILayout.EndHorizontal();

                    // 日志辅助器
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("日志辅助器", "日志输出工具"), GUILayout.Width(120));
                    int logHelperSelectedIndex = EditorGUILayout.Popup(m_logHelperTypeNameIndex, m_logHelperTypeNames);
                    if (logHelperSelectedIndex != m_logHelperTypeNameIndex)
                    {
                        m_logHelperTypeNameIndex = logHelperSelectedIndex;
                        m_logHelperTypeName.stringValue = logHelperSelectedIndex <= 0 ? null : m_logHelperTypeNames[logHelperSelectedIndex];
                    }
                    EditorGUILayout.EndHorizontal();

                    // JSON辅助器
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("JSON辅助器", "JSON序列化工具"), GUILayout.Width(120));
                    int jsonHelperSelectedIndex = EditorGUILayout.Popup(m_jsonHelperTypeNameIndex, m_jsonHelperTypeNames);
                    if (jsonHelperSelectedIndex != m_jsonHelperTypeNameIndex)
                    {
                        m_jsonHelperTypeNameIndex = jsonHelperSelectedIndex;
                        m_jsonHelperTypeName.stringValue = jsonHelperSelectedIndex <= 0 ? null : m_jsonHelperTypeNames[jsonHelperSelectedIndex];
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(3);

                    // 辅助器状态
                    string helperStatus = GetHelperStatus();
                    EditorGUILayout.HelpBox(helperStatus,
                        IsAllHelpersConfigured() ? MessageType.Info : MessageType.Warning);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawPerformanceSettings(RootModule rootModule)
        {
            m_showPerformanceSetting = EditorGUILayout.BeginFoldoutHeaderGroup(m_showPerformanceSetting,
                new GUIContent("性能设置", "游戏性能和帧率配置"));

            if (m_showPerformanceSetting)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 游戏帧率
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("游戏帧率", "目标帧率设置"), GUILayout.Width(100));
                    int frameRate = EditorGUILayout.IntSlider(m_frameRate.intValue, 1, 120);
                    EditorGUILayout.EndHorizontal();

                    if (frameRate != m_frameRate.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.FrameRate = frameRate;
                        }
                        else
                        {
                            m_frameRate.intValue = frameRate;
                        }
                    }

                    // 帧率建议
                    string frameRateAdvice = GetFrameRateAdvice(frameRate);
                    EditorGUILayout.HelpBox(frameRateAdvice, MessageType.Info);

                    EditorGUILayout.Space(5);

                    // 游戏速度
                    EditorGUILayout.LabelField("游戏速度", EditorStyles.boldLabel);

                    float gameSpeed = EditorGUILayout.Slider("速度倍率", m_gameSpeed.floatValue, 0f, 8f);

                    // 快速选择按钮
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("快速设置:", GUILayout.Width(60));
                    int selectedGameSpeed = GUILayout.Toolbar(GetSelectedGameSpeed(gameSpeed), m_gameSpeedForDisplay);
                    EditorGUILayout.EndHorizontal();

                    if (selectedGameSpeed >= 0)
                    {
                        gameSpeed = GetGameSpeed(selectedGameSpeed);
                    }

                    if (Mathf.Abs(gameSpeed - m_gameSpeed.floatValue) > 0.001f)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.GameSpeed = gameSpeed;
                        }
                        else
                        {
                            m_gameSpeed.floatValue = gameSpeed;
                        }
                    }

                    // 游戏速度说明
                    if (Mathf.Approximately(gameSpeed, 0f))
                    {
                        EditorGUILayout.HelpBox("游戏暂停", MessageType.Warning);
                    }
                    else if (gameSpeed > 1f)
                    {
                        EditorGUILayout.HelpBox($"加速模式: {gameSpeed}x", MessageType.Info);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawSystemSettings(RootModule rootModule)
        {
            m_showSystemSetting = EditorGUILayout.BeginFoldoutHeaderGroup(m_showSystemSetting,
                new GUIContent("系统设置", "运行时系统行为配置"));

            if (m_showSystemSetting)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 后台运行
                    bool runInBackground = EditorGUILayout.ToggleLeft(
                        new GUIContent("可在后台运行", "游戏窗口失去焦点时继续运行"),
                        m_runInBackground.boolValue);

                    if (runInBackground != m_runInBackground.boolValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.RunInBackground = runInBackground;
                        }
                        else
                        {
                            m_runInBackground.boolValue = runInBackground;
                        }
                    }

                    // 从不休眠
                    bool neverSleep = EditorGUILayout.ToggleLeft(
                        new GUIContent("从不休眠", "防止系统进入睡眠模式"),
                        m_neverSleep.boolValue);

                    if (neverSleep != m_neverSleep.boolValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.NeverSleep = neverSleep;
                        }
                        else
                        {
                            m_neverSleep.boolValue = neverSleep;
                        }
                    }

                    EditorGUILayout.Space(3);

                    // 系统设置说明
                    string systemStatus = GetSystemStatus(runInBackground, neverSleep);
                    MessageType messageType = (runInBackground || neverSleep) ? MessageType.Info : MessageType.None;
                    EditorGUILayout.HelpBox(systemStatus, messageType);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawStatistics(RootModule rootModule)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("配置概览", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("辅助器配置:", GUILayout.Width(80));
                    string helperStatus = IsAllHelpersConfigured() ? "完整" : "不完整";
                    EditorGUILayout.LabelField(helperStatus, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("当前帧率:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{m_frameRate.intValue} FPS", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("游戏速度:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{m_gameSpeed.floatValue}x", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("系统状态:", GUILayout.Width(80));
                    string systemStatus = GetSystemStatusSummary();
                    EditorGUILayout.LabelField(systemStatus, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("内存池强制检查开启状态:", GUILayout.Width(150));
                    string systemStatus = GetMemorySettingStatusSummary();
                    EditorGUILayout.LabelField(systemStatus, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                // 操作按钮
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("刷新类型", GUILayout.Height(25)))
                    {
                        RefreshTypeNames();
                    }

                    if (GUILayout.Button("保存配置", GUILayout.Height(25)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("根模块配置已保存");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private string GetHelperStatus()
        {
            int configuredCount = 0;
            int totalCount = 3;

            if (!string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue)) configuredCount++;
            if (!string.IsNullOrEmpty(m_logHelperTypeName.stringValue)) configuredCount++;
            if (!string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue)) configuredCount++;

            return $"辅助器配置: {configuredCount}/{totalCount} 已配置" +
                   (configuredCount < totalCount ? "\n建议配置所有辅助器以获得完整功能" : "");
        }
        
        private string GetMemorySettingCheckStatus(MemoryStrictCheckType type)
        {
            string tips1 = "";
            string tips2 = "内存池已启用严格检查，这将极大程序影响性能。";
            switch (type)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    tips1 = "总是开启";
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    tips1 = "仅在开发模式启用";
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    tips1 = "仅在编辑器中启用";
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    tips1 = "总是禁用";
                    tips2 = "禁用状态，不影响性能。";
                    break;
            }

            return $"内存池强制检查开启状态: {tips1}\n{tips2}";
        }

        private bool IsAllHelpersConfigured()
        {
            return !string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue) &&
                   !string.IsNullOrEmpty(m_logHelperTypeName.stringValue) &&
                   !string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue);
        }

        private bool IsMemorySettingWarning()
        {
            return (MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex != MemoryStrictCheckType.AlwaysDisable;
        }

        private string GetFrameRateAdvice(int frameRate)
        {
            if (frameRate <= 30) return "低帧率模式 - 适合性能要求低的设备";
            if (frameRate <= 60) return "标准帧率 - 适合大多数游戏";
            if (frameRate <= 90) return "高帧率模式 - 适合动作游戏";
            return "超高帧率 - 适合竞技游戏，消耗更多资源";
        }

        private string GetSystemStatus(bool runInBackground, bool neverSleep)
        {
            List<string> features = new List<string>();

            if (runInBackground) features.Add("后台运行");
            if (neverSleep) features.Add("不休眠");

            if (features.Count == 0) return "标准系统模式";
            return "启用功能: " + string.Join("，", features);
        }

        private string GetMemorySettingStatusSummary()
        {
            string tips1 = "未配置";
            switch ((MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    tips1 = "总是开启";
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    tips1 = "仅在开发模式启用";
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    tips1 = "仅在编辑器中启用";
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    tips1 = "总是禁用";
                    break;
            }
            return tips1;
        }

        private string GetSystemStatusSummary()
        {
            List<string> status = new List<string>();

            if (m_runInBackground.boolValue) status.Add("后台");
            if (m_neverSleep.boolValue) status.Add("不休眠");

            return status.Count > 0 ? string.Join("+", status) : "标准";
        }

        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (int i = 0; i < m_gameSpeedArr.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, m_gameSpeedArr[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0)
            {
                return m_gameSpeedArr[0];
            }

            if (selectedGameSpeed >= m_gameSpeedArr.Length)
            {
                return m_gameSpeedArr[^1];
            }
            return m_gameSpeedArr[selectedGameSpeed];
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }
            m_stringUtilHelperTypeName = serializedObject?.FindProperty("stringUtilHelperTypeName");
            m_logHelperTypeName = serializedObject?.FindProperty("logHelperTypeName");
            m_jsonHelperTypeName = serializedObject?.FindProperty("jsonHelperTypeName");
            m_gameSpeed = serializedObject?.FindProperty("gameSpeed");
            m_frameRate = serializedObject?.FindProperty("frameRate");
            m_runInBackground = serializedObject?.FindProperty("runInBackground");
            m_neverSleep = serializedObject?.FindProperty("neverSleep");
            m_memoryStrictCheckType = serializedObject?.FindProperty("m_memoryStrictCheckType");
            m_editorLanguage = serializedObject?.FindProperty("editorLanguage");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            List<string> textHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            textHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(Utility.StringUtil.IStringUtilHelper)));
            m_stringUtilHelperTypeNames = textHelperTypeNames.ToArray();
            m_stringUtilHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue))
            {
                m_stringUtilHelperTypeNameIndex = textHelperTypeNames.IndexOf(m_stringUtilHelperTypeName.stringValue);
                if (m_stringUtilHelperTypeNameIndex <= 0)
                {
                    m_stringUtilHelperTypeNameIndex = 0;
                    m_stringUtilHelperTypeName.stringValue = null;
                }
            }

            List<string> logHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            logHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(DGameLog.ILogHelper)));
            m_logHelperTypeNames = logHelperTypeNames.ToArray();
            m_logHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_logHelperTypeName.stringValue))
            {
                m_logHelperTypeNameIndex = logHelperTypeNames.IndexOf(m_logHelperTypeName.stringValue);
                if (m_logHelperTypeNameIndex <= 0)
                {
                    m_logHelperTypeNameIndex = 0;
                    m_logHelperTypeName.stringValue = null;
                }
            }

            List<string> jsonHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            jsonHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(Utility.IJsonHelper)));
            m_jsonHelperTypeNames = jsonHelperTypeNames.ToArray();
            m_jsonHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue))
            {
                m_jsonHelperTypeNameIndex = jsonHelperTypeNames.IndexOf(m_jsonHelperTypeName.stringValue);
                if (m_jsonHelperTypeNameIndex <= 0)
                {
                    m_jsonHelperTypeNameIndex = 0;
                    m_jsonHelperTypeName.stringValue = null;
                }
            }

            List<string> tempList = new List<string>();
            System.Type enumType = typeof(MemoryStrictCheckType);
            // Array enumValues = Enum.GetValues(typeof(MemoryStrictCheckType));
            string[] enumNames = Enum.GetNames(enumType);
            for (int i = 0; i < enumNames.Length; i++)
            {
                var enumName = enumNames[i];

                if (enumName == "AlwaysEnable")
                {
                    tempList.Add("总是启用");
                }
                else if (enumName == "OnlyEnableWhenDevelopment")
                {
                    tempList.Add("仅在开发模式启用");
                }
                else if (enumName == "OnlyEnableInEditor")
                {
                    tempList.Add("仅在编辑器中启用");
                }
                else if (enumName == "AlwaysDisable")
                {
                    tempList.Add("总是禁用");
                }
            }

            m_memoryStrictCheckTypeNames = tempList.ToArray();
            m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex;
            if (m_memoryStrictCheckType.enumValueIndex <= 0)
            {
                m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex  = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}