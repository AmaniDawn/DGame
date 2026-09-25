using System.IO;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.ServerLifecyle
{
    /// <summary>
    /// Isolates server-lifecycle tests from the live editor pipeline server, which shares some global
    /// state in-process. Call Capture() in [SetUp] and Restore() in [TearDown].
    ///
    /// It records whether a live server was advertised before the test and restores command discovery
    /// (RuntimePipelineManager.Awake switches it to reflection). The main-thread dispatcher is
    /// per-server now, so there is nothing global to swap.
    /// </summary>
    public static class GlobalServerStateGuard
    {
        static string s_DescriptorPath;
        static bool s_HadLiveServer;

        public static void Capture()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            s_DescriptorPath = InstanceDescriptor.GetDescriptorFilePath(projectPath);
            // Key off the live server OBJECT, not the descriptor file: a prior test may have left the
            // live server running while the shared descriptor was already deleted (object/file desync).
            // Keying off the file would then wrongly skip the restart below.
            s_HadLiveServer = PipelineServerStartup.Server != null;
        }

        public static void Restore()
        {
            // A server-lifecycle test starts/stops its own EditorPipelineServer on the live port,
            // which tears down the live listener and deletes the shared `.unity-pipeline-port`
            // descriptor. Rewriting the descriptor file is NOT enough — it would advertise a port
            // nothing listens on (the symptom: menu shows "running" but the server is unreachable).
            // Fully restart the live server so the listener and descriptor are consistent (this also
            // re-arms the watchdog and auto-tick). Mirrors RuntimePipelineServerTests.TearDown.
            try
            {
                if (s_HadLiveServer)
                    PipelineServerStartup.RestartServer();
                else if (s_DescriptorPath != null && File.Exists(s_DescriptorPath))
                    File.Delete(s_DescriptorPath); // No live server before the test — clear any stray descriptor.
            }
            catch { /* best effort */ }

            // RuntimePipelineManager.Awake switches discovery to reflection; restore TypeCache.
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }
    }
}
