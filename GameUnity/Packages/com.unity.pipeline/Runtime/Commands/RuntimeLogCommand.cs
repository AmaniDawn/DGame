using Unity.Pipeline.Commands;
using UnityEngine;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Runtime logging commands for Unity Player builds.
    /// Provides console logging capabilities from CLI.
    /// </summary>
    static class RuntimeLogCommand
    {
        const string Group = "console";

        [CliGroupCommand(Group, "Write a message to Unity console", MainThreadRequired = true, Tags = new[] { "observability/console", "runtime" }, Effect = CommandEffect.Additive)]
        internal static ExitCode Write(CommandContext cmd,
            [CliArg("message", "Message to log to console", Required = true)] string message,
            [CliArg("level", "Log level: info, warning, error")] string level = "info")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                cmd.Log.Error("Message cannot be empty.");
                return ExitCode.InvalidUsage;
            }

            Debug.developerConsoleVisible = false;
            string logged;

            switch (level?.ToLowerInvariant() ?? "info")
            {
                case "info":
                case "log":
                    Debug.Log($"[Pipeline] {message}");
                    logged = "info";
                    break;

                case "warning":
                case "warn":
                    Debug.LogWarning($"[Pipeline] {message}");
                    logged = "warning";
                    break;

                case "error":
                case "err":
                    Debug.LogError($"[Pipeline] {message}");
                    logged = "error";
                    break;

                default:
                    Debug.LogWarning($"[Pipeline] Unknown log level '{level}', defaulting to info: {message}");
                    Debug.Log($"[Pipeline] {message}");
                    cmd.Log.Warning($"Unknown log level '{level}'; logged at info. Use info, warning or error.");
                    logged = "info";
                    break;
            }

            cmd.Result.Write(new { level = logged, message });
            return ExitCode.Success;
        }
    }
}