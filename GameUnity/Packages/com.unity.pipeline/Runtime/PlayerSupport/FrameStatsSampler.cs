using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Pipeline.Runtime.Telemetry
{
    /// <summary>
    /// Samples per-frame timing on the main thread so the runtime telemetry command can report fps and
    /// frame-time statistics over a rolling window.
    ///
    /// A Player build has no Editor profiler window to read from and no <c>EditorApplication.update</c>
    /// to drive sampling, so <see cref="RuntimePipelineManager"/> owns one shared instance and feeds it
    /// every frame from its <c>Update</c>. The sampler is deliberately allocation-free on the hot path:
    /// each frame time is written into a fixed ring buffer and only reduced to a <see cref="FrameStatsSnapshot"/>
    /// on demand when a telemetry request arrives (far rarer than the frame rate).
    ///
    /// Sampling uses <c>Time.unscaledDeltaTime</c> so reported fps reflects real wall-clock frame pacing
    /// independent of <c>Time.timeScale</c> — a paused or slow-motion game still reports its true fps.
    /// </summary>
    public sealed class FrameStatsSampler : IDisposable
    {
        /// <summary>
        /// The process-wide sampler fed by the active <see cref="RuntimePipelineManager"/>. Null when no
        /// manager is present (e.g. EditMode tests, or a scene without the component). Telemetry consumers
        /// must treat a null sampler as "frame stats unavailable" rather than failing — memory and counter
        /// data that do not depend on per-frame sampling can still be reported without it.
        /// </summary>
        public static FrameStatsSampler Shared { get; set; }

        private readonly float[] m_FrameTimesMs;
        private int m_Head;
        private int m_Count;
        private float m_LastFrameTimeMs;

        // ProfilerRecorders pull counters straight from the profiler stream. They are started lazily on
        // the first snapshot (creation must happen on the main thread) and disposed with the sampler.
        // Recorders that do not resolve on this platform/Unity version report Valid == false and are
        // simply skipped when building a snapshot.
        private readonly List<CounterRecorder> m_Counters = new List<CounterRecorder>();
        private bool m_CountersStarted;

        private struct CounterRecorder
        {
            public string Name;
            public ProfilerRecorder Recorder;
        }

        /// <summary>Number of frames the rolling window can hold.</summary>
        public int Capacity => m_FrameTimesMs.Length;

        /// <summary>Number of frames currently recorded (ramps up to <see cref="Capacity"/>).</summary>
        public int SampleCount => m_Count;

        public FrameStatsSampler(int windowFrames = 120)
        {
            if (windowFrames < 1)
                windowFrames = 1;
            m_FrameTimesMs = new float[windowFrames];
        }

        /// <summary>
        /// Record one completed frame. Call once per frame from the main thread.
        /// <paramref name="deltaSeconds"/> is the unscaled delta time of the frame just completed
        /// (typically <c>Time.unscaledDeltaTime</c>).
        /// </summary>
        public void Sample(float deltaSeconds)
        {
            var ms = deltaSeconds * 1000f;
            m_LastFrameTimeMs = ms;
            m_FrameTimesMs[m_Head] = ms;
            m_Head = (m_Head + 1) % m_FrameTimesMs.Length;
            if (m_Count < m_FrameTimesMs.Length)
                m_Count++;

            // FrameTimingManager only yields data for frames in which timings were captured, so the
            // capture call has to happen on the per-frame path, not when a snapshot is requested.
            FrameTimingManager.CaptureFrameTimings();
        }

        /// <summary>
        /// Reduce the current window to an immutable snapshot. Must be called on the main thread (it reads
        /// profiler recorders and the frame timing manager).
        /// </summary>
        public FrameStatsSnapshot GetSnapshot()
        {
            var snap = new FrameStatsSnapshot();

            if (m_Count > 0)
            {
                float sum = 0f, min = float.MaxValue, max = 0f;
                for (int i = 0; i < m_Count; i++)
                {
                    var v = m_FrameTimesMs[i];
                    sum += v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                var avg = sum / m_Count;
                snap.Available = true;
                snap.SampleWindow = m_Count;
                snap.AverageFrameTimeMs = avg;
                snap.MinFrameTimeMs = min;
                snap.MaxFrameTimeMs = max;
                snap.LastFrameTimeMs = m_LastFrameTimeMs;
                snap.Fps = avg > 0f ? 1000f / avg : 0f;
            }

            ReadFrameTimingManager(snap);
            ReadCounters(snap);
            return snap;
        }

        // CPU/GPU frame time from FrameTimingManager. Only populated when the platform reports timings
        // (requires "Frame Timing Stats" in Player settings / -enable-frame-timing-stats); otherwise the
        // snapshot's GpuTimingAvailable stays false rather than reporting misleading zeros.
        private static void ReadFrameTimingManager(FrameStatsSnapshot snap)
        {
            var timings = new FrameTiming[1];
            var captured = FrameTimingManager.GetLatestTimings(1, timings);
            if (captured > 0)
            {
                snap.GpuTimingAvailable = true;
                snap.CpuFrameTimeMs = timings[0].cpuFrameTime;
                snap.GpuFrameTimeMs = timings[0].gpuFrameTime;
            }
        }

        private void ReadCounters(FrameStatsSnapshot snap)
        {
            EnsureCountersStarted();
            var counters = new Dictionary<string, double>();
            foreach (var c in m_Counters)
            {
                if (c.Recorder.Valid)
                    counters[c.Name] = c.Recorder.LastValue;
            }
            snap.Counters = counters;
        }

        private void EnsureCountersStarted()
        {
            if (m_CountersStarted)
                return;
            m_CountersStarted = true;

            // Common render/memory counters. Stat names that do not exist on the running platform resolve
            // to invalid recorders and are filtered out at snapshot time.
            TryAddCounter("DrawCalls", ProfilerCategory.Render, "Draw Calls Count");
            TryAddCounter("SetPassCalls", ProfilerCategory.Render, "SetPass Calls Count");
            TryAddCounter("Triangles", ProfilerCategory.Render, "Triangles Count");
            TryAddCounter("Vertices", ProfilerCategory.Render, "Vertices Count");
            TryAddCounter("GcAllocInFrame", ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void TryAddCounter(string reportedName, ProfilerCategory category, string statName)
        {
            m_Counters.Add(new CounterRecorder
            {
                Name = reportedName,
                Recorder = ProfilerRecorder.StartNew(category, statName)
            });
        }

        public void Dispose()
        {
            foreach (var c in m_Counters)
            {
                if (c.Recorder.Valid)
                    c.Recorder.Dispose();
            }
            m_Counters.Clear();
            m_CountersStarted = false;
        }
    }

    /// <summary>
    /// Immutable point-in-time reduction of <see cref="FrameStatsSampler"/>'s rolling window plus the
    /// counters/frame-timing read at snapshot time. Serialized as the frame-stats portion of the runtime
    /// telemetry response.
    /// </summary>
    [Serializable]
    public class FrameStatsSnapshot
    {
        /// <summary>True when at least one frame has been sampled (i.e. a manager is feeding the sampler).</summary>
        public bool Available { get; set; }

        /// <summary>Number of frames included in the averages.</summary>
        public int SampleWindow { get; set; }

        /// <summary>Frames per second derived from the average frame time over the window.</summary>
        public float Fps { get; set; }

        public float AverageFrameTimeMs { get; set; }
        public float MinFrameTimeMs { get; set; }
        public float MaxFrameTimeMs { get; set; }
        public float LastFrameTimeMs { get; set; }

        /// <summary>True when FrameTimingManager returned CPU/GPU timings for a recent frame.</summary>
        public bool GpuTimingAvailable { get; set; }
        public double CpuFrameTimeMs { get; set; }
        public double GpuFrameTimeMs { get; set; }

        /// <summary>Valid profiler counters by reported name (e.g. DrawCalls, Triangles).</summary>
        public Dictionary<string, double> Counters { get; set; }
    }
}
