using NUnit.Framework;
using Unity.Pipeline.Config;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for RuntimePipelineServer port range separation and lifecycle.
    /// Validates that runtime servers use 7900-7999 range (different from Editor's 7800-7899).
    /// </summary>
    /// <remarks>
    /// Excluded from the default (dogfood) run: starts real Editor/Runtime servers in the production
    /// port ranges (and inspects descriptor-derived StartedAt), which would bind/clobber the live
    /// server. Run deliberately from the Test Runner window.
    /// </remarks>
    [Explicit("Starts real servers in the production port ranges; would disturb the live server. Run manually from the Test Runner window.")]
    [Category("ServerLifecycle")]
    public class RuntimePipelineServerTests
    {
        private RuntimePipelineConfig m_TestConfig;
        private RuntimePipelineServer m_RuntimeServer;
        private EditorPipelineServer m_EditorServer;

        // Whether the live editor pipeline server was advertising before this test, so TearDown can
        // restore it. Starting the test EditorPipelineServer overwrites the shared descriptor and
        // stopping it deletes the descriptor, which breaks discovery of the live server.
        private bool m_GlobalServerWasRunning;

        [SetUp]
        public void SetUp()
        {
            // Remember whether a live server was running so TearDown can restore it (and so we never
            // start one that wasn't there, e.g. in CI).
            m_GlobalServerWasRunning =
                InstanceDescriptor.ReadFromProjectRoot(System.IO.Path.GetDirectoryName(Application.dataPath)) != null;

            // Create test configuration
            m_TestConfig = ScriptableObject.CreateInstance<RuntimePipelineConfig>();
            m_TestConfig.enableInBuilds = true;

            // Real servers: this suite validates the PRODUCTION port ranges (editor 7800-7849,
            // runtime 7900-7949) and descriptor-derived behavior (StartedAt), which the no-descriptor
            // test servers can't provide. It therefore starts real servers and is [Explicit] (see the
            // class attribute) so it never runs in the dogfood loop against the live server.
            m_RuntimeServer = new RuntimePipelineServer(m_TestConfig);
            m_EditorServer = new EditorPipelineServer();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up servers
            if (m_RuntimeServer != null && m_RuntimeServer.IsRunning)
            {
                m_RuntimeServer.Stop();
            }

            if (m_EditorServer != null && m_EditorServer.IsRunning)
            {
                m_EditorServer.Stop();
            }

            // Clean up config
            if (m_TestConfig != null)
            {
                Object.DestroyImmediate(m_TestConfig);
            }

            // Restore the live editor server's descriptor that the test EditorPipelineServer
            // overwrote/deleted, so discovery works again after this suite. Only if it was running
            // before (don't start one that wasn't there).
            if (m_GlobalServerWasRunning)
            {
                PipelineServerStartup.RestartServer();
            }
        }

        [Test]
        public void RuntimeServer_PortRange_UsesDifferentRangeFromEditor()
        {
            // Act - Start runtime server
            m_RuntimeServer.Start();

            // Assert - Runtime should use 7900-7999 range
            if (m_RuntimeServer.IsRunning)
            {
                Assert.GreaterOrEqual(m_RuntimeServer.Port, 7900, "Runtime server should use port >= 7900");
                Assert.LessOrEqual(m_RuntimeServer.Port, 7999, "Runtime server should use port <= 7999");
            }
        }

        [Test]
        public void EditorServer_PortRange_UsesEditorRange()
        {
            // Act - Start editor server
            m_EditorServer.Start();

            // Assert - Editor should use 7800-7899 range
            if (m_EditorServer.IsRunning)
            {
                Assert.GreaterOrEqual(m_EditorServer.Port, 7800, "Editor server should use port >= 7800");
                Assert.LessOrEqual(m_EditorServer.Port, 7899, "Editor server should use port <= 7899");
            }
        }

        [Test]
        public void DualServers_StartBoth_UseNonConflictingPorts()
        {
            // Act - Start both servers
            m_EditorServer.Start();
            m_RuntimeServer.Start();

            // Assert - Both should start successfully with different ports
            Assert.IsTrue(m_EditorServer.IsRunning, "Editor server should be running");
            Assert.IsTrue(m_RuntimeServer.IsRunning, "Runtime server should be running");

            // Verify port ranges
            Assert.GreaterOrEqual(m_EditorServer.Port, 7800, "Editor server should use Editor range");
            Assert.LessOrEqual(m_EditorServer.Port, 7899, "Editor server should use Editor range");

            Assert.GreaterOrEqual(m_RuntimeServer.Port, 7900, "Runtime server should use Runtime range");
            Assert.LessOrEqual(m_RuntimeServer.Port, 7999, "Runtime server should use Runtime range");

            // Verify no port conflicts
            Assert.AreNotEqual(m_EditorServer.Port, m_RuntimeServer.Port, "Servers should use different ports");
        }

        [Test]
        public void RuntimeServer_AutoPortAssignment_FindsAvailablePort()
        {
            // Act - Start with auto port assignment (0)
            m_RuntimeServer.Start(0);

            // Assert
            if (m_RuntimeServer.IsRunning)
            {
                Assert.Greater(m_RuntimeServer.Port, 0, "Should assign a valid port");
                Assert.GreaterOrEqual(m_RuntimeServer.Port, 7900, "Should use Runtime port range");
                Assert.LessOrEqual(m_RuntimeServer.Port, 7999, "Should use Runtime port range");
            }
        }

        [Test]
        public void RuntimeServer_SpecificPort_UsesRequestedPortInRange()
        {
            // Arrange - Choose a specific port in the runtime range
            const int requestedPort = 7950;

            // Act
            m_RuntimeServer.Start(requestedPort);

            // Assert
            if (m_RuntimeServer.IsRunning)
            {
                Assert.AreEqual(requestedPort, m_RuntimeServer.Port, "Should use requested port if available");
            }
        }

        [Test]
        public void RuntimeServer_Lifecycle_CanStartAndStopMultipleTimes()
        {
            // Test multiple start/stop cycles
            for (int i = 0; i < 3; i++)
            {
                // Start
                m_RuntimeServer.Start();
                Assert.IsTrue(m_RuntimeServer.IsRunning, $"Server should start on cycle {i + 1}");

                var port = m_RuntimeServer.Port;
                Assert.GreaterOrEqual(port, 7900, $"Should use Runtime range on cycle {i + 1}");
                Assert.LessOrEqual(port, 7999, $"Should use Runtime range on cycle {i + 1}");

                // Stop
                m_RuntimeServer.Stop();
                Assert.IsFalse(m_RuntimeServer.IsRunning, $"Server should stop on cycle {i + 1}");
            }
        }

        [Test]
        public void RuntimeServer_Configuration_ValidatesSecuritySettings()
        {
            // Test that the server works with the same validation as RuntimePipelineManager
            var validation = m_TestConfig.Validate();
            Assert.IsTrue(validation.IsValid, $"Test configuration should be valid: {validation.Message}");

            // Server should start with valid config
            m_RuntimeServer.Start();
            Assert.IsTrue(m_RuntimeServer.IsRunning, "Server should start with valid configuration");
        }

        [Test]
        public void RuntimeServer_StartedAt_TracksStartTime()
        {
            // Arrange
            var beforeStart = System.DateTime.UtcNow;

            // Act
            m_RuntimeServer.Start();

            var afterStart = System.DateTime.UtcNow;

            // Assert
            if (m_RuntimeServer.IsRunning)
            {
                var startedAt = m_RuntimeServer.StartedAt;
                Assert.GreaterOrEqual(startedAt, beforeStart, "StartedAt should be >= before start time");
                Assert.LessOrEqual(startedAt, afterStart, "StartedAt should be <= after start time");
            }
        }

        [Test]
        public void RuntimeServer_PortAvailabilityCheck_HandlesPortConflicts()
        {
            // Arrange - Start editor server on a specific port first to create potential conflict
            m_EditorServer.Start();

            // Act - Start runtime server (should automatically find different port)
            m_RuntimeServer.Start();

            // Assert - Both should be running on different ports
            if (m_EditorServer.IsRunning && m_RuntimeServer.IsRunning)
            {
                Assert.AreNotEqual(m_EditorServer.Port, m_RuntimeServer.Port, "Should avoid port conflicts");

                // Runtime should still be in its designated range
                Assert.GreaterOrEqual(m_RuntimeServer.Port, 7900, "Runtime should stay in its range even when avoiding conflicts");
                Assert.LessOrEqual(m_RuntimeServer.Port, 7999, "Runtime should stay in its range even when avoiding conflicts");
            }
        }
    }
}