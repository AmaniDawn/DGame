#if UNITY_2023_2_OR_NEWER
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.Observability
{
    /// <summary>
    /// Records the analytics events the Pipeline sends and lists the most recent ones, so a client
    /// can check what a command it ran was reported as. Nothing is recorded until a client asks.
    /// </summary>
    static class AnalyticsEventsCommand
    {
        const string Group = "diagnostics";

        [CliGroupCommand(Group,
            "Start recording the analytics events the Pipeline sends, for diagnostics recent_analytics to list. Recording lasts until the next domain reload.",
            Tags = new[] { "observability/analytics" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode RecordAnalytics(CommandContext cmd)
        {
            PipelineAnalytics.IsRecording = true;
            cmd.Log.Info("Recording analytics events.");
            return ExitCode.Success;
        }

        // Main thread: a command's event is recorded by a main-thread post queued before its reply,
        // so a read that queues behind it always sees it.
        [CliGroupCommand(Group,
            "List the last analytics events the Pipeline sent while recording, newest last: each one's name, time and payload. Empty until diagnostics record_analytics starts recording.",
            Tags = new[] { "observability/analytics" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode RecentAnalytics(CommandContext cmd)
        {
            foreach (var sent in PipelineAnalytics.RecentEvents)
                cmd.Result.WriteRecord(sent);
            return ExitCode.Success;
        }
    }
}
#endif
