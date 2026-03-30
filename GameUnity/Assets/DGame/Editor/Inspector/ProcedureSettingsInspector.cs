using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(ProcedureSettings))]
    internal sealed class ProcedureSettingsInspector : DGameInspector
    {
        private SerializedProperty m_availableProcedureTypeNames;
        private SerializedProperty m_startProcedureTypeName;

        private string[] m_procedureTypeNames;
        private List<string> m_curAvailableProcedureTypeNames;
        private int m_startProcedureIndex = -1;

        // UI状态
        private Vector2 m_scrollPosition;
        private bool m_showAvailableProcedures = true;
        private bool m_showStartProcedure = true;
        private string m_searchFilter = "";

        // 颜色定义
        private Color m_headerColor = new Color(0.1f, 0.5f, 0.8f, 1f);
        private Color m_selectedColor = new Color(0.2f, 0.8f, 0.3f, 0.1f);
        private Color m_warningColor = new Color(1f, 0.6f, 0.2f, 1f);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            ProcedureSettings t = (ProcedureSettings)target;

            // 绘制标题区域
            DrawInspectorHeader();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                {
                    // 可用流程列表
                    DrawAvailableProceduresSection();

                    // 起始流程设置
                    DrawStartProcedureSection();

                    // 统计信息
                    DrawStatisticsSection();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void DrawInspectorHeader()
        {
            // 标题背景
            // Rect headerRect = EditorGUILayout.GetControlRect(false, 50);
            // EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, float.MaxValue, 50),
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

            EditorGUILayout.LabelField(new GUIContent("游戏启动流程配置系统", "Procedure Configuration System"),
                titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 状态指示
            var statusText = string.IsNullOrEmpty(m_startProcedureTypeName.stringValue) ?
                "入口流程未设置" : "配置完成";
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = string.IsNullOrEmpty(m_startProcedureTypeName.stringValue) ?
                    Color.red : Color.green }
            };

            EditorGUILayout.LabelField(statusText, statusStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawAvailableProceduresSection()
        {
            m_showAvailableProcedures = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAvailableProcedures,
                new GUIContent("可用流程列表", "勾选需要使用的流程类型"));

            if (m_showAvailableProcedures)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 搜索框
                    DrawSearchFilter();

                    if (m_procedureTypeNames.Length > 0)
                    {
                        // 全选/全不选工具栏
                        DrawSelectionToolbar();

                        // 流程列表
                        DrawProcedureList();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("未找到任何流程类型\n请确保有继承自ProcedureBase的类", MessageType.Warning);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(10);
        }

        private void DrawSearchFilter()
        {
            EditorGUILayout.BeginHorizontal();
            {
                m_searchFilter = EditorGUILayout.TextField("", m_searchFilter, "SearchTextField");

                if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18)))
                {
                    m_searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawSelectionToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                int selectedCount = m_curAvailableProcedureTypeNames.Count;
                int totalCount = m_procedureTypeNames.Length;

                GUILayout.Label($"已选择: {selectedCount}/{totalCount}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("全选", GUILayout.Width(60)))
                {
                    SelectAllProcedures();
                }

                if (GUILayout.Button("全不选", GUILayout.Width(60)))
                {
                    DeselectAllProcedures();
                }

                if (GUILayout.Button("反选", GUILayout.Width(60)))
                {
                    InvertSelection();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawProcedureList()
        {
            var filteredProcedures = GetFilteredProcedures();

            if (filteredProcedures.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到匹配的流程类型", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("TextArea");
            {
                foreach (string procedureName in filteredProcedures)
                {
                    bool selected = m_curAvailableProcedureTypeNames.Contains(procedureName);
                    bool isStartProcedure = procedureName == m_startProcedureTypeName.stringValue;

                    // 高亮显示起始流程
                    if (isStartProcedure)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("⭐", GUILayout.Width(20));
                        DrawProcedureItem(procedureName, selected, isStartProcedure);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        DrawProcedureItem(procedureName, selected, isStartProcedure);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawProcedureItem(string procedureName, bool selected, bool isStartProcedure)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // 选择框
                EditorGUI.BeginDisabledGroup(isStartProcedure);
                bool newSelected = EditorGUILayout.ToggleLeft("", selected, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();

                if (newSelected != selected && !isStartProcedure)
                {
                    if (newSelected)
                    {
                        m_curAvailableProcedureTypeNames.Add(procedureName);
                    }
                    else
                    {
                        m_curAvailableProcedureTypeNames.Remove(procedureName);
                    }
                    WriteAvailableProcedureTypeNames();
                }

                // 流程名称
                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = isStartProcedure ? Color.green : (selected ? Color.white : Color.gray) },
                    fontStyle = isStartProcedure ? FontStyle.Bold : FontStyle.Normal
                };

                EditorGUILayout.LabelField(procedureName, labelStyle);

                // 设置为起始流程按钮
                if (selected && !isStartProcedure)
                {
                    if (GUILayout.Button("设为入口", GUILayout.Width(60)))
                    {
                        SetAsStartProcedure(procedureName);
                    }
                }
                else if (isStartProcedure)
                {
                    EditorGUILayout.LabelField("入口流程", EditorStyles.miniLabel, GUILayout.Width(60));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStartProcedureSection()
        {
            m_showStartProcedure = EditorGUILayout.BeginFoldoutHeaderGroup(m_showStartProcedure,
                new GUIContent("起始流程设置"));

            if (m_showStartProcedure)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    if (m_curAvailableProcedureTypeNames.Count > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("起始流程:", GUILayout.Width(70));

                            int selectedIndex = EditorGUILayout.Popup(m_startProcedureIndex,
                                m_curAvailableProcedureTypeNames.ToArray());

                            if (selectedIndex != m_startProcedureIndex)
                            {
                                m_startProcedureIndex = selectedIndex;
                                m_startProcedureTypeName.stringValue = m_curAvailableProcedureTypeNames[selectedIndex];
                            }

                            // 快速操作按钮
                            if (GUILayout.Button("刷新", GUILayout.Width(50)))
                            {
                                RefreshTypeNames();
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        if (m_startProcedureIndex >= 0)
                        {
                            EditorGUILayout.HelpBox($"当前入口流程: {m_curAvailableProcedureTypeNames[m_startProcedureIndex]}",
                                MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("请设置一个流程入口", MessageType.Warning);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("请先在可用流程列表中勾选至少一个流程", MessageType.Warning);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(10);
        }

        private void DrawStatisticsSection()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("总流程类型:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(m_procedureTypeNames.Length.ToString(), EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("已启用流程:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(m_curAvailableProcedureTypeNames.Count.ToString(), EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("入口流程:", GUILayout.Width(100));
                    string startProcedure = string.IsNullOrEmpty(m_startProcedureTypeName.stringValue) ?
                        "未设置" : m_startProcedureTypeName.stringValue;
                    EditorGUILayout.LabelField(startProcedure, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                // 操作按钮
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("强制刷新"))
                    {
                        RefreshTypeNames();
                    }

                    if (GUILayout.Button("保存配置"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("流程配置已保存");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private string[] GetFilteredProcedures()
        {
            if (string.IsNullOrEmpty(m_searchFilter))
            {
                return m_procedureTypeNames;
            }

            return m_procedureTypeNames.Where(name =>
                name.IndexOf(m_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        }

        private void SelectAllProcedures()
        {
            m_curAvailableProcedureTypeNames = m_procedureTypeNames.ToList();
            WriteAvailableProcedureTypeNames();
        }

        private void DeselectAllProcedures()
        {
            m_curAvailableProcedureTypeNames.Clear();
            m_startProcedureTypeName.stringValue = null;
            m_startProcedureIndex = -1;
            WriteAvailableProcedureTypeNames();
        }

        private void InvertSelection()
        {
            var newSelection = new List<string>();
            foreach (string procedureName in m_procedureTypeNames)
            {
                if (!m_curAvailableProcedureTypeNames.Contains(procedureName))
                {
                    newSelection.Add(procedureName);
                }
            }
            m_curAvailableProcedureTypeNames = newSelection;
            WriteAvailableProcedureTypeNames();
        }

        private void SetAsStartProcedure(string procedureName)
        {
            m_startProcedureTypeName.stringValue = procedureName;
            m_startProcedureIndex = m_curAvailableProcedureTypeNames.IndexOf(procedureName);
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_availableProcedureTypeNames = serializedObject.FindProperty("availableProcedureTypeNames");
            m_startProcedureTypeName = serializedObject.FindProperty("startProcedureTypeName");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_procedureTypeNames = TypeUtil.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();
            int oldCount = m_curAvailableProcedureTypeNames.Count;
            m_curAvailableProcedureTypeNames = m_curAvailableProcedureTypeNames.Where(x => m_procedureTypeNames.Contains(x)).ToList();

            if (m_curAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if(!string.IsNullOrEmpty(m_startProcedureTypeName.stringValue))
            {
                m_startProcedureIndex = m_curAvailableProcedureTypeNames.IndexOf(m_startProcedureTypeName.stringValue);

                if (m_startProcedureIndex < 0)
                {
                    m_startProcedureTypeName.stringValue = null;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void WriteAvailableProcedureTypeNames()
        {
            m_availableProcedureTypeNames.ClearArray();

            if (m_curAvailableProcedureTypeNames == null)
            {
                return;
            }

            m_curAvailableProcedureTypeNames.Sort();
            int count = m_curAvailableProcedureTypeNames.Count;

            for (int i = 0; i < count; i++)
            {
                m_availableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_curAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(m_startProcedureTypeName.stringValue))
            {
                m_startProcedureIndex = m_curAvailableProcedureTypeNames.IndexOf(m_startProcedureTypeName.stringValue);

                if (m_startProcedureIndex < 0)
                {
                    m_startProcedureTypeName.stringValue = null;
                }
            }
        }

        private void ReadAvailableProcedureTypeNames()
        {
            if (m_curAvailableProcedureTypeNames == null)
            {
                m_curAvailableProcedureTypeNames = new List<string>();
            }
            m_curAvailableProcedureTypeNames.Clear();
            int cnt = m_availableProcedureTypeNames.arraySize;

            for (int i = 0; i < cnt; i++)
            {
                m_curAvailableProcedureTypeNames.Add(m_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }
    }
}