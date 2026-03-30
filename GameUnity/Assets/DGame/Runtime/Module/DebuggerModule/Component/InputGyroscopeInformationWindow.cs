using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputGyroscopeInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Gyroscope Control");
                BeginPanel();
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启用陀螺仪传感器
                        if (GUILayout.Button("Enable Gyroscope", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.gyro.enabled = true;
                        }

                        GUILayout.Space(8);

                        // 禁用陀螺仪传感器
                        if (GUILayout.Button("Disable Gyroscope", DebuggerStyles.ButtonStyle,
                            GUILayout.Height(DebuggerStyles.ButtonHeight)))
                        {
                            Input.gyro.enabled = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(8);

                    Color32 enabledColor = Input.gyro.enabled ? DebuggerStyles.SuccessColor : DebuggerStyles.SecondaryTextColor;
                    DrawItemColored("Enabled", Input.gyro.enabled.ToString(), enabledColor);
                    DrawItem("Update Interval", Utility.StringUtil.Format("{0:F4} s", Input.gyro.updateInterval), "更新间隔（秒）");
                }
                EndPanel();

                if (Input.gyro.enabled)
                {
                    DrawSectionTitle("Device Attitude");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        Vector3 euler = Input.gyro.attitude.eulerAngles;
                        DrawItem("Pitch (X)", Utility.StringUtil.Format("{0:F2}°", euler.x), "俯仰角");
                        DrawItem("Yaw (Y)", Utility.StringUtil.Format("{0:F2}°", euler.y), "偏航角");
                        DrawItem("Roll (Z)", Utility.StringUtil.Format("{0:F2}°", euler.z), "翻滚角");
                    }
                    EndPanel();

                    DrawSectionTitle("Motion Data");
                    BeginPanel();
                    {
                        ResetRowIndex();
                        Vector3 gravity = Input.gyro.gravity;
                        DrawItem("Gravity", Utility.StringUtil.Format("({0:F3}, {1:F3}, {2:F3})",
                            gravity.x, gravity.y, gravity.z), "重力加速度向量");

                        Vector3 rotRate = Input.gyro.rotationRate;
                        DrawItem("Rotation Rate", Utility.StringUtil.Format("({0:F3}, {1:F3}, {2:F3})",
                            rotRate.x, rotRate.y, rotRate.z), "旋转速率（rad/s）");

                        Vector3 rotRateUnbiased = Input.gyro.rotationRateUnbiased;
                        DrawItem("Rotation Rate Unbiased", Utility.StringUtil.Format("({0:F3}, {1:F3}, {2:F3})",
                            rotRateUnbiased.x, rotRateUnbiased.y, rotRateUnbiased.z), "无偏旋转速率");

                        Vector3 userAccel = Input.gyro.userAcceleration;
                        DrawItem("User Acceleration", Utility.StringUtil.Format("({0:F3}, {1:F3}, {2:F3})",
                            userAccel.x, userAccel.y, userAccel.z), "用户加速度");
                    }
                    EndPanel();
                }
                else
                {
                    DrawSectionTitle("Gyroscope Data");
                    BeginPanel();
                    {
                        DrawInfoMessage("Enable gyroscope to view motion data...");
                    }
                    EndPanel();
                }
            }
        }
    }
}