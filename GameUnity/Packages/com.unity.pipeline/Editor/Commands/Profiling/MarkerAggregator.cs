using System;
using System.Collections.Generic;
using UnityEditor.Profiling;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>The raw samples of one thread of one frame, in preorder with child counts.</summary>
    interface IRawSampleSource
    {
        int SampleCount { get; }
        ulong GetSampleTimeNs(int index);
        int GetSampleChildrenCount(int index);
        int GetSampleMarkerId(int index);
        string GetMarkerName(int markerId);
        long GetGcAllocBytes(int index);
    }

    readonly struct RawFrameDataViewSource : IRawSampleSource
    {
        readonly RawFrameDataView m_View;

        public RawFrameDataViewSource(RawFrameDataView view) { m_View = view; }

        public int SampleCount => m_View.sampleCount;
        public ulong GetSampleTimeNs(int index) => m_View.GetSampleTimeNs(index);
        public int GetSampleChildrenCount(int index) => m_View.GetSampleChildrenCount(index);
        public int GetSampleMarkerId(int index) => m_View.GetSampleMarkerId(index);
        public string GetMarkerName(int markerId) => m_View.GetMarkerName(markerId);
        public long GetGcAllocBytes(int index) => m_View.GetSampleMetadataCount(index) > 0 ? m_View.GetSampleMetadataAsLong(index, 0) : 0;
    }

    /// <summary>A link to one sample, enough to drill into it with <c>profiler_sample</c>.</summary>
    struct SampleRef
    {
        public int Frame;
        public int Thread;
        public int Sample;
        public ulong Value;

        public bool IsSet => Sample > 0;

        /// <summary>True when <paramref name="candidate"/> should replace <paramref name="current"/>: larger value, then earlier frame, thread, sample.</summary>
        public static bool Heavier(in SampleRef candidate, in SampleRef current)
        {
            if (!candidate.IsSet)
                return false;
            if (!current.IsSet || candidate.Value != current.Value)
                return !current.IsSet || candidate.Value > current.Value;
            if (candidate.Frame != current.Frame)
                return candidate.Frame < current.Frame;
            if (candidate.Thread != current.Thread)
                return candidate.Thread < current.Thread;
            return candidate.Sample < current.Sample;
        }
    }

    /// <summary>One marker's cost on one thread of one frame.</summary>
    sealed class MarkerFrameStats
    {
        public long Calls;
        public ulong SelfNs;
        public ulong TotalNs;
        public ulong GcBytes;
        public SampleRef HeaviestSelf;
        public SampleRef HeaviestGc;
    }

    /// <summary>The markers of one thread of one frame.</summary>
    sealed class ThreadFrameStats
    {
        public readonly Dictionary<int, MarkerFrameStats> Markers = new Dictionary<int, MarkerFrameStats>();
    }

    /// <summary>One marker's cost over an analyzed range. Per-frame lists hold only the frames it appears in.</summary>
    sealed class MarkerStats
    {
        public long Calls;
        public ulong SelfNs;
        public ulong TotalNs;
        public ulong GcBytes;
        public int FramesPresent;
        public readonly List<ulong> FrameSelfNs = new List<ulong>();
        public readonly List<ulong> FrameTotalNs = new List<ulong>();
        public readonly List<ulong> FrameGcBytes = new List<ulong>();
        public readonly List<long> FrameCalls = new List<long>();
        public ulong MaxFrameTotalNs;
        public int MaxFrameTotalFrame = -1;
        public SampleRef HeaviestSelf;
        public SampleRef HeaviestGc;
    }

    /// <summary>
    /// Aggregates raw samples per marker id, ported from the Profiler window's top-markers builder:
    /// self time is duration minus direct children (clamped to 0), GC bytes come from direct
    /// <c>GC.Alloc</c> children, the hidden root sample is skipped, and a recursive marker's total
    /// counts only its outermost instance. <see cref="WalkThread{T}"/> is thread safe; <see cref="AddFrame"/> is not.
    /// </summary>
    sealed class MarkerAggregator
    {
        sealed class Entry
        {
            public int SampleIndex;
            public int MarkerId;
            public int RemainingChildren;
            public ulong DurationNs;
            public ulong ChildNs;
            public ulong ChildGc;
            public bool Outermost;
        }

        readonly int m_GcAllocMarkerId;
        readonly Dictionary<int, MarkerStats> m_Markers = new Dictionary<int, MarkerStats>();
        readonly Dictionary<int, MarkerFrameStats> m_Scratch = new Dictionary<int, MarkerFrameStats>();
        readonly HashSet<int> m_Seen = new HashSet<int>();

        public MarkerAggregator(int gcAllocMarkerId, int[] excludedMarkerIds)
        {
            m_GcAllocMarkerId = gcAllocMarkerId;
            ExcludedMarkerIds = new HashSet<int>(excludedMarkerIds ?? Array.Empty<int>());
        }

        public int FrameCount { get; private set; }

        public int GcAllocMarkerId => m_GcAllocMarkerId;

        /// <summary>Marker ids <see cref="WalkThread{T}"/> should leave out; read-only once walks start.</summary>
        public HashSet<int> ExcludedMarkerIds { get; }

        public IReadOnlyDictionary<int, MarkerStats> Markers => m_Markers;

        /// <summary>Marker ids seen since the caller last removed them, so it can name them from the view they came from.</summary>
        public List<int> NewMarkerIds { get; } = new List<int>();

        /// <summary>Aggregates one thread of one frame. Keeps all state local, so it may run on any thread.</summary>
        public static ThreadFrameStats WalkThread<T>(T source, int frame, int thread, int gcAllocMarkerId, HashSet<int> excluded) where T : IRawSampleSource
        {
            var result = new ThreadFrameStats();
            var stack = new List<Entry>();
            var pool = new Stack<Entry>();
            var onStack = new Dictionary<int, int>();

            for (var i = 0; i < source.SampleCount; i++)
            {
                // Child counts, not timings, define the hierarchy: durations are lossy near boundaries.
                var duration = source.GetSampleTimeNs(i);
                var childCount = source.GetSampleChildrenCount(i);
                var markerId = source.GetSampleMarkerId(i);

                while (stack.Count > 0 && stack[stack.Count - 1].RemainingChildren == 0)
                    PopAndProcess(stack, pool, onStack, result, frame, thread, excluded);

                if (stack.Count > 0)
                {
                    var parent = stack[stack.Count - 1];
                    parent.ChildNs += duration;
                    if (gcAllocMarkerId >= 0 && markerId == gcAllocMarkerId)
                    {
                        var bytes = source.GetGcAllocBytes(i);
                        if (bytes > 0)
                            parent.ChildGc += (ulong)bytes;
                    }
                    parent.RemainingChildren--;
                }

                var entry = pool.Count > 0 ? pool.Pop() : new Entry();
                entry.SampleIndex = i;
                entry.MarkerId = markerId;
                entry.RemainingChildren = childCount;
                entry.DurationNs = duration;
                entry.ChildNs = 0;
                entry.ChildGc = 0;
                onStack.TryGetValue(markerId, out var count);
                entry.Outermost = count == 0;
                onStack[markerId] = count + 1;
                stack.Add(entry);
            }

            while (stack.Count > 0)
                PopAndProcess(stack, pool, onStack, result, frame, thread, excluded);

            return result;
        }

        static void PopAndProcess(List<Entry> stack, Stack<Entry> pool, Dictionary<int, int> onStack,
            ThreadFrameStats result, int frame, int thread, HashSet<int> excluded)
        {
            var entry = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            onStack[entry.MarkerId]--;

            // Index 0 is the thread's hidden root sample.
            if (entry.SampleIndex != 0 && (excluded == null || !excluded.Contains(entry.MarkerId)))
            {
                if (!result.Markers.TryGetValue(entry.MarkerId, out var stats))
                {
                    stats = new MarkerFrameStats();
                    result.Markers.Add(entry.MarkerId, stats);
                }

                var self = entry.ChildNs > entry.DurationNs ? 0UL : entry.DurationNs - entry.ChildNs;
                stats.Calls++;
                stats.SelfNs += self;
                if (entry.Outermost)
                    stats.TotalNs += entry.DurationNs;
                stats.GcBytes += entry.ChildGc;

                var selfRef = new SampleRef { Frame = frame, Thread = thread, Sample = entry.SampleIndex, Value = self };
                if (SampleRef.Heavier(selfRef, stats.HeaviestSelf))
                    stats.HeaviestSelf = selfRef;
                if (entry.ChildGc > 0)
                {
                    var gcRef = new SampleRef { Frame = frame, Thread = thread, Sample = entry.SampleIndex, Value = entry.ChildGc };
                    if (SampleRef.Heavier(gcRef, stats.HeaviestGc))
                        stats.HeaviestGc = gcRef;
                }
            }

            pool.Push(entry);
        }

        /// <summary>Folds the threads of one frame into the range totals. Call in frame order: a tie for a marker's slowest frame goes to the earlier frame.</summary>
        public void AddFrame(int frame, IReadOnlyList<ThreadFrameStats> threads)
        {
            FrameCount++;
            m_Scratch.Clear();
            foreach (var thread in threads)
            {
                foreach (var pair in thread.Markers)
                {
                    if (!m_Scratch.TryGetValue(pair.Key, out var sum))
                    {
                        sum = new MarkerFrameStats();
                        m_Scratch.Add(pair.Key, sum);
                    }

                    var s = pair.Value;
                    sum.Calls += s.Calls;
                    sum.SelfNs += s.SelfNs;
                    sum.TotalNs += s.TotalNs;
                    sum.GcBytes += s.GcBytes;
                    if (SampleRef.Heavier(s.HeaviestSelf, sum.HeaviestSelf))
                        sum.HeaviestSelf = s.HeaviestSelf;
                    if (SampleRef.Heavier(s.HeaviestGc, sum.HeaviestGc))
                        sum.HeaviestGc = s.HeaviestGc;
                }
            }

            foreach (var pair in m_Scratch)
            {
                if (!m_Markers.TryGetValue(pair.Key, out var stats))
                {
                    stats = new MarkerStats();
                    m_Markers.Add(pair.Key, stats);
                }

                if (m_Seen.Add(pair.Key))
                    NewMarkerIds.Add(pair.Key);

                var s = pair.Value;
                stats.Calls += s.Calls;
                stats.SelfNs += s.SelfNs;
                stats.TotalNs += s.TotalNs;
                stats.GcBytes += s.GcBytes;
                stats.FramesPresent++;
                stats.FrameSelfNs.Add(s.SelfNs);
                stats.FrameTotalNs.Add(s.TotalNs);
                stats.FrameGcBytes.Add(s.GcBytes);
                stats.FrameCalls.Add(s.Calls);
                if (s.TotalNs > stats.MaxFrameTotalNs || stats.MaxFrameTotalFrame < 0)
                {
                    stats.MaxFrameTotalNs = s.TotalNs;
                    stats.MaxFrameTotalFrame = frame;
                }

                if (SampleRef.Heavier(s.HeaviestSelf, stats.HeaviestSelf))
                    stats.HeaviestSelf = s.HeaviestSelf;
                if (SampleRef.Heavier(s.HeaviestGc, stats.HeaviestGc))
                    stats.HeaviestGc = s.HeaviestGc;
            }
        }
    }
}
