using System;
using System.Reflection;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Console;
using Unity.Pipeline.Models;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// The console commands: <c>console</c> to read captured output, <c>console_status</c> for its
    /// counters alone, and <c>clear_console</c> to empty it. <c>console</c> backs the CLI's
    /// <c>console [--tail N] [--level log|warn|error] [--follow]</c>.
    ///
    /// All three live in the runtime assembly, so they are available from both the Editor and player
    /// builds — console capture is driven by <see cref="ConsoleLogCapture"/>, which runs in both
    /// contexts.
    ///
    /// The package has no streaming transport (the HTTP server handles one request at a time), so
    /// <c>--follow</c> is realized client-side: the CLI polls this command, prints new entries, and
    /// passes the returned <see cref="ConsoleLogResponse.Cursor"/> back as <c>since</c> on the next
    /// call. The flag mapping is:
    ///   --tail N             -> tail
    ///   --level X            -> level   (minimum severity threshold)
    ///   --follow             -> repeated calls with since = previous cursor and since-session = previous session
    ///
    /// Reads only the in-memory buffer and the last ground-truth sample (no Unity main-thread
    /// APIs), so it is marked <c>MainThreadRequired = false</c> and keeps answering while the
    /// Editor is blocked compiling or importing — which is when a client most needs it.
    /// </summary>
    static class ConsoleCommand
    {
        const string Group = "console";

        /// <summary>Default number of entries returned when <c>tail</c> is not supplied.</summary>
        public const int DefaultTail = 100;

        [CliGroupCommand(Group, "Get captured Unity console output (Editor or Player; supports tail, level filtering, and follow via a cursor)", MainThreadRequired = false, Tags = new[] { "observability/console" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Get(CommandContext cmd,
            [CliArg("tail", "Maximum number of most-recent entries to return")] int tail = DefaultTail,
            [CliArg("level", "Minimum severity to include: log | warn | error")] string level = ConsoleLogBuffer.LevelLog,
            [CliArg("since", "Cursor: only return entries newer than this seq. Use the 'cursor' from a previous response to follow.")] long since = -1,
            [CliArg("since-session", "Session the 'since' cursor came from: pass the 'session' from a previous response. A cursor from another session is refused and the tail returned with reset=true.")] string since_session = null)
        {
            // One result, not a record per entry: the cursor and session that follow it belong to the read.
            cmd.Result.Write(Query(tail, level, since, since_session));
            return ExitCode.Success;
        }

        /// <summary>What <c>console get</c> reads: the entries, and the cursor to follow them with.</summary>
        internal static ConsoleLogResponse Query(int tail = DefaultTail, string level = ConsoleLogBuffer.LevelLog, long since = -1, string since_session = null)
        {
            // Guard against nonsensical tail values; <= 0 would otherwise mean "unlimited" in the
            // buffer, which is not what a caller passing e.g. --tail 0 expects.
            if (tail <= 0)
                tail = DefaultTail;

            var minSeverity = ConsoleLogBuffer.SeverityFromLevelName(level);
            var response = ConsoleLogCapture.Buffer.Query(since, tail, minSeverity, since_session);
            response.GroundTruth = ReadGroundTruth();
            return response;
        }

        [CliGroupCommand(Group, "Console ground truth and buffer counters without pulling entries: compile-failure flag, Editor console counts, and the buffer's retained counts and cursor.", MainThreadRequired = false, Tags = new[] { "observability/console" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Status(CommandContext cmd)
        {
            cmd.Result.Write(ReadStatus());
            return ExitCode.Success;
        }

        /// <summary>What <c>console status</c> reads: the counters and ground truth, with no entries.</summary>
        internal static ConsoleLogResponse ReadStatus()
        {
            // Same shape as `console` with no entries, so a client polling during a long compile
            // gets the counters and the compile-failure flag without paying for the payload.
            var response = ConsoleLogCapture.Buffer.Stats();
            response.GroundTruth = ReadGroundTruth();
            return response;
        }

        [CliGroupCommand(Group, "Clear the captured log buffer and the Unity Editor console.", Tags = new[] { "observability/console" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode Clear(CommandContext cmd)
        {
            // Clearing keeps the sequence counter, so a client following the console sees dropped=true
            // on its next poll rather than silently skipping the window that was cleared.
            ConsoleLogCapture.Buffer.Clear();

            // Best-effort: also clear Unity's own console window. UnityEditor.LogEntries is internal,
            // so reach it by name and swallow any failure (the API is not part of the public contract).
            // Resolving the type by string is also what keeps this command in the runtime assembly:
            // there is no compile-time UnityEditor dependency, and in a player GetType returns null so
            // the chain no-ops and only the captured buffer is cleared.
            // LogEntries.Clear is a static, non-public method, so include NonPublic in the binding
            // flags — GetMethod("Clear") with no flags only finds public methods and would silently
            // return null.
            // Note it leaves editor compile errors standing: those are logged as sticky entries, which
            // Unity's Clear skips, and only a completed compile removes them.
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

            cmd.Result.Write(new { cleared = true });
            return ExitCode.Success;
        }

        /// <summary>
        /// A copy of the last ground-truth sample with its age filled in. Copied rather than
        /// returned directly so the published snapshot stays immutable and shared between callers.
        /// </summary>
        static ConsoleGroundTruth ReadGroundTruth()
        {
            var sample = ConsoleLogCapture.GroundTruth;
            if (sample == null)
                return null;

            var ageMs = (long)(DateTime.UtcNow - sample.SampledUtc).TotalMilliseconds;
            return new ConsoleGroundTruth
            {
                SampledUtc = sample.SampledUtc,
                AgeMs = ageMs < 0 ? 0 : ageMs,
                CompilationFailed = sample.CompilationFailed,
                Compiling = sample.Compiling,
                ConsoleErrors = sample.ConsoleErrors,
                ConsoleWarnings = sample.ConsoleWarnings,
                ConsoleLogs = sample.ConsoleLogs,
                Seeded = sample.Seeded
            };
        }
    }
}
