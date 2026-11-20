using System;
using System.Collections.Generic;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>Parses the comma-separated marker name lists the profiling commands take.</summary>
    static class MarkerFilter
    {
        /// <summary>Trimmed, non-empty names. Marker names are case-sensitive in Unity, so the set compares ordinally.</summary>
        internal static HashSet<string> Parse(string commaSeparated)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(commaSeparated))
                return set;

            foreach (var part in commaSeparated.Split(','))
            {
                var name = part.Trim();
                if (name.Length > 0)
                    set.Add(name);
            }

            return set;
        }
    }

    /// <summary>
    /// The call tree of one thread of one frame built from raw samples. Siblings with the same marker are merged,
    /// like the Profiler's Hierarchy view, and each node remembers its longest instance so it can be drilled into.
    /// Pure and thread safe.
    /// </summary>
    sealed class RawCallTree
    {
        internal const int MaxHotPathDepth = 64;

        internal sealed class Node
        {
            public int MarkerId;
            public ulong TotalNs;
            public ulong SelfNs;
            public int Instances;
            public int HeaviestSample;
            public ulong HeaviestNs;
            public readonly List<Node> Children = new List<Node>();
            internal readonly Dictionary<int, Node> ByMarker = new Dictionary<int, Node>();
        }

        RawCallTree(Node root) { Root = root; }

        /// <summary>The thread's hidden root sample; its total is the thread's time in the frame.</summary>
        internal Node Root { get; }

        internal static RawCallTree Build<T>(T source) where T : IRawSampleSource
        {
            var count = source.SampleCount;
            var root = new Node();
            var tree = new RawCallTree(root);
            if (count == 0)
                return tree;

            root.MarkerId = source.GetSampleMarkerId(0);
            root.TotalNs = source.GetSampleTimeNs(0);
            root.Instances = 1;

            // Child counts, not timings, define the hierarchy: durations are lossy near boundaries.
            var stack = new List<(Node Node, int Remaining)> { (root, source.GetSampleChildrenCount(0)) };
            for (var i = 1; i < count; i++)
            {
                while (stack.Count > 1 && stack[stack.Count - 1].Remaining == 0)
                    stack.RemoveAt(stack.Count - 1);

                var top = stack[stack.Count - 1];
                stack[stack.Count - 1] = (top.Node, top.Remaining - 1);

                var markerId = source.GetSampleMarkerId(i);
                var duration = source.GetSampleTimeNs(i);
                if (!top.Node.ByMarker.TryGetValue(markerId, out var child))
                {
                    child = new Node { MarkerId = markerId };
                    top.Node.ByMarker.Add(markerId, child);
                    top.Node.Children.Add(child);
                }

                child.TotalNs += duration;
                child.Instances++;
                if (child.HeaviestSample == 0 || duration > child.HeaviestNs)
                {
                    child.HeaviestSample = i;
                    child.HeaviestNs = duration;
                }

                stack.Add((child, source.GetSampleChildrenCount(i)));
            }

            ComputeSelf(root);
            return tree;
        }

        static void ComputeSelf(Node root)
        {
            var pending = new Stack<Node>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                ulong children = 0;
                foreach (var child in node.Children)
                {
                    children += child.TotalNs;
                    pending.Push(child);
                }

                node.SelfNs = node.TotalNs > children ? node.TotalNs - children : 0;
            }
        }

        static Node Heaviest(Node parent, ISet<int> excluded)
        {
            Node best = null;
            foreach (var child in parent.Children)
            {
                if (excluded != null && excluded.Contains(child.MarkerId))
                    continue;
                if (best == null || child.TotalNs > best.TotalNs ||
                    (child.TotalNs == best.TotalNs && child.HeaviestSample < best.HeaviestSample))
                    best = child;
            }

            return best;
        }

        /// <summary>
        /// Follows the heaviest child from the root down, skipping excluded markers. The path ends at the first
        /// node whose own time is at least that of its heaviest child: that node is the hot leaf.
        /// </summary>
        internal List<Node> HotPath(ISet<int> excludedMarkerIds)
        {
            var path = new List<Node>();
            var node = Root;
            while (path.Count < MaxHotPathDepth)
            {
                var next = Heaviest(node, excludedMarkerIds);
                if (next == null)
                    break;

                path.Add(next);
                var below = Heaviest(next, excludedMarkerIds);
                if (below == null || next.SelfNs >= below.TotalNs)
                    break;
                node = next;
            }

            return path;
        }
    }
}
