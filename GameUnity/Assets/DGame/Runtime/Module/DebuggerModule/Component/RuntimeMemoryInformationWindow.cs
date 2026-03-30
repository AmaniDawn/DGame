using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace DGame
{
    public partial class DebuggerDriver
    {
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase
            where T : UnityEngine.Object
        {
            private readonly List<Sample> m_samples = new List<Sample>();
            private readonly Comparison<Sample> m_sampleComparer = SampleComparer;
            private DateTime m_sampleTime = DateTime.MinValue;
            private long m_sampleSize = 0L;
            private long m_duplicateSampleSize = 0L;
            private int m_duplicateSimpleCount = 0;

            protected override void OnDrawScrollableWindow()
            {
                string typeName = typeof(T).Name;

                DrawSectionTitle(Utility.StringUtil.Format("{0} Runtime Memory", typeName));
                BeginPanel();
                {
                    // 采样按钮
                    if (GUILayout.Button(Utility.StringUtil.Format("Take Sample for {0}", typeName),
                            DebuggerStyles.ButtonStyle, GUILayout.Height(DebuggerStyles.LargeButtonHeight)))
                    {
                        TakeSample();
                    }

                    GUILayout.Space(8);

                    if (m_sampleTime <= DateTime.MinValue)
                    {
                        DrawInfoMessage(Utility.StringUtil.Format("Click 'Take Sample' to analyze {0} memory...", typeName));
                    }
                    else
                    {
                        // 摘要信息
                        string summary;
                        if (m_duplicateSimpleCount > 0)
                        {
                            summary = Utility.StringUtil.Format(
                                "{0} {1}s ({2}) sampled at {3:yyyy-MM-dd HH:mm:ss}\n{4} items ({5}) might be duplicated",
                                m_samples.Count, typeName, GetByteLengthString(m_sampleSize),
                                m_sampleTime.ToLocalTime(), m_duplicateSimpleCount,
                                GetByteLengthString(m_duplicateSampleSize));

                            GUILayout.Label(DebuggerStyles.ColorBoldText(summary, DebuggerStyles.WarningColor),
                                DebuggerStyles.RichLabelStyle);
                        }
                        else
                        {
                            summary = Utility.StringUtil.Format(
                                "{0} {1}s ({2}) sampled at {3:yyyy-MM-dd HH:mm:ss}",
                                m_samples.Count, typeName, GetByteLengthString(m_sampleSize),
                                m_sampleTime.ToLocalTime());

                            GUILayout.Label(DebuggerStyles.ColorBoldText(summary, DebuggerStyles.PrimaryColor),
                                DebuggerStyles.RichLabelStyle);
                        }

                        GUILayout.Space(8);

                        if (m_samples.Count > 0)
                        {
                            // 表格头
                            DrawTableHeader(
                                (Utility.StringUtil.Format("{0} Name", typeName), 0),
                                ("Type", 180f),
                                ("Size", 100f)
                            );

                            // 数据行
                            int count = 0;
                            for (int i = 0; i < m_samples.Count; i++)
                            {
                                if (m_samples[i].Highlight)
                                {
                                    // 高亮重复项
                                    DrawTableRowHighlight(
                                        (m_samples[i].Name, 0),
                                        (m_samples[i].Type, 180f),
                                        (GetByteLengthString(m_samples[i].Size), 100f)
                                    );
                                }
                                else
                                {
                                    DrawTableRow(i % 2 == 1,
                                        (m_samples[i].Name, 0),
                                        (m_samples[i].Type, 180f),
                                        (GetByteLengthString(m_samples[i].Size), 100f)
                                    );
                                }

                                count++;
                                if (count >= Constant.SHOW_SAMPLE_COUNT)
                                {
                                    break;
                                }
                            }

                            if (m_samples.Count > Constant.SHOW_SAMPLE_COUNT)
                            {
                                GUILayout.Space(4);
                                GUILayout.Label(
                                    DebuggerStyles.ColorText(
                                        Utility.StringUtil.Format("... and {0} more items",
                                            m_samples.Count - Constant.SHOW_SAMPLE_COUNT),
                                        DebuggerStyles.SecondaryTextColor),
                                    DebuggerStyles.RichLabelStyle);
                            }
                        }
                    }
                }
                EndPanel();
            }

            private void DrawTableRowHighlight(params (string value, float width)[] columns)
            {
                GUILayout.BeginHorizontal(DebuggerStyles.WarningBoxStyle);
                {
                    foreach (var (value, width) in columns)
                    {
                        if (width > 0)
                        {
                            GUILayout.Label(DebuggerStyles.ColorText(value, DebuggerStyles.WarningColor),
                                DebuggerStyles.RichLabelStyle, GUILayout.Width(width));
                        }
                        else
                        {
                            GUILayout.Label(DebuggerStyles.ColorText(value, DebuggerStyles.WarningColor),
                                DebuggerStyles.RichLabelStyle);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            private void TakeSample()
            {
                m_sampleTime = DateTime.UtcNow;
                m_sampleSize = 0L;
                m_duplicateSampleSize = 0L;
                m_duplicateSimpleCount = 0;
                m_samples.Clear();

                T[] samples = Resources.FindObjectsOfTypeAll<T>();

                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
#if UNITY_5_6_OR_NEWER
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
#else
                    sampleSize = Profiler.GetRuntimeMemorySize(samples[i]);
#endif
                    m_sampleSize += sampleSize;
                    m_samples.Add(new Sample(samples[i].name, samples[i].GetType().Name, sampleSize));
                }

                m_samples.Sort(m_sampleComparer);

                for (int i = 1; i < m_samples.Count; i++)
                {
                    if (m_samples[i].Name == m_samples[i - 1].Name && m_samples[i].Type == m_samples[i - 1].Type &&
                        m_samples[i].Size == m_samples[i - 1].Size)
                    {
                        m_samples[i].Highlight = true;
                        m_duplicateSampleSize += m_samples[i].Size;
                        m_duplicateSimpleCount++;
                    }
                }
            }

            private static int SampleComparer(Sample a, Sample b)
            {
                int result = b.Size.CompareTo(a.Size);

                if (result != 0)
                {
                    return result;
                }

                result = String.Compare(a.Type, b.Type, StringComparison.Ordinal);

                if (result != 0)
                {
                    return result;
                }

                return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
            }

            private sealed class Sample
            {
                private readonly string m_name;
                public string Name => m_name;
                private readonly string m_type;
                public string Type => m_type;
                private readonly long m_size;
                public long Size => m_size;

                public bool Highlight { get; set; }

                public Sample(string name, string type, long size)
                {
                    m_name = name;
                    m_type = type;
                    m_size = size;
                    Highlight = false;
                }
            }
        }
    }
}