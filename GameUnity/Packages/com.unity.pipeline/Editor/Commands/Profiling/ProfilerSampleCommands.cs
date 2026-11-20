using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>
    /// Reads one sample (a node of the merged call tree) out of a capture session. A sampleId is a raw sample index
    /// within one frame and thread, so one from <see cref="CommandNames.DiagnosticsProfilerFrame"/>, a marker ranking or
    /// an earlier read can be passed back in, even after the capture was reloaded.
    /// </summary>
    static class ProfilerSampleCommands
    {
        const string Group = "diagnostics";

        [CliGroupCommand(Group, "Read one sample of a capture session (" + CommandNames.DiagnosticsProfilerSession + "), found by marker name path or sampleId: its total/self ms, calls, GC alloc and share of the thread, and its children with their share of it, with marker descriptions, for one frame and thread.", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode ProfilerSample(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ".", Required = true)] string session = "",
            [CliArg("frame", "Frame index to read. Defaults to the last frame. An out-of-range value fails, and the error gives the available frame range.")] int frame = -1,
            [CliArg("thread", "Thread name to read (e.g. 'Main Thread'). Defaults to the first (main) thread; an unknown name fails, and the error lists the available thread names.")] string thread = "",
            [CliArg("marker", "Marker name path to drill into, '/'-separated (e.g. 'PlayerLoop/Update.ScriptRunBehaviourUpdate'). Defaults to the thread root, returning the top-level markers.")] string marker = "",
            [CliArg("max-children", "Maximum child markers to return under the selected node, sorted by total time descending. Default 25.")] int maxChildren = 25,
            [CliArg("sample-id", "A sampleId (raw sample index) from " + CommandNames.DiagnosticsProfilerFrame + ", " + CommandNames.DiagnosticsProfilerMarkers + " or an earlier " + CommandNames.DiagnosticsProfilerSample + ", for the same frame and thread. Overrides marker.")] int sampleId = -1,
            [CliArg("callstacks", "Also return the GC allocations (bytes and callstack) made directly under the selected marker. Needs a capture recorded with GC.Alloc call stacks enabled.")] bool callstacks = false,
            [CliArg("related-thread", "Also return the samples on this thread that overlap in time with the selected marker's instances. Needs a marker or sample-id.")] string relatedThread = "")
        {
            var s = ProfilerCaptureSessions.Require(session, out var error);
            if (s == null)
                return error.Report(cmd);

            var generation = s.LoadGeneration;
            if (!ProfilerCaptureSessions.MakeResident(s, out error))
                return error.Report(cmd);

            var frameIndex = frame < 0 ? s.Last : frame;
            if (frameIndex < s.First || frameIndex > s.Last)
            {
                cmd.Log.Error($"Frame {frame} is out of range: the capture has frames {s.First}..{s.Last}.");
                return ExitCode.NotFound;
            }

            var threads = s.Threads(frameIndex, out error);
            if (threads == null)
                return error.Report(cmd);

            var threadNames = threads.Select(t => t.Name).ToList();
            var threadIndex = string.IsNullOrEmpty(thread) ? (threadNames.Count > 0 ? 0 : -1) : threadNames.IndexOf(thread);
            if (threadIndex < 0)
            {
                cmd.Log.Error($"Thread '{thread}' not found in frame {frameIndex}. Available threads: {string.Join(", ", threadNames)}.");
                return ExitCode.NotFound;
            }

            var resolvedThread = threadNames[threadIndex];

            var view = s.GetHierarchy(frameIndex, threadIndex, out error);
            if (view == null)
            {
                if (error != null)
                    return error.Report(cmd);

                cmd.Log.Error($"No CPU hierarchy data for frame {frameIndex}, thread '{resolvedThread}'.");
                return ExitCode.Failed;
            }

            // The same denominator profiler_frame uses for percentOfThread: the thread's root sample (raw sample 0).
            double threadMs;
            using (var rawThread = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                threadMs = rawThread != null && rawThread.valid && rawThread.sampleCount > 0 ? rawThread.GetSampleTimeNs(0) * 1e-6 : 0;

            var nodeId = view.GetRootItemID();
            string markerPath = null;
            if (sampleId >= 0)
            {
                if (!TryResolveSample(view, frameIndex, threadIndex, sampleId, out nodeId))
                {
                    cmd.Log.Error($"Sample {sampleId} does not exist in frame {frameIndex}, thread '{resolvedThread}'.");
                    return ExitCode.NotFound;
                }

                markerPath = HierarchyPath(view, nodeId);
            }
            else
            {
                var walked = new List<string>();
                foreach (var segment in (marker ?? "").Split('/'))
                {
                    if (segment.Length == 0)
                        continue;

                    var childId = FindChildByName(view, nodeId, segment);
                    if (childId == HierarchyFrameDataView.invalidSampleId)
                    {
                        cmd.Log.Error($"Marker '{segment}' not found under '{string.Join("/", walked)}' in frame {frameIndex}, thread '{resolvedThread}'. " +
                            $"Its children: {string.Join(", ", ChildNames(view, nodeId))}.");
                        return ExitCode.NotFound;
                    }

                    nodeId = childId;
                    walked.Add(segment);
                }

                if (walked.Count > 0)
                    markerPath = string.Join("/", walked);
            }

            var selectedObjects = ResolveObjects(view, nodeId, out var objectCount, out var objectsFrom);
            var selectedMs = view.GetItemColumnDataAsFloat(nodeId, HierarchyFrameDataView.columnTotalTime);
            var isRoot = nodeId == view.GetRootItemID();

            var relatedIndex = -1;
            if (!string.IsNullOrEmpty(relatedThread))
            {
                if (isRoot)
                {
                    cmd.Log.Error("related-thread needs a marker or sample-id to compare against.");
                    return ExitCode.InvalidUsage;
                }

                relatedIndex = threadNames.FindIndex(n => string.Equals(n, relatedThread, StringComparison.OrdinalIgnoreCase));
                if (relatedIndex < 0)
                {
                    cmd.Log.Error($"Thread '{relatedThread}' not found in frame {frameIndex}. Available threads: {string.Join(", ", threadNames)}.");
                    return ExitCode.NotFound;
                }
            }

            var response = JObject.FromObject(new
            {
                sessionId = s.Id,
                capture = s.Path,
                loaded = s.LoadGeneration != generation,
                frame = frameIndex,
                frameRange = new { first = s.First, last = s.Last },
                thread = resolvedThread,
                threadMs = Math.Round(threadMs, 3),
                marker = markerPath == null ? null : Describe(view, nodeId, markerPath, threadMs, null, selectedObjects, objectCount, objectsFrom),
                children = TopChildren(view, nodeId, maxChildren, threadMs, isRoot ? threadMs : selectedMs,
                    selectedObjects, objectCount, objectsFrom ?? (objectCount > 0 ? markerPath : null))
            });

            if (callstacks)
                ProfilerSampleDetails.AddCallstacks(response, view, nodeId);

            if (relatedIndex >= 0)
                response["related"] = JToken.FromObject(ProfilerSampleDetails.Related(view, nodeId, frameIndex, threadIndex, relatedIndex, threadNames[relatedIndex], maxChildren));

            cmd.Result.Write(response);
            return ExitCode.Success;
        }

        static int FindChildByName(HierarchyFrameDataView view, int parentId, string name)
        {
            var children = new List<int>();
            view.GetItemChildren(parentId, children);
            foreach (var childId in children)
            {
                if (string.Equals(view.GetItemName(childId), name, StringComparison.Ordinal))
                    return childId;
            }
            return HierarchyFrameDataView.invalidSampleId;
        }

        static List<string> ChildNames(HierarchyFrameDataView view, int parentId)
        {
            var children = new List<int>();
            view.GetItemChildren(parentId, children);
            return children.Select(view.GetItemName).ToList();
        }

        static List<object> TopChildren(HierarchyFrameDataView view, int parentId, int maxChildren, double threadMs,
            double parentMs, JArray inheritedObjects, int inheritedCount, string inheritedFrom)
        {
            var children = new List<int>();
            view.GetItemChildren(parentId, children);
            return children
                .Select(id => new { id, total = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime) })
                .OrderByDescending(c => c.total)
                .Take(Math.Max(0, maxChildren))
                .Select(c =>
                {
                    var objects = SampleObjects.Collect(view, c.id, out var count);
                    var from = (string)null;
                    if (count == 0)
                    {
                        objects = inheritedObjects;
                        count = inheritedCount;
                        from = inheritedFrom;
                    }
                    return (object)Describe(view, c.id, view.GetItemName(c.id), threadMs, parentMs, objects, count, from);
                })
                .ToList();
        }

        /// <summary>
        /// <paramref name="name"/> is the display name: the full path for the selected marker, the marker name for a child.
        /// The description is looked up by the sample's own marker name. <paramref name="objects"/> are the Unity objects
        /// the sample ran for, own or inherited (<paramref name="objectsFrom"/> is then the path they were taken from).
        /// </summary>
        static JObject Describe(HierarchyFrameDataView view, int id, string name, double threadMs, double? parentMs,
            JArray objects, int objectCount, string objectsFrom)
        {
            var raw = new List<int>();
            view.GetItemRawFrameDataViewIndices(id, raw);
            var totalMs = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime);
            var entry = MarkerDescriptions.Annotate(new
            {
                sampleId = raw.Count > 0 ? raw.Min() : -1,
                instances = view.GetItemMergedSamplesCount(id),
                name,
                totalMs,
                selfMs = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime),
                calls = (int)view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls),
                gcAllocBytes = (long)view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnGcMemory),
                percentOfThread = Percent(totalMs, threadMs),
                percentOfParent = parentMs.HasValue ? Percent(totalMs, parentMs.Value) : (double?)null,
                hasChildren = view.HasItemChildren(id)
            }, view.GetItemName(id));

            if (objectCount > 0)
            {
                entry["objects"] = objects.DeepClone();
                entry["objectCount"] = objectCount;
                if (objectsFrom != null)
                    entry["objectsFrom"] = objectsFrom;
            }

            return entry;
        }

        static double Percent(double part, double whole) => whole <= 0 ? 0 : Math.Round(100.0 * part / whole, 1);

        /// <summary>
        /// The Unity objects of the item, or of its nearest ancestor that has any: Unity records the object only on
        /// the sample that ran for it (a script's <c>Update</c>), not on the samples below it. <paramref name="from"/>
        /// is the path of the ancestor the objects were taken from, null when they are the item's own.
        /// </summary>
        static JArray ResolveObjects(HierarchyFrameDataView view, int itemId, out int count, out string from)
        {
            from = null;
            count = 0;
            var root = view.GetRootItemID();
            if (itemId == root)
                return null;

            var objects = SampleObjects.Collect(view, itemId, out count);
            if (count > 0)
                return objects;

            var ancestors = new List<int>();
            view.GetItemAncestors(itemId, ancestors);
            foreach (var ancestor in ancestors.Where(a => a != root).OrderByDescending(view.GetItemDepth))
            {
                objects = SampleObjects.Collect(view, ancestor, out count);
                if (count > 0)
                {
                    from = HierarchyPath(view, ancestor);
                    return objects;
                }
            }

            return null;
        }

        /// <summary>The hierarchy item holding a raw sample of this frame and thread, or false when there is no such sample.</summary>
        static bool TryResolveSample(HierarchyFrameDataView view, int frame, int thread, int sampleId, out int itemId)
        {
            itemId = view.GetRootItemID();
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, thread))
            {
                if (raw == null || !raw.valid || sampleId <= 0 || sampleId >= raw.sampleCount)
                    return false;

                var source = new RawFrameDataViewSource(raw);
                var chain = RawSampleTree.Ancestors(source, sampleId);
                chain.Add(sampleId);

                var children = new List<int>();
                foreach (var index in chain)
                {
                    var markerId = raw.GetSampleMarkerId(index);
                    children.Clear();
                    view.GetItemChildren(itemId, children);
                    var next = HierarchyFrameDataView.invalidSampleId;
                    foreach (var child in children)
                    {
                        if (view.GetItemMarkerID(child) == markerId && view.ItemContainsRawFrameDataViewIndex(child, index))
                        {
                            next = child;
                            break;
                        }
                    }

                    if (next == HierarchyFrameDataView.invalidSampleId)
                        return false;
                    itemId = next;
                }
            }

            return true;
        }

        /// <summary>The '/'-separated marker path from the thread's top-level marker down to the item.</summary>
        internal static string HierarchyPath(HierarchyFrameDataView view, int id)
        {
            var root = view.GetRootItemID();
            var ancestors = new List<int>();
            view.GetItemAncestors(id, ancestors);
            var names = ancestors
                .Where(a => a != root)
                .OrderBy(view.GetItemDepth)
                .Select(view.GetItemName)
                .ToList();
            names.Add(view.GetItemName(id));
            return string.Join("/", names);
        }
    }
}
