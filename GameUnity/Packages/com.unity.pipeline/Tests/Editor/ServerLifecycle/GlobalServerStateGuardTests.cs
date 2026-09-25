using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using Unity.Pipeline.Tests.Runtime;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.ServerLifecyle
{
    /// <summary>
    /// Regression test for the full-suite run leaving the live editor server unreachable.
    ///
    /// <see cref="GlobalServerStateGuard.Restore"/> is the safety net that server-lifecycle tests
    /// rely on (in [TearDown]) to hand the live editor server back intact. Its contract is not "the
    /// descriptor file is present" but "the live server is actually running and reachable". A guard
    /// that only rewrites the descriptor file leaves it advertising a port nothing listens on —
    /// which is exactly how the suite used to finish with the server "running" in the menu but
    /// unreachable.
    /// </summary>
    [Explicit("Manipulates the live editor server; run deliberately (e.g. from the Test Runner window).")]
    [Category("ServerLifecycle")]
    public class GlobalServerStateGuardTests
    {
        [TearDown]
        public void TearDown()
        {
            // Safety net: never leave the live server down for the next test/dogfood command, even if
            // an assertion above failed before the guard could restore it.
            PipelineServerStartup.EnsureServerStarted();
        }

        [Test]
        public async Task Restore_AfterLiveListenerTornDown_LeavesServerReachable()
        {
            // Arrange: a live server is running and advertised (as in every interactive editor).
            PipelineServerStartup.EnsureServerStarted();
            Assert.IsNotNull(PipelineServerStartup.Server, "Precondition: a live server should exist");
            Assert.IsTrue(PipelineServerStartup.Server.IsRunning, "Precondition: live server should be running");

            GlobalServerStateGuard.Capture();

            // Simulate a server-lifecycle test that tears down the live listener and deletes the
            // shared descriptor (what new EditorPipelineServer().Start()/Stop() does on the live port).
            PipelineServerStartup.StopServer();
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            Assert.IsNull(InstanceDescriptor.ReadFromProjectRoot(projectRoot),
                "Sanity: descriptor should be gone after the live server stopped");

            // Act
            GlobalServerStateGuard.Restore();

            // Assert: the guard must leave a live, reachable server — not just a descriptor file.
            var server = PipelineServerStartup.Server;
            Assert.IsNotNull(server, "Restore() must leave a live server running");
            Assert.IsTrue(server.IsRunning, "Restore() must leave the live server running");

            var descriptor = InstanceDescriptor.ReadFromProjectRoot(projectRoot);
            Assert.IsNotNull(descriptor, "Restore() must leave a discovery descriptor");
            Assert.AreEqual(server.Port, descriptor.Port,
                "Descriptor port must match the running server's port");

            using (var client = new PipelineClient($"http://localhost:{descriptor.Port}", descriptor.EvalToken))
            {
                var status = await client.GetStatusAsync();
                Assert.IsTrue(status.IsSuccess,
                    "Live server must answer /api/status after Restore() — the descriptor must point at a live listener");
            }
        }
    }
}
