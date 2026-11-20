using Unity.Pipeline.Commands;
using UnityEngine;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Runtime application control commands for Unity Player builds.
    /// Provides basic application lifecycle and performance control.
    /// </summary>
    static class RuntimeApplicationCommand
    {
        const string Group = "runtime";

        [CliGroupCommand(Group, "Gracefully quit the Unity application. In the Editor, behaves like editor quit.", MainThreadRequired = true, Tags = new[] { "runtime/application" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode Quit(CommandContext cmd,
            [CliArg("exit-code", "Exit code for the application")] int exitCode = 0)
        {
#if UNITY_EDITOR
            EditorQuit.Quit(save: false, discard: false, exitCode);
            cmd.Log.Info($"Editor quit scheduled with exit code {exitCode}");
#else
            // Schedule quit on next frame to allow HTTP response to be sent
            var quitObject = new GameObject("PipelineQuitScheduler");
            var quitScheduler = quitObject.AddComponent<QuitScheduler>();
            quitScheduler.Initialize(exitCode);

            cmd.Log.Info($"Application quit scheduled with exit code {exitCode}");
#endif
            cmd.Result.Write(new { exitCode });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Set the target frame rate for the application", MainThreadRequired = true, Tags = new[] { "runtime/application" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetTargetFrameRate(CommandContext cmd,
            [CliArg("frame-rate", "Target frame rate (-1 for platform default, 0 for unlimited)", Required = true)] int frameRate)
        {
            var previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = frameRate;

            var frameRateDescription = frameRate switch
            {
                -1 => "platform default",
                0 => "unlimited (VSync)",
                _ => $"{frameRate} FPS"
            };

            cmd.Log.Info($"Target frame rate set to {frameRateDescription} (was {previousFrameRate})");
            cmd.Result.Write(new { frameRate, previousFrameRate });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Set the time scale for the application", MainThreadRequired = true, Tags = new[] { "runtime/application" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetTimeScale(CommandContext cmd,
            [CliArg("scale", "Time scale multiplier (0.0 to pause, 1.0 for normal speed)", Required = true)] float scale)
        {
            if (scale < 0f)
            {
                cmd.Log.Error("Time scale cannot be negative; use 0 to pause.");
                return ExitCode.InvalidUsage;
            }

            var previousScale = Time.timeScale;
            Time.timeScale = scale;

            var scaleDescription = scale switch
            {
                0f => "paused",
                1f => "normal speed",
                _ => $"{scale}x speed"
            };

            cmd.Log.Info($"Time scale set to {scaleDescription} (was {previousScale}x)");
            cmd.Result.Write(new { scale, previousScale });
            return ExitCode.Success;
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