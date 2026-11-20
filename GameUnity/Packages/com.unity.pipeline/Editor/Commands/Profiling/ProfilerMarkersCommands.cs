using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    internal enum MarkerSort { Self, Total, Gc, Calls }

    static class ProfilerMarkersCommands
    {
        const string Group = "diagnostics";
        const double NsToMs = 1e-6;

        [CliGroupCommand(Group, "Aggregate CPU (default) or GPU (domain gpu) markers over a frame range of a capture session, as the Profile Analyzer does: per-frame self and total time, calls and GC allocation, with marker descriptions.", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static async Task<ExitCode> ProfilerMarkers(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ".", Required = true)] string session = "",
            [CliArg("first-frame", "First frame to analyze. Defaults to the capture's second frame, because the first is usually partial.")] int firstFrame = -1,
            [CliArg("last-frame", "Last frame to analyze. Defaults to the capture's last frame.")] int lastFrame = -1,
            [CliArg("thread", "Thread name, or 'all' to rank leaf cost across every thread. Default 'Main Thread'; with domain gpu the default means every thread that has GPU samples. Pass 'all' or a thread name to be explicit.")] string thread = "Main Thread",
            [CliArg("sort", "Rank markers by: self (default) | total | gc | calls; with domain gpu, self or total. Self, total and gc rank by cost per analyzed frame.")] string sort = "self",
            [CliArg("filter", "Case-insensitive substring of the marker name.")] string filter = "",
            [CliArg("exclude", "Comma-separated marker names to leave out of the ranking and of selfPercent, e.g. 'WaitForTargetFPS,Gfx.WaitForPresentOnGfxThread'. Idle, Semaphore.WaitForSignal and GC.Alloc are always left out.")] string exclude = "",
            [CliArg("limit", "Maximum markers to return. Default 25.")] int limit = 25,
            [CliArg("domain", "cpu (default) or gpu: aggregate GPU time per marker instead of CPU time. GPU needs a capture recorded with the GPU module (see " + CommandNames.DiagnosticsProfilerRecord + " areas).")] string domain = "cpu")
        {
            if (!string.Equals(domain, "cpu", StringComparison.OrdinalIgnoreCase) && !string.Equals(domain, "gpu", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Log.Error($"Unknown domain '{domain}'. Use cpu or gpu.");
                return ExitCode.InvalidUsage;
            }

            if (string.Equals(domain, "gpu", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(sort, true, out GpuSort gpuSort) || !Enum.IsDefined(typeof(GpuSort), gpuSort))
                {
                    cmd.Log.Error($"Unknown sort '{sort}' for domain gpu. Use self or total.");
                    return ExitCode.InvalidUsage;
                }

                var allThreads = string.Equals(thread, "all", StringComparison.OrdinalIgnoreCase) || thread == "Main Thread";
                return await ProfilerGpuCommands.AggregateMarkersAsync(cmd, session, firstFrame, lastFrame, gpuSort, filter, exclude, allThreads ? "all" : thread, limit);
            }

            if (!Enum.TryParse(sort, true, out MarkerSort sortBy) || !Enum.IsDefined(typeof(MarkerSort), sortBy))
            {
                cmd.Log.Error($"Unknown sort '{sort}'. Use self, total, gc or calls.");
                return ExitCode.InvalidUsage;
            }

            var s = ProfilerCaptureSessions.Require(session, out var error);
            if (s == null)
                return error.Report(cmd);

            var generation = s.LoadGeneration;
            if (!s.TryResolveRange(firstFrame, lastFrame, out var first, out var last, out var note, out error))
                return error.Report(cmd);

            // Created before the first await: it captures the execution context, only set until then.
            var slicer = new AnalysisSlicer(s);
            try
            {
                var (aggregate, aggregateError) = await s.AggregateAsync(first, last, thread, slicer);
                if (aggregate == null)
                    return aggregateError.Report(cmd);

                if (note != null)
                    cmd.Log.Info(note);
                if (aggregate.SkippedFrames > 0)
                    cmd.Log.Warning($"{aggregate.SkippedFrames} frames had no data and were skipped.");
                var analyzed = Math.Max(1, aggregate.Aggregator.FrameCount - aggregate.SkippedFrames);
                if (s.LoadGeneration != generation)
                    cmd.Log.Debug($"Loaded '{s.Path}' into the Profiler for this analysis.");
                cmd.Log.Info($"Analyzed frames {first}..{last} ({analyzed} with data) of session {s.Id}, thread '{thread}', sorted by {sortBy.ToString().ToLowerInvariant()}.");

                // Exclusion is output-only, so the cached aggregate serves every exclude list.
                var excludeNames = MarkerFilter.Parse(exclude);
                var kept = aggregate.Aggregator.Markers
                    .Select(p => new { id = p.Key, stats = p.Value, name = NameOf(aggregate, p.Key) })
                    .Where(m => !excludeNames.Contains(m.name))
                    .ToList();
                var matched = aggregate.Names.Values
                    .Where(n => excludeNames.Contains(n))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();
                if (matched.Count > 0)
                    cmd.Log.Info($"Excluded {matched.Count} markers: {string.Join(", ", matched)}.");

                var selfShareNs = 0UL;
                foreach (var m in kept)
                    selfShareNs += m.stats.SelfNs;

                var candidates = kept
                    .Where(m => string.IsNullOrEmpty(filter) || m.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

                IOrderedEnumerable<(int id, MarkerStats stats, string name)> ordered;
                var typed = candidates.Select(m => (m.id, m.stats, m.name));
                switch (sortBy)
                {
                    case MarkerSort.Total: ordered = typed.OrderByDescending(m => m.stats.TotalNs); break;
                    case MarkerSort.Gc: ordered = typed.OrderByDescending(m => m.stats.GcBytes); break;
                    case MarkerSort.Calls: ordered = typed.OrderByDescending(m => m.stats.Calls); break;
                    default: ordered = typed.OrderByDescending(m => m.stats.SelfNs); break;
                }

                var threadNames = new Dictionary<(int, int), string>();
                string ThreadName(int frame, int threadIndex)
                {
                    if (threadNames.TryGetValue((frame, threadIndex), out var cached))
                        return cached;
                    var list = s.Threads(frame, out _);
                    var threadLabel = list != null && threadIndex >= 0 && threadIndex < list.Count ? list[threadIndex].Name : null;
                    threadNames[(frame, threadIndex)] = threadLabel;
                    return threadLabel;
                }

                foreach (var marker in ordered.ThenBy(m => m.name, StringComparer.Ordinal).Take(Math.Max(0, limit)))
                {
                    cmd.Result.WriteRecord(Describe(aggregate, marker.id, marker.stats, marker.name, analyzed,
                        selfShareNs == 0 ? 0 : Math.Round(100.0 * marker.stats.SelfNs / selfShareNs, 3), ThreadName));
                }

                return ExitCode.Success;
            }
            catch (AnalysisInterruptedException ex)
            {
                cmd.Log.Error(ex.Message);
                return ExitCode.Failed;
            }
        }

        static string NameOf(MarkerAggregate aggregate, int id)
        {
            return aggregate.Names.TryGetValue(id, out var name) ? name : $"marker#{id}";
        }

        static JObject Describe(MarkerAggregate aggregate, int id, MarkerStats m, string name, int analyzed, double selfPercent, Func<int, int, string> threadName)
        {
            aggregate.Categories.TryGetValue(id, out var category);
            var gcSorted = m.FrameGcBytes.Select(v => (double)v).OrderBy(v => v).ToList();
            var json = MarkerDescriptions.Annotate(new
            {
                name,
                category,
                framesPresent = m.FramesPresent,
                calls = m.Calls,
                selfPercent,
                selfMs = TimeStats(m.FrameSelfNs, analyzed, null),
                totalMs = TimeStats(m.FrameTotalNs, analyzed, m.MaxFrameTotalFrame),
                gcAllocBytes = new
                {
                    sum = (long)m.GcBytes,
                    mean = gcSorted.Count == 0 ? 0 : Math.Round(gcSorted.Average(), 1),
                    max = gcSorted.Count == 0 ? 0 : (long)gcSorted[gcSorted.Count - 1]
                },
                heaviest = new
                {
                    frame = m.HeaviestSelf.Frame,
                    thread = threadName(m.HeaviestSelf.Frame, m.HeaviestSelf.Thread),
                    sampleId = m.HeaviestSelf.Sample,
                    selfMs = Math.Round(m.HeaviestSelf.Value * NsToMs, 3)
                }
            }, name);

            if (m.HeaviestGc.IsSet)
            {
                json["heaviestGc"] = JObject.FromObject(new
                {
                    frame = m.HeaviestGc.Frame,
                    thread = threadName(m.HeaviestGc.Frame, m.HeaviestGc.Thread),
                    sampleId = m.HeaviestGc.Sample,
                    gcAllocBytes = (long)m.HeaviestGc.Value
                });
            }

            return json;
        }

        /// <summary>Statistics over the frames a marker is present in, plus its cost spread over every analyzed frame.</summary>
        static Dictionary<string, object> TimeStats(List<ulong> perFrameNs, int analyzed, int? maxFrame)
        {
            var ms = perFrameNs.Select(v => v * NsToMs).OrderBy(v => v).ToList();
            var sum = ms.Sum();
            var result = new Dictionary<string, object>
            {
                ["sum"] = Math.Round(sum, 3),
                ["mean"] = ms.Count == 0 ? 0 : Math.Round(sum / ms.Count, 3),
                ["median"] = Math.Round(ProfilerAnalysisMath.Percentile(ms, 0.5), 3),
                ["min"] = ms.Count == 0 ? 0 : Math.Round(ms[0], 3),
                ["max"] = ms.Count == 0 ? 0 : Math.Round(ms[ms.Count - 1], 3)
            };
            if (maxFrame.HasValue)
                result["maxFrame"] = maxFrame.Value;
            result["perAnalyzedFrame"] = Math.Round(sum / analyzed, 3);
            return result;
        }
    }
}
