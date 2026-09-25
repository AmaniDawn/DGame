using NUnit.Framework;
using Unity.Pipeline.Runtime.Telemetry;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Unit tests for <see cref="FrameStatsSampler"/>'s windowing math. These run in EditMode without a
    /// Player or RuntimePipelineManager — the sampler is a plain class fed synthetic frame times.
    /// </summary>
    public class FrameStatsSamplerTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void NewSampler_HasNoSamples_SnapshotUnavailable()
        {
            using (var sampler = new FrameStatsSampler())
            {
                var snap = sampler.GetSnapshot();

                Assert.IsFalse(snap.Available, "a sampler with no frames must report unavailable");
                Assert.AreEqual(0, snap.SampleWindow);
                Assert.IsNotNull(snap.Counters, "counters map should never be null");
            }
        }

        [Test]
        public void Sample_ComputesAverageMinMaxAndFps()
        {
            using (var sampler = new FrameStatsSampler())
            {
                // 10ms, 20ms, 30ms -> avg 20ms -> 50 fps.
                sampler.Sample(0.010f);
                sampler.Sample(0.020f);
                sampler.Sample(0.030f);

                var snap = sampler.GetSnapshot();

                Assert.IsTrue(snap.Available);
                Assert.AreEqual(3, snap.SampleWindow);
                Assert.AreEqual(20f, snap.AverageFrameTimeMs, Tolerance);
                Assert.AreEqual(10f, snap.MinFrameTimeMs, Tolerance);
                Assert.AreEqual(30f, snap.MaxFrameTimeMs, Tolerance);
                Assert.AreEqual(30f, snap.LastFrameTimeMs, Tolerance);
                Assert.AreEqual(50f, snap.Fps, 0.01f);
            }
        }

        [Test]
        public void Sample_BeyondCapacity_KeepsRollingWindow()
        {
            using (var sampler = new FrameStatsSampler(windowFrames: 3))
            {
                Assert.AreEqual(3, sampler.Capacity);

                // Feed 5 frames into a 3-frame window; only the last three (30/40/50ms) should survive.
                sampler.Sample(0.010f);
                sampler.Sample(0.020f);
                sampler.Sample(0.030f);
                sampler.Sample(0.040f);
                sampler.Sample(0.050f);

                var snap = sampler.GetSnapshot();

                Assert.AreEqual(3, snap.SampleWindow, "window must not exceed capacity");
                Assert.AreEqual(40f, snap.AverageFrameTimeMs, Tolerance);
                Assert.AreEqual(30f, snap.MinFrameTimeMs, Tolerance);
                Assert.AreEqual(50f, snap.MaxFrameTimeMs, Tolerance);
                Assert.AreEqual(50f, snap.LastFrameTimeMs, Tolerance);
            }
        }

        [Test]
        public void Sample_ZeroDelta_DoesNotDivideByZeroFps()
        {
            using (var sampler = new FrameStatsSampler())
            {
                sampler.Sample(0f);

                var snap = sampler.GetSnapshot();

                Assert.IsTrue(snap.Available);
                Assert.AreEqual(0f, snap.Fps, Tolerance, "zero average frame time must yield 0 fps, not Infinity/NaN");
            }
        }
    }
}
