using System.Globalization;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class TimeInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Time Scale");
                BeginPanel();
                {
                    string scaleDesc = GetTimeScaleDescription(Time.timeScale);
                    Color32 scaleColor = GetTimeScaleColor(Time.timeScale);
                    DrawItemColored("Time Scale",
                        Utility.StringUtil.Format("{0}x [{1}]", Time.timeScale, scaleDesc), scaleColor);
                }
                EndPanel();

                DrawSectionTitle("Total Time");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Realtime Since Startup",
                        FormatTime(Time.realtimeSinceStartup), "从应用启动开始的真实时间");
                    DrawItem("Time Since Level Load",
                        FormatTime(Time.timeSinceLevelLoad), "从当前场景加载开始的时间");
                    DrawItem("Time",
                        FormatTime(Time.time), "从游戏开始的总时间（受时间缩放影响）");
                    DrawItem("Fixed Time",
                        Time.fixedTime.ToString(CultureInfo.InvariantCulture), "FixedUpdate使用的累计时间（秒）");
                    DrawItem("Unscaled Time",
                        FormatTime(Time.unscaledTime), "不受时间缩放影响的总时间");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Fixed Unscaled Time",
                        Time.fixedUnscaledTime.ToString(CultureInfo.InvariantCulture), "不受时间缩放影响的FixedUpdate累计时间");
#endif
                }
                EndPanel();

                DrawSectionTitle("Delta Time");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Delta Time",
                        Utility.StringUtil.Format("{0:F4} s ({1:F1} ms)", Time.deltaTime, Time.deltaTime * 1000f), "上一帧到当前帧的时间间隔");
                    DrawItem("Fixed Delta Time",
                        Utility.StringUtil.Format("{0:F4} s ({1:F1} ms)", Time.fixedDeltaTime, Time.fixedDeltaTime * 1000f), "FixedUpdate的固定时间间隔");
                    DrawItem("Unscaled Delta Time",
                        Utility.StringUtil.Format("{0:F4} s ({1:F1} ms)", Time.unscaledDeltaTime, Time.unscaledDeltaTime * 1000f), "不受时间缩放影响的帧间隔");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Fixed Unscaled Delta Time",
                        Utility.StringUtil.Format("{0:F4} s", Time.fixedUnscaledDeltaTime), "不受时间缩放影响FixedUpdate的固定时间间隔");
#endif
                    DrawItem("Smooth Delta Time",
                        Utility.StringUtil.Format("{0:F4} s", Time.smoothDeltaTime), "平滑处理的时间增量");
                    DrawItem("Maximum Delta Time",
                        Utility.StringUtil.Format("{0:F4} s", Time.maximumDeltaTime), "最大允许的帧时间间隔");
#if UNITY_5_5_OR_NEWER
                    DrawItem("Maximum Particle Delta Time",
                        Utility.StringUtil.Format("{0:F4} s", Time.maximumParticleDeltaTime), "粒子系统的最大时间间隔");
#endif
                }
                EndPanel();

                DrawSectionTitle("Frame Count");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Frame Count", Time.frameCount.ToString(), "游戏运行的总帧数");
                    DrawItem("Rendered Frame Count", Time.renderedFrameCount.ToString(), "实际渲染的帧数");
                    DrawItem("Capture Framerate", Time.captureFramerate.ToString(), "截图/录像的固定帧率");
#if UNITY_2019_2_OR_NEWER
                    DrawItem("Capture Delta Time",
                        Time.captureDeltaTime.ToString(CultureInfo.InvariantCulture), "截图/录像的固定帧时间间隔");
#endif
#if UNITY_5_6_OR_NEWER
                    DrawItem("In Fixed Time Step", Time.inFixedTimeStep.ToString(), "当前是否在FixedUpdate中执行");
#endif
                }
                EndPanel();
            }

            private string FormatTime(float seconds)
            {
                int totalSeconds = (int)seconds;
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int secs = totalSeconds % 60;

                if (hours > 0)
                {
                    return Utility.StringUtil.Format("{0}h {1}m {2}s ({3:F2}s)", hours, minutes, secs, seconds);
                }
                else if (minutes > 0)
                {
                    return Utility.StringUtil.Format("{0}m {1}s ({2:F2}s)", minutes, secs, seconds);
                }
                else
                {
                    return Utility.StringUtil.Format("{0:F2} s", seconds);
                }
            }

            private string GetTimeScaleDescription(float timeScale)
            {
                if (timeScale <= 0f)
                {
                    return "Paused";
                }

                if (timeScale < 1f)
                {
                    return "Slower";
                }

                if (timeScale > 1f)
                {
                    return "Faster";
                }

                return "Normal";
            }

            private Color32 GetTimeScaleColor(float timeScale)
            {
                if (timeScale <= 0f)
                {
                    return DebuggerStyles.ErrorColor;
                }

                if (timeScale < 1f)
                {
                    return DebuggerStyles.WarningColor;
                }

                if (timeScale > 1f)
                {
                    return DebuggerStyles.PrimaryColor;
                }

                return DebuggerStyles.SuccessColor;
            }
        }
    }
}
