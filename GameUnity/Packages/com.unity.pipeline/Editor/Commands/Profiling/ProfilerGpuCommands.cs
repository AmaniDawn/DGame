using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    internal enum GpuSort { Self, Total }

    static class ProfilerGpuCommands
    {
        const string NoSamplesNote = "No GPU samples in this range. Record with the GPU Usage Profiler module enabled, on a graphics API that supports GPU recording (SystemInfo.supportsGpuRecorder).";

        const int SlowRangeFrames = 300;
        internal const string NoGpuColumnsMessage = "This Editor's Profiler API exposes no GPU hierarchy columns.";

        static int s_TotalGpuColumn = -1;
        static int s_SelfGpuColumn = -1;
        static bool s_ColumnsResolved;

        sealed class GpuMarker
        {
            public readonly List<double> SelfMs = new List<double>();
            public readonly List<double> TotalMs = new List<double>();
            public readonly List<int> Frames = new List<int>();
            public double SelfSumMs;
            public double TotalSumMs;
            public readonly HashSet<string> Threads = new HashSet<string>(StringComparer.Ordinal);
            public double HeaviestSelfMs = -1;
            public int HeaviestFrame;
            public int HeaviestSample = -1;
            public string HeaviestThread;
        }

        sealed class FrameSums
        {
            public double SelfMs;
            public double TotalMs;
            public double HeaviestSelfMs = -1;
            public int HeaviestSample = -1;
            public string HeaviestThread;
        }

        /// <summary>
        /// The <c>domain gpu</c> path of <c>diagnostics profiler_markers</c>: self and total GPU time per marker over a frame range.
        /// <paramref name="thread"/> is one thread name, or 'all' for every thread with GPU samples.
        /// </summary>
        internal static async Task<ExitCode> AggregateMarkersAsync(CommandContext cmd, string session, int firstFrame, int lastFrame, GpuSort sortBy,
            string filter, string exclude, string thread, int limit)
        {
            var s = ProfilerCaptureSessions.Require(session, out var error);
            if (s == null)
                return error.Report(cmd);

            ResolveColumns();
            if (s_TotalGpuColumn < 0 || s_SelfGpuColumn < 0)
            {
                cmd.Log.Error(NoGpuColumnsMessage);
                return ExitCode.Failed;
            }

            var generation = s.LoadGeneration;
            if (!s.TryResolveRange(firstFrame, lastFrame, out var first, out var last, out var note, out error))
                return error.Report(cmd);

            // Created before the first await: it captures the execution context, only set until then.
            var slicer = new AnalysisSlicer(s);
            try
            {
                if (note != null)
                    cmd.Log.Info(note);

                var allThreads = string.Equals(thread, "all", StringComparison.OrdinalIgnoreCase);
                var markers = new Dictionary<string, GpuMarker>(StringComparer.Ordinal);
                var gpuThreads = new HashSet<string>(StringComparer.Ordinal);
                var gpuFrameMs = new List<double>();
                var onPath = new Dictionary<string, int>(StringComparer.Ordinal);
                var childLists = new List<List<int>>();
                var indices = new List<int>();
                for (var frame = first; frame <= last; frame++)
                {
                    if (s.TryGetTiming(frame, out var timing, out error))
                    {
                        if (timing.GpuMs.HasValue)
                            gpuFrameMs.Add(timing.GpuMs.Value);
                    }
                    else if (error != null)
                        return error.Report(cmd);

                    var perFrame = new Dictionary<string, FrameSums>(StringComparer.Ordinal);
                    for (var t = 0; ; t++)
                    {
                        await slicer.CheckpointAsync();

                        string threadName;
                        int sampleCount;
                        using (var raw = ProfilerDriver.GetRawFrameDataView(frame, t))
                        {
                            if (raw == null || !raw.valid)
                                break;
                            threadName = raw.threadName;
                            sampleCount = raw.sampleCount;
                        }

                        // A thread with only its root sample cannot hold GPU samples.
                        if (sampleCount <= 1)
                            continue;
                        if (!allThreads && !string.Equals(threadName, thread, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Not the session's cached views, which merge same-named samples: this view keeps each GPU
                        // instance apart so it can be linked to, and a view per thread per frame would stay alive
                        // for the whole session.
                        using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                                   frame, t, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                        {
                            if (view == null || !view.valid)
                                continue;
                            var root = view.GetRootItemID();
                            if (view.GetItemColumnDataAsFloat(root, s_TotalGpuColumn) <= 0)
                                continue;

                            gpuThreads.Add(threadName);
                            Walk(view, root, threadName, onPath, perFrame, markers, childLists, indices);
                        }
                    }

                    foreach (var pair in perFrame)
                    {
                        var marker = markers[pair.Key];
                        marker.SelfMs.Add(pair.Value.SelfMs);
                        marker.TotalMs.Add(pair.Value.TotalMs);
                        marker.SelfSumMs += pair.Value.SelfMs;
                        marker.TotalSumMs += pair.Value.TotalMs;
                        marker.Frames.Add(frame);
                        if (pair.Value.HeaviestSelfMs > marker.HeaviestSelfMs)
                        {
                            marker.HeaviestSelfMs = pair.Value.HeaviestSelfMs;
                            marker.HeaviestFrame = frame;
                            marker.HeaviestThread = pair.Value.HeaviestThread;
                            marker.HeaviestSample = pair.Value.HeaviestSample;
                        }
                    }
                }

                if (markers.Count == 0)
                    cmd.Log.Warning(allThreads ? NoSamplesNote : $"No GPU samples on thread '{thread}'.");
                if (last - first + 1 > SlowRangeFrames)
                    cmd.Log.Warning($"GPU aggregation reads a hierarchy view per frame and thread, so a range of {last - first + 1} frames is slow. Narrow first-frame and last-frame, or set thread.");

                var excludeNames = MarkerFilter.Parse(exclude);
                var kept = markers.Where(p => !excludeNames.Contains(p.Key)).ToList();
                var matched = excludeNames.Where(markers.ContainsKey).OrderBy(n => n, StringComparer.Ordinal).ToList();
                if (matched.Count > 0)
                    cmd.Log.Info($"Excluded {matched.Count} markers: {string.Join(", ", matched)}.");
                var selfShareMs = kept.Sum(p => p.Value.SelfSumMs);

                var filtered = kept.Where(p => string.IsNullOrEmpty(filter) || p.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                var ordered = sortBy == GpuSort.Total
                    ? filtered.OrderByDescending(p => p.Value.TotalSumMs)
                    : filtered.OrderByDescending(p => p.Value.SelfSumMs);

                if (s.LoadGeneration != generation)
                    cmd.Log.Debug($"Loaded '{s.Path}' into the Profiler for this analysis.");
                var gpuFrameStats = SeriesStats.From(gpuFrameMs);
                cmd.Log.Info($"Analyzed GPU time over frames {first}..{last} of session {s.Id}, threads {string.Join(", ", gpuThreads.OrderBy(n => n, StringComparer.Ordinal))}, sorted by {sortBy.ToString().ToLowerInvariant()}." +
                    (gpuFrameStats == null ? "" : $" GPU frame time: {Newtonsoft.Json.JsonConvert.SerializeObject(gpuFrameStats.ToJson())} ms."));

                var records = ordered
                        .ThenBy(p => p.Key, StringComparer.Ordinal)
                        .Take(Math.Max(0, limit))
                        .Select(p => MarkerDescriptions.Annotate(new
                        {
                            name = p.Key,
                            threads = p.Value.Threads.OrderBy(n => n, StringComparer.Ordinal).ToList(),
                            framesPresent = p.Value.Frames.Count,
                            selfPercent = selfShareMs <= 0 ? 0 : Math.Round(100.0 * p.Value.SelfSumMs / selfShareMs, 3),
                            gpuSelfMs = Stats(p.Value.SelfMs, p.Value.Frames),
                            gpuTotalMs = Stats(p.Value.TotalMs, p.Value.Frames),
                            heaviest = new
                            {
                                frame = p.Value.HeaviestFrame,
                                thread = p.Value.HeaviestThread,
                                sampleId = p.Value.HeaviestSample,
                                gpuSelfMs = Math.Round(p.Value.HeaviestSelfMs, 3)
                            }
                        }, p.Key));
                foreach (var record in records)
                    cmd.Result.WriteRecord(record);
                return ExitCode.Success;
            }
            catch (AnalysisInterruptedException ex)
            {
                cmd.Log.Error(ex.Message);
                return ExitCode.Failed;
            }
        }

        /// <summary>
        /// Adds each GPU-timed item under <paramref name="id"/> to <paramref name="perFrame"/> by name: self time
        /// always, total time only for the outermost occurrence of a name. Subtrees with no GPU time are skipped.
        /// </summary>
        static void Walk(HierarchyFrameDataView view, int id, string thread, Dictionary<string, int> onPath,
            Dictionary<string, FrameSums> perFrame, Dictionary<string, GpuMarker> markers,
            List<List<int>> childLists, List<int> indices, int depthLevel = 0)
        {
            // One reusable children list per depth: a list is only needed while its level is being walked.
            if (depthLevel == childLists.Count)
                childLists.Add(new List<int>());
            var children = childLists[depthLevel];
            children.Clear();
            view.GetItemChildren(id, children);
            foreach (var child in children)
            {
                var total = view.GetItemColumnDataAsFloat(child, s_TotalGpuColumn);
                if (total <= 0)
                    continue;

                var name = view.GetItemName(child);
                if (!perFrame.TryGetValue(name, out var sums))
                {
                    sums = new FrameSums();
                    perFrame.Add(name, sums);
                }

                if (!markers.TryGetValue(name, out var marker))
                {
                    marker = new GpuMarker();
                    markers.Add(name, marker);
                }

                marker.Threads.Add(thread);
                var self = view.GetItemColumnDataAsFloat(child, s_SelfGpuColumn);
                sums.SelfMs += self;
                if (self > sums.HeaviestSelfMs)
                {
                    sums.HeaviestSelfMs = self;
                    sums.HeaviestSample = FirstRawIndex(view, child, indices);
                    sums.HeaviestThread = thread;
                }

                onPath.TryGetValue(name, out var depth);
                if (depth == 0)
                    sums.TotalMs += total;

                onPath[name] = depth + 1;
                Walk(view, child, thread, onPath, perFrame, markers, childLists, indices, depthLevel + 1);
                onPath[name] = depth;
            }
        }

        static object Stats(List<double> perFrame, List<int> frames)
        {
            var sorted = perFrame.OrderBy(v => v).ToList();
            var maxIndex = perFrame.IndexOf(perFrame.Max());
            return new
            {
                sum = Math.Round(perFrame.Sum(), 3),
                mean = Math.Round(perFrame.Average(), 3),
                median = Math.Round(ProfilerAnalysisMath.Percentile(sorted, 0.5), 3),
                max = Math.Round(sorted[sorted.Count - 1], 3),
                maxFrame = frames[maxIndex]
            };
        }

        /// <summary>The GPU hierarchy columns, which are internal constants of <see cref="HierarchyFrameDataView"/>. False when this Editor has none.</summary>
        internal static bool TryGetGpuColumns(out int total, out int self)
        {
            ResolveColumns();
            total = s_TotalGpuColumn;
            self = s_SelfGpuColumn;
            return total >= 0 && self >= 0;
        }

        /// <summary>
        /// The GPU counterpart of a CPU hot path: from the root, the heaviest child by GPU total that is not excluded,
        /// ending at the first step whose own GPU time is at least that of its heaviest child. Main thread only.
        /// </summary>
        internal static List<object> GpuHotPath(HierarchyFrameDataView view, int totalCol, int selfCol, ISet<string> excludedNames, out double firstTotalMs)
        {
            var steps = new List<object>();
            firstTotalMs = 0;
            var rootMs = view.GetItemColumnDataAsFloat(view.GetRootItemID(), totalCol);
            var parentMs = rootMs;
            var node = view.GetRootItemID();
            while (steps.Count < RawCallTree.MaxHotPathDepth)
            {
                var next = HeaviestChild(view, node, totalCol, excludedNames);
                if (next == HierarchyFrameDataView.invalidSampleId)
                    break;

                var totalMs = view.GetItemColumnDataAsFloat(next, totalCol);
                var selfMs = view.GetItemColumnDataAsFloat(next, selfCol);
                var below = HeaviestChild(view, next, totalCol, excludedNames);
                var leaf = below == HierarchyFrameDataView.invalidSampleId || selfMs >= view.GetItemColumnDataAsFloat(below, totalCol);
                var name = view.GetItemName(next);
                if (steps.Count == 0)
                    firstTotalMs = totalMs;

                steps.Add(MarkerDescriptions.Annotate(new
                {
                    sampleId = FirstRawIndex(view, next),
                    name,
                    instances = view.GetItemMergedSamplesCount(next),
                    totalMs = Math.Round(totalMs, 3),
                    selfMs = Math.Round(selfMs, 3),
                    percentOfThread = Percent(totalMs, rootMs),
                    percentOfParent = Percent(totalMs, parentMs),
                    hotLeaf = leaf
                }, name));

                if (leaf)
                    break;
                parentMs = totalMs;
                node = next;
            }

            return steps;
        }

        static double Percent(double part, double whole) => whole <= 0 ? 0 : Math.Round(100.0 * part / whole, 1);

        static int HeaviestChild(HierarchyFrameDataView view, int parent, int totalCol, ISet<string> excludedNames)
        {
            var children = new List<int>();
            view.GetItemChildren(parent, children);
            var best = HierarchyFrameDataView.invalidSampleId;
            var bestMs = 0f;
            foreach (var child in children)
            {
                var ms = view.GetItemColumnDataAsFloat(child, totalCol);
                if (ms <= 0 || excludedNames.Contains(view.GetItemName(child)))
                    continue;
                if (best == HierarchyFrameDataView.invalidSampleId || ms > bestMs)
                {
                    best = child;
                    bestMs = ms;
                }
            }

            return best;
        }

        /// <summary>The lowest raw sample index of a hierarchy item, which <see cref="CommandNames.DiagnosticsProfilerSample"/> accepts as a sampleId; -1 when it has none.</summary>
        static int FirstRawIndex(HierarchyFrameDataView view, int id, List<int> indices = null)
        {
            indices = indices ?? new List<int>();
            indices.Clear();
            view.GetItemRawFrameDataViewIndices(id, indices);
            return indices.Count > 0 ? indices.Min() : -1;
        }

        sealed class GpuGroup
        {
            public double SelfMs;
            public int Instances;
            public double HeaviestSelfMs = -1;
            public int HeaviestItem;
            public HierarchyFrameDataView HeaviestView;
            public string HeaviestThread;
        }

        /// <summary>
        /// The GPU side of <c>diagnostics profiler_frame</c>: hot paths through each GPU thread's call tree, and the GPU samples
        /// with the most self time grouped by marker. Serial and on the main thread, since GPU time per sample only
        /// exists in hierarchy views.
        /// </summary>
        internal static ExitCode FrameBreakdown(CommandContext cmd, ProfilerCaptureSession s, int frame, FrameTiming timing, List<ThreadInfo> threads,
            string thread, bool allThreads, int limit, ISet<string> excludeNames, string selectedBy, double target, int generation)
        {
            TryGetGpuColumns(out var totalCol, out var selfCol);
            cmd.Log.Info("GPU samples under one parent can overlap in time, so child times may add up to more than the parent's.");

            var threadIndices = new List<int>();
            if (allThreads)
                threadIndices.AddRange(threads.Select(t => t.Index));
            else
            {
                var index = s.ResolveThread(frame, thread, out var resolveError);
                if (index < 0)
                    return resolveError.Report(cmd);
                threadIndices.Add(index);
            }

            var hotPaths = new List<(double Total, object Entry)>();
            var groups = new Dictionary<string, GpuGroup>(StringComparer.Ordinal);
            var matchedExclusions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var index in threadIndices)
            {
                var view = s.GetHierarchy(frame, index, out var error);
                if (view == null)
                {
                    if (error != null)
                        return error.Report(cmd);
                    continue;
                }

                var root = view.GetRootItemID();
                var rootMs = view.GetItemColumnDataAsFloat(root, totalCol);
                if (rootMs <= 0)
                    continue;

                var threadName = threads[index].Name;
                var steps = GpuHotPath(view, totalCol, selfCol, excludeNames, out var firstTotalMs);
                if (steps.Count > 0)
                    hotPaths.Add((firstTotalMs, new { thread = threadName, threadMs = Math.Round(rootMs, 3), steps }));

                var pending = new Stack<int>();
                pending.Push(root);
                var children = new List<int>();
                while (pending.Count > 0)
                {
                    children.Clear();
                    view.GetItemChildren(pending.Pop(), children);
                    foreach (var child in children)
                    {
                        if (view.GetItemColumnDataAsFloat(child, totalCol) <= 0)
                            continue;
                        pending.Push(child);

                        var name = view.GetItemName(child);
                        if (excludeNames.Contains(name))
                        {
                            matchedExclusions.Add(name);
                            continue;
                        }
                        if (!groups.TryGetValue(name, out var group))
                        {
                            group = new GpuGroup();
                            groups.Add(name, group);
                        }

                        var self = view.GetItemColumnDataAsFloat(child, selfCol);
                        group.SelfMs += self;
                        group.Instances += view.GetItemMergedSamplesCount(child);
                        if (self > group.HeaviestSelfMs)
                        {
                            group.HeaviestSelfMs = self;
                            group.HeaviestItem = child;
                            group.HeaviestView = view;
                            group.HeaviestThread = threadName;
                        }
                    }
                }
            }

            if (hotPaths.Count == 0)
                cmd.Log.Warning(NoSamplesNote);

            var topSelf = groups
                .OrderByDescending(p => p.Value.SelfMs).ThenBy(p => p.Key, StringComparer.Ordinal)
                .Take(Math.Max(0, limit))
                .Select(p => MarkerDescriptions.Annotate(new
                {
                    name = p.Key,
                    instances = p.Value.Instances,
                    selfMs = Math.Round(p.Value.SelfMs, 3),
                    sampleId = FirstRawIndex(p.Value.HeaviestView, p.Value.HeaviestItem),
                    thread = p.Value.HeaviestThread,
                    heaviestSelfMs = Math.Round(p.Value.HeaviestSelfMs, 3),
                    path = ProfilerSampleCommands.HierarchyPath(p.Value.HeaviestView, p.Value.HeaviestItem)
                }, p.Key))
                .ToList();

            cmd.Result.Write(new
            {
                sessionId = s.Id,
                domain = "gpu",
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
                topGc = new List<object>(),
                excluded = matchedExclusions.OrderBy(n => n, StringComparer.Ordinal).ToList()
            });
            return ExitCode.Success;
        }

        static void ResolveColumns()
        {
            if (s_ColumnsResolved)
                return;
            s_ColumnsResolved = true;
            s_TotalGpuColumn = ReadColumn("columnTotalGpuTime");
            s_SelfGpuColumn = ReadColumn("columnSelfGpuTime");
        }

        static int ReadColumn(string name)
        {
            var field = typeof(HierarchyFrameDataView).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                return -1;

            try
            {
                var value = field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
                return value is int column ? column : -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
