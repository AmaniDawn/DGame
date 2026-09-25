using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;
using Unity.Pipeline.Security;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Thin PlayMode smoke suite for paths that genuinely require a running player:
    /// the autoStart lifecycle (MonoBehaviour Start) and quit (DontDestroyOnLoad, play-mode only).
    /// General command behavior is covered by the EditMode *CommandTests — this only covers
    /// play-mode-only paths and one end-to-end server check.
    /// </summary>
    public class RuntimeServerPlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator AutoStart_StartsServer_AndServesRequest()
        {
            var go = new GameObject("SmokeRuntimeManager");
            var mgr = go.AddComponent<RuntimePipelineManager>();
            mgr.enableInBuilds = true;
            mgr.autoStart = true;

            // Enabling the GameObject runs Start() -> autoStart -> StartServer.
            go.SetActive(true);

            float t = 0f;
            while (!mgr.IsServerRunning && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(mgr.IsServerRunning, "autoStart should start the runtime server");

            var serve = ServeStatus(mgr.ActualPort, SecurityTokenManager.GetOrCreateToken());
            while (!serve.IsCompleted) yield return null;
            Assert.IsTrue(serve.Result, "runtime server should serve a runtime_status request");

            mgr.StopServer();
            Object.Destroy(go);
        }

        static async Task<bool> ServeStatus(int port, string token)
        {
            using (var client = new PipelineClient($"http://localhost:{port}", token))
            {
                var response = await client.ExecuteCommandAsync("runtime_status", null);
                return response.IsSuccess;
            }
        }

        [Test]
        public void Quit_SchedulesQuit()
        {
            // QuitApplication uses DontDestroyOnLoad, which is play-mode only. Application.Quit is a
            // no-op in the editor, but destroy the scheduler anyway so nothing lingers.
            var result = RuntimeApplicationCommand.QuitApplication(0);
            Assert.That(result, Does.Contain("Application quit scheduled with exit code 0"));

            foreach (var go in PipelineUtils.FindObjectsByType<GameObject>())
            {
                if (go.name.Contains("QuitScheduler")) { Object.Destroy(go); break; }
            }
        }
    }
}
