using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Commands for controlling Unity Editor play mode state.
    /// Enables automation workflows to control Editor play/stop/pause from CLI tools.
    /// </summary>
    public static class PlayModeCommands
    {
        // TODO Pipeline: should all commands have name with namespace: editor.play build.generate instead of using _

        [CliCommand("editor_play", "Enter Unity Editor play mode")]
        public static string EnterPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Editor is already in play mode");
                return "Already in play mode";
            }

            EditorApplication.isPlaying = true;
            return "Entered play mode";
        }

        [CliCommand("editor_stop", "Exit Unity Editor play mode")]
        public static string ExitPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Editor is already in edit mode");
                return "Already in edit mode";
            }

            EditorApplication.isPlaying = false;
            return "Exited play mode";
        }

        [CliCommand("editor_pause", "Pause Unity Editor play mode")]
        public static string PausePlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot pause - Editor is not in play mode");
                return "Cannot pause - not in play mode";
            }

            EditorApplication.isPaused = !EditorApplication.isPaused;
            string state = EditorApplication.isPaused ? "paused" : "unpaused";
            return $"Play mode {state}";
        }
    }
}