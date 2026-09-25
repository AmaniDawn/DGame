using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Pipeline.Config;
using Unity.Pipeline.Commands;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Runtime.Telemetry;
using UnityEngine;

namespace Unity.Pipeline
{
    /// <summary>
    /// Unified Pipeline runtime manager that handles both server creation and event processing.
    /// Replaces the need for separate RuntimePipelineConfig and RuntimePipelineProcessor.
    ///
    /// Simply add this component to a GameObject in your scene to enable Pipeline runtime support.
    /// Only one instance should exist in your project.
    /// </summary>
    [AddComponentMenu("Pipeline/Runtime Pipeline Manager")]
    [DisallowMultipleComponent]
    public class RuntimePipelineManager : MonoBehaviour
    {
        [Header("Server Configuration")]
        [Tooltip("Enable Pipeline HTTP server in Player builds. SECURITY WARNING: Only enable in development/QA builds, never production without proper security measures.")]
        public bool enableInBuilds = false;

        [Tooltip("HTTP port for Pipeline server. Use 0 for auto-assignment from range 7900-7999.")]
        [Range(0, 65535)]
        public int port = 0;

        [Header("Advanced")]
        [Tooltip("Request timeout in milliseconds. Higher values allow longer-running commands.")]
        [Range(1000, 60000)]
        public int requestTimeoutMs = 30000;

        [Tooltip("Enable detailed logging of all remote requests for security auditing.")]
        public bool enableAuditLogging = true;

        [Header("Runtime")]
        [Tooltip("Enable automatic server startup when the component starts (for runtime builds only).")]
        public bool autoStart = true;

        [Tooltip("Maximum work items to process per frame to maintain performance.")]
        [Range(1, 50)]
        public int maxWorkItemsPerFrame = 10;

        // Absolute roots this build is allowed to hot reload source files from (Assets + loaded
        // package locations). Baked at build time by the build processor (a running Player cannot
        // resolve the project layout) and published to HotReloadFileScope when the server starts.
        // Hidden from the inspector: it is build-injected, not user-edited.
        [SerializeField, HideInInspector] private List<string> m_AllowedReloadRoots = new List<string>();

        // Internal state
        private RuntimePipelineServer m_Server;
        private RuntimePipelineConfig m_RuntimeConfig;

        // True only for the instance that installed FrameStatsSampler.Shared, so a rejected duplicate's
        // OnDestroy never tears down the real sampler.
        private bool m_OwnsSampler;

        /// <summary>
        /// Get the runtime server instance if it's running.
        /// </summary>
        public RuntimePipelineServer Server => m_Server;

        /// <summary>
        /// Whether the server is currently running.
        /// </summary>
        public bool IsServerRunning => m_Server != null && m_Server.IsRunning;

        /// <summary>
        /// Get the actual port the server is running on.
        /// </summary>
        public int ActualPort => m_Server?.Port ?? 0;

        public RuntimePipelineConfig Config => m_RuntimeConfig;

        /// <summary>
        /// Absolute roots this build may hot reload source files from. Baked at build time.
        /// </summary>
        public IReadOnlyList<string> AllowedReloadRoots => m_AllowedReloadRoots;

        /// <summary>
        /// Set the allowed hot reload roots. Called by the build processor to bake the project's
        /// Assets and package locations into the build.
        /// </summary>
        public void SetAllowedReloadRoots(IEnumerable<string> roots)
        {
            m_AllowedReloadRoots = roots != null ? new List<string>(roots) : new List<string>();
        }

