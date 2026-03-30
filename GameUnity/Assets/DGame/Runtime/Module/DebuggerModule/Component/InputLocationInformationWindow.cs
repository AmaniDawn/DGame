using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputLocationInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Location Service Control");
                BeginPanel();
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启动定位服务
                        if (GUILayout.Button("Enable Location", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.location.Start();
                        }

                        GUILayout.Space(8);

                        // 停止定位服务
                        if (GUILayout.Button("Disable Location", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.location.Stop();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EndPanel();

                DrawSectionTitle("Location Status");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 enabledColor = Input.location.isEnabledByUser ? DebuggerStyles.SuccessColor : DebuggerStyles.ErrorColor;
                    DrawItemColored("Is Enabled By User", Input.location.isEnabledByUser.ToString(), enabledColor);

                    Color32 statusColor = GetLocationStatusColor(Input.location.status);
                    DrawItemColored("Status", Input.location.status.ToString(), statusColor);
                }
                EndPanel();

                if (Input.location.status == LocationServiceStatus.Running)
                {
                    DrawSectionTitle("Location Data");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        DrawItem("Latitude", Utility.StringUtil.Format("{0:F6}°", Input.location.lastData.latitude), "纬度");
                        DrawItem("Longitude", Utility.StringUtil.Format("{0:F6}°", Input.location.lastData.longitude), "经度");
                        DrawItem("Altitude", Utility.StringUtil.Format("{0:F2} m", Input.location.lastData.altitude), "海拔高度");
                    }
                    EndPanel();

                    DrawSectionTitle("Accuracy");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        DrawItem("Horizontal Accuracy", Utility.StringUtil.Format("{0:F2} m", Input.location.lastData.horizontalAccuracy), "水平精度（米）");
                        DrawItem("Vertical Accuracy", Utility.StringUtil.Format("{0:F2} m", Input.location.lastData.verticalAccuracy), "垂直精度（米）");
                        DrawItem("Timestamp", Input.location.lastData.timestamp.ToString("F2"), "时间戳");
                    }
                    EndPanel();
                }
                else if (Input.location.status == LocationServiceStatus.Failed)
                {
                    DrawSectionTitle("Location Data");
                    BeginPanel();
                    {
                        DrawErrorMessage("Location service failed. Please check device permissions.");
                    }
                    EndPanel();
                }
                else if (Input.location.status == LocationServiceStatus.Initializing)
                {
                    DrawSectionTitle("Location Data");
                    BeginPanel();
                    {
                        DrawInfoMessage("Location service is initializing...");
                    }
                    EndPanel();
                }
            }

            private Color32 GetLocationStatusColor(LocationServiceStatus status)
            {
                switch (status)
                {
                    case LocationServiceStatus.Running:
                        return DebuggerStyles.SuccessColor;
                    case LocationServiceStatus.Initializing:
                        return DebuggerStyles.WarningColor;
                    case LocationServiceStatus.Failed:
                        return DebuggerStyles.ErrorColor;
                    default:
                        return DebuggerStyles.SecondaryTextColor;
                }
            }
        }
    }
}