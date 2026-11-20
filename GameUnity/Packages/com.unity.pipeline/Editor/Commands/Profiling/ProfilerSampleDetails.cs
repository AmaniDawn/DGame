using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>The optional parts of a <c>diagnostics profiler_sample</c> response: GC callstacks and overlapping samples on another thread.</summary>
    static class ProfilerSampleDetails
    {
        const int MaxAllocations = 20;
        const int MaxRelatedDepth = 2;
        const double NsToMs = 1e-6;
        const string GcAllocName = "GC.Alloc";
        const string NoCallstacksNote = "No callstacks recorded. Enable Call Stacks > GC.Alloc in the Profiler window (ProfilerDriver.memoryRecordMode) and record again.";

        /// <summary>
        /// Adds <c>allocations</c>, <c>allocationsTotal</c> and <c>allocationsBytes</c> for the GC.Alloc samples directly
        /// under <paramref name="nodeId"/> (or the node itself when it is a GC.Alloc).
        /// </summary>
        internal static void AddCallstacks(JObject response, HierarchyFrameDataView view, int nodeId)
        {
            var allocId = view.GetItemName(nodeId) == GcAllocName ? nodeId : FindChild(view, nodeId, GcAllocName);
            if (allocId == HierarchyFrameDataView.invalidSampleId)
            {
                response["allocations"] = new JArray();
                response["allocationsTotal"] = 0;
                response["allocationsBytes"] = 0;
                response["callstackNote"] = $"No {GcAllocName} under this marker in this frame.";
                return;
            }

            var bytes = new List<double>();
            view.GetItemMergedSamplesColumnDataAsDoubles(allocId, HierarchyFrameDataView.columnGcMemory, bytes);

            var allocations = new JArray();
            var anyCallstack = false;
            for (var i = 0; i < bytes.Count && i < MaxAllocations; i++)
            {
                var callstack = view.ResolveItemMergedSampleCallstack(allocId, i);
                if (string.IsNullOrEmpty(callstack))
                    callstack = null;
                else
                    anyCallstack = true;

                allocations.Add(new JObject { ["bytes"] = (long)bytes[i], ["callstack"] = callstack });
            }

            // Callstacks past the first MaxAllocations are not resolved, so judge only what was listed.
            response["allocations"] = allocations;
            response["allocationsTotal"] = bytes.Count;
            response["allocationsBytes"] = (long)bytes.Sum();
            if (bytes.Count > 0 && !anyCallstack)
                response["callstackNote"] = NoCallstacksNote;
        }

        /// <summary>
        /// The samples at depth 1-2 of <paramref name="relatedThreadIndex"/> that overlap in time with the selected
        /// node's instances, most overlap first.
        /// </summary>
        internal static object Related(HierarchyFrameDataView view, int nodeId, int frame, int threadIndex,
            int relatedThreadIndex, string relatedThreadName, int limit)
        {
            var indices = new List<int>();
            view.GetItemRawFrameDataViewIndices(nodeId, indices);

            var instances = new List<(ulong start, ulong duration)>();
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, threadIndex))
            {
                if (raw == null || !raw.valid)
                    return new { thread = relatedThreadName, samples = new List<object>() };

                foreach (var index in indices)
                    instances.Add((raw.GetSampleStartTimeNs(index), raw.GetSampleTimeNs(index)));
            }

            instances = MergeIntervals(instances);

            var samples = new List<(string name, int depth, ulong start, ulong duration, ulong overlap)>();
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, relatedThreadIndex))
            {
                if (raw == null || !raw.valid)
                    return new { thread = relatedThreadName, samples = new List<object>() };

                var remaining = new List<int>();
                for (var i = 0; i < raw.sampleCount; i++)
                {
                    while (remaining.Count > 0 && remaining[remaining.Count - 1] == 0)
                        remaining.RemoveAt(remaining.Count - 1);

                    var depth = remaining.Count;
                    if (remaining.Count > 0)
                        remaining[remaining.Count - 1]--;
                    remaining.Add(raw.GetSampleChildrenCount(i));

                    if (depth < 1 || depth > MaxRelatedDepth)
                        continue;

                    var start = raw.GetSampleStartTimeNs(i);
                    var duration = raw.GetSampleTimeNs(i);
                    var overlap = TotalOverlapNs(instances, start, duration);

                    if (overlap > 0)
                        samples.Add((raw.GetSampleName(i), depth, start, duration, overlap));
                }
            }

            var origin = instances.Count == 0 ? 0UL : instances.Min(i => i.start);
            return new
            {
                thread = relatedThreadName,
                samples = samples
                    .OrderByDescending(s => s.overlap)
                    .Take(Math.Max(0, limit))
                    .Select(s => MarkerDescriptions.Annotate(new
                    {
                        name = s.name,
                        depth = s.depth,
                        startOffsetMs = Math.Round(((double)s.start - origin) * NsToMs, 3),
                        durationMs = Math.Round(s.duration * NsToMs, 3),
                        overlapMs = Math.Round(s.overlap * NsToMs, 3)
                    }, s.name))
                    .ToList()
            };
        }

        static List<(ulong start, ulong duration)> MergeIntervals(List<(ulong start, ulong duration)> intervals)
        {
            var merged = new List<(ulong start, ulong duration)>();
            foreach (var interval in intervals.OrderBy(i => i.start))
            {
                if (merged.Count > 0)
                {
                    var last = merged[merged.Count - 1];
                    var lastEnd = last.start + last.duration;
                    if (interval.start <= lastEnd)
                    {
                        var end = Math.Max(lastEnd, interval.start + interval.duration);
                        merged[merged.Count - 1] = (last.start, end - last.start);
                        continue;
                    }
                }

                merged.Add(interval);
            }

            return merged;
        }

        /// <summary>The overlap of one interval with sorted, non-overlapping intervals, in ns.</summary>
        internal static ulong TotalOverlapNs(List<(ulong start, ulong duration)> merged, ulong start, ulong duration)
        {
            var end = start + duration;
            var lo = 0;
            var hi = merged.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (merged[mid].start + merged[mid].duration <= start)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            ulong total = 0;
            for (var i = lo; i < merged.Count && merged[i].start < end; i++)
                total += OverlapNs(merged[i].start, merged[i].duration, start, duration);
            return total;
        }

        /// <summary>How long [aStart, aStart + aDuration) and [bStart, bStart + bDuration) overlap, in ns.</summary>
        internal static ulong OverlapNs(ulong aStart, ulong aDuration, ulong bStart, ulong bDuration)
        {
            var start = Math.Max(aStart, bStart);
            var end = Math.Min(aStart + aDuration, bStart + bDuration);
            return end > start ? end - start : 0;
        }

        static int FindChild(HierarchyFrameDataView view, int parentId, string name)
        {
            var children = new List<int>();
            view.GetItemChildren(parentId, children);
            foreach (var child in children)
            {
                if (view.GetItemName(child) == name)
                    return child;
            }

            return HierarchyFrameDataView.invalidSampleId;
        }
    }
}
