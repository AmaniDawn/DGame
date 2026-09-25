using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline;
using Unity.Pipeline.Models;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using Unity.Pipeline.Security;
using Unity.Pipeline.Tests.Runtime;
using Unity.Pipeline.Threading;
using UnityEngine;
using UnityEngine.TestTools;
using Newtonsoft.Json;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for CLI instance discovery logic.
    /// These test the file-based discovery mechanism that CLI tools will use to find running Editor instances.
    /// </summary>
    public class InstanceDiscoveryTests
    {
        // Non-null only when this suite started the server itself (no live server was running).
        private EditorPipelineServer m_OwnedServer;
        // Port of the live (preferred) or owned server whose descriptor we test against.
        private int m_ServerPort;
        // Auth token of that server (from the descriptor for a live server, or the session token).
        private string m_ServerToken;
        private string m_TestProjectPath;

        [SetUp]
        public void SetUp()
        {
            // Setup command discovery
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            // Use current project path for testing
            m_TestProjectPath = Path.GetDirectoryName(Application.dataPath);

            // Discovery is a read-only mechanism over the shared .unity-pipeline-port descriptor.
            // PREFER the already-running live server and NEVER disturb it: if a live server is
            // advertising a descriptor, test against that (read-only). Only when none is running do
            // we start our own descriptor-writing server, remembering we own it so TearDown stops
            // ONLY what we started — it must never stop or delete the live server's descriptor.
            var existing = InstanceDescriptor.ReadFromProjectRoot(m_TestProjectPath);
            if (existing != null && existing.Port > 0 && MockCliDiscovery.IsInstanceRunning(existing))
            {
                m_ServerPort = existing.Port;
                m_ServerToken = existing.EvalToken;
            }
            else
            {
                m_OwnedServer = new EditorPipelineServer();
                m_OwnedServer.Start();
                m_ServerPort = m_OwnedServer.Port;
                m_ServerToken = SecurityTokenManager.GetOrCreateToken();
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Never stop the live server — only a server this suite started itself.
            m_OwnedServer?.Stop();
            m_OwnedServer = null;
        }

        [Test]
        public void InstanceDiscovery_ReadDescriptorFile_ParsesCorrectly()
        {
            // Arrange - Server should have created descriptor file during startup
            var descriptorPath = InstanceDescriptor.GetDescriptorFilePath(m_TestProjectPath);

            // Act - Read descriptor using CLI discovery logic
            var instance = MockCliDiscovery.ReadInstanceDescriptor(descriptorPath);

            // Assert - Should successfully parse instance information
            Assert.IsNotNull(instance, "Should be able to read instance descriptor");
            Assert.AreEqual(m_ServerPort, instance.Port, "Port should match running server");
            Assert.Greater(instance.Pid, 0, "Should have valid process ID");
            Assert.AreEqual(m_TestProjectPath, instance.ProjectPath, "Project path should match");
            Assert.IsNotEmpty(instance.ProjectName, "Project name should not be empty");
            Assert.IsNotEmpty(instance.UnityVersion, "Unity version should not be empty");
        }

        [Test]
        public void InstanceDiscovery_ScanDirectory_FindsRunningInstances()
        {
            // Arrange - We have a running server in the test project
            var searchPaths = new[] { m_TestProjectPath };

            // Act - Discover instances using CLI discovery logic
            var instances = MockCliDiscovery.DiscoverInstances(searchPaths);

            // Assert - Should find our test instance
            Assert.Greater(instances.Count(), 0, "Should find at least one running instance");

            var testInstance = instances.FirstOrDefault(i => i.Port == m_ServerPort);
            Assert.IsNotNull(testInstance, "Should find our test server instance");
            Assert.IsTrue(testInstance.IsRunning, "Instance should be marked as running");
        }

        [Test]
        public async Task Server_BasicStatusEndpoint_WorksWithoutEditorProvider()
        {
            // Arrange - Start server WITHOUT EditorStatusProviderImpl (uses fallback)
            var testServer = new TestEditorPipelineServer();
            // Don't set status provider - should use default implementation
            testServer.Start();

            try
            {
                // Give server time to fully start
                await Task.Delay(200);

                // Act - Test status endpoint with default provider
                using (var client = new PipelineClient(testServer))
                {
                    var response = await client.GetAsync("/api/status");

                    // Assert
                    Assert.IsTrue(response.IsSuccess,
                        $"Basic status endpoint should work. Status: {response.StatusCode}, Content: {response.RawResponse}");

                    // Verify it's basic server status (no Editor APIs)
                    Assert.AreEqual("ready", response.JsonResponse["status"]?.ToString(), "Basic status should return 'ready'");
                }
            }
            finally
            {
                testServer?.Stop();
            }
        }

        [Test]
        public async Task Server_BasicCommandsEndpoint_WorksWithoutEditorProvider()
        {
            // Arrange - Start server WITHOUT EditorStatusProviderImpl
            var testServer = new TestEditorPipelineServer();
            testServer.Start();

            try
            {
                // Act - Test commands endpoint
                using (var client = new PipelineClient(testServer))
                {
                    var response = await client.GetAsync("/api/commands");

                    // Assert
                    Assert.IsTrue(response.IsSuccess,
                        $"Basic commands endpoint should work. Status: {response.StatusCode}, Content: {response.RawResponse}");
                }
            }
            finally
            {
                testServer?.Stop();
            }
        }

        [Test]
        public void Server_PortAssignment_WorksCorrectly()
        {
            // Arrange
            var testServer = new TestEditorPipelineServer();

            try
            {
                // Act
                testServer.Start();

                // Assert
                Assert.IsTrue(testServer.IsRunning, "Server should be running");
                Assert.Greater(testServer.Port, 0, "Server should have a valid port");
                Assert.GreaterOrEqual(testServer.Port, 7800, "Port should be in valid range");
                Assert.LessOrEqual(testServer.Port, 7899, "Port should be in valid range");
            }
            finally
            {
                testServer?.Stop();
            }
        }

        [Test]
        public void Dispatcher_BasicOperation_WorksCorrectly()
        {
            // A freshly initialized dispatcher runs Invoke synchronously on the calling (main) thread.
            var dispatcher = new Dispatcher();
            dispatcher.Initialize();
            try
            {
                var result = dispatcher.Invoke(() => "test_result", 1000);
                Assert.AreEqual("test_result", result, "Dispatcher should execute and return result");
            }
            finally
            {
                dispatcher.Shutdown();
            }
        }

        [Test]
        public void EditorStatusCommand_DirectCall_WorksOnMainThread()
        {
            // This test ensures the editor_status command works correctly when called directly from main thread
            // Act - Direct call to the editor_status command (should work since Unity tests run on main thread)
            var status = Unity.Pipeline.Editor.Commands.EditorStatusCommand.GetEditorStatus();

            // Assert
            Assert.IsNotNull(status, "Status should not be null");
            Assert.IsNotNull(status.Status, "Status.Status should not be null");
            Assert.IsNotNull(status.UnityVersion, "Status.UnityVersion should not be null");
            Assert.IsNotNull(status.ProjectPath, "Status.ProjectPath should not be null");
        }

        [Test]
        public void EditorStatusCommand_Discovery_VerifyAvailable()
        {
            // Arrange & Act - Check if editor_status command is discoverable
            var commands = CommandRegistry.DiscoverCommands().ToList();

            // Assert - editor_status command should be found
            var editorStatusCommand = commands.FirstOrDefault(c => c.Name == "editor_status");
            Assert.IsNotNull(editorStatusCommand, "editor_status command should be discoverable");

            // Also verify we can invoke it via reflection (same path as server execution)
            var result = editorStatusCommand.Method.Invoke(null, new object[0]);
            Assert.IsNotNull(result, "Command execution via reflection should return a result");
        }

        [Test]
        public async Task EditorStatusEndpoint_GetDetailedEditorInfo_RespondsCorrectly()
        {
            // Arrange - First verify the command is available
            var commands = CommandRegistry.DiscoverCommands().ToList();
            var editorStatusCommand = commands.FirstOrDefault(c => c.Name == "editor_status");

            if (editorStatusCommand == null)
            {
                Assert.Fail($"editor_status command not found. Available commands: [{string.Join(", ", commands.Select(c => c.Name))}]");
            }

            // Give dispatcher time to initialize
            await Task.Delay(200);

            // Act - Make HTTP request to /api/editor_status (executes command via main thread marshaling)
            using (var client = new PipelineClient($"http://localhost:{m_ServerPort}", m_ServerToken))
            {
                var response = await client.GetAsync("/api/editor_status");

                Assert.IsTrue(response.IsSuccess,
                    $"Editor status endpoint should work. Status: {response.StatusCode}, Content: {response.RawResponse}");

                // Verify it has Editor-specific data (from editor_status command)
                var statusJson = response.JsonResponse;
                Assert.IsNotNull(statusJson["status"], "Should include overall status");
                Assert.IsNotNull(statusJson["compiling"], "Should include compilation state");
                Assert.IsNotNull(statusJson["playMode"], "Should include play mode state");
                Assert.IsNotNull(statusJson["unityVersion"], "Should include Unity version");

                // Verify it's rich Editor data, not basic server data
                Assert.Contains(statusJson["status"]?.ToString(), new[] { "ready", "compiling", "playing", "reloading" });
            }
        }

        [Test]
        public async Task InstanceDiscovery_ValidateConnection_ConfirmsServerReachable()
        {
            // Arrange - Get our test instance
            var descriptorPath = InstanceDescriptor.GetDescriptorFilePath(m_TestProjectPath);
            var instance = MockCliDiscovery.ReadInstanceDescriptor(descriptorPath);
            Assert.IsNotNull(instance, "Should be able to read instance descriptor");

            // Give server a moment to fully start if needed
            await Task.Delay(100);

            // Act - Validate connection to the instance
            var isValid = await MockCliDiscovery.ValidateConnection(instance);

            // Assert - Connection should be valid
            Assert.IsTrue(isValid, "Should be able to connect to running server");
        }

        [Test]
        public void InstanceDiscovery_StaleDescriptor_DetectedAsNotRunning()
        {
            // Arrange - Create a fake descriptor with non-existent PID
            var staleDescriptor = new InstanceDescriptor
            {
                Pid = 99999, // Very unlikely to exist
                Port = 9999,
                ProjectPath = m_TestProjectPath,
                ProjectName = "TestProject",
                UnityVersion = Application.unityVersion,
                Mode = "editor",
                StartedAt = DateTime.UtcNow.AddHours(-1),
                LastHeartbeat = DateTime.UtcNow.AddMinutes(-10)
            };

            // Act - Check if this represents a running instance
            var isRunning = MockCliDiscovery.IsInstanceRunning(staleDescriptor);

            // Assert - Should be detected as not running
            Assert.IsFalse(isRunning, "Stale descriptor should be detected as not running");
        }
    }

    /// <summary>
    /// Mock implementation of CLI discovery logic.
    /// Simulates what the real unity-cli will do for instance discovery and connection validation.
    /// </summary>
    public static class MockCliDiscovery
    {
        /// <summary>
        /// Read instance descriptor from a .unity-pipeline-port file.
        /// Simulates CLI reading discovery files.
        /// </summary>
        public static InstanceDescriptor ReadInstanceDescriptor(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<InstanceDescriptor>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Discover all running Pipeline instances in specified directories.
        /// Simulates CLI scanning for .unity-pipeline-port files.
        /// </summary>
        public static IEnumerable<DiscoveredInstance> DiscoverInstances(IEnumerable<string> searchPaths)
        {
            var instances = new List<DiscoveredInstance>();

            foreach (var path in searchPaths)
            {
                if (!Directory.Exists(path))
                    continue;

                var descriptorPath = InstanceDescriptor.GetDescriptorFilePath(path);
                var descriptor = ReadInstanceDescriptor(descriptorPath);

                if (descriptor != null)
                {
                    instances.Add(new DiscoveredInstance
                    {
                        Descriptor = descriptor,
                        IsRunning = IsInstanceRunning(descriptor),
                        DescriptorPath = descriptorPath
                    });
                }
            }

            return instances;
        }

        /// <summary>
        /// Check if an instance is actually running by validating the PID.
        /// Simulates CLI process validation logic.
        /// </summary>
        public static bool IsInstanceRunning(InstanceDescriptor descriptor)
        {
            if (descriptor?.Pid == null || descriptor.Pid <= 0)
                return false;

            try
            {
                var process = System.Diagnostics.Process.GetProcessById(descriptor.Pid);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate that an instance is reachable via HTTP.
        /// Simulates CLI connection validation using basic /api/status endpoint.
        /// </summary>
        public static async Task<bool> ValidateConnection(InstanceDescriptor instance)
        {
            if (instance?.Port == null || instance.Port <= 0)
                return false;

            try
            {
                using (var client = new PipelineClient($"http://localhost:{instance.Port}", instance.EvalToken))
                {
                    var response = await client.GetAsync("/api/status");
                    return response.IsSuccess && response.JsonResponse?["status"]?.ToString() == "ready";
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Represents a discovered Pipeline instance from CLI perspective.
    /// Contains both the descriptor data and validation status.
    /// </summary>
    public class DiscoveredInstance
    {
        public InstanceDescriptor Descriptor { get; set; }
        public bool IsRunning { get; set; }
        public string DescriptorPath { get; set; }

        // Convenience properties
        public int Port => Descriptor?.Port ?? 0;
        public string ProjectPath => Descriptor?.ProjectPath ?? string.Empty;
        public string ProjectName => Descriptor?.ProjectName ?? string.Empty;
    }
}
