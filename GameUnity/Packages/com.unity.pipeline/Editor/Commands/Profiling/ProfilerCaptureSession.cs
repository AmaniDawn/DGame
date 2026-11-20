using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using Unity.Profiling.LowLevel;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    readonly struct ThreadInfo
    {
        public readonly int Index;
        public readonly string Name;
        public readonly string Group;

        public ThreadInfo(int index, string name, string group)
        {
            Index = index;
            Name = name;
            Group = group;
        }
    }

    /// <summary>Frame timings of a range; <see cref="Error"/> is set when reading them failed.</summary>
    sealed class TimingSeries
    {
        public List<FrameTiming> Timings;
        public int Skipped;
        public CaptureError Error;
    }

    /// <summary>Why a capture session could not serve a request: the message to log and the exit code to end with.</summary>
    sealed class CaptureError
    {
        internal CaptureError(string message, ExitCode code)
        {
            Message = message;
            Code = code;
        }

        internal string Message { get; }

        internal ExitCode Code { get; }

        /// <summary>Logs the error to <paramref name="cmd"/> and returns its exit code.</summary>
        internal ExitCode Report(CommandContext cmd)
        {
            cmd.Log.Error(Message);
            return Code;
        }
    }

    /// <summary>Markers aggregated over a frame range, with the id to name and category tables of the capture.</summary>
    sealed class MarkerAggregate
    {
        public MarkerAggregator Aggregator;
        public readonly Dictionary<int, string> Names = new Dictionary<int, string>();
        public readonly Dictionary<int, string> Categories = new Dictionary<int, string>();
        public int FirstFrame;
        public int LastFrame;
        public int SkippedFrames;
    }

    /// <summary>
    /// One saved Profiler capture. Only one capture at a time can be in the Editor's Profiler
    /// (<see cref="ProfilerCaptureSessions"/> decides which), so native views are only held while this
    /// session is resident. Managed results (threads, timings, aggregates) survive eviction.
    /// </summary>
    sealed class ProfilerCaptureSession : IDisposable
    {
        const string MainActiveCounter = "CPU Main Thread Active Time";
        const string RenderActiveCounter = "CPU Render Thread Active Time";
        const string GcAllocCounter = "GC Allocated In Frame";
        const string GpuFrameCounter = "GPU Frame Time";
        const int GpuFrameLatency = 4;
        const double NsToMs = 1e-6;

        /// <summary>Markers that are waiting or bookkeeping rather than work; always left out of aggregates.</summary>
        internal static readonly string[] DefaultExcludedMarkers = { "Semaphore.WaitForSignal", "Idle", "GC.Alloc" };

        readonly Action<int, int> m_OnFrameRecorded;
        readonly Action m_OnHistoryChanged;
        readonly Dictionary<(int frame, int thread), HierarchyFrameDataView> m_Hierarchies = new Dictionary<(int, int), HierarchyFrameDataView>();
        readonly Dictionary<int, List<ThreadInfo>> m_Threads = new Dictionary<int, List<ThreadInfo>>();
        readonly Dictionary<int, FrameTiming?> m_Timings = new Dictionary<int, FrameTiming?>();
        readonly Dictionary<(int, int, string), MarkerAggregate> m_Aggregates = new Dictionary<(int, int, string), MarkerAggregate>();
        bool m_Subscribed;
        volatile bool m_Stale;
        bool? m_IsEditorCapture;
        List<CounterInfo> m_Counters;
        DateTime m_WriteTimeUtc;

        internal ProfilerCaptureSession(string path)
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Path = path;
            m_OnHistoryChanged = OnHistoryChanged;
            m_OnFrameRecorded = (a, b) => OnHistoryChanged();
        }

        internal string Id { get; }
        internal string Path { get; }

        /// <summary>Incremented on every load, so ids from an earlier load can be told apart.</summary>
        internal int LoadGeneration { get; private set; }
        internal int First { get; private set; }
        internal int Last { get; private set; }
        internal bool IsClosed { get; private set; }
        internal int FrameCount => Last - First + 1;
        internal DateTime WriteTimeUtc => m_WriteTimeUtc;

        /// <summary>The Profiler's history changed since this session loaded; its native views must not be used.</summary>
        internal bool IsStale => m_Stale;

        /// <summary>Loads this capture into the Profiler. Called only by <see cref="ProfilerCaptureSessions"/>.</summary>
        internal bool Load(out CaptureError error)
        {
            error = null;
            if (!File.Exists(Path))
            {
                error = Error($"Capture file not found: '{Path}'.", ExitCode.NotFound);
                return false;
            }

            if (!ProfilerDriver.LoadProfile(Path, false))
            {
                error = Error($"ProfilerDriver.LoadProfile failed for '{Path}'. Is it a valid .data capture?");
                return false;
            }

            if (ProfilerDriver.lastFrameIndex < 0)
            {
                error = Error($"Capture '{Path}' contains no frames.");
                return false;
            }

            var writeTime = File.GetLastWriteTimeUtc(Path);
            if (LoadGeneration > 0 && writeTime != m_WriteTimeUtc)
                ClearManagedCaches();

            m_WriteTimeUtc = writeTime;
            First = ProfilerDriver.firstFrameIndex;
            Last = ProfilerDriver.lastFrameIndex;
            LoadGeneration++;

            // Subscribe after loading, so this session's own load does not invalidate it.
            m_Stale = false;
            Unsubscribe();
            ProfilerDriver.profileLoaded += m_OnHistoryChanged;
            ProfilerDriver.profileCleared += m_OnHistoryChanged;
            ProfilerDriver.NewProfilerFrameRecorded += m_OnFrameRecorded;
            m_Subscribed = true;
            return true;
        }

        /// <summary>
        /// Profiler events can arrive on any thread, and views must be disposed on the main thread, so this only
        /// flags the session. <see cref="ProfilerCaptureSessions.MakeResident(ProfilerCaptureSession, out CaptureError)"/> releases it.
        /// </summary>
        void OnHistoryChanged()
        {
            m_Stale = true;
        }

        /// <summary>Releases native views and event subscriptions; keeps managed caches.</summary>
        internal void Evict()
        {
            Unsubscribe();
            foreach (var view in m_Hierarchies.Values)
                view?.Dispose();
            m_Hierarchies.Clear();
        }

        void Unsubscribe()
        {
            if (!m_Subscribed)
                return;
            ProfilerDriver.profileLoaded -= m_OnHistoryChanged;
            ProfilerDriver.profileCleared -= m_OnHistoryChanged;
            ProfilerDriver.NewProfilerFrameRecorded -= m_OnFrameRecorded;
            m_Subscribed = false;
        }

        void ClearManagedCaches()
        {
            m_Threads.Clear();
            m_Timings.Clear();
            m_Aggregates.Clear();
            m_IsEditorCapture = null;
            m_Counters = null;
        }

        public void Dispose()
        {
            Evict();
            ClearManagedCaches();
            IsClosed = true;
        }

        internal static CaptureError Error(string message, ExitCode code = ExitCode.Failed) => new CaptureError(message, code);

        /// <summary>Resolves an analysis frame range against this capture; the capture is loaded first so the range is current.</summary>
        internal bool TryResolveRange(int requestedFirst, int requestedLast, out int first, out int last, out string note, out CaptureError error)
        {
            first = last = 0;
            note = null;
            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return false;

            if (!ProfilerAnalysisMath.TryResolveRange(requestedFirst, requestedLast, First, Last, out first, out last, out note, out var message))
            {
                error = Error(message, ExitCode.NotFound);
                return false;
            }

            return true;
        }

        /// <summary>
        /// The cached hierarchy view for a frame and thread, with same-named siblings merged as in the Profiler's
        /// Hierarchy view. Null when there is no data.
        /// </summary>
        internal HierarchyFrameDataView GetHierarchy(int frame, int thread, out CaptureError error)
        {
            error = null;
            if (!m_Stale && m_Hierarchies.TryGetValue((frame, thread), out var cached))
                return cached;

            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return null;

            // The load may have invalidated the cache.
            if (m_Hierarchies.TryGetValue((frame, thread), out cached))
                return cached;

            var view = ProfilerDriver.GetHierarchyFrameDataView(
                frame, thread, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false);
            if (view == null || !view.valid)
            {
                view?.Dispose();
                return null;
            }

            m_Hierarchies[(frame, thread)] = view;
            return view;
        }

        /// <summary>Thread indices are dense, so the first invalid view marks the end.</summary>
        internal List<ThreadInfo> Threads(int frame, out CaptureError error)
        {
            error = null;
            if (m_Threads.TryGetValue(frame, out var cached))
                return cached;

            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return null;

            var list = new List<ThreadInfo>();
            for (var i = 0; ; i++)
            {
                using (var raw = ProfilerDriver.GetRawFrameDataView(frame, i))
                {
                    if (raw == null || !raw.valid)
                        break;
                    list.Add(new ThreadInfo(i, raw.threadName, raw.threadGroupName));
                }
            }

            m_Threads[frame] = list;
            return list;
        }

        /// <summary>An empty name resolves to the first thread; <c>"all"</c> is the caller's to handle. Returns -1 with <paramref name="error"/> set on failure.</summary>
        internal int ResolveThread(int frame, string name, out CaptureError error)
        {
            var threads = Threads(frame, out error);
            if (threads == null)
                return -1;

            if (string.IsNullOrEmpty(name))
            {
                if (threads.Count > 0)
                    return 0;
            }
            else
            {
                foreach (var t in threads)
                {
                    if (string.Equals(t.Name, name, StringComparison.Ordinal))
                        return t.Index;
                }
            }

            var names = new List<string>();
            foreach (var t in threads)
                names.Add(t.Name);
            error = Error($"Thread '{name}' not found in frame {frame}. Available threads: {string.Join(", ", names)}.", ExitCode.NotFound);
            return -1;
        }

        /// <summary>
        /// Timings of one frame. False with a null <paramref name="error"/> means the frame has no data
        /// and the caller should skip it.
        /// </summary>
        internal bool TryGetTiming(int frame, out FrameTiming timing, out CaptureError error)
        {
            error = null;
            timing = default;
            if (m_Timings.TryGetValue(frame, out var cached))
            {
                if (!cached.HasValue)
                    return false;
                timing = cached.Value;
                return true;
            }

            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return false;

            FrameTiming? result = null;
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                if (raw != null && raw.valid)
                {
                    double? gpu = null;
                    if (frame + GpuFrameLatency <= Last)
                    {
                        using (var gpuRaw = ProfilerDriver.GetRawFrameDataView(frame + GpuFrameLatency, 0))
                        {
                            var ns = ReadCounter(gpuRaw, GpuFrameCounter);
                            if (ns.HasValue)
                                gpu = ns.Value * NsToMs;
                        }
                    }

                    var main = ReadCounter(raw, MainActiveCounter);
                    var render = ReadCounter(raw, RenderActiveCounter);
                    result = new FrameTiming(frame, raw.frameTimeMs,
                        main.HasValue ? main.Value * NsToMs : (double?)null,
                        render.HasValue ? render.Value * NsToMs : (double?)null,
                        gpu, ReadCounter(raw, GcAllocCounter));
                }
            }

            m_Timings[frame] = result;
            if (!result.HasValue)
                return false;
            timing = result.Value;
            return true;
        }

        /// <summary>The counters this capture recorded, by category then name. Read from the last frame, which knows every marker.</summary>
        internal List<CounterInfo> Counters(out CaptureError error)
        {
            error = null;
            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return null;

            if (m_Counters != null)
                return m_Counters;

            var list = new List<CounterInfo>();
            using (var raw = ProfilerDriver.GetRawFrameDataView(Last, 0))
            {
                if (raw == null || !raw.valid)
                {
                    error = Error($"No data in frame {Last}.");
                    return null;
                }

                var markers = new List<FrameDataView.MarkerInfo>();
                raw.GetMarkers(markers);
                foreach (var info in markers)
                {
                    if ((info.flags & MarkerFlags.Counter) == 0)
                        continue;

                    var unit = info.metadataInfo != null && info.metadataInfo.Length > 0 ? info.metadataInfo[0].unit.ToString() : "Undefined";
                    // These two are recorded in nanoseconds but carry no unit metadata.
                    if (unit == "Undefined" && (info.name == MainActiveCounter || info.name == RenderActiveCounter))
                        unit = "TimeNanoseconds";
                    list.Add(new CounterInfo(info.id, info.name, raw.GetCategoryInfo(info.category).name, unit));
                }
            }

            list.Sort((a, b) =>
            {
                var byCategory = string.CompareOrdinal(a.Category, b.Category);
                return byCategory != 0 ? byCategory : string.CompareOrdinal(a.Name, b.Name);
            });
            m_Counters = list;
            return list;
        }

        /// <summary>A counter's value on a frame's main thread, clamped to 0. Null when the capture has no such counter.</summary>
        internal static long? ReadCounter(RawFrameDataView raw, string name)
        {
            if (raw == null || !raw.valid)
                return null;
            var id = raw.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return null;
            return Math.Max(0, raw.GetCounterValueAsLong(id));
        }

        /// <summary>True when the capture was taken of the Editor: its frames know the EditorLoop marker.</summary>
        internal bool IsEditorCapture(out CaptureError error)
        {
            error = null;
            if (m_IsEditorCapture.HasValue)
                return m_IsEditorCapture.Value;

            if (!ProfilerCaptureSessions.MakeResident(this, out error))
                return false;

            // The last frame knows every marker the capture registered.
            using (var raw = ProfilerDriver.GetRawFrameDataView(Last, 0))
            {
                if (raw == null || !raw.valid)
                {
                    error = Error($"No data in frame {Last}.");
                    return false;
                }

                m_IsEditorCapture = raw.GetMarkerId("EditorLoop") != FrameDataView.invalidMarkerId;
            }

            return m_IsEditorCapture.Value;
        }

        /// <summary>
        /// Timings of <paramref name="first"/>..<paramref name="last"/>, read in slices (see <see cref="AnalysisSlicer"/>).
        /// Frames without data are counted in <see cref="TimingSeries.Skipped"/>.
        /// </summary>
        internal async Task<TimingSeries> TimingsAsync(int first, int last, AnalysisSlicer slicer)
        {
            var series = new TimingSeries { Timings = new List<FrameTiming>() };
            for (var frame = first; frame <= last; frame++)
            {
                await slicer.CheckpointAsync();
                if (TryGetTiming(frame, out var timing, out var error))
                    series.Timings.Add(timing);
                else if (error != null)
                    return new TimingSeries { Error = error };
                else
                    series.Skipped++;
            }

            return series;
        }

        /// <summary>
        /// Markers aggregated over <paramref name="first"/>..<paramref name="last"/> on one thread by name, or on
        /// every thread for <c>"all"</c>. Cached, so repeat calls and comparisons cost nothing. Runs in slices (see
        /// <see cref="AnalysisSlicer"/>) and caches only a run that completed.
        /// </summary>
        internal async Task<(MarkerAggregate Aggregate, CaptureError Error)> AggregateAsync(int first, int last, string thread, AnalysisSlicer slicer)
        {
            var key = (first, last, thread ?? "");
            if (m_Aggregates.TryGetValue(key, out var cached))
                return (cached, null);

            if (!ProfilerCaptureSessions.MakeResident(this, out var error))
                return (null, error);

            var all = string.Equals(thread, "all", StringComparison.OrdinalIgnoreCase);
            var singleThreadIndex = 0;
            if (!all)
            {
                singleThreadIndex = ResolveThread(first, thread, out error);
                if (singleThreadIndex < 0)
                    return (null, error);
            }

            int gcAllocId;
            int[] excluded;
            using (var raw = ProfilerDriver.GetRawFrameDataView(first, 0))
            {
                if (raw == null || !raw.valid)
                    return (null, Error($"No data in frame {first}."));

                gcAllocId = raw.GetMarkerId("GC.Alloc");
                excluded = new int[DefaultExcludedMarkers.Length];
                for (var i = 0; i < excluded.Length; i++)
                    excluded[i] = raw.GetMarkerId(DefaultExcludedMarkers[i]);
            }

            var result = new MarkerAggregate
            {
                Aggregator = new MarkerAggregator(gcAllocId == FrameDataView.invalidMarkerId ? -1 : gcAllocId, excluded),
                FirstFrame = first,
                LastFrame = last
            };

            // Views are created and disposed on the main thread; samples are aggregated on workers and folded back in frame order.
            const int chunkFrames = 16;
            var gcId = result.Aggregator.GcAllocMarkerId;
            var excludedIds = result.Aggregator.ExcludedMarkerIds;
            string threadName = null;
            for (var chunkStart = first; chunkStart <= last; chunkStart += chunkFrames)
            {
                var count = Math.Min(chunkFrames, last - chunkStart + 1);
                var views = new List<(int Thread, RawFrameDataView View)>[count];
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        views[i] = CreateViews(chunkStart + i, all, singleThreadIndex, ref threadName);
                        await slicer.CheckpointAsync();
                    }

                    var items = new List<(int Frame, int Slot, int Thread, RawFrameDataView View)>();
                    var stats = new ThreadFrameStats[count][];
                    for (var i = 0; i < count; i++)
                    {
                        stats[i] = new ThreadFrameStats[views[i].Count];
                        for (var j = 0; j < views[i].Count; j++)
                            items.Add((i, j, views[i][j].Thread, views[i][j].View));
                    }

                    var start = chunkStart;
                    await slicer.OffMainThreadAsync(() =>
                    {
                        Parallel.For(0, items.Count, n =>
                        {
                            var item = items[n];
                            stats[item.Frame][item.Slot] = MarkerAggregator.WalkThread(
                                new RawFrameDataViewSource(item.View), start + item.Frame, item.Thread, gcId, excludedIds);
                        });
                        return true;
                    });

                    RawFrameDataView naming = null;
                    for (var i = 0; i < count; i++)
                    {
                        result.Aggregator.AddFrame(chunkStart + i, stats[i]);
                        if (views[i].Count == 0)
                            result.SkippedFrames++;
                        else
                            naming = views[i][0].View;
                    }

                    // Names come from a view, on this thread.
                    if (naming != null)
                        Resolve(result, naming);
                }
                finally
                {
                    foreach (var list in views)
                    {
                        if (list == null)
                            continue;
                        foreach (var entry in list)
                            entry.View.Dispose();
                    }
                }
            }

            m_Aggregates[key] = result;
            return (result, null);
        }

        /// <summary>The raw views to aggregate for one frame: every thread, or the named one, with their thread indices. Main thread only.</summary>
        static List<(int Thread, RawFrameDataView View)> CreateViews(int frame, bool all, int threadIndex, ref string threadName)
        {
            var views = new List<(int, RawFrameDataView)>();
            if (all)
            {
                for (var t = 0; ; t++)
                {
                    var raw = ProfilerDriver.GetRawFrameDataView(frame, t);
                    if (raw == null || !raw.valid)
                    {
                        raw?.Dispose();
                        break;
                    }

                    views.Add((t, raw));
                }

                return views;
            }

            var single = ProfilerDriver.GetRawFrameDataView(frame, threadIndex);
            if (single != null && single.valid)
            {
                threadName ??= single.threadName;
                if (single.threadName == threadName)
                {
                    views.Add((threadIndex, single));
                    return views;
                }
            }

            single?.Dispose();
            if (threadName == null)
                return views;

            // Thread indices can shift between frames; find it by name.
            var (byNameIndex, byName) = FindThread(frame, threadName);
            if (byName != null)
                views.Add((byNameIndex, byName));
            return views;
        }

        static (int Index, RawFrameDataView View) FindThread(int frame, string name)
        {
            for (var t = 0; ; t++)
            {
                var raw = ProfilerDriver.GetRawFrameDataView(frame, t);
                if (raw == null || !raw.valid)
                {
                    raw?.Dispose();
                    return (-1, null);
                }

                if (raw.threadName == name)
                    return (t, raw);
                raw.Dispose();
            }
        }

        /// <summary>Records the name and category of every marker id the aggregator has seen but not yet named.</summary>
        static void Resolve(MarkerAggregate aggregate, RawFrameDataView raw)
        {
            var pending = aggregate.Aggregator.NewMarkerIds;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var id = pending[i];
                var name = raw.GetMarkerName(id);
                if (string.IsNullOrEmpty(name))
                    continue;
                aggregate.Names[id] = name;
                aggregate.Categories[id] = raw.GetCategoryInfo(raw.GetMarkerCategoryIndex(id)).name;
                pending.RemoveAt(i);
            }
        }
    }
}
