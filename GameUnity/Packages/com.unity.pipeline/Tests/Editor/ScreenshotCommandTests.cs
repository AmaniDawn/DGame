using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the <c>screenshot</c> command (CLI-112). Discovery and validation tests run
    /// everywhere; tests that actually render are skipped under -nographics (no graphics device to
    /// render a RenderTexture), which is common in CI.
    /// </summary>
    public class ScreenshotCommandTests
    {
        const byte k_PngByte0 = 0x89;
        const byte k_PngByte1 = 0x50; // 'P'
        const byte k_PngByte2 = 0x4E; // 'N'
        const byte k_PngByte3 = 0x47; // 'G'

        string m_TempDir;

        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            m_TempDir = Path.Combine(Path.GetTempPath(), "pipeline-screenshot-tests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_TempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TempDir != null && Directory.Exists(m_TempDir))
                Directory.Delete(m_TempDir, recursive: true);
        }

        static bool HasGraphics => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        [Test]
        public void Screenshot_IsDiscovered_WithExpectedSchema()
        {
            var commands = CommandRegistry.DiscoverCommands();
            var cmd = commands.FirstOrDefault(c => c.Name == "screenshot");

            Assert.IsNotNull(cmd, "Should discover the screenshot command");
            Assert.IsTrue(cmd.MainThreadRequired, "screenshot renders Unity APIs and must run on the main thread");

            var paramNames = cmd.Parameters.Select(p => p.Name).ToList();
            CollectionAssert.AreEquivalent(new[] { "view", "output", "width", "height" }, paramNames);

            // All parameters are optional (they have defaults).
            Assert.IsTrue(cmd.Parameters.All(p => !p.Required), "All screenshot parameters should be optional");
        }

        [Test]
        public void Screenshot_InvalidView_ReturnsFailureWithoutRendering()
        {
            // Validation happens before any camera lookup or rendering, so this is graphics-agnostic.
            var result = ScreenshotCommand.CaptureScreenshot(view: "panorama");

            Assert.IsFalse(result.Success, "An unknown view should fail");
            StringAssert.Contains("Invalid view", result.Message);
            Assert.IsNull(result.Path, "No file path should be returned on failure");
        }

        [Test]
        public void Screenshot_NegativeDimensions_ReturnFailure()
        {
            var result = ScreenshotCommand.CaptureScreenshot(view: "game", width: -10, height: 100);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(">= 0", result.Message);
        }

        [Test]
        public void Screenshot_GameView_WritesPngFile()
        {
            if (!HasGraphics)
                Assert.Ignore("No graphics device (running with -nographics); skipping actual render.");

            var go = new GameObject("Pipeline_Test_Camera");
            try
            {
                go.AddComponent<Camera>();
                var outputPath = Path.Combine(m_TempDir, "game.png");

                var result = ScreenshotCommand.CaptureScreenshot(
                    view: "game", output: outputPath, width: 320, height: 240);

                Assert.IsTrue(result.Success, $"Capture should succeed. Message: {result.Message}");
                Assert.AreEqual(outputPath, result.Path);
                Assert.AreEqual(320, result.Width);
                Assert.AreEqual(240, result.Height);
                Assert.IsTrue(File.Exists(outputPath), "PNG file should be written to disk");

                var bytes = File.ReadAllBytes(outputPath);
                Assert.Greater(bytes.Length, 8, "PNG should not be empty");
                Assert.AreEqual(k_PngByte0, bytes[0]);
                Assert.AreEqual(k_PngByte1, bytes[1]);
                Assert.AreEqual(k_PngByte2, bytes[2]);
                Assert.AreEqual(k_PngByte3, bytes[3]);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Screenshot_GameView_WithNoCamera_ReturnsFailure()
        {
            // Only meaningful when there is genuinely no camera in the scene; the editor's default
            // test scene normally has none, but guard so the test is not flaky if one exists.
            if (Camera.main != null || Camera.allCamerasCount > 0)
                Assert.Ignore("A camera is present in the test scene; cannot exercise the no-camera path.");

            var result = ScreenshotCommand.CaptureScreenshot(view: "game");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("No camera", result.Message);
        }

        [Test]
        public void Screenshot_ViaClient_ReturnsPath()
        {
            if (!HasGraphics)
                Assert.Ignore("No graphics device (running with -nographics); skipping actual render.");

            // Use the isolated PipelineTestServer (not the live editor server) so running the suite
            // never disturbs the server agents drive over HTTP. Mirrors CodeEvalCommandTests.
            var go = new GameObject("Pipeline_Test_Camera");
            try
            {
                go.AddComponent<Camera>();
                var outputPath = Path.Combine(m_TempDir, "via-http.png");

                using (var server = new PipelineTestServer())
                {
                    var response = server.Execute("screenshot",
                        new { view = "game", output = outputPath, width = 256, height = 256 });
                    Assert.IsTrue(response.IsSuccess, response.Error);

                    var result = response.GetTypedResponse<ScreenshotResponse>();
                    Assert.IsNotNull(result, "Should deserialize a ScreenshotResponse");
                    Assert.IsTrue(result.Success, result.Message);
                    Assert.AreEqual(outputPath, result.Path);
                    Assert.IsTrue(File.Exists(outputPath), "PNG file should exist on disk after HTTP capture");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
