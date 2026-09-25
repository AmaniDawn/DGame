using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.Observability
{
    /// <summary>
    /// Read-only observability commands over the captured Editor console (CLI-198). Logs are captured
    /// continuously by <see cref="ConsoleLogBuffer"/>; these commands let an agent read them back as
    /// structured data and clear the buffer (and the Unity console) between runs.
    /// </summary>
    public static class ConsoleCommands
    {
        [CliCommand("get_console_logs", "Read recently captured Editor console logs (structured).")]
        public static object GetConsoleLogs(
            [CliArg("severity", "Filter: all | log | warning | error. 'all' = every entry; 'log' = Log only; 'warning' = Warning only; 'error' = Error/Exception/Assert only.")] string severity = "all",
            [CliArg("limit", "Max entries to return (most-recent first), capped at 1000.")] int limit = 100)
        {
            // Snapshot is oldest-first; filter by tier, then take the most-recent `limit` entries
            // and present them newest-first.
            var snapshot = ConsoleLogBuffer.Snapshot();
            var filtered = new List<ConsoleLogEntryDto>(snapshot.Count);
            foreach (var entry in snapshot)
            {
                if (MatchesSeverity(entry.Type, severity))
                    filtered.Add(entry);
            }

            var clampedLimit = Math.Min(Math.Max(limit, 1), ConsoleLogBuffer.MaxEntries);

            var logs = new List<ConsoleLogEntryDto>(Math.Min(clampedLimit, filtered.Count));
            for (var i = filtered.Count - 1; i >= 0 && logs.Count < clampedLimit; i--)
                logs.Add(filtered[i]);

            return new
            {
                total = filtered.Count,
                returned = logs.Count,
                logs
            };
        }

        [CliCommand("clear_console", "Clear the captured log buffer and the Unity Editor console.")]
        public static object ClearConsole()
        {
            ConsoleLogBuffer.Clear();

            // Best-effort: also clear Unity's own console window. UnityEditor.LogEntries is internal,
            // so reach it via reflection and swallow any failure (API is not part of the public contract).
            // LogEntries.Clear is a static, non-public method, so include NonPublic in the binding flags —
            // GetMethod("Clear") with no flags only finds public methods and would silently return null.
            try
            {
                Type.GetType("UnityEditor.LogEntries,UnityEditor")
                    ?.GetMethod("Clear", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    ?.Invoke(null, null);
            }
            catch
            {
                // Ignore — clearing the Editor console is auxiliary; the buffer is already cleared.
            }

            return new { cleared = true };
        }

        /// <summary>
        /// Map a Unity <see cref="UnityEngine.LogType"/> name onto the requested severity tier.
        /// The tiers are distinct: "all" matches every entry; "log" matches Log only;
        /// "warning" matches Warning only; "error" matches Error/Exception/Assert only.
        /// Any unknown value falls back to "all" (matches everything).
        /// </summary>
        private static bool MatchesSeverity(string type, string severity)
        {
            switch (severity?.ToLowerInvariant())
            {
                case "log":
                    return type == "Log";
                case "warning":
                    return type == "Warning";
                case "error":
                    return type == "Error" || type == "Exception" || type == "Assert";
                case "all":
                default:
                    return true;
            }
        }
    }
}
