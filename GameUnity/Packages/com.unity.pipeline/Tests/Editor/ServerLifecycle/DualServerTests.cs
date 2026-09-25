using System.Collections;
using NUnit.Framework;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Tests.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor.ServerLifecyle
{
    /// <summary>
    /// Tests for simultaneous operation of Editor and Runtime servers.
    /// Validates port range separation and dual server functionality.
    ///
    /// Marked [Explicit]: starts a real EditorPipelineServer + RuntimePipelineManager, which
    /// conflict with the live editor server present in every editor session. Excluded from the
    /// normal/dogfood run; run deliberately (e.g. Test Runner window). See [[GlobalServerStateGuard]].
    /// </summary>
    [Explicit("Server-lifecycle test; conflicts with the live editor server. Run deliberately.")]
    [Category("ServerLifecycle")]
    public class DualServerTests
    {
        private GameObject m_TestGameObject;
        private RuntimePipelineManager m_RuntimeManager;
        private EditorPipelineServer m_EditorServer;

        [SetUp]
        public void SetUp()
        {
            // RuntimePipelineManager + EditorPipelineServer here mutate global state shared with
            // the live editor server (descriptor file, command discovery).
            GlobalServerStateGuard.Capture();

            // Create test GameObject with RuntimePipelineManager
            m_TestGameObject = new GameObject("TestRuntimePipelineManager");
            m_RuntimeManager = m_TestGameObject.AddComponent<RuntimePipelineManager>();

            // Configure for testing
            m_RuntimeManager.enableInBuilds = true;

            // Create Editor server for dual-server tests
            m_EditorServer = new EditorPipelineServer();
        }

        [TearDown]
        public void TearDown()
        {
            // Stop servers
            if (m_RuntimeManager != null && m_RuntimeManager.IsServerRunning)
            {
                m_RuntimeManager.StopServer();
            }

            if (m_EditorServer != null && m_EditorServer.IsRunning)
            {
                m_EditorServer.Stop();
            }

            // Clean up GameObjects
            if (m_TestGameObject != null)
            {
                Object.DestroyImmediate(m_TestGameObject);
            }

            // Restore global state the servers/manager mutated (runs last, after our own cleanup).
            GlobalServerStateGuard.Restore();
        }

        [UnityTest]
        public IEnumerator DualServers_EditorAndRuntime_UsesDifferentPortRanges()
        {
            // Act - Start Editor server first
            m_EditorServer.Start();
            yield return null;

            // Start Runtime server
            m_RuntimeManager.StartServer();
            yield return null;

            // Assert - Both servers should be running on different port ranges
            Assert.IsTrue(m_EditorServer.IsRunning, "Editor server should be running");
            Assert.IsTrue(m_RuntimeManager.IsServerRunning, "Runtime server should be running");

            // Editor should use 7800-7899
            Assert.GreaterOrEqual(m_EditorServer.Port, 7800, "Editor server should use port >= 7800");
            Assert.LessOrEqual(m_EditorServer.Port, 7899, "Editor server should use port <= 7899");

            // Runtime should use 7900-7999
            Assert.GreaterOrEqual(m_RuntimeManager.ActualPort, 7900, "Runtime server should use port >= 7900");
            Assert.LessOrEqual(m_RuntimeManager.ActualPort, 7999, "Runtime server should use port <= 7999");

            // Ports should not conflict
            Assert.AreNotEqual(m_EditorServer.Port, m_RuntimeManager.ActualPort, "Editor and Runtime servers should use different ports");
        }

        [UnityTest]
        public IEnumerator DualServers_BothStatusEndpoints_ReturnValidResponses()
        {
            // Arrange - Start both servers
            m_EditorServer.Start();
            m_RuntimeManager.StartServer();
            yield return null;

            if (!m_EditorServer.IsRunning || !m_RuntimeManager.IsServerRunning)
            {
                Assert.Fail("Both servers should be running for this test");
                yield break;
            }

            // Test Editor server status endpoint
            PipelineResponse editorResponse = null;
            using (var editorClient = new PipelineClient(m_EditorServer))
                yield return editorClient.GetAsync("/api/status", r => editorResponse = r);

            Assert.IsTrue(editorResponse.IsSuccess, $"Editor status endpoint should work: {editorResponse.Error}");
            Assert.IsNotEmpty(editorResponse.RawResponse, "Editor status response should not be empty");

            // Test Runtime server status endpoint
            PipelineResponse runtimeResponse = null;
            using (var runtimeClient = new PipelineClient(m_RuntimeManager))
                yield return runtimeClient.GetAsync("/api/status", r => runtimeResponse = r);

            Assert.IsTrue(runtimeResponse.IsSuccess, $"Runtime status endpoint should work: {runtimeResponse.Error}");
            Assert.IsNotEmpty(runtimeResponse.RawResponse, "Runtime status response should not be empty");
        }

        [UnityTest]
        public IEnumerator DualServers_Authentication_EachRequiresOwnToken()
        {
            // Arrange - Start both servers
            m_EditorServer.Start();
            m_RuntimeManager.StartServer();
            yield return null;

            if (!m_EditorServer.IsRunning || !m_RuntimeManager.IsServerRunning)
            {
                Assert.Fail("Both servers should be running for this test");
                yield break;
            }

            // The runtime client authenticates with the server's own token; a request should be
            // accepted (not 401). (Rejection of missing/invalid tokens is covered by ServerAuthTests.)
            PipelineResponse runtimeResponse = null;
            using (var runtimeClient = new PipelineClient(m_RuntimeManager))
                yield return runtimeClient.GetAsync("/api/status", r => runtimeResponse = r);

            Assert.AreNotEqual(401, runtimeResponse.StatusCode, "Runtime server should accept its own token");
        }

        [UnityTest]
        public IEnumerator DualServers_StartStopSequence_WorksCorrectly()
        {
            // Test various start/stop sequences to ensure no interference

            // Sequence 1: Start Editor first
            m_EditorServer.Start();
            yield return new WaitForSeconds(0.1f);
            m_RuntimeManager.StartServer();
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(m_EditorServer.IsRunning, "Editor should be running");
            Assert.IsTrue(m_RuntimeManager.IsServerRunning, "Runtime should be running");

            // Stop both
            m_EditorServer.Stop();
            m_RuntimeManager.StopServer();
            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(m_EditorServer.IsRunning, "Editor should be stopped");
            Assert.IsFalse(m_RuntimeManager.IsServerRunning, "Runtime should be stopped");

            // Sequence 2: Start Runtime first
            m_RuntimeManager.StartServer();
            yield return new WaitForSeconds(0.1f);
            m_EditorServer.Start();
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(m_EditorServer.IsRunning, "Editor should be running");
            Assert.IsTrue(m_RuntimeManager.IsServerRunning, "Runtime should be running");

            // Should still use correct port ranges regardless of start order
            Assert.GreaterOrEqual(m_EditorServer.Port, 7800, "Editor should use Editor range");
            Assert.LessOrEqual(m_EditorServer.Port, 7899, "Editor should use Editor range");
            Assert.GreaterOrEqual(m_RuntimeManager.ActualPort, 7900, "Runtime should use Runtime range");
            Assert.LessOrEqual(m_RuntimeManager.ActualPort, 7999, "Runtime should use Runtime range");
        }
    }
}