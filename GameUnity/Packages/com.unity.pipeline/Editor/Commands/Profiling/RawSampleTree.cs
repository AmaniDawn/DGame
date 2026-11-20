using System.Collections.Generic;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>
    /// Ancestry of raw samples. A raw sample index is stable within one thread of one frame of a capture, so
    /// it is what the profiling commands hand out as a sampleId. Pure and thread safe.
    /// </summary>
    static class RawSampleTree
    {
        /// <summary>
        /// The ancestors of a sample, outermost first, without the hidden root sample. Empty when the index is the
        /// root or out of range, which callers treat as a sample that does not exist.
        /// </summary>
        internal static List<int> Ancestors<T>(T source, int sampleIndex) where T : IRawSampleSource
        {
            var result = new List<int>();
            if (sampleIndex <= 0 || sampleIndex >= source.SampleCount)
                return result;

            var stack = new List<(int Index, int Remaining)>();
            for (var i = 0; i < sampleIndex; i++)
            {
                while (stack.Count > 0 && stack[stack.Count - 1].Remaining == 0)
                    stack.RemoveAt(stack.Count - 1);

                if (stack.Count > 0)
                {
                    var top = stack[stack.Count - 1];
                    stack[stack.Count - 1] = (top.Index, top.Remaining - 1);
                }

                stack.Add((i, source.GetSampleChildrenCount(i)));
            }

            // Settle the stack for the sample itself: finished parents are not its ancestors.
            while (stack.Count > 0 && stack[stack.Count - 1].Remaining == 0)
                stack.RemoveAt(stack.Count - 1);

            foreach (var entry in stack)
            {
                if (entry.Index != 0)
                    result.Add(entry.Index);
            }

            return result;
        }

        /// <summary>The marker names from the outermost ancestor to the sample, joined with '/'.</summary>
        internal static string Path<T>(T source, int sampleIndex) where T : IRawSampleSource
        {
            var names = new List<string>();
            foreach (var index in Ancestors(source, sampleIndex))
                names.Add(source.GetMarkerName(source.GetSampleMarkerId(index)));
            if (sampleIndex > 0 && sampleIndex < source.SampleCount)
                names.Add(source.GetMarkerName(source.GetSampleMarkerId(sampleIndex)));
            return string.Join("/", names);
        }
    }
}
