using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Unity.Pipeline.Models;
using Unity.Pipeline.Tests.Runtime;
using UnityEngine;

// Apply the guard to every test in this (editor test) assembly.
[assembly: Unity.Pipeline.Tests.Editor.LiveServerGuard]

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Assembly-wide guard that runs after EVERY test in this assembly. If a live editor pipeline
    /// server was advertising its descriptor (.unity-pipeline-port) before the test, this asserts
    /// afterwards that the descriptor is still intact (present, same port) and the server is still
    /// listening — failing, and naming, any test that deletes/clobbers the descriptor or kills the
    /// listener. That turns "the whole dogfood run mysteriously dies" into a single named failure.
    ///
    /// Temporary safety net until no test disturbs the live server. Known limitation: it cannot
    /// detect a server whose command dispatcher is dead but whose listener still answers /api/status,
    /// because exec-ing a real (main-thread) command from a teardown would deadlock.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class LiveServerGuardAttribute : Attribute, ITestAction
    {
        private InstanceDescriptor m_Before;

        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test)
        {
            m_Before = InstanceDescriptor.ReadFromProjectRoot(ProjectRoot());
        }

        public void AfterTest(ITest test)
        {
            // Only protect a live server that already existed before this test ran. If none was
            // running (e.g. CI, or a test that legitimately starts/stops its own), there's nothing
            // to protect.
            if (m_Before == null || m_Before.Port <= 0)
                return;

            var after = InstanceDescriptor.ReadFromProjectRoot(ProjectRoot());

            // [Explicit] suites (ServerLifecycle, RuntimePipelineServerTests, …) are excluded from the
            // dogfood loop and knowingly disturb the live server — but they MUST hand it back intact in
            // their TearDown (GlobalServerStateGuard.Restore / PipelineServerStartup.RestartServer).
            // We don't require the descriptor to be byte-identical (a restart may pick a fresh port),
            // but the restore has to leave a live, reachable server. Asserting health here turns a
            // broken self-restore — which otherwise wedges the whole session/dogfood loop with no clue
            // who did it — into a single named failure. (AfterTest runs after [TearDown].)
            if (IsExplicit(test))
            {
                Assert.IsNotNull(after,
                    $"'{test.Name}' ([Explicit]) left no live pipeline server descriptor after TearDown — its self-restore failed.");
                Assert.IsTrue(IsListening(after.Port, after.EvalToken),
                    $"'{test.Name}' ([Explicit]) left the live pipeline server (port {after.Port}) not responding on /api/status after TearDown — its self-restore failed.");
                return;
            }

            // Regular dogfood tests must not disturb the live server at all.
            Assert.IsNotNull(after,
                $"'{test.Name}' deleted the live pipeline server descriptor (.unity-pipeline-port) — it disturbed the global server.");
            Assert.AreEqual(m_Before.Port, after.Port,
                $"'{test.Name}' changed the live pipeline server descriptor port (was {m_Before.Port}, now {after.Port}) — it clobbered the global server.");
            Assert.IsTrue(IsListening(after.Port, after.EvalToken),
                $"'{test.Name}' left the live pipeline server (port {after.Port}) not responding on /api/status — it disturbed the global server.");
        }

        // True if the test or its declaring fixture is [Explicit] (checked at type level too, since
        // Unity doesn't reliably propagate class-level [Explicit] to leaf RunState).
        private static bool IsExplicit(ITest test)
        {
            if (test.RunState == RunState.Explicit)
                return true;
            var type = test.TypeInfo?.Type;
            if (type != null && type.GetCustomAttributes(true).Any(a => a.GetType().Name == "ExplicitAttribute"))
                return true;
            var method = test.Method?.MethodInfo;
            if (method != null && method.GetCustomAttributes(true).Any(a => a.GetType().Name == "ExplicitAttribute"))
                return true;
            return false;
        }

        private static bool IsListening(int port, string token)
        {
            try
            {
                // Run on the threadpool so this (main-thread) teardown can block on it without
                // deadlocking: /api/status is served on the listener thread and needs no dispatcher.
                // Every route now requires the bearer token, so authenticate with the descriptor's token.
                var probe = Task.Run(async () =>
                {
                    using (var client = new PipelineClient($"http://localhost:{port}", token))
                        return await client.GetStatusAsync();
                });
                return probe.Wait(3000) && probe.Result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        private static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);
    }
}
