using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    static class ProfilerFrameCommands
    {
        const string Group = "diagnostics";
        const double NsToMs = 1e-6;
        const string DefaultThread = "Main Thread";

        [CliGroupCommand(Group, "Break down one frame of a capture session: timings, bottleneck, the hot path through each thread's call tree, and the samples with the most self time and the most direct GC allocation, each with a sampleId, path and marker description for " + CommandNames.DiagnosticsProfilerSample + ".", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static async Task<ExitCode> ProfilerFrame(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ".", Required = true)] string session = "",
            [CliArg("frame", "Frame index. Defaults to the slowest frame (by CPU frame time, or by GPU time with domain gpu) in the default range.")] int frame = -1,
            [CliArg("thread", "Thread name, or 'all'. Default 'Main Thread'. With domain gpu the default means every thread that has GPU samples; pass 'all' or a thread name to be explicit.")] string thread = DefaultThread,
            [CliArg("domain", "cpu (default) or gpu: rank and trace GPU time per sample instead of CPU time.")] string domain = "cpu",
            [CliArg("limit", "How many entries to list in each of hotPaths, topSelf and topGc. Default 10.")] int limit = 10,
            [CliArg("exclude", "Comma-separated marker names to leave out of hotPaths and topSelf, e.g. 'WaitForTargetFPS,Gfx.WaitForPresentOnGfxThread'. Idle, Semaphore.WaitForSignal and GC.Alloc are always left out.")] string exclude = "",
            [CliArg("target-frame-ms", "Frame budget in ms. 0 uses the Profiler's Target FPS (60 when unavailable).")] double targetFrameMs = 0)
        {
            if (!string.Equals(domain, "cpu", StringComparison.OrdinalIgnoreCase) && !string.Equals(domain, "gpu", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Log.Error($"Unknown domain '{domain}'. Use cpu or gpu.");
                return ExitCode.InvalidUsage;
            }
            var gpu = string.Equals(domain, "gpu", StringComparison.OrdinalIgnoreCase);
            var allGpuThreads = string.Equals(thread, "all", StringComparison.OrdinalIgnoreCase) || thread == DefaultThread;
            if (gpu && !ProfilerGpuCommands.TryGetGpuColumns(out _, out _))
            {
                cmd.Log.Error(ProfilerGpuCommands.NoGpuColumnsMessage);
                return ExitCode.Failed;
            }

            var s = ProfilerCaptureSessions.Require(session, out var error);
            if (s == null)
                return error.Report(cmd);

            var generation = s.LoadGeneration;
            var target = targetFrameMs > 0 ? targetFrameMs : 1000.0 / ProfilerAnalysisMath.TargetFrameRate();

            var slowest = frame < 0;
            if (!s.TryResolveRange(slowest ? -1 : frame, slowest ? -1 : frame, out var first, out var last, out var note, out error))
                return error.Report(cmd);
            if (slowest && note != null)
                cmd.Log.Info(note);

            // Created before the first await: it captures the execution context, only set until then.
            var slicer = new AnalysisSlicer(s);
            try
            {
                string selectedBy;
                if (slowest)
                {
                    var series = await s.TimingsAsync(first, last, slicer);
                    if (series.Error != null)
                        return series.Error.Report(cmd);

                    var found = false;
                    var best = default(FrameTiming);
                    var bestKey = 0.0;
                    foreach (var t in series.Timings)
                    {
                        var key = gpu ? t.GpuMs : t.CpuMs;
                        if (key.HasValue && (!found || key.Value > bestKey))
                        {
                            best = t;
                            bestKey = key.Value;
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        cmd.Log.Error(gpu ? $"No frame in {first}..{last} has GPU timing." : $"No frame in {first}..{last} has data.");
                        return ExitCode.Failed;
                    }

                    frame = best.Frame;
                    selectedBy = "slowest";
                }
                else
                {
                    selectedBy = "argument";
                }

                if (!s.TryGetTiming(frame, out var timing, out error))
                    return (error ?? ProfilerCaptureSession.Error($"Frame {frame} has no data.")).Report(cmd);

                var threads = s.Threads(frame, out error);
                if (threads == null)
                    return error.Report(cmd);

                if (gpu)
                {
                    var gpuExclude = MarkerFilter.Parse(exclude);
                    return ProfilerGpuCommands.FrameBreakdown(cmd, s, frame, timing, threads, thread, allGpuThreads, limit, gpuExclude, selectedBy, target, generation);
                }

                var threadIndices = new List<int>();
                if (string.Equals(thread, "all", StringComparison.OrdinalIgnoreCase))
                    threadIndices.AddRange(threads.Select(t => t.Index));
                else
                {
                    var index = s.ResolveThread(frame, thread, out error);
                    if (index < 0)
                        return error.Report(cmd);
                    threadIndices.Add(index);
                }

                var excludeNames = MarkerFilter.Parse(exclude);
                var (aggregate, aggregateError) = await s.AggregateAsync(frame, frame, thread, slicer);
                if (aggregate == null)
                    return aggregateError.Report(cmd);

                var views = new List<(int Thread, RawFrameDataView View)>();
                try
                {
                    foreach (var index in threadIndices)
                    {
                        var view = ProfilerDriver.GetRawFrameDataView(frame, index);
                        if (view == null || !view.valid)
                        {
                            view?.Dispose();
                            continue;
                        }

                        views.Add((index, view));
                        await slicer.CheckpointAsync();
                    }

                    var excludedIds = new HashSet<int>();
                    var matched = new List<string>();
                    if (views.Count > 0)
                    {
                        var any = views[0].View;
                        foreach (var name in ProfilerCaptureSession.DefaultExcludedMarkers)
                            AddMarkerId(excludedIds, any, name);
                        foreach (var name in excludeNames)
                        {
                            if (AddMarkerId(excludedIds, any, name))
                                matched.Add(name);
                        }
                    }

                    matched.Sort(StringComparer.Ordinal);

                    var trees = await slicer.OffMainThreadAsync(() =>
                    {
                        var built = new RawCallTree[views.Count];
                        Parallel.For(0, views.Count, i =>
                        {
                            built[i] = RawCallTree.Build(new RawFrameDataViewSource(views[i].View));
                        });
                        return built;
                    });

                    var viewByThread = views.ToDictionary(v => v.Thread, v => v.View);
                    var topSelf = RankMarkers(aggregate, threads, viewByThread, excludeNames, limit, byGc: false);
                    var topGc = RankMarkers(aggregate, threads, viewByThread, excludeNames, limit, byGc: true);

                    var hotPaths = new List<(ulong Total, object Entry)>();
                    for (var i = 0; i < views.Count; i++)
                    {
                        var path = trees[i].HotPath(excludedIds);
                        if (path.Count == 0)
                            continue;

                        var steps = HotSteps(trees[i], path, views[i].View);
                        hotPaths.Add((path[0].TotalNs, new
                        {
                            thread = views[i].View.threadName,
                            threadMs = Math.Round(trees[i].Root.TotalNs * NsToMs, 3),
                            steps
                        }));
                    }

                    if (hotPaths.Count == 0)
                        cmd.Log.Warning("No thread has a hot path after exclusions.");

                    cmd.Result.Write(new
                    {
                        sessionId = s.Id,
                        domain = "cpu",
                        frame,
                        frameSelectedBy = selectedBy,
                        loaded = s.LoadGeneration != generation,
                        timing = new
                        {
                            cpuMs = Math.Round(timing.CpuMs, 3),
                            mainActiveMs = ProfilerSummaryCommands.Round(timing.MainActiveMs),
                            renderActiveMs = ProfilerSummaryCommands.Round(timing.RenderActiveMs),
                            gpuMs = ProfilerSummaryCommands.Round(timing.GpuMs),
                            gcAllocBytes = timing.GcAllocBytes,
                            bottleneck = ProfilerAnalysisMath.Bottleneck(timing, target),
                            targetFrameMs = Math.Round(target, 3)
                        },
                        threads = threads.Select(t => t.Name).ToList(),
                        hotPaths = hotPaths.OrderByDescending(h => h.Total).Take(Math.Max(0, limit)).Select(h => h.Entry).ToList(),
                        topSelf,
                        topGc,
                        excluded = matched
                    });
                    return ExitCode.Success;
                }
                finally
                {
                    foreach (var entry in views)
                        entry.View.Dispose();
                }
            }
            catch (AnalysisInterruptedException ex)
            {
                cmd.Log.Error(ex.Message);
                return ExitCode.Failed;
            }
        }

        static bool AddMarkerId(HashSet<int> ids, RawFrameDataView view, string name)
        {
            var id = view.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return false;
            ids.Add(id);
            return true;
        }

        static List<object> HotSteps(RawCallTree tree, List<RawCallTree.Node> path, RawFrameDataView view)
        {
            var steps = new List<object>();
            var parentNs = tree.Root.TotalNs;
            for (var i = 0; i < path.Count; i++)
            {
                var node = path[i];
                var name = view.GetMarkerName(node.MarkerId);
                steps.Add(MarkerDescriptions.Annotate(new
                {
                    sampleId = node.HeaviestSample,
                    name,
                    instances = node.Instances,
                    totalMs = Math.Round(node.TotalNs * NsToMs, 3),
                    selfMs = Math.Round(node.SelfNs * NsToMs, 3),
                    percentOfThread = Percent(node.TotalNs, tree.Root.TotalNs),
                    percentOfParent = Percent(node.TotalNs, parentNs),
                    hotLeaf = i == path.Count - 1
                }, name));
                parentNs = node.TotalNs;
            }

            return steps;
        }

        static double Percent(ulong part, ulong whole) => whole == 0 ? 0 : Math.Round(100.0 * part / whole, 1);

        /// <summary>The markers of the frame with the most self time (or direct GC allocation), each linked to its heaviest instance.</summary>
        static List<object> RankMarkers(MarkerAggregate aggregate, List<ThreadInfo> threads,
            Dictionary<int, RawFrameDataView> viewByThread, HashSet<string> excludeNames, int limit, bool byGc)
        {
            var rows = new List<(int Id, string Name, MarkerStats Stats, SampleRef Ref)>();
            foreach (var pair in aggregate.Aggregator.Markers)
            {
                aggregate.Names.TryGetValue(pair.Key, out var name);
                name ??= $"marker#{pair.Key}";
                if (!byGc && excludeNames.Contains(name))
                    continue;

                var reference = byGc ? pair.Value.HeaviestGc : pair.Value.HeaviestSelf;
                if (byGc && (pair.Value.GcBytes == 0 || !reference.IsSet))
                    continue;
                rows.Add((pair.Key, name, pair.Value, reference));
            }

            var ordered = byGc
                ? rows.OrderByDescending(r => r.Stats.GcBytes)
                : rows.OrderByDescending(r => r.Stats.SelfNs);

            var result = new List<object>();
            foreach (var row in ordered.ThenBy(r => r.Name, StringComparer.Ordinal).Take(Math.Max(0, limit)))
            {
                aggregate.Categories.TryGetValue(row.Id, out var category);
                string path = null;
                if (row.Ref.IsSet && viewByThread.TryGetValue(row.Ref.Thread, out var view))
                    path = RawSampleTree.Path(new RawFrameDataViewSource(view), row.Ref.Sample);

                var threadName = row.Ref.Thread >= 0 && row.Ref.Thread < threads.Count ? threads[row.Ref.Thread].Name : null;
                if (byGc)
                {
                    result.Add(MarkerDescriptions.Annotate(new
                    {
                        name = row.Name,
                        category,
                        calls = row.Stats.Calls,
                        gcAllocBytes = (long)row.Stats.GcBytes,
                        sampleId = row.Ref.Sample,
                        thread = threadName,
                        heaviestGcBytes = (long)row.Ref.Value,
                        path
                    }, row.Name));
                }
                else
                {
                    result.Add(MarkerDescriptions.Annotate(new
                    {
                        name = row.Name,
                        category,
                        calls = row.Stats.Calls,
                        selfMs = Math.Round(row.Stats.SelfNs * NsToMs, 3),
                        totalMs = Math.Round(row.Stats.TotalNs * NsToMs, 3),
                        gcAllocBytes = (long)row.Stats.GcBytes,
                        sampleId = row.Ref.Sample,
                        thread = threadName,
                        heaviestSelfMs = Math.Round(row.Ref.Value * NsToMs, 3),
                        path
                    }, row.Name));
                }
            }

            return result;
        }
    }
}
