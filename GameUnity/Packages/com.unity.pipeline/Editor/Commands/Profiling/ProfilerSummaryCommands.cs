using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    internal enum FrameSort
    {
        Cpu,
        Gc,
        Gpu,
        Main,
        Render
    }

    /// <summary>Ranking and hiccup rules for <see cref="ProfilerSummaryCommands"/>; pure.</summary>
    internal static class FrameRanking
    {
        /// <summary>The value a frame is ranked by. Null drops the frame; a frame without GC allocation has no GC key.</summary>
        internal static double? Key(FrameTiming t, FrameSort sort)
        {
            switch (sort)
            {
                case FrameSort.Cpu: return t.CpuMs;
                case FrameSort.Gc: return t.GcAllocBytes.HasValue && t.GcAllocBytes.Value > 0 ? t.GcAllocBytes.Value : (double?)null;
                case FrameSort.Gpu: return t.GpuMs;
                case FrameSort.Main: return t.MainActiveMs;
                case FrameSort.Render: return t.RenderActiveMs;
                default: return null;
            }
        }

        /// <summary>Frames with a key, largest key first, earlier frame on ties.</summary>
        internal static List<FrameTiming> Rank(IEnumerable<FrameTiming> frames, FrameSort sort, int limit)
        {
            var max = Math.Max(0, limit);
            var top = new List<(FrameTiming t, double key)>();
            foreach (var t in frames)
            {
                var key = Key(t, sort);
                if (!key.HasValue)
                    continue;

                var pos = top.Count;
                while (pos > 0 && (key.Value > top[pos - 1].key || (key.Value == top[pos - 1].key && t.Frame < top[pos - 1].t.Frame)))
                    pos--;
                if (pos >= max)
                    continue;

                top.Insert(pos, (t, key.Value));
                if (top.Count > max)
                    top.RemoveAt(top.Count - 1);
            }

            return top.Select(p => p.t).ToList();
        }

        internal static bool IsHiccup(double cpuMs, double medianCpuMs, double factor)
        {
            return medianCpuMs > 0 && cpuMs >= medianCpuMs * factor;
        }
    }

    static class ProfilerSummaryCommands
    {
        const string Group = "diagnostics";

        [CliGroupCommand(Group, "Summarize a capture session over a frame range: frame-time and GC statistics, frames over budget, hiccups (frames far above the median CPU time), the median frame, and the frames ranked by CPU, GC, GPU or thread active time. Break one down with " + CommandNames.DiagnosticsProfilerFrame + ".", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static async Task<ExitCode> ProfilerSummary(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ".", Required = true)] string session = "",
            [CliArg("first-frame", "First frame to analyze. Defaults to the capture's second frame, because the first is usually partial.")] int firstFrame = -1,
            [CliArg("last-frame", "Last frame to analyze. Defaults to the capture's last frame.")] int lastFrame = -1,
            [CliArg("target-frame-ms", "Frame budget in ms. 0 uses the Profiler's Target FPS (60 when unavailable).")] double targetFrameMs = 0,
            [CliArg("spikes", "How many frames to list, ranked by sort. Default 5.")] int spikes = 5,
            [CliArg("sort", "Rank the listed frames by: cpu (default) | gc | gpu | main | render.")] string sort = "cpu",
            [CliArg("hiccup-factor", "A frame is a hiccup when its CPU time is at least this many times the median. Default 2.")] double hiccupFactor = 2.0)
        {
            var sortBy = FrameSort.Cpu;
            var known = false;
            foreach (var candidate in (FrameSort[])Enum.GetValues(typeof(FrameSort)))
            {
                if (string.Equals(candidate.ToString(), sort, StringComparison.OrdinalIgnoreCase))
                {
                    sortBy = candidate;
                    known = true;
                }
            }

            if (!known)
            {
                cmd.Log.Error($"Unknown sort '{sort}'. Use cpu, gc, gpu, main or render.");
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
                if (note != null)
                    cmd.Log.Info(note);

                var target = targetFrameMs > 0 ? targetFrameMs : 1000.0 / ProfilerAnalysisMath.TargetFrameRate();
                var series = await s.TimingsAsync(first, last, slicer);
                if (series.Error != null)
                    return series.Error.Report(cmd);

                var timings = series.Timings;
                if (series.Skipped > 0)
                    cmd.Log.Warning($"{series.Skipped} frames had no data and were skipped.");
                if (timings.Count == 0)
                {
                    cmd.Log.Error($"No frame in {first}..{last} has data.");
                    return ExitCode.Failed;
                }

                var isEditor = s.IsEditorCapture(out error);
                if (error != null)
                    return error.Report(cmd);

                var ordered = timings.OrderBy(t => t.CpuMs).ToList();
                var median = ordered[(int)((ordered.Count - 1) * 0.5)];
                var over = timings.Count(t => t.CpuMs > target);
                var medianCpu = median.CpuMs;
                if (sortBy == FrameSort.Gpu && !timings.Any(t => t.GpuMs.HasValue))
                    cmd.Log.Warning("No frame has GPU timing. Record with the GPU Usage module enabled; a capture's last 4 frames never have a GPU value.");

                cmd.Result.Write(new
                {
                    sessionId = s.Id,
                    capture = s.Path,
                    loaded = s.LoadGeneration != generation,
                    captureKind = isEditor ? "editor" : "player",
                    frameRange = new { first = s.First, last = s.Last, count = s.FrameCount },
                    analyzedRange = new { first, last, count = timings.Count },
                    sort = sortBy.ToString().ToLowerInvariant(),
                    cpuFrameMs = SeriesStats.From(timings.Select(t => t.CpuMs))?.ToJson(),
                    mainActiveMs = SeriesStats.From(timings.Where(t => t.MainActiveMs.HasValue).Select(t => t.MainActiveMs.Value))?.ToJson(),
                    renderActiveMs = SeriesStats.From(timings.Where(t => t.RenderActiveMs.HasValue).Select(t => t.RenderActiveMs.Value))?.ToJson(),
                    gpuFrameMs = SeriesStats.From(timings.Where(t => t.GpuMs.HasValue).Select(t => t.GpuMs.Value))?.ToJson(),
                    gcAllocBytes = SeriesStats.From(timings.Where(t => t.GcAllocBytes.HasValue).Select(t => (double)t.GcAllocBytes.Value))?.ToJson(0),
                    budget = new
                    {
                        targetFrameMs = Math.Round(target, 3),
                        framesOverBudget = over,
                        percentOverBudget = Math.Round(100.0 * over / timings.Count, 1)
                    },
                    medianFrame = median.Frame,
                    hiccupFrames = timings.Count(t => FrameRanking.IsHiccup(t.CpuMs, medianCpu, hiccupFactor)),
                    gcFrames = timings.Count(t => t.GcAllocBytes.HasValue && t.GcAllocBytes.Value > 0),
                    spikes = FrameRanking.Rank(timings, sortBy, spikes)
                        .Select(t => SpikeJson(t, target, medianCpu, hiccupFactor))
                        .ToList()
                });
                return ExitCode.Success;
            }
            catch (AnalysisInterruptedException ex)
            {
                cmd.Log.Error(ex.Message);
                return ExitCode.Failed;
            }
        }

        static object SpikeJson(FrameTiming t, double target, double medianCpu, double hiccupFactor)
        {
            return new
            {
                frame = t.Frame,
                cpuMs = Math.Round(t.CpuMs, 3),
                mainActiveMs = Round(t.MainActiveMs),
                renderActiveMs = Round(t.RenderActiveMs),
                gpuMs = Round(t.GpuMs),
                gcAllocBytes = t.GcAllocBytes,
                bottleneck = ProfilerAnalysisMath.Bottleneck(t, target),
                overBudget = t.CpuMs > target,
                hiccup = FrameRanking.IsHiccup(t.CpuMs, medianCpu, hiccupFactor)
            };
        }

        internal static double? Round(double? value) => value.HasValue ? Math.Round(value.Value, 3) : (double?)null;
    }
}