        void Awake()
        {
            // Ensure only one instance exists
            var existing = PipelineUtils.FindObjectsByType<RuntimePipelineManager>();
            if (existing.Length > 1)
            {
                Debug.LogWarning($"Pipeline: Multiple RuntimePipelineManager instances found. Destroying duplicate on '{name}'.");
                Destroy(this);
                return;
            }

            // Don't destroy on load for persistent operation across scenes
            DontDestroyOnLoad(gameObject);

            // Set up command discovery (the server owns its own dispatcher, initialized on Start).
            CommandRegistry.SetDiscovery(null); // Triggers reflection-based discovery

            // Auto-discover and register hot-reloadable methods. Hot reload requires this manager to
            // be present (it owns the communication workflow), so discovery lives here rather than in
            // each gameplay script's Awake.
            RegisterDiscoveredHotReloadMethods();

            // Own the process-wide frame-stats sampler. A Player has no profiler window or
            // EditorApplication.update to drive sampling, so the manager feeds it from Update (below) and
            // the runtime_status telemetry command reads FrameStatsSampler.Shared. Created here (rather than
            // on server start) so fps history is already warm by the time an agent connects.
            FrameStatsSampler.Shared = new FrameStatsSampler();
            m_OwnsSampler = true;
        }

        void Start()
        {
            if (autoStart && enableInBuilds)
            {
                StartServer();
            }
        }

        /// <summary>
        /// Scan loaded user assemblies for methods tagged [HotReload] (in-place workflow) and
        /// [HotReloadWithOverrides] (helper workflow) and register them as reload targets. This replaces any
        /// per-script RegisterReloadableType call in Awake.
        /// </summary>
        private static void RegisterDiscoveredHotReloadMethods()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            int registered = 0;

            foreach (var assembly in PipelineUtils.GetLoadedAssemblies())
            {
                if (!ShouldScanForHotReload(assembly))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types; // best effort: keep the types that did load
                }

                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    foreach (var method in type.GetMethods(flags))
                    {
                        var inPlace = method.GetCustomAttribute<HotReloadAttribute>();
                        if (inPlace != null)
                        {
                            // The weaver keys dispatch on TypeName.MethodName, so register with the
                            // default id (no custom Id) to match.
                            HotReloadRegistry.RegisterReloadableMethod(
                                method, new HotReloadWithOverridesAttribute { RequireMainThread = inPlace.RequireMainThread });
                            registered++;
                            continue;
                        }

                        var reloadable = method.GetCustomAttribute<HotReloadWithOverridesAttribute>();
                        if (reloadable != null)
                        {
                            HotReloadRegistry.RegisterReloadableMethod(method, reloadable);
                            registered++;
                        }
                    }
                }
            }

            if (registered > 0)
                Debug.Log($"Pipeline: Auto-discovered and registered {registered} hot-reloadable method(s).");
        }

        /// <summary>
        /// Skip engine/framework assemblies that cannot contain user hot-reload methods.
        /// </summary>
        private static bool ShouldScanForHotReload(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return false;

            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (var prefix in s_HotReloadSkipPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static readonly string[] s_HotReloadSkipPrefixes =
        {
            "System", "mscorlib", "netstandard", "Microsoft", "Mono.", "nunit",
            "UnityEngine", "UnityEditor", "Unity.Collections", "Unity.Burst",
            "Unity.Mathematics", "Newtonsoft", "log4net", "ICSharpCode",
        };

        void Update()
        {
            // Feed the frame-stats sampler once per frame on the main thread. Uses unscaled delta so
            // reported fps reflects real frame pacing regardless of Time.timeScale.
            FrameStatsSampler.Shared?.Sample(Time.unscaledDeltaTime);

            // Pump this server's own dispatcher for main-thread operations (player builds have no
            // EditorApplication.update to auto-pump it).
            m_Server?.Dispatcher.ProcessWorkQueue(maxWorkItemsPerFrame);

            // Drive the watchdog (player builds have no EditorApplication.update). No-op unless the
            // server's watchdog is enabled and armed.
            m_Server?.WatchdogTick();
        }

        void OnApplicationQuit()
        {
            StopServer();
        }

        void OnDestroy()
        {
            // Dispose the sampler's profiler recorders and drop the shared reference, but only if this
            // manager is the one that installed it (a rejected duplicate must not tear down the real one).
            if (m_OwnsSampler)
            {
                FrameStatsSampler.Shared?.Dispose();
                FrameStatsSampler.Shared = null;
                m_OwnsSampler = false;
            }
        }

        /// <summary>
        /// Manually start the Pipeline server.
        /// </summary>
        public void StartServer()
        {
            if (m_Server != null && m_Server.IsRunning)
            {
                Debug.LogWarning("Pipeline: Server already started or initialization attempted.");
                return;
            }

            try
            {
                System.Console.WriteLine("Pipeline: Starting runtime server from RuntimePipelineManager...");

                if (!enableInBuilds)
                {
                    Debug.LogWarning("Pipeline: Runtime server disabled in configuration (enableInBuilds = false)");
                    return;
                }

                // Create runtime config from component settings
                m_RuntimeConfig = CreateRuntimeConfig();

                // Validate configuration
                var validation = m_RuntimeConfig.Validate();
                if (!validation.IsValid)
                {
                    Debug.LogError($"Pipeline: Invalid runtime configuration: {validation.Message}");
                    return;
                }

                if (validation.Level == "warning")
                {
                    Debug.LogWarning($"Pipeline: Runtime configuration warning: {validation.Message}");
                }

                // Create runtime server
                m_Server = new RuntimePipelineServer(m_RuntimeConfig);

                // Start server on configured port
                var serverPort = port == 0 ? 0 : port;
                m_Server.Start(serverPort);

                if (m_Server.IsRunning)
                {
                    // The hot-reload registry marshals main-thread overrides through a dispatcher
                    // when hot-reloaded code is invoked from a background thread at runtime. Inject
                    // this server's dispatcher here (a player has exactly one manager), so editor and
                    // per-test servers never clobber the global registry's dispatcher.
                    Unity.Pipeline.HotReload.HotReloadRegistry.Dispatcher = m_Server.Dispatcher;

                    // Publish the build-baked allowed roots so reload commands can validate that
                    // incoming files are inside the project before compiling and injecting them.
                    Unity.Pipeline.HotReload.HotReloadRegistry.AllowedReloadRoots = m_AllowedReloadRoots;

                    System.Console.WriteLine($"Pipeline: Runtime server started successfully on port {m_Server.Port}");
                }
                else
                {
                    Debug.LogError("Pipeline: Failed to start runtime server");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Pipeline: Runtime initialization failed: {ex.Message}");
                Debug.LogError($"Pipeline: Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Manually stop the Pipeline server.
        /// </summary>
        public void StopServer()
        {
            try
            {
                if (m_Server != null && m_Server.IsRunning)
                {
                    System.Console.WriteLine("Pipeline: Stopping runtime server...");
                    // Drop the registry's reference to this server's (about to be shut down) dispatcher.
                    if (Unity.Pipeline.HotReload.HotReloadRegistry.Dispatcher == m_Server.Dispatcher)
                        Unity.Pipeline.HotReload.HotReloadRegistry.Dispatcher = null;
                    m_Server.Stop(); // Stop() shuts down the server's own dispatcher.
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Pipeline: Runtime shutdown error: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a RuntimePipelineConfig from the component settings.
        /// </summary>
        private RuntimePipelineConfig CreateRuntimeConfig()
        {
            var config = ScriptableObject.CreateInstance<RuntimePipelineConfig>();

            // Copy settings from component to config
            config.enableInBuilds = this.enableInBuilds;
            config.port = this.port;
            config.requestTimeoutMs = this.requestTimeoutMs;
            config.enableAuditLogging = this.enableAuditLogging;

            return config;
        }

        /// <summary>
        /// Validate the current configuration.
        /// </summary>
        public ValidationResult ValidateConfiguration()
        {
            if (m_RuntimeConfig == null)
                m_RuntimeConfig = CreateRuntimeConfig();

            return m_RuntimeConfig.Validate();
        }
    }
}