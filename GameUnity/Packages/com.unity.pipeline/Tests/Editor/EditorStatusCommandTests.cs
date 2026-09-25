using NUnit.Framework;
using Unity.Pipeline.Editor.Commands;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the editor_status command (EditorStatusCommand), exercised directly and via
    /// PipelineClient.
    /// </summary>
    public class EditorStatusCommandTests
    {
        #region Direct

        [Test]
        public void GetEditorStatus_ReturnsValidData()
        {
            var status = EditorStatusCommand.GetEditorStatus();

            Assert.IsNotNull(status);
            Assert.IsNotEmpty(status.Status, "Should report an overall status");
            Assert.AreEqual(Application.unityVersion, status.UnityVersion, "Should report the Unity version");
            Assert.IsNotEmpty(status.ProjectPath, "Should report the project path");
        }

        [Test]
        public void GetEditorStatus_NotPlaying_ReportsStopped()
        {
            // This suite runs in EditMode (not play mode).
            var status = EditorStatusCommand.GetEditorStatus();
            Assert.AreEqual("stopped", status.PlayMode);
        }

        #endregion

        #region ViaClient

        [Test]
        public void EditorStatus_ViaClient_ReturnsValidJson()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("editor_status", null);

                Assert.IsTrue(response.IsSuccess, $"editor_status should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
