using UnityEditor;
using UnityEngine;
using Unity.Pipeline.Models;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Threading;
using UnityEditor.MPE;

namespace Unity.Pipeline.Editor
{
    /// <summary>
    /// Automatically starts Pipeline HTTP server when Unity Editor loads.
    /// Handles startup, domain reload persistence, and cleanup.
    ///
    /// Static owner of the live editor server: a static owner survives domain reloads cleanly
    /// (re-created by [InitializeOnLoad]), whereas a ScriptableObject's lifetime does not track the
    /// server across editor events. <see cref="EditorPipelineManager"/> is an optional, inspectable
    /// settings asset whose config is read here at start.
    /// </summary>
    [InitializeOnLoad]
    public static class PipelineServerStartup
    {
        private static EditorPipelineServer m_Server;

        /// <summary>
        /// The live editor pipeline server instance (null when stopped). Exposed so the test guard
        /// can disable its watchdog for a test run and the EditorPipelineManager inspector can read
        /// live status.
        /// </summary>
        public static EditorPipelineServer Server => m_Server;

        static PipelineServerStartup()
        {
            // Don't start server in AssetImportWorker processes
            if (!IsMainProcess())
                return;
            // Setup command discovery using TypeCache for fast Editor performance
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            // Clean up any stale instance descriptor files from previous sessions
            CleanupStaleDescriptors();

            // Start the pipeline server (respecting the settings asset's autoStart if one exists)
            if (EditorPipelineManager.Load()?.AutoStart ?? true)
                StartServer();

            // Handle domain reloads and editor shutdown
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;

            // Handle domain reload detection
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        public static void EnsureServerStarted()
        {
            StartServer();
        }

        /// <summary>
        /// Force a clean restart of the editor pipeline server. Unlike EnsureServerStarted, this
        /// works even when the current server's listener has died but still reports IsRunning
        /// (e.g. after a test disrupted it). Used by tests to revive the live server they disrupted.
        /// </summary>
        public static void RestartServer()
        {
            StopServer();
            StartServer();
        }

        [MenuItem("Pipeline/Start Server")]
        private static void MenuStartServer()
        {
            StartServer();
            if (m_Server != null && m_Server.IsRunning)
                Debug.Log($"Pipeline Server started on port {m_Server.Port}");
            else
                Debug.LogWarning("Pipeline Server failed to start");
        }

        [MenuItem("Pipeline/Start Server", true)]
        private static bool MenuStartServerValidate() => m_Server == null || !m_Server.IsRunning;

        [MenuItem("Pipeline/Stop Server")]
        private static void MenuStopServer()
        {
            StopServer();
        }

        [MenuItem("Pipeline/Stop Server", true)]
        private static bool MenuStopServerValidate() => m_Server != null && m_Server.IsRunning;

        /// <summary>
        /// Select the EditorPipelineManager settings asset, creating it under Assets/Settings/Pipeline
        /// on first use (the live server otherwise runs from built-in defaults).
        /// </summary>
        [MenuItem("Pipeline/Settings...")]
        private static void OpenSettings()
        {
            var mgr = EditorPipelineManager.Load();
            if (mgr == null)
            {
                const string folder = "Assets/Settings/Pipeline";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                        AssetDatabase.CreateFolder("Assets", "Settings");
                    AssetDatabase.CreateFolder("Assets/Settings", "Pipeline");
                }

                mgr = ScriptableObject.CreateInstance<EditorPipelineManager>();
                AssetDatabase.CreateAsset(mgr, folder + "/EditorPipelineManager.asset");
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = mgr;
            EditorGUIUtility.PingObject(mgr);
        }

        internal static bool IsMainProcess()
        {
            // Check command line arguments for asset import worker indicators
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-readonly" || args[i] == "--virtual-project-clone")
                    return true;
            }

            if (AssetDatabase.IsAssetImportWorkerProcess())
                return false;

            if (ProcessService.level != ProcessLevel.Main)
                return false;

            return true;
        }

        /// <summary>
        /// Start the Pipeline HTTP server, reading configuration from the EditorPipelineManager
        /// settings asset if one exists (otherwise using defaults).
        /// </summary>
        private static void StartServer()
        {
            if (m_Server != null && m_Server.IsRunning)
                return;

            try
            {
                var cfg = EditorPipelineManager.Load();
                m_Server = new EditorPipelineServer
                {
                    // Self-healing watchdog: if the listener dies without a Stop() (an unexpected fault
                    // outside a domain reload), the watchdog re-opens it so the dogfood loop doesn't
                    // wedge. Set before Start() (Start arms the watchdog).
                    WatchdogEnabled = cfg?.WatchdogEnabled ?? true,
                    WatchdogIntervalSeconds = cfg?.WatchdogIntervalSeconds ?? 5,
                    LogRequestsResponses = cfg?.LogRequestsResponses ?? false
                };

                // Rotate the transaction log once per Unity session (main thread; SessionState-gated).
                // The append path runs off-thread and can't touch SessionState.
                PipelineTransactionLog.RotateForNewSession();

                m_Server.Start(cfg?.Port ?? 0); // 0 auto-assigns from the 7800-7849 range.

                if (m_Server.WatchdogEnabled)
                {
                    // The watchdog rides EditorApplication.update, but a backgrounded/idle editor stops
                    // ticking once the listener dies (no requests left to wake it) — the exact moment the
                    // watchdog must run. Keep auto-tick on so the update loop keeps spinning regardless of
                    // focus, which keeps both the watchdog AND the dispatcher message pump alive.
                    Commands.AutoTickCommand.SetAutoTick(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start Pipeline Server: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the Pipeline HTTP server.
        /// </summary>
        public static void StopServer()
        {
            if (m_Server != null)
            {
                try
                {
                    m_Server.Stop();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error stopping Pipeline Server: {ex.Message}");
                }
                finally
                {
                    m_Server = null;
                }
            }
        }

        /// <summary>
        /// Clean up stale instance descriptor files from previous Editor sessions.
        /// </summary>
        private static void CleanupStaleDescriptors()
        {
            var projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

            // Try to read existing descriptor
            var existing = InstanceDescriptor.ReadFromProjectRoot(projectPath);
            if (existing != null)
            {
                // Check if process is still running
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(existing.Pid);
                    if (process.HasExited)
                    {
                        // Process is dead, remove stale file
                        InstanceDescriptor.RemoveFromProjectRoot(projectPath);
                    }
                }
                catch
                {
                    // Process doesn't exist or access denied, remove stale file
                    InstanceDescriptor.RemoveFromProjectRoot(projectPath);
                }
            }
        }

        /// <summary>
        /// Handle play mode state changes.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Server continues running through play mode changes
            // Status endpoint will reflect current play mode via EditorApplication.isPlaying
        }

        /// <summary>
        /// Handle Editor shutdown.
        /// </summary>
        private static void OnEditorQuitting()
        {
            StopServer(); // Stop() shuts down the server's own dispatcher.
        }

        /// <summary>
        /// Handle before assembly reload (domain reload).
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            // Server will be automatically recreated after reload due to [InitializeOnLoad]
            // Instance descriptor file will be cleaned up and recreated
        }

        /// <summary>
        /// Handle after assembly reload (domain reload).
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            // Server should already be restarted via [InitializeOnLoad]
            // This is mainly for logging/verification
        }
    }
}
