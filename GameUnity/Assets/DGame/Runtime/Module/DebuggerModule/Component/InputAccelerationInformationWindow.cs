using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputAccelerationInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Current Acceleration");
                BeginPanel();
                {
                    Vector3 accel = Input.acceleration;
                    DrawItem("Acceleration X", Utility.StringUtil.Format("{0:F4} g", accel.x), "X轴加速度");
                    DrawItem("Acceleration Y", Utility.StringUtil.Format("{0:F4} g", accel.y), "Y轴加速度");
                    DrawItem("Acceleration Z", Utility.StringUtil.Format("{0:F4} g", accel.z), "Z轴加速度");
                    DrawItem("Magnitude", Utility.StringUtil.Format("{0:F4} g", accel.magnitude), "加速度向量长度");
                }
                EndPanel();

                DrawSectionTitle("Acceleration Events");
                BeginPanel();
                {
                    ResetRowIndex();
                    int eventCount = Input.accelerationEventCount;
                    Color32 countColor = eventCount > 0 ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Event Count", eventCount.ToString(), countColor);

                    if (eventCount > 0)
                    {
                        GUILayout.Space(8);

                        // 表格头
                        DrawTableHeader(
                            ("#", 30f),
                            ("Acceleration (X, Y, Z)", 0),
                            ("Delta Time", 80f)
                        );

                        // 加速度事件数据行
                        AccelerationEvent[] events = Input.accelerationEvents;
                        int displayCount = Mathf.Min(events.Length, 10); // 最多显示10条

                        for (int i = 0; i < displayCount; i++)
                        {
                            DrawTableRow(i % 2 == 1,
                                (i.ToString(), 30f),
                                (Utility.StringUtil.Format("({0:F3}, {1:F3}, {2:F3})",
                                    events[i].acceleration.x,
                                    events[i].acceleration.y,
                                    events[i].acceleration.z), 0),
                                (Utility.StringUtil.Format("{0:F4}s", events[i].deltaTime), 80f)
                            );
                        }

                        if (events.Length > 10)
                        {
                            GUILayout.Space(4);
                            GUILayout.Label(
                                DebuggerStyles.ColorText(
                                    Utility.StringUtil.Format("... and {0} more events", events.Length - 10),
                                    DebuggerStyles.SecondaryTextColor),
                                DebuggerStyles.RichLabelStyle);
                        }
                    }
                    else
                    {
                        GUILayout.Space(4);
                        DrawInfoMessage("No acceleration events this frame...");
                    }
                }
                EndPanel();
            }
        }
    }
}