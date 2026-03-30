using System;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class MemoryPoolPoolInformationWindow : ScrollableDebuggerWindowBase
        {
            private readonly Dictionary<string, List<MemoryCollectorInfo>> m_memoryPoolInfos =
                new Dictionary<string, List<MemoryCollectorInfo>>(StringComparer.Ordinal);

            private readonly Comparison<MemoryCollectorInfo> m_normalClassNameComparer = NormalClassNameComparer;
            private readonly Comparison<MemoryCollectorInfo> m_fullClassNameComparer = FullClassNameComparer;
            private bool m_showFullClassName = false;

            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Memory Pool Overview");
                BeginPanel();
                {
                    Color32 checkColor = MemoryPool.EnableStrictCheck ? DebuggerStyles.WarningColor : DebuggerStyles.TextColor;
                    DrawItemColored("Enable Strict Check", MemoryPool.EnableStrictCheck.ToString(), checkColor);

                    int capacity = MemoryPool.Capacity;
                    Color32 capacityColor = capacity > 0 ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Total Memory Collectors", capacity.ToString(), capacityColor);
                }
                EndPanel();

                DrawSectionTitle("Display Options");
                BeginPanel();
                {
                    m_showFullClassName = GUILayout.Toggle(m_showFullClassName, "  Show Full Class Name", DebuggerStyles.ToggleStyle);
                }
                EndPanel();

                // 收集并分组内存池信息
                m_memoryPoolInfos.Clear();
                var memoryPoolInfos = MemoryPool.GetAllMemoryCollectorInfos();

                foreach (var memoryPoolInfo in memoryPoolInfos)
                {
                    string assemblyName = memoryPoolInfo.ClassType.Assembly.GetName().Name;
                    List<MemoryCollectorInfo> results = null;

                    if (!m_memoryPoolInfos.TryGetValue(assemblyName, out results))
                    {
                        results = new List<MemoryCollectorInfo>();
                        m_memoryPoolInfos.Add(assemblyName, results);
                    }

                    results.Add(memoryPoolInfo);
                }

                if (m_memoryPoolInfos.Count == 0)
                {
                    DrawSectionTitle("Memory Collectors");
                    BeginPanel();
                    {
                        DrawInfoMessage("No memory collectors registered yet...");
                    }
                    EndPanel();
                }
                else
                {
                    foreach (KeyValuePair<string, List<MemoryCollectorInfo>> assemblyMemoryPoolInfo in m_memoryPoolInfos)
                    {
                        DrawAssemblySection(assemblyMemoryPoolInfo.Key, assemblyMemoryPoolInfo.Value);
                    }
                }
            }

            private void DrawAssemblySection(string assemblyName, List<MemoryCollectorInfo> infos)
            {
                DrawSectionTitle(Utility.StringUtil.Format("Assembly: {0}", assemblyName));
                BeginPanel();
                {
                    if (infos.Count == 0)
                    {
                        DrawInfoMessage("No memory collectors in this assembly...");
                    }
                    else
                    {
                        infos.Sort(m_showFullClassName ? m_fullClassNameComparer : m_normalClassNameComparer);

                        // 表格头
                        DrawTableHeader(
                            (m_showFullClassName ? "Full Class Name" : "Class Name", 0),
                            ("Unused", 55f),
                            ("Using", 50f),
                            ("Spawn", 50f),
                            ("Recycle", 55f),
                            ("Add", 45f),
                            ("Remove", 55f)
                        );

                        // 数据行
                        for (int i = 0; i < infos.Count; i++)
                        {
                            DrawMemoryPoolRow(infos[i], i % 2 == 1);
                        }
                    }
                }
                EndPanel();
            }

            private void DrawMemoryPoolRow(MemoryCollectorInfo info, bool isAlt)
            {
                string className = m_showFullClassName ? info.ClassType.FullName : info.ClassType.Name;
                bool hasActivity = info.UsingCount > 0 || info.SpawnCount > 0;

                if (hasActivity)
                {
                    // 高亮活跃的内存池
                    GUILayout.BeginHorizontal(DebuggerStyles.PrimaryBoxStyle);
                    {
                        GUILayout.Label(DebuggerStyles.ColorText(className, DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle);
                        GUILayout.Label(DebuggerStyles.ColorText(info.UnusedCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(55f));
                        GUILayout.Label(DebuggerStyles.ColorText(info.UsingCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(50f));
                        GUILayout.Label(DebuggerStyles.ColorText(info.SpawnCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(50f));
                        GUILayout.Label(DebuggerStyles.ColorText(info.RecycleCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(55f));
                        GUILayout.Label(DebuggerStyles.ColorText(info.AddCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(45f));
                        GUILayout.Label(DebuggerStyles.ColorText(info.RemoveCount.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(55f));
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    DrawTableRow(isAlt,
                        (className, 0),
                        (info.UnusedCount.ToString(), 55f),
                        (info.UsingCount.ToString(), 50f),
                        (info.SpawnCount.ToString(), 50f),
                        (info.RecycleCount.ToString(), 55f),
                        (info.AddCount.ToString(), 45f),
                        (info.RemoveCount.ToString(), 55f)
                    );
                }
            }

            private static int NormalClassNameComparer(MemoryCollectorInfo a, MemoryCollectorInfo b)
                => String.Compare(a.ClassType.Name, b.ClassType.Name, StringComparison.Ordinal);

            private static int FullClassNameComparer(MemoryCollectorInfo a, MemoryCollectorInfo b)
                => String.Compare(a.ClassType.FullName, b.ClassType.FullName, StringComparison.Ordinal);
        }
    }
}