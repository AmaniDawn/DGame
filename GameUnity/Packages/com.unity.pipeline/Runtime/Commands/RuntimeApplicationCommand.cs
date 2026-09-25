using Unity.Pipeline.Commands;
using UnityEngine;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Runtime application control commands for Unity Player builds.
    /// Provides basic application lifecycle and performance control.
    /// </summary>
    public static class RuntimeApplicationCommand
    {
        [CliCommand("quit", "Gracefully quit the Unity application", MainThreadRequired = true, RuntimeOnly = true)]
        public static string QuitApplication(
            [CliArg("exitCode", "Exit code for the application")] int exitCode = 0)
        {
            // Schedule quit on next frame to allow HTTP response to be sent
            var quitObject = new GameObject("PipelineQuitScheduler");
            var quitScheduler = quitObject.AddComponent<QuitScheduler>();
            quitScheduler.Initialize(exitCode);

            return $"Application quit scheduled with exit code {exitCode}";
        }

        [CliCommand("set_target_framerate", "Set the target frame rate for the application", MainThreadRequired = true, RuntimeOnly = true)]
        public static string SetTargetFrameRate(
            [CliArg("frameRate", "Target frame rate (-1 for platform default, 0 for unlimited)", Required = true)] int frameRate)
        {
            var previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = frameRate;

            var frameRateDescription = frameRate switch
            {
                -1 => "platform default",
                0 => "unlimited (VSync)",
                _ => $"{frameRate} FPS"
            };

            return $"Target frame rate set to {frameRateDescription} (was {previousFrameRate})";
        }

        [CliCommand("set_timescale", "Set the time scale for the application", MainThreadRequired = true, RuntimeOnly = true)]
        public static string SetTimeScale(
            [CliArg("scale", "Time scale multiplier (0.0 to pause, 1.0 for normal speed)", Required = true)] float scale)
        {
            if (scale < 0f)
            {
                return "Error: Time scale cannot be negative";
            }

            var previousScale = Time.timeScale;
            Time.timeScale = scale;

            var scaleDescription = scale switch
            {
                0f => "paused",
                1f => "normal speed",
                _ => $"{scale}x speed"
            };

            return $"Time scale set to {scaleDescription} (was {previousScale}x)";
        }

        /// <summary>
        /// MonoBehaviour to handle delayed application quit.
        /// Allows HTTP response to be sent before application terminates.
        /// </summary>
        private class QuitScheduler : MonoBehaviour
        {
            private int m_ExitCode;
            private float m_QuitTime;

            public void Initialize(int exitCode)
            {
                m_ExitCode = exitCode;
                m_QuitTime = Time.realtimeSinceStartup + 0.1f; // 100ms delay
                DontDestroyOnLoad(gameObject);
            }

            private void Update()
            {
                if (Time.realtimeSinceStartup >= m_QuitTime)
                {
                    Debug.Log($"Pipeline: Quitting application with exit code {m_ExitCode}");
                    Application.Quit(m_ExitCode);
                }
            }
        }
    }
}