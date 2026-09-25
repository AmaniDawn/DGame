using System;
using System.IO;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Command to get detailed Editor status information.
    /// Returns rich Editor state including compilation, play mode, and project details.
    /// Accessible via "unity request editor_status" and "/api/editor_status" endpoint.
    /// </summary>
    public static class EditorStatusCommand
    {
        [CliCommand("editor_status", "Get detailed Unity Editor status and state information")]
        public static StatusResponse GetEditorStatus()
        {
            return new StatusResponse
            {
                Status = GetOverallStatus(),
                Compiling = EditorApplication.isCompiling,
                DomainReloadInProgress = EditorApplication.isUpdating,
                PlayMode = GetPlayModeStatus(),
                LastHeartbeat = DateTime.UtcNow,
                ProjectPath = Path.GetDirectoryName(Application.dataPath),
                UnityVersion = Application.unityVersion
            };
        }

        /// <summary>
        /// Get overall Editor status based on current state.
        /// </summary>
        private static string GetOverallStatus()
        {
            if (EditorApplication.isCompiling)
                return "compiling";

            if (EditorApplication.isUpdating)
                return "reloading";

            if (EditorApplication.isPlaying || EditorApplication.isPaused)
                return "playing";

            return "ready";
        }

        /// <summary>
        /// Get current play mode status.
        /// </summary>
        private static string GetPlayModeStatus()
        {
            if (EditorApplication.isPaused)
                return "paused";

            if (EditorApplication.isPlaying)
                return "playing";

            return "stopped";
        }
    }
}