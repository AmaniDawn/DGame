using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Runtime.Telemetry;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Runtime status information for Unity Player builds.
    /// Provides Player-appropriate status without Editor-specific APIs.
    /// </summary>
    public static class RuntimeStatusCommand
    {
        [CliCommand("runtime_status", "Get comprehensive runtime application status", MainThreadRequired = true, RuntimeOnly = true)]
        public static RuntimeStatusResponse GetRuntimeStatus()
        {
            try
            {
                return new RuntimeStatusResponse
                {
                    // Application info
                    UnityVersion = Application.unityVersion,
                    Platform = Application.platform.ToString(),
                    BuildGuid = Application.buildGUID,
                    Version = Application.version,

                    // Runtime state
                    IsPlaying = true, // Always true in Player builds
                    TargetFrameRate = Application.targetFrameRate,
                    TimeScale = Time.timeScale,
                    RealTimeSinceStartup = Time.realtimeSinceStartup,

                    // Performance info
                    FrameCount = Time.frameCount,
                    LoadedLevelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    LoadedLevelBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex,

                    // Live performance telemetry (fps, frame time, memory, profiler counters)
                    Performance = BuildPerformanceTelemetry(),

                    // Timestamp
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Pipeline: Runtime status command failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Assemble the live performance section from two independent sources:
        /// the per-frame <see cref="FrameStatsSampler"/> (fps / frame time / profiler counters), which only
        /// exists while a <see cref="RuntimePipelineManager"/> is feeding it, and the Profiler memory APIs,
        /// which can be read on demand at any time. Frame-stat fields stay at their defaults (and
        /// <see cref="RuntimePerformanceTelemetry.FrameStatsAvailable"/> stays false) when no sampler is
        /// running — e.g. in EditMode tests — so a missing sampler degrades gracefully rather than throwing.
        /// </summary>
        private static RuntimePerformanceTelemetry BuildPerformanceTelemetry()
        {
            var perf = new RuntimePerformanceTelemetry();

            var sampler = FrameStatsSampler.Shared;
            if (sampler != null)
            {
                var snap = sampler.GetSnapshot();
                perf.FrameStatsAvailable = snap.Available;
                perf.SampleWindow = snap.SampleWindow;
                perf.Fps = snap.Fps;
                perf.AverageFrameTimeMs = snap.AverageFrameTimeMs;
                perf.MinFrameTimeMs = snap.MinFrameTimeMs;
                perf.MaxFrameTimeMs = snap.MaxFrameTimeMs;
                perf.LastFrameTimeMs = snap.LastFrameTimeMs;
                perf.GpuTimingAvailable = snap.GpuTimingAvailable;
                perf.CpuFrameTimeMs = snap.CpuFrameTimeMs;
                perf.GpuFrameTimeMs = snap.GpuFrameTimeMs;
                perf.Counters = snap.Counters;
            }
            else
            {
                perf.Counters = new Dictionary<string, double>();
            }

            // Memory is sampled directly (no per-frame accumulation needed). These return 0 in a
            // non-development build where the profiler is stripped, which is reported faithfully.
            perf.TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            perf.TotalReservedMemory = Profiler.GetTotalReservedMemoryLong();
            perf.MonoUsedSize = Profiler.GetMonoUsedSizeLong();
            perf.MonoHeapSize = Profiler.GetMonoHeapSizeLong();
            perf.GcMemory = GC.GetTotalMemory(false);

            return perf;
        }
    }

    /// <summary>
    /// Response model for runtime status information.
    /// </summary>
    [Serializable]
    public class RuntimeStatusResponse
    {
        // Application Information
        public string UnityVersion { get; set; }
        public string Platform { get; set; }
        public string BuildGuid { get; set; }
        public string Version { get; set; }
        
        // Runtime State
        public bool IsPlaying { get; set; } // Always true for Player builds
        public int TargetFrameRate { get; set; }
        public float TimeScale { get; set; }
        public float RealTimeSinceStartup { get; set; }

        // Performance Information
        public int FrameCount { get; set; }
        public string LoadedLevelName { get; set; }
        public int LoadedLevelBuildIndex { get; set; }

        // Live performance telemetry
        public RuntimePerformanceTelemetry Performance { get; set; }

        // Metadata
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Live performance telemetry for a running Player: rolling fps / frame-time statistics, optional
    /// CPU/GPU frame timing, memory usage, and a map of profiler counters. Frame-stat and counter fields
    /// are populated from the per-frame <see cref="FrameStatsSampler"/>; memory fields are read on demand.
    /// </summary>
    [Serializable]
    public class RuntimePerformanceTelemetry
    {
        // Frame timing — sampled over a rolling window by the RuntimePipelineManager.
        /// <summary>False when no sampler is running; the fps / frame-time fields below are then meaningless.</summary>
        public bool FrameStatsAvailable { get; set; }
        public int SampleWindow { get; set; }
        public float Fps { get; set; }
        public float AverageFrameTimeMs { get; set; }
        public float MinFrameTimeMs { get; set; }
        public float MaxFrameTimeMs { get; set; }
        public float LastFrameTimeMs { get; set; }

        // CPU/GPU frame timing via FrameTimingManager (not available on every platform).
        public bool GpuTimingAvailable { get; set; }
        public double CpuFrameTimeMs { get; set; }
        public double GpuFrameTimeMs { get; set; }

        // Memory (bytes) via the Profiler API; 0 when the profiler is stripped (non-development build).
        public long TotalAllocatedMemory { get; set; }
        public long TotalReservedMemory { get; set; }
        public long MonoUsedSize { get; set; }
        public long MonoHeapSize { get; set; }
        public long GcMemory { get; set; }

        // Profiler counters by reported name (e.g. DrawCalls, Triangles, GcAllocInFrame).
        public Dictionary<string, double> Counters { get; set; }
    }
}