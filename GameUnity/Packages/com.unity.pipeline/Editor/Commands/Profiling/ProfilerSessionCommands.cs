using System.Linq;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>Opens and closes capture sessions, the handle every Profiler capture analysis command takes.</summary>
    static class ProfilerSessionCommands
    {
        const string Group = "diagnostics";

        [CliGroupCommand(Group, "Open a capture session on a saved Profiler .data capture and return its sessionId, which every analysis command takes. Reopening the same file returns its existing session. Without capture, lists the open sessions. Loads the capture into this Editor's Profiler, replacing what it holds: a capture open in the Profiler window or an unsaved recording is discarded, so loading requires confirm=true (use dry-run to preview).", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode ProfilerSession(CommandContext cmd,
            [CliArg("capture", "Path to a .data capture (absolute or relative to the project root), from " + CommandNames.DiagnosticsProfilerRecord + " or the Profiler window's Save. Omit to list the open sessions.")] string capture = "",
            [CliArg("confirm", "Apply the load, replacing the Editor's current Profiler history. Without it the call is refused. Not needed when the capture is already loaded.")] bool confirm = false,
            [CliArg("dry-run", "Preview the capture to load and the Profiler history it would replace, without loading anything.")] bool dryRun = false)
        {
            if (string.IsNullOrWhiteSpace(capture))
            {
                foreach (var open in ProfilerCaptureSessions.All)
                {
                    cmd.Result.WriteRecord(new
                    {
                        sessionId = open.Id,
                        capture = open.Path,
                        resident = ProfilerCaptureSessions.IsResident(open)
                    });
                }

                return ExitCode.Success;
            }

            var session = ProfilerCaptureSessions.Open(capture, confirm, dryRun, out var error, out var loaded, out var preview);
            if (preview != null)
            {
                cmd.Result.Write(preview);
                return ExitCode.Success;
            }

            if (session == null)
                return error.Report(cmd);

            var isEditor = session.IsEditorCapture(out error);
            if (error != null)
                return error.Report(cmd);

            var threads = session.Threads(session.First, out error);
            if (threads == null)
                return error.Report(cmd);

            cmd.Result.Write(new
            {
                sessionId = session.Id,
                capture = session.Path,
                loaded,
                captureKind = isEditor ? "editor" : "player",
                frameRange = new { first = session.First, last = session.Last, count = session.FrameCount },
                threads = threads.Select(t => new { index = t.Index, name = t.Name, group = t.Group }).ToList(),
                markerDescriptions = MarkerDescriptions.Available
            });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Close a capture session, or all of them, releasing its cached frame views and Profiler event subscriptions. The Profiler keeps whatever frames it holds.", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode ProfilerClose(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ", or 'all' to close every session.", Required = true)] string session = "")
        {
            if (string.Equals(session, "all", System.StringComparison.OrdinalIgnoreCase))
            {
                cmd.Result.Write(new { closed = ProfilerCaptureSessions.CloseAll() });
                return ExitCode.Success;
            }

            if (!ProfilerCaptureSessions.Close(session ?? ""))
            {
                ProfilerCaptureSessions.Get(session, out var error);
                return error.Report(cmd);
            }

            cmd.Result.Write(new { closed = 1 });
            return ExitCode.Success;
        }
    }
}
