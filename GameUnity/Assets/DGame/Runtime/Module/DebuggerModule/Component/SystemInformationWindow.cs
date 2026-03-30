using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class SystemInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("System Information");
                BeginPanel();
                {
                    // 设备唯一标识符
                    DrawItem("Device Unique ID", SystemInfo.deviceUniqueIdentifier, "设备唯一标识符");
                    // 设备名称
                    DrawItem("Device Name", SystemInfo.deviceName, "设备名称");
                    // 设备类型
                    DrawItem("Device Type", SystemInfo.deviceType.ToString(), "设备类型");
                    // 设备型号
                    DrawItem("Device Model", SystemInfo.deviceModel, "设备型号");
                }
                EndPanel();

                DrawSectionTitle("Processor");
                BeginPanel();
                {
                    ResetRowIndex();
                    // CPU型号
                    DrawItem("Processor Type", SystemInfo.processorType, "CPU型号");
                    // CPU核心数量
                    DrawItem("Processor Count", SystemInfo.processorCount.ToString(), "CPU核心数量");
                    // CPU频率(MHz)
                    DrawItem("Processor Frequency", Utility.StringUtil.Format("{0} MHz", SystemInfo.processorFrequency), "CPU频率(MHz)");
                    // 系统内存大小
                    DrawItem("System Memory Size", Utility.StringUtil.Format("{0} MB", SystemInfo.systemMemorySize), "系统内存大小(MB)");
                }
                EndPanel();

                DrawSectionTitle("Operating System");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_5_5_OR_NEWER
                    // 操作系统
                    DrawItem("Operating System Family", SystemInfo.operatingSystemFamily.ToString(), "操作系统");
#endif
                    // 操作系统版本号
                    DrawItem("Operating System", SystemInfo.operatingSystem, "操作系统版本号");
                }
                EndPanel();

                DrawSectionTitle("Battery");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_5_6_OR_NEWER
                    // 电池状态
                    DrawItem("Battery Status", SystemInfo.batteryStatus.ToString(), "电池状态");
                    // 电池电量(百分比)
                    DrawItem("Battery Level", GetBatteryLevelString(SystemInfo.batteryLevel), "电池电量(百分比)");
#endif
                }
                EndPanel();

                DrawSectionTitle("Features");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_5_4_OR_NEWER
                    // 是否支持音频
                    DrawItem("Supports Audio", SystemInfo.supportsAudio.ToString(), "是否支持音频");
#endif
                    // 是否支持定位服务(GPS)
                    DrawItem("Supports Location Service", SystemInfo.supportsLocationService.ToString(), "是否支持定位服务(GPS)");
                    // 是否支持加速统计
                    DrawItem("Supports Accelerometer", SystemInfo.supportsAccelerometer.ToString(), "是否支持加速统计");
                    // 是否支持陀螺仪
                    DrawItem("Supports Gyroscope", SystemInfo.supportsGyroscope.ToString(), "是否支持陀螺仪");
                    // 是否支持震动功能
                    DrawItem("Supports Vibration", SystemInfo.supportsVibration.ToString(), "是否支持震动功能");
                }
                EndPanel();

                DrawSectionTitle("Application");
                BeginPanel();
                {
                    ResetRowIndex();
                    // 应用是否为正版/未被篡改
                    DrawItem("Genuine", Application.genuine.ToString(), "应用是否为正版");
                    // 是否可进行正版验证
                    DrawItem("Genuine Check Available", Application.genuineCheckAvailable.ToString(), "是否可进行正版验证");
                }
                EndPanel();
            }

            private string GetBatteryLevelString(float batteryLevel)
            {
                if (batteryLevel < 0f)
                {
                    return "Unavailable";
                }

                // 电量百分比显示
                return batteryLevel.ToString("P0");
            }
        }
    }
}
