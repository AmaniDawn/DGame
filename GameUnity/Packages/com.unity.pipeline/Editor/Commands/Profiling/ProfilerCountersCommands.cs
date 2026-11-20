using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    readonly struct CounterInfo
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string Category;
        public readonly string Unit;

        public CounterInfo(int id, string name, string category, string unit)
        {
            Id = id;
            Name = name;
            Category = category;
            Unit = unit;
        }
    }

    static class ProfilerCountersCommands
    {
        const string Group = "diagnostics";
        const string TimeUnit = "TimeNanoseconds";
        const double NsToMs = 1e-6;
        internal const int MaxPerFrameValues = 500;

        [CliGroupCommand(Group, "List the counters recorded in a capture session, or summarize chosen counters over a frame range: min, median, p95, max and mean, optionally per frame.", Tags = new[] { "observability/profiling" }, Effect = CommandEffect.Idempotent)]
        internal static async Task<ExitCode> ProfilerCounters(CommandContext cmd,
            [CliArg("session", "A sessionId from " + CommandNames.DiagnosticsProfilerSession + ".", Required = true)] string session = "",
            [CliArg("counters", "Comma-separated counter names, e.g. 'GC Allocated In Frame,CPU Main Thread Active Time'. A name shared by several categories returns each; write 'Category/Name' to pick one. Empty lists the available counters.")] string counters = "",
            [CliArg("first-frame", "First frame to analyze. Defaults to the capture's second frame, because the first is usually partial.")] int firstFrame = -1,
            [CliArg("last-frame", "Last frame to analyze. Defaults to the capture's last frame.")] int lastFrame = -1,
            [CliArg("per-frame", "Also return each counter's value on every analyzed frame.")] bool perFrame = false)
        {
            var s = ProfilerCaptureSessions.Require(session, out var error);
            if (s == null)
                return error.Report(cmd);

            var generation = s.LoadGeneration;
            var available = s.Counters(out error);
            if (available == null)
                return error.Report(cmd);

            if (string.IsNullOrWhiteSpace(counters))
            {
                cmd.Result.Write(new
                {
                    sessionId = s.Id,
                    loaded = s.LoadGeneration != generation,
                    counters = available.Select(c => new { name = c.Name, category = c.Category, unit = UnitName(c) }).ToList()
                });
                return ExitCode.Success;
            }

            if (!s.TryResolveRange(firstFrame, lastFrame, out var first, out var last, out var note, out error))
                return error.Report(cmd);

            // Created before the first await: it captures the execution context, only set until then.
            var slicer = new AnalysisSlicer(s);
            if (note != null)
                cmd.Log.Info(note);
            cmd.Log.Info("Counter values are read from the main thread, where Unity's built-in counters are emitted.");

            var unknown = new List<string>();
            var wanted = new List<CounterInfo>();
            foreach (var requested in counters.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0))
            {
                var matches = available.Where(c => string.Equals(c.Name, requested, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 0)
                    matches = available.Where(c => string.Equals($"{c.Category}/{c.Name}", requested, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matches.Count == 0)
                    unknown.Add(requested);
                else
                    wanted.AddRange(matches.Where(m => !wanted.Any(w => w.Id == m.Id)));

                if (matches.Count > 1)
                    cmd.Log.Warning($"'{requested}' exists in {matches.Count} categories and all are returned. Use 'Category/Name' to pick one.");
            }

            var values = wanted.Select(_ => new List<(int frame, double value)>()).ToList();
            var skipped = 0;
            try
            {
                for (var frame = first; frame <= last; frame++)
                {
                    await slicer.CheckpointAsync();
                    using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0))
                    {
                        if (raw == null || !raw.valid)
                        {
                            skipped++;
                            continue;
                        }

                        for (var i = 0; i < wanted.Count; i++)
                        {
                            var value = Read(raw, wanted[i]);
                            if (value.HasValue)
                                values[i].Add((frame, value.Value));
                        }
                    }
                }
            }
            catch (AnalysisInterruptedException ex)
            {
                cmd.Log.Error(ex.Message);
                return ExitCode.Failed;
            }

            if (skipped > 0)
                cmd.Log.Warning($"{skipped} frames had no data and were skipped.");

            var results = new List<object>();
            var truncatedNoted = false;
            for (var i = 0; i < wanted.Count; i++)
            {
                var c = wanted[i];
                var isTime = c.Unit == TimeUnit;
                var series = values[i].Select(v => isTime ? v.value * NsToMs : v.value).ToList();
                var stats = SeriesStats.From(series)?.ToJson(isTime ? 3 : 1);
                if (perFrame)
                {
                    var truncated = series.Count > MaxPerFrameValues;
                    if (truncated && !truncatedNoted)
                    {
                        truncatedNoted = true;
                        cmd.Log.Warning($"Per-frame values are limited to the first {MaxPerFrameValues} frames of the range. Narrow first-frame and last-frame to see the rest.");
                    }

                    results.Add(new
                    {
                        name = c.Name,
                        category = c.Category,
                        unit = UnitName(c),
                        stats,
                        valuesTotal = series.Count,
                        valuesTruncated = truncated,
                        values = LimitPerFrame(values[i]).Select((v, k) => new { frame = v.frame, value = Math.Round(series[k], 3) }).ToList()
                    });
                }
                else
                {
                    results.Add(new { name = c.Name, category = c.Category, unit = UnitName(c), stats });
                }
            }

            foreach (var name in unknown)
                cmd.Log.Warning($"Unknown counter '{name}'.");

            cmd.Result.Write(new
            {
                sessionId = s.Id,
                loaded = s.LoadGeneration != generation,
                analyzedRange = new { first, last, count = last - first + 1 - skipped },
                counters = results,
                unknown
            });
            return ExitCode.Success;
        }

        internal static List<T> LimitPerFrame<T>(List<T> values) =>
            values.Count <= MaxPerFrameValues ? values : values.GetRange(0, MaxPerFrameValues);

        static string UnitName(CounterInfo c) => c.Unit == TimeUnit ? "ms" : c.Unit;

        static double? Read(UnityEditor.Profiling.RawFrameDataView raw, CounterInfo counter)
        {
            try
            {
                return raw.GetCounterValueAsDouble(counter.Id);
            }
            catch (ArgumentException)
            {
                // The counter was not registered yet on this frame.
                return null;
            }
        }
    }
}
