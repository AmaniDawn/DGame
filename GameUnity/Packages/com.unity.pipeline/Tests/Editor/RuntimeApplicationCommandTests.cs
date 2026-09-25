using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;
using Unity.Pipeline.Tests.Editor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the runtime application commands (quit, set_target_framerate, set_timescale),
    /// exercised directly and via PipelineClient.
    /// </summary>
    public class RuntimeApplicationCommandTests
    {
        #region Direct - set_target_framerate

        [Test]
        public void SetTargetFrameRate_UpdatesApplication()
        {
            var original = Application.targetFrameRate;
            try
            {
                var result = RuntimeApplicationCommand.SetTargetFrameRate(60);
                Assert.That(result, Does.Contain("60 FPS"));
                Assert.AreEqual(60, Application.targetFrameRate);
            }
            finally { Application.targetFrameRate = original; }
        }

        [Test]
        public void SetTargetFrameRate_Zero_IsUnlimited()
        {
            var original = Application.targetFrameRate;
            try
            {
                var result = RuntimeApplicationCommand.SetTargetFrameRate(0);
                Assert.That(result, Does.Contain("unlimited"));
                Assert.AreEqual(0, Application.targetFrameRate);
            }
            finally { Application.targetFrameRate = original; }
        }

        #endregion

        #region Direct - set_timescale

        [Test]
        public void SetTimeScale_UpdatesTime()
        {
            var original = Time.timeScale;
            try
            {
                var result = RuntimeApplicationCommand.SetTimeScale(0.5f);
                Assert.That(result, Does.Contain("0.5"));
                Assert.AreEqual(0.5f, Time.timeScale, 0.001f);
            }
            finally { Time.timeScale = original; }
        }

        [Test]
        public void SetTimeScale_Zero_Pauses()
        {
            var original = Time.timeScale;
            try
            {
                var result = RuntimeApplicationCommand.SetTimeScale(0f);
                Assert.That(result, Does.Contain("paused"));
                Assert.AreEqual(0f, Time.timeScale, 0.001f);
            }
            finally { Time.timeScale = original; }
        }

        [Test]
        public void SetTimeScale_Negative_ReturnsError()
        {
            Assert.AreEqual("Error: Time scale cannot be negative", RuntimeApplicationCommand.SetTimeScale(-1f));
        }

        #endregion

        // Note: `quit` (QuitApplication) is play-mode-only — it calls DontDestroyOnLoad, which
        // throws in EditMode — so it is covered in the PlayMode smoke suite, not here.

        #region ViaClient

        [Test]
        public void SetTargetFrameRate_ViaClient_Updates()
        {
            var original = Application.targetFrameRate;
            using (var server = new PipelineTestServer())
            {
                try
                {
                    var response = server.Execute("set_target_framerate", new { frameRate = 30 });
                    Assert.IsTrue(response.IsSuccess, $"should succeed: {response.Error}");
                    Assert.AreEqual(30, Application.targetFrameRate);
                }
                finally { Application.targetFrameRate = original; }
            }
        }

        #endregion
    }
}
