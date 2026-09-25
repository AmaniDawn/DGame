using System;
using NUnit.Framework;
using Unity.Pipeline.Editor.Commands.Capture;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Pipeline.Tests.Editor.Capture
{
    /// <summary>
    /// Tests for the visual-feedback commands (CLI-199), exercised directly and via PipelineClient.
    /// Render tests are GPU-gated: under batchmode/headless the graphics device is
    /// <see cref="GraphicsDeviceType.Null"/> and the tests self-ignore rather than fail.
    /// </summary>
    public class CaptureCommandsTests
    {
        private const string CameraName = "CLI199_Cam";

        // PNG file signature: 0x89 'P' 'N' 'G' 0x0D 0x0A 0x1A 0x0A.
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private GameObject m_CameraObject;

        [SetUp]
        public void SetUp()
        {
            m_CameraObject = new GameObject(CameraName);
            m_CameraObject.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_CameraObject != null)
                UnityEngine.Object.DestroyImmediate(m_CameraObject);
        }

        private static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private static void AssertPngSignature(byte[] bytes)
        {
            Assert.GreaterOrEqual(bytes.Length, PngSignature.Length, "PNG payload too short to contain a signature");
            for (var i = 0; i < PngSignature.Length; i++)
                Assert.AreEqual(PngSignature[i], bytes[i], $"PNG signature mismatch at byte {i}");
        }

        #region Direct

        [Test]
        public void CaptureGameView_NamedCamera_ReturnsPng()
        {
            if (IsHeadless)
                Assert.Ignore("No GPU in batchmode");

            var result = CaptureCommands.CaptureGameView(64, 64, CameraName);

            Assert.IsNotNull(result, "Capture should return a result");
            Assert.IsNotEmpty(result.Base64, "Base64 payload should be non-empty");
            Assert.AreEqual(64, result.Width, "Width should match the requested size");
            Assert.AreEqual(64, result.Height, "Height should match the requested size");
            Assert.AreEqual("png", result.Encoding);
            Assert.AreEqual($"camera:{CameraName}", result.Source);
            Assert.IsNull(result.SavedPath, "No savePath was requested");

            var bytes = Convert.FromBase64String(result.Base64);
            AssertPngSignature(bytes);
            Assert.AreEqual(bytes.Length, result.Bytes, "Reported byte length should match decoded payload");
        }

        [Test]
        public void CaptureSceneView_ReturnsPng()
        {
            if (IsHeadless)
                Assert.Ignore("No GPU in batchmode");

            var sv = SceneView.lastActiveSceneView;
            if (sv == null)
                Assert.Ignore("No Scene View");

            var result = CaptureCommands.CaptureSceneView(64, 64);

            Assert.IsNotNull(result, "Capture should return a result");
            Assert.IsNotEmpty(result.Base64, "Base64 payload should be non-empty");
            Assert.AreEqual("sceneView", result.Source);

            var bytes = Convert.FromBase64String(result.Base64);
            AssertPngSignature(bytes);
        }

        #endregion

        #region ViaClient

        [Test]
        public void CaptureGameView_ViaClient_Succeeds()
        {
            if (IsHeadless)
                Assert.Ignore("No GPU in batchmode");

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("capture_game_view", new { width = 64, height = 64, camera = CameraName });

                Assert.IsTrue(response.IsSuccess, $"capture_game_view should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
