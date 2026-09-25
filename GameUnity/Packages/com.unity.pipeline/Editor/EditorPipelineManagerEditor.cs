using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor
{
    /// <summary>
    /// Custom inspector for the <see cref="EditorPipelineManager"/> settings asset: shows live server
    /// status (accurate running check + actual port, read from the static owner) and offers
    /// Start/Stop/Restart buttons, alongside the editable configuration. Watchdog edits are pushed to
    /// a running server immediately; port/autoStart apply on the next start.
    /// </summary>
    [CustomEditor(typeof(EditorPipelineManager))]
    public class EditorPipelineManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mgr = (EditorPipelineManager)target;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                // Push edited watchdog config to the live server (owned by PipelineServerStartup), so
                // changes take effect without a restart.
                var server = PipelineServerStartup.Server;
                if (server != null)
                {
                    server.WatchdogEnabled = mgr.WatchdogEnabled;
                    server.WatchdogIntervalSeconds = mgr.WatchdogIntervalSeconds;
                    server.LogRequestsResponses = mgr.LogRequestsResponses;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Running", mgr.IsServerRunning);
                EditorGUILayout.IntField("Actual Port", mgr.ActualPort);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(mgr.IsServerRunning))
                {
                    if (GUILayout.Button("Start"))
                        mgr.StartServer();
                }
                using (new EditorGUI.DisabledScope(!mgr.IsServerRunning))
                {
                    if (GUILayout.Button("Stop"))
                        mgr.StopServer();
                }
                if (GUILayout.Button("Restart"))
                    mgr.RestartServer();
            }

            // Keep the live status display current while the inspector is open.
            Repaint();
        }
    }
}
