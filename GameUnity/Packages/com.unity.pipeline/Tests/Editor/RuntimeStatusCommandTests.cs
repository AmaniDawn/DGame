using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the runtime_status command (RuntimeStatusCommand), exercised both directly
    /// (static call) and through the HTTP server via PipelineClient.
    /// </summary>
    public class RuntimeStatusCommandTests
    {
        #region Direct

        [Test]
        public void GetRuntimeStatus_ReturnsValidData()
        {
            var response = RuntimeStatusCommand.GetRuntimeStatus();

            Assert.IsNotNull(response);
            Assert.IsNotEmpty(response.UnityVersion, "Should have Unity version");
            Assert.IsNotEmpty(response.Platform, "Should have platform info");
            Assert.IsTrue(response.IsPlaying, "runtime_status reports IsPlaying = true");
            Assert.IsNotNull(response.LoadedLevelName, "Should have scene name");
        }

        #endregion

        #region ViaClient

        [Test]
        public void RuntimeStatus_ViaClient_ReturnsValidJson()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("runtime_status", null);

                Assert.IsTrue(response.IsSuccess, $"Command should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
