using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>Per-frame timings of one frame, as the Profiler's Bottlenecks view defines them.</summary>
    readonly struct FrameTiming
    {
        public readonly int Frame;
        public readonly double CpuMs;
        public readonly double? MainActiveMs;
        public readonly double? RenderActiveMs;
        public readonly double? GpuMs;
        public readonly long? GcAllocBytes;

        public FrameTiming(int frame, double cpuMs, double? mainActiveMs, double? renderActiveMs, double? gpuMs, long? gcAllocBytes)
        {
            Frame = frame;
            CpuMs = cpuMs;
            MainActiveMs = mainActiveMs;
            RenderActiveMs = renderActiveMs;
            GpuMs = gpuMs;
            GcAllocBytes = gcAllocBytes;
        }
    }

    /// <summary>min / p25 / median / p75 / p95 / max / mean of a series, following Profile Analyzer's conventions.</summary>
    sealed class SeriesStats
    {
        public double Min, P25, Median, P75, P95, Max, Mean;

        public static SeriesStats From(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
                return null;

            return new SeriesStats
            {
                Min = sorted[0],
                P25 = ProfilerAnalysisMath.Percentile(sorted, 0.25),
                Median = ProfilerAnalysisMath.Percentile(sorted, 0.5),
                P75 = ProfilerAnalysisMath.Percentile(sorted, 0.75),
                P95 = ProfilerAnalysisMath.Percentile(sorted, 0.95),
                Max = sorted[sorted.Count - 1],
                Mean = sorted.Average()
            };
        }

        public object ToJson(int digits = 3)
        {
            return new
            {
                min = Math.Round(Min, digits),
                p25 = Math.Round(P25, digits),
                median = Math.Round(Median, digits),
                p75 = Math.Round(P75, digits),
                p95 = Math.Round(P95, digits),
                max = Math.Round(Max, digits),
                mean = Math.Round(Mean, digits)
            };
        }
    }

    static class ProfilerAnalysisMath
    {
        const double FallbackTargetFps = 60;

        static PropertyInfo s_TargetFpsProperty;
        static bool s_TargetFpsResolved;

        /// <summary>Profile Analyzer's percentile: the lower index, no interpolation.</summary>
        internal static double Percentile(IReadOnlyList<double> sorted, double p)
        {
            if (sorted.Count == 0)
                return 0;
            return sorted[(int)((sorted.Count - 1) * p)];
        }

        /// <summary>
        /// Resolves the analyzed frame range. A capture's first frame is usually partial, so it is skipped
        /// by default when the capture has more than one frame.
        /// </summary>
        internal static bool TryResolveRange(int requestedFirst, int requestedLast, int historyFirst, int historyLast,
            out int first, out int last, out string note, out string error)
        {
            note = null;
            error = null;
            last = requestedLast < 0 ? historyLast : requestedLast;

            if (requestedFirst >= 0)
                first = requestedFirst;
            else if (historyLast > historyFirst)
            {
                first = historyFirst + 1;
                note = $"Skipped frame {historyFirst}, the capture's first frame, which is usually partial. Pass first-frame={historyFirst} to include it.";
            }
            else
                first = historyFirst;

            if (first < historyFirst || last > historyLast || first > last)
            {
                error = $"Frame range {first}..{last} is outside the capture's {historyFirst}..{historyLast}.";
                return false;
            }

            return true;
        }

        /// <summary>"gpu", "cpu", "none", or "unknown" when neither CPU active time was recorded.</summary>
        internal static string Bottleneck(FrameTiming t, double targetMs)
        {
            if (!t.MainActiveMs.HasValue && !t.RenderActiveMs.HasValue)
                return "unknown";

            var cpu = Math.Max(t.MainActiveMs ?? 0, t.RenderActiveMs ?? 0);
            if (t.GpuMs.HasValue && t.GpuMs.Value > targetMs && t.GpuMs.Value >= cpu)
                return "gpu";
            if (cpu > targetMs)
                return "cpu";
            return "none";
        }

        /// <summary>The Profiler's Target FPS (internal API), or 60.</summary>
        internal static double TargetFrameRate()
        {
            try
            {
                if (!s_TargetFpsResolved)
                {
                    s_TargetFpsResolved = true;
                    var type = typeof(ProfilerDriver).Assembly.GetType("UnityEditor.Profiling.ProfilerUserSettings");
                    s_TargetFpsProperty = type?.GetProperty("targetFramesPerSecond", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (s_TargetFpsProperty?.GetValue(null) is IConvertible value)
                {
                    var fps = value.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                    if (fps > 0)
                        return fps;
                }
            }
            catch (Exception)
            {
            }

            return FallbackTargetFps;
        }
    }
}
