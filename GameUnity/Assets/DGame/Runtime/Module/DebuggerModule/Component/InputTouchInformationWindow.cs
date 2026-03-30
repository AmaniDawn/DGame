using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputTouchInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Touch Support");
                BeginPanel();
                {
                    Color32 supportColor = Input.touchSupported ? DebuggerStyles.SuccessColor : DebuggerStyles.SecondaryTextColor;
                    DrawItemColored("Touch Supported", Input.touchSupported.ToString(), supportColor);
                    DrawItem("Touch Pressure Supported", Input.touchPressureSupported.ToString(), "是否支持触摸压力感应");
                    DrawItem("Stylus Touch Supported", Input.stylusTouchSupported.ToString(), "是否支持触控笔输入");
                    DrawItem("Simulate Mouse With Touches", Input.simulateMouseWithTouches.ToString(), "是否用触摸模拟鼠标输入");
                    DrawItem("Multi Touch Enabled", Input.multiTouchEnabled.ToString(), "是否启用多点触控");
                }
                EndPanel();

                DrawSectionTitle("Active Touches");
                BeginPanel();
                {
                    ResetRowIndex();
                    int touchCount = Input.touchCount;
                    Color32 countColor = touchCount > 0 ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Touch Count", touchCount.ToString(), countColor);

                    if (touchCount > 0)
                    {
                        GUILayout.Space(8);

                        // 表格头
                        DrawTableHeader(
                            ("ID", 40f),
                            ("Position", 120f),
                            ("Delta", 100f),
                            ("Phase", 80f),
                            ("Pressure", 60f)
                        );

                        // 触摸数据行
                        Touch[] touches = Input.touches;
                        for (int i = 0; i < touches.Length; i++)
                        {
                            DrawTableRow(i % 2 == 1,
                                (touches[i].fingerId.ToString(), 40f),
                                (Utility.StringUtil.Format("({0:F0}, {1:F0})", touches[i].position.x, touches[i].position.y), 120f),
                                (Utility.StringUtil.Format("({0:F1}, {1:F1})", touches[i].deltaPosition.x, touches[i].deltaPosition.y), 100f),
                                (touches[i].phase.ToString(), 80f),
                                (touches[i].pressure.ToString("F2"), 60f)
                            );
                        }
                    }
                    else
                    {
                        GUILayout.Space(4);
                        DrawInfoMessage("No active touches detected...");
                    }
                }
                EndPanel();
            }
        }
    }
}