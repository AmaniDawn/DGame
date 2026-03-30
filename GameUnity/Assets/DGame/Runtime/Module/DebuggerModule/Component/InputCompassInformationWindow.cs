using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputCompassInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Compass Control");
                BeginPanel();
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启用电子罗盘传感器
                        if (GUILayout.Button("Enable Compass", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.compass.enabled = true;
                        }

                        GUILayout.Space(8);

                        // 禁用电子罗盘传感器
                        if (GUILayout.Button("Disable Compass", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.compass.enabled = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(8);

                    Color32 enabledColor = Input.compass.enabled ? DebuggerStyles.SuccessColor : DebuggerStyles.SecondaryTextColor;
                    DrawItemColored("Enabled", Input.compass.enabled.ToString(), enabledColor);
                }
                EndPanel();

                if (Input.compass.enabled)
                {
                    DrawSectionTitle("Heading Information");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        DrawItem("Magnetic Heading", Utility.StringUtil.Format("{0:F2}°", Input.compass.magneticHeading), "磁北方向（度）");
                        DrawItem("True Heading", Utility.StringUtil.Format("{0:F2}°", Input.compass.trueHeading), "真北方向（度）");
                        DrawItem("Heading Accuracy", Utility.StringUtil.Format("{0:F2}°", Input.compass.headingAccuracy), "朝向精度（度）");
                    }
                    EndPanel();

                    DrawSectionTitle("Raw Magnetic Field");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        Vector3 rawVector = Input.compass.rawVector;
                        DrawItem("Raw Vector X", Utility.StringUtil.Format("{0:F4} μT", rawVector.x), "磁场X分量");
                        DrawItem("Raw Vector Y", Utility.StringUtil.Format("{0:F4} μT", rawVector.y), "磁场Y分量");
                        DrawItem("Raw Vector Z", Utility.StringUtil.Format("{0:F4} μT", rawVector.z), "磁场Z分量");
                        DrawItem("Magnitude", Utility.StringUtil.Format("{0:F4} μT", rawVector.magnitude), "磁场强度");
                        DrawItem("Timestamp", Input.compass.timestamp.ToString("F2"), "时间戳");
                    }
                    EndPanel();
                }
                else
                {
                    DrawSectionTitle("Compass Data");
                    BeginPanel();
                    {
                        DrawInfoMessage("Enable compass to view heading data...");
                    }
                    EndPanel();
                }
            }
        }
    }
}