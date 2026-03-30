using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class SettingsWindow : ScrollableDebuggerWindowBase
        {
            private DebuggerDriver m_debuggerDriver;
            private float m_lastIconX;
            private float m_lastIconY;
            private float m_lastWindowX;
            private float m_lastWindowY;
            private float m_lastWindowWidth;
            private float m_lastWindowHeight;
            private float m_lastWindowScale;

            // 预设的缩放选项
            private static readonly float[] ScalePresets = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };
            private static readonly string[] ScaleLabels = { "0.5x", "1.0x", "1.5x", "2.0x", "2.5x", "3.0x" };

            public override void Initialize(params object[] args)
            {
                m_debuggerDriver = DebuggerDriver.Instance;

                if (m_debuggerDriver == null)
                {
                    DLogger.Fatal("[DGame] DebuggerDriver is null");
                    return;
                }

                m_lastIconX = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, DefaultIconRect.x);
                m_lastIconY = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, DefaultIconRect.y);
                m_lastWindowX = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, DefaultWindowRect.x);
                m_lastWindowY = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, DefaultWindowRect.y);
                m_lastWindowWidth = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, DefaultWindowRect.width);
                m_lastWindowHeight = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, DefaultWindowRect.height);
                m_lastWindowScale = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, DefaultWindowScale);
                m_debuggerDriver.IconRect = new Rect(m_lastIconX, m_lastIconY, DefaultIconRect.width, DefaultIconRect.height);
                m_debuggerDriver.WindowRect = new Rect(m_lastWindowX, m_lastWindowY, m_lastWindowWidth, m_lastWindowHeight);
                m_debuggerDriver.WindowScale = m_lastWindowScale;
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (Mathf.Abs(m_lastIconX - m_debuggerDriver.IconRect.x) > 0.01f)
                {
                    m_lastIconX = m_debuggerDriver.IconRect.x;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, m_lastIconX);
                }

                if (Mathf.Abs(m_lastIconY - m_debuggerDriver.IconRect.y) > 0.01f)
                {
                    m_lastIconY = m_debuggerDriver.IconRect.y;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, m_lastIconY);
                }

                if (Mathf.Abs(m_lastWindowX - m_debuggerDriver.WindowRect.x) > 0.01f)
                {
                    m_lastWindowX = m_debuggerDriver.WindowRect.x;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, m_lastWindowX);
                }

                if (Mathf.Abs(m_lastWindowY - m_debuggerDriver.WindowRect.y) > 0.01f)
                {
                    m_lastWindowY = m_debuggerDriver.WindowRect.y;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, m_lastWindowY);
                }

                if (Mathf.Abs(m_lastWindowWidth - m_debuggerDriver.WindowRect.width) > 0.01f)
                {
                    m_lastWindowWidth = m_debuggerDriver.WindowRect.width;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, m_lastWindowWidth);
                }

                if (Mathf.Abs(m_lastWindowHeight - m_debuggerDriver.WindowRect.height) > 0.01f)
                {
                    m_lastWindowHeight = m_debuggerDriver.WindowRect.height;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, m_lastWindowHeight);
                }

                if (Mathf.Abs(m_lastWindowScale - m_debuggerDriver.WindowScale) > 0.01f)
                {
                    m_lastWindowScale = m_debuggerDriver.WindowScale;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, m_lastWindowScale);
                }
            }

            protected override void OnDrawScrollableWindow()
            {
                // 窗口设置区段
                DrawSectionTitle("Window Settings");
                BeginPanel();
                {
                    // 位置说明
                    DrawItem("Position", "Drag window title to move");

                    // 当前尺寸显示
                    DrawItem("Current Size", $"{m_debuggerDriver.WindowRect.width:F0} x {m_debuggerDriver.WindowRect.height:F0}");
                    DrawItem("Current Scale", $"{m_debuggerDriver.WindowScale:F2}x");

                    GUILayout.Space(8);

                    // 宽度调整
                    DrawSliderWithButtons("Width", ref m_debuggerDriver, true);

                    GUILayout.Space(4);

                    // 高度调整
                    DrawSliderWithButtons("Height", ref m_debuggerDriver, false);

                    GUILayout.Space(4);

                    // 缩放调整
                    DrawScaleSlider();
                }
                EndPanel();

                GUILayout.Space(8);

                // 缩放预设区段
                DrawSectionTitle("Scale Presets");
                BeginPanel();
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (int i = 0; i < ScalePresets.Length; i++)
                        {
                            bool isSelected = Mathf.Abs(m_debuggerDriver.WindowScale - ScalePresets[i]) < 0.01f;
                            string label = isSelected ? $"<b>{ScaleLabels[i]}</b>" : ScaleLabels[i];

                            if (GUILayout.Button(label, DebuggerStyles.ButtonStyle, GUILayout.Height(DebuggerStyles.LargeButtonHeight)))
                            {
                                m_debuggerDriver.WindowScale = ScalePresets[i];
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EndPanel();

                GUILayout.Space(8);

                // 重置区段
                DrawSectionTitle("Reset");
                BeginPanel();
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Reset Layout", DebuggerStyles.ButtonStyle, GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            m_debuggerDriver.ResetWindowLayout();
                        }

                        if (GUILayout.Button("Reset Scale Only", DebuggerStyles.ButtonStyle, GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            m_debuggerDriver.WindowScale = DefaultWindowScale;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EndPanel();
            }

            private void DrawSliderWithButtons(string label, ref DebuggerDriver driver, bool isWidth)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(label + ":", DebuggerStyles.LabelStyle, GUILayout.Width(70f));

                    float value = isWidth ? driver.WindowRect.width : driver.WindowRect.height;
                    float maxValue = isWidth ? Screen.width - 20f : Screen.height - 20f;

                    // 减少按钮
                    if (GUILayout.RepeatButton("-", DebuggerStyles.ButtonStyle, GUILayout.Width(30f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                    {
                        value -= 5f;
                    }

                    // 滑块
                    value = GUILayout.HorizontalSlider(value, 100f, maxValue, GUILayout.Height(20f));

                    // 增加按钮
                    if (GUILayout.RepeatButton("+", DebuggerStyles.ButtonStyle, GUILayout.Width(30f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                    {
                        value += 5f;
                    }

                    // 数值显示
                    GUILayout.Label($"{value:F0}", DebuggerStyles.ValueStyle, GUILayout.Width(50f));

                    value = Mathf.Clamp(value, 100f, maxValue);

                    // 应用变更
                    if (isWidth)
                    {
                        if (Mathf.Abs(value - driver.WindowRect.width) > 0.01f)
                        {
                            driver.WindowRect = new Rect(driver.WindowRect.x, driver.WindowRect.y, value, driver.WindowRect.height);
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(value - driver.WindowRect.height) > 0.01f)
                        {
                            driver.WindowRect = new Rect(driver.WindowRect.x, driver.WindowRect.y, driver.WindowRect.width, value);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            private void DrawScaleSlider()
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Scale:", DebuggerStyles.LabelStyle, GUILayout.Width(70f));

                    float scale = m_debuggerDriver.WindowScale;

                    // 减少按钮
                    if (GUILayout.RepeatButton("-", DebuggerStyles.ButtonStyle, GUILayout.Width(30f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                    {
                        scale -= 0.05f;
                    }

                    // 滑块
                    scale = GUILayout.HorizontalSlider(scale, 0.5f, 4f, GUILayout.Height(20f));

                    // 增加按钮
                    if (GUILayout.RepeatButton("+", DebuggerStyles.ButtonStyle, GUILayout.Width(30f), GUILayout.Height(DebuggerStyles.SmallButtonHeight)))
                    {
                        scale += 0.05f;
                    }

                    // 数值显示
                    GUILayout.Label($"{scale:F2}x", DebuggerStyles.ValueStyle, GUILayout.Width(50f));

                    scale = Mathf.Clamp(scale, 0.5f, 4f);

                    if (Mathf.Abs(scale - m_debuggerDriver.WindowScale) > 0.01f)
                    {
                        m_debuggerDriver.WindowScale = scale;
                    }
                }
                GUILayout.EndHorizontal();
            }

            public override void OnExit()
            {
                Utility.PlayerPrefsUtil.Save();
                base.OnExit();
            }
        }
    }
}
