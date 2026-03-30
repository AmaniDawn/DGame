using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private sealed class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            private readonly List<Record> m_records = new List<Record>();
            private readonly Comparison<Record> m_recordComparer = RecordComparer;
            private DateTime m_sampleTime = DateTime.MinValue;
            private int m_sampleCount = 0;
            private long m_sampleSize = 0L;

            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Runtime Memory Summary");
                BeginPanel();
                {
                    // 采样按钮
                    if (GUILayout.Button("Take Sample", DebuggerStyles.ButtonStyle, GUILayout.Height(DebuggerStyles.LargeButtonHeight)))
                    {
                        TakeSample();
                    }

                    GUILayout.Space(8);

                    if (m_sampleTime <= DateTime.MinValue)
                    {
                        DrawInfoMessage("Click 'Take Sample' to analyze runtime memory...");
                    }
                    else
                    {
                        // 摘要信息
                        string summary = Utility.StringUtil.Format("{0} Objects ({1}) sampled at {2:yyyy-MM-dd HH:mm:ss}",
                            m_sampleCount, GetByteLengthString(m_sampleSize), m_sampleTime.ToLocalTime());

                        GUILayout.Label(DebuggerStyles.ColorBoldText(summary, DebuggerStyles.PrimaryColor),
                            DebuggerStyles.RichLabelStyle);

                        GUILayout.Space(8);

                        // 表格头
                        DrawTableHeader(
                            ("Type", 0),
                            ("Count", 100f),
                            ("Size", 120f)
                        );

                        // 数据行
                        for (int i = 0; i < m_records.Count; i++)
                        {
                            DrawTableRow(i % 2 == 1,
                                (m_records[i].Name, 0),
                                (m_records[i].Count.ToString(), 100f),
                                (GetByteLengthString(m_records[i].Size), 120f)
                            );
                        }
                    }
                }
                EndPanel();
            }

            private void TakeSample()
            {
                m_records.Clear();
                m_sampleTime = DateTime.UtcNow;
                m_sampleCount = 0;
                m_sampleSize = 0L;

                UnityEngine.Object[] samples = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();

                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
#if UNITY_5_6_OR_NEWER
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
#else
                    sampleSize = Profiler.GetRuntimeMemorySize(samples[i]);
#endif
                    string name = samples[i].GetType().Name;
                    m_sampleCount++;
                    m_sampleSize += sampleSize;

                    Record record = null;

                    foreach (Record r in m_records)
                    {
                        if (r.Name == name)
                        {
                            record = r;
                            break;
                        }
                    }

                    if (record == null)
                    {
                        record = new Record(name);
                        m_records.Add(record);
                    }

                    record.Count++;
                    record.Size += sampleSize;
                }

                m_records.Sort(m_recordComparer);
            }

            private static int RecordComparer(Record a, Record b)
            {
                int result = b.Size.CompareTo(a.Size);

                if (result != 0)
                {
                    return result;
                }

                result = a.Count.CompareTo(b.Count);

                if (result != 0)
                {
                    return result;
                }

                return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
            }
        }

        private sealed class Record
        {
            private readonly string m_name;
            public string Name => m_name;
            public int Count { get; set; }

            public long Size { get; set; }

            public Record(string name)
            {
                m_name = name;
            }
        }
    }
}
