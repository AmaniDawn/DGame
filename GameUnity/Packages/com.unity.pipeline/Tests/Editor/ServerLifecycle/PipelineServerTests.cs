using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using Unity.Pipeline.Tests.Runtime;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.ServerLifecyle
{
    /// <summary>
    /// Tests for PipelineServer lifecycle and basic HTTP functionality.
    /// These test the public interface behaviors that CLI tools will rely on.
    ///
    /// Marked [Explicit]: they start/stop a real EditorPipelineServer and manage the shared
    /// `.unity-pipeline-port` descriptor, which conflicts with the live editor server that exists
    /// in every editor session. They are excluded from the normal/dogfood run and meant to be run
    /// deliberately (e.g. from the Test Runner window). See [[GlobalServerStateGuard]].
    /// </summary>
    [Explicit("Server-lifecycle test; conflicts with the live editor server. Run deliberately.")]
    [Category("ServerLifecycle")]
    public class PipelineServerTests
    {
        // These tests start/stop their own EditorPipelineServer, which writes/deletes the shared
        // `.unity-pipeline-port` descriptor and would corrupt the live editor server. Guard it.
        [SetUp]
        public void SetUp() => GlobalServerStateGuard.Capture();

        [TearDown]
        public void TearDown() => GlobalServerStateGuard.Restore();

        [Test]
        public void ServerLifecycle_StartAndStop_ManagesRunningState()
        {
            // Arrange
            var server = new EditorPipelineServer();

            // Assert initial state
            Assert.IsFalse(server.IsRunning, "Server should not be running initially");
            Assert.AreEqual(0, server.Port, "Port should be 0 when not running");

            // Act - Start server
            server.Start();

            // Assert - Started state
            Assert.IsTrue(server.IsRunning, "Server should be running after Start()");
            Assert.Greater(server.Port, 0, "Port should be assigned after Start()");
            Assert.GreaterOrEqual(server.Port, 7800, "Port should be in range 7800-7899");
            Assert.LessOrEqual(server.Port, 7899, "Port should be in range 7800-7899");

            // Act - Stop server
            server.Stop();

            // Assert - Stopped state
            Assert.IsFalse(server.IsRunning, "Server should not be running after Stop()");
            // Note: Port may retain value after stop for diagnostic purposes
        }

        [Test]
        public async Task StatusEndpoint_GetApiStatus_ReturnsBasicServerInfo()
        {
            // Arrange
            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Act - Call /api/status endpoint (basic server health)
                    var response = await client.GetHttpAsync("/api/status");
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    // Assert - Response structure
                    Assert.IsTrue(response.IsSuccessStatusCode,
                        $"Basic status endpoint should return success, got: {response.StatusCode}");
                    Assert.AreEqual("application/json", response.Content.Headers.ContentType.MediaType,
                        "Status endpoint should return JSON content type");

                    // Assert - Basic server status JSON structure (not StatusResponse)
                    var statusJson = JObject.Parse(jsonContent);
                    Assert.IsNotNull(statusJson, "Should be able to parse basic status JSON");

                    // Verify basic server fields
                    Assert.AreEqual("ready", statusJson["status"]?.ToString(), "Server status should be ready");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public void InstanceDescriptor_ServerStartStop_ManagesDescriptorFile()
        {
            // Arrange
            var server = new EditorPipelineServer();
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var descriptorFilePath = InstanceDescriptor.GetDescriptorFilePath(projectPath);

            // Ensure no existing descriptor file
            if (File.Exists(descriptorFilePath))
            {
                File.Delete(descriptorFilePath);
            }

            // Act - Start server
            server.Start();

            // Assert - Descriptor file created
            Assert.IsTrue(File.Exists(descriptorFilePath),
                "Instance descriptor file should be created when server starts");

            // Read and verify descriptor content
            var descriptor = InstanceDescriptor.ReadFromProjectRoot(projectPath);
            Assert.IsNotNull(descriptor, "Should be able to read descriptor file");
            Assert.AreEqual(server.Port, descriptor.Port, "Descriptor should contain correct port");
            Assert.Greater(descriptor.Pid, 0, "Descriptor should contain valid PID");
            Assert.IsNotEmpty(descriptor.ProjectPath, "Descriptor should contain project path");

            // Act - Stop server
            server.Stop();

            // Assert - Descriptor file removed
            Assert.IsFalse(File.Exists(descriptorFilePath),
                "Instance descriptor file should be removed when server stops");
        }
    }
}