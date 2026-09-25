using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor
{
    /// <summary>
    /// Inspectable configuration and control surface for the live editor pipeline server. This is a
    /// settings asset, NOT the server's owner — <see cref="PipelineServerStartup"/> owns the server
    /// instance (a static owner survives domain reloads cleanly, whereas a ScriptableObject's
    /// lifetime does not track the server across editor events). The owner reads this asset's config
    /// when starting; the custom inspector drives Start/Stop through the owner and shows live status.
    ///
    /// The asset is optional: without it the owner uses the defaults below. "Pipeline/Settings"
    /// creates it on demand.
    /// </summary>
    public class EditorPipelineManager : ScriptableObject
    {
        [Tooltip("HTTP port for the editor server. 0 = auto-assign from the 7800-7849 range. Applies on next start.")]
        [SerializeField, Range(0, 65535)] private int m_Port = 0;

        [Tooltip("Start the server automatically when the editor loads. Applies on next editor load.")]
        [SerializeField] private bool m_AutoStart = true;

        [Tooltip("Self-heal: if the HTTP listener dies without a Stop(), re-open it on a timer. " +
                 "Keeps auto-tick on so the editor keeps ticking while unfocused (required for the watchdog).")]
        [SerializeField] private bool m_WatchdogEnabled = true;

        [Tooltip("How often the watchdog checks the listener, in seconds.")]
        [SerializeField, Range(1, 60)] private int m_WatchdogIntervalSeconds = 5;

        [Tooltip("Log every command request/response (raw JSON) handled by the editor server to " +
                 "<project>/Logs/pipeline.log. Editor only; applies live.")]
        [SerializeField] private bool m_LogRequestsResponses = false;

        public int Port => m_Port;
        public bool AutoStart => m_AutoStart;
        public bool WatchdogEnabled => m_WatchdogEnabled;
        public int WatchdogIntervalSeconds => m_WatchdogIntervalSeconds;
        public bool LogRequestsResponses => m_LogRequestsResponses;

        /// <summary>Whether the live server (owned by PipelineServerStartup) is actually running.</summary>
        public bool IsServerRunning => PipelineServerStartup.Server != null && PipelineServerStartup.Server.IsRunning;

        /// <summary>The port the live server is actually listening on, or 0 when stopped.</summary>
        public int ActualPort => PipelineServerStartup.Server?.Port ?? 0;

        /// <summary>Start the live server (delegates to the static owner, which reads this config).</summary>
        public void StartServer() => PipelineServerStartup.EnsureServerStarted();

        /// <summary>Stop the live server.</summary>
        public void StopServer() => PipelineServerStartup.StopServer();

        /// <summary>Restart the live server.</summary>
        public void RestartServer() => PipelineServerStartup.RestartServer();

        /// <summary>
        /// Load the single settings asset if one exists, otherwise null (the owner falls back to
        /// defaults). No caching — assets are cheap to look up and this is only read at start/inspect.
        /// </summary>
        public static EditorPipelineManager Load()
        {
            var guids = AssetDatabase.FindAssets("t:EditorPipelineManager");
            if (guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<EditorPipelineManager>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
