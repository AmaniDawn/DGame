using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(DebuggerDriver))]
    internal sealed class DebuggerDriverInspector : Editor
    {
        private SerializedProperty m_reporterScrollerSkin;
        private SerializedProperty m_activeWindowType;
        private SerializedProperty m_showFullWindow;
        private SerializedProperty m_consoleWindow;

        // UI状态
        private Vector2 m_scrollPosition;
        private bool m_showBasicSettings = true;
        private bool m_showWindowSettings = true;
        private bool m_showModuleSettings = true;
        private bool m_showAdvancedSettings = true;

        // 颜色定义
        private Color m_headerColor = new Color(0.1f, 0.5f, 0.8f, 1f);
        private Color m_warningColor = new Color(1f, 0.6f, 0.2f, 1f);
        private Color m_successColor = new Color(0.2f, 0.8f, 0.3f, 1f);

        // 调试窗口类型选项
        private static readonly string[] m_activeWindowTypeNames = new string[]
        {
            "始终开启",
            "仅开发版本开启",
            "仅编辑器开启",
            "始终关闭"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DebuggerDriver debuggerDriver = (DebuggerDriver)target;

            // 绘制标题区域
            DrawInspectorHeader();

            DrawBasicSettings(debuggerDriver);
            DrawWindowSettings(debuggerDriver);
            DrawModuleSettings(debuggerDriver);
            DrawAdvancedSettings(debuggerDriver);
            DrawStatistics(debuggerDriver);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInspectorHeader()
        {
            // 标题背景
            // Rect headerRect = EditorGUILayout.GetControlRect(false, 60);
            // EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, position.width, 60),
            //     new Color(0.1f, 0.1f, 0.1f, 0.8f));

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

            EditorGUILayout.LabelField(new GUIContent("调试器配置", "Debugger System Configuration"),
                titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 副标题
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            };

            EditorGUILayout.LabelField("配置运行时调试和性能分析系统", subtitleStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawBasicSettings(DebuggerDriver debuggerDriver)
        {
            m_showBasicSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showBasicSettings,
                new GUIContent("基础设置", "调试器基本行为配置"));

            if (m_showBasicSettings)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 调试窗口开启模式
                    EditorGUILayout.LabelField("窗口开启模式", EditorStyles.boldLabel);
                    int selectedIndex = EditorGUILayout.Popup("开启条件",
                        m_activeWindowType.enumValueIndex, m_activeWindowTypeNames);

                    if (selectedIndex != m_activeWindowType.enumValueIndex)
                    {
                        m_activeWindowType.enumValueIndex = selectedIndex;
                    }

                    // 显示当前模式说明
                    string modeDescription = GetActiveWindowTypeDescription(selectedIndex);
                    EditorGUILayout.HelpBox(modeDescription, MessageType.Info);

                    EditorGUILayout.Space(5);

                    // 全屏窗口模式
                    EditorGUILayout.LabelField("窗口显示模式", EditorStyles.boldLabel);
                    bool showFullWindow = EditorGUILayout.ToggleLeft(
                        new GUIContent("默认显示完整窗口", "启动时直接显示完整调试窗口"),
                        m_showFullWindow.boolValue);

                    if (showFullWindow != m_showFullWindow.boolValue)
                    {
                        m_showFullWindow.boolValue = showFullWindow;
                    }

                    if (showFullWindow)
                    {
                        EditorGUILayout.HelpBox("启动时直接显示完整调试界面", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("启动时显示悬浮图标，点击后展开完整界面", MessageType.Info);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawWindowSettings(DebuggerDriver debuggerDriver)
        {
            m_showWindowSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showWindowSettings,
                new GUIContent("窗口设置", "调试窗口位置和样式配置"));

            if (m_showWindowSettings)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 图标位置
                    EditorGUILayout.LabelField("悬浮图标位置", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("X:", GUILayout.Width(20));
                        float iconX = EditorGUILayout.FloatField(debuggerDriver.IconRect.x);
                        EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
                        float iconY = EditorGUILayout.FloatField(debuggerDriver.IconRect.y);

                        if (Mathf.Abs(iconX - debuggerDriver.IconRect.x) > 0.1f ||
                            Mathf.Abs(iconY - debuggerDriver.IconRect.y) > 0.1f)
                        {
                            debuggerDriver.IconRect = new Rect(iconX, iconY,
                                debuggerDriver.IconRect.width, debuggerDriver.IconRect.height);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 窗口位置和大小
                    EditorGUILayout.LabelField("完整窗口设置", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("位置:", GUILayout.Width(40));
                        float windowX = EditorGUILayout.FloatField(debuggerDriver.WindowRect.x, GUILayout.Width(60));
                        float windowY = EditorGUILayout.FloatField(debuggerDriver.WindowRect.y, GUILayout.Width(60));

                        EditorGUILayout.LabelField("大小:", GUILayout.Width(40));
                        float width = EditorGUILayout.FloatField(debuggerDriver.WindowRect.width, GUILayout.Width(60));
                        float height = EditorGUILayout.FloatField(debuggerDriver.WindowRect.height, GUILayout.Width(60));

                        if (Mathf.Abs(windowX - debuggerDriver.WindowRect.x) > 0.1f ||
                            Mathf.Abs(windowY - debuggerDriver.WindowRect.y) > 0.1f ||
                            Mathf.Abs(width - debuggerDriver.WindowRect.width) > 0.1f ||
                            Mathf.Abs(height - debuggerDriver.WindowRect.height) > 0.1f)
                        {
                            debuggerDriver.WindowRect = new Rect(windowX, windowY, width, height);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 窗口缩放
                    EditorGUILayout.LabelField("窗口缩放", EditorStyles.boldLabel);
                    float windowScale = EditorGUILayout.Slider("缩放比例", debuggerDriver.WindowScale, 0.5f, 3f);
                    if (Mathf.Abs(windowScale - debuggerDriver.WindowScale) > 0.01f)
                    {
                        debuggerDriver.WindowScale = windowScale;
                    }

                    EditorGUILayout.Space(3);

                    // 窗口设置操作按钮
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("重置窗口布局", GUILayout.Height(25)))
                        {
                            debuggerDriver.ResetWindowLayout();
                            Debug.Log("调试器窗口布局已重置");
                        }

                        if (GUILayout.Button("保存窗口位置", GUILayout.Height(25)))
                        {
                            PlayerPrefs.Save();
                            Debug.Log("窗口位置已保存");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawModuleSettings(DebuggerDriver debuggerDriver)
        {
            m_showModuleSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showModuleSettings,
                new GUIContent("模块设置", "调试器各功能模块配置"));

            if (m_showModuleSettings)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 控制台设置
                    EditorGUILayout.LabelField("控制台模块", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_consoleWindow,
                        new GUIContent("控制台窗口", "日志输出和异常显示配置"));

                    EditorGUILayout.Space(5);

                    // 皮肤设置
                    // EditorGUILayout.LabelField("界面皮肤", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_reporterScrollerSkin,
                        new GUIContent("界面皮肤设置", "调试器GUI皮肤配置"));

                    EditorGUILayout.Space(5);

                    // 可用模块列表
                    EditorGUILayout.LabelField("可用调试模块", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(GetAvailableModulesDescription(), MessageType.Info);

                    EditorGUILayout.Space(3);

                    // 模块管理按钮
                    // EditorGUILayout.BeginHorizontal();
                    // {
                    //     if (GUILayout.Button("刷新模块", GUILayout.Height(25)))
                    //     {
                    //         // 这里可以添加刷新模块的逻辑
                    //         // Debug.Log("调试模块已刷新");
                    //     }
                    //
                    //     if (GUILayout.Button("重置模块", GUILayout.Height(25)))
                    //     {
                    //         // 这里可以添加重置模块的逻辑
                    //         // Debug.Log("调试模块已重置");
                    //     }
                    // }
                    // EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawAdvancedSettings(DebuggerDriver debuggerDriver)
        {
            m_showAdvancedSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAdvancedSettings,
                new GUIContent("高级设置", "性能监控和内存分析配置"));

            if (m_showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // FPS计数器设置
                    EditorGUILayout.LabelField("FPS监控", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox($"FPS计数器: 实时监控游戏帧率\n更新间隔: {DebuggerDriver.Constant.DEFAULT_DEBUGGER_WINDOW_FPS_UPDATE_INTERVAL}秒", MessageType.Info);

                    EditorGUILayout.Space(5);

                    // 内存监控
                    EditorGUILayout.LabelField("内存分析", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("包含完整的内存分析模块:\n• 总体内存统计\n• 各类型资源内存占用\n• 对象池状态监控", MessageType.Info);

                    EditorGUILayout.Space(5);

                    // 性能分析
                    EditorGUILayout.LabelField("性能分析", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("性能分析模块提供:\n• CPU使用率监控\n• GPU性能统计\n• 渲染批次分析", MessageType.Info);

                    EditorGUILayout.Space(3);

                    // 高级功能按钮
                    // EditorGUILayout.BeginHorizontal();
                    // {
                    //     if (GUILayout.Button("导出性能报告", GUILayout.Height(25)))
                    //     {
                    //         // 这里可以添加导出性能报告的逻辑
                    //         // Debug.Log("性能报告已导出");
                    //     }
                    //
                    //     if (GUILayout.Button("清理调试数据", GUILayout.Height(25)))
                    //     {
                    //         // 这里可以添加清理调试数据的逻辑
                    //         // Debug.Log("调试数据已清理");
                    //     }
                    // }
                    // EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8);
        }

        private void DrawStatistics(DebuggerDriver debuggerDriver)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("配置概览", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("开启模式:", GUILayout.Width(80));
                    string modeName = m_activeWindowTypeNames[m_activeWindowType.enumValueIndex];
                    EditorGUILayout.LabelField(modeName, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("窗口模式:", GUILayout.Width(80));
                    string windowMode = m_showFullWindow.boolValue ? "完整窗口" : "悬浮图标";
                    EditorGUILayout.LabelField(windowMode, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("窗口缩放:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{debuggerDriver.WindowScale:F1}x", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("图标位置:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{debuggerDriver.IconRect.x:F0},{debuggerDriver.IconRect.y:F0}",
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("窗口位置:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{debuggerDriver.WindowRect.x:F0},{debuggerDriver.WindowRect.y:F0}",
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                // 操作按钮
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("测试调试器", GUILayout.Height(25)))
                    {
                        if (Application.isPlaying)
                        {
                            debuggerDriver.ActiveWindow = true;
                            Debug.Log("调试器已激活");
                        }
                        else
                        {
                            Debug.Log("请在运行模式下测试调试器");
                        }
                    }

                    if (GUILayout.Button("保存配置", GUILayout.Height(25)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        PlayerPrefs.Save();
                        Debug.Log("调试器配置已保存");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private string GetActiveWindowTypeDescription(int index)
        {
            switch (index)
            {
                case 0: return "调试窗口始终开启，适合开发和测试阶段";
                case 1: return "仅在Development Build中开启，适合测试版本";
                case 2: return "仅在Unity编辑器中开启，适合开发调试";
                case 3: return "调试窗口始终关闭，适合发布版本";
                default: return "未知开启模式";
            }
        }

        private string GetAvailableModulesDescription()
        {
            return "已注册调试模块:\n" +
                   "• 控制台日志\n" +
                   "• 系统信息\n" +
                   "• 性能分析\n" +
                   "• 内存监控\n" +
                   "• 输入信息\n" +
                   "• 场景信息\n" +
                   "• 对象池状态\n" +
                   "• 设置面板";
        }

        private void OnEnable()
        {
            m_reporterScrollerSkin = serializedObject.FindProperty("reporterScrollerSkin");
            m_activeWindowType = serializedObject.FindProperty("activeWindowType");
            m_showFullWindow = serializedObject.FindProperty("m_showFullWindow");
            m_consoleWindow = serializedObject.FindProperty("m_consoleWindow");
        }
    }
}