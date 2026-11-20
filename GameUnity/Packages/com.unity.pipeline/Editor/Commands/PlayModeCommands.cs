using System;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Commands for controlling Unity Editor play mode state.
    /// Enables automation workflows to control Editor play/stop/pause from CLI tools.
    /// </summary>
    static class PlayModeCommands
    {
        const string Group = "editor.playmode";

        // TODO Pipeline: should all commands have name with namespace: editor.play build.generate instead of using _

        /// <summary>
        /// Enter Play mode. A client driving Play mode usually leaves the Editor unfocused, and Unity
        /// pauses the player of an unfocused Editor whose Run In Background is off, so this turns Run
        /// In Background on for the Play session (AUTHAPI-114) through the recorded override that
        /// <see cref="PipelineServerStartup"/> restores when Play mode exits.
        /// </summary>
        [CliGroupCommand(Group, "Enter Unity Editor play mode. Turns Run In Background on until play mode exits, so the game keeps running while the Editor is unfocused; builds keep the project's value", Tags = new[] { "editor/playmode" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode Start(CommandContext cmd,
            [CliArg("run-in-background", "Turn Run In Background on for this Play session (true, default) so the game keeps running while the Editor is unfocused, or keep the project's own value (false), to reproduce how the game behaves when it loses focus")] bool runInBackground = true)
        {
            if (EditorApplication.isPlaying)
            {
                cmd.Log.Info("Already in play mode.");
                return ExitCode.Success;
            }

            var note = BeginRunInBackgroundForClientPlaySession(runInBackground);
            // Unity can still refuse the request for another reason; restore Run In Background if so.
            // Subscribed first: ExitingEditMode fires inside the isPlaying setter and cancels it.
            if (PipelineServerStartup.IsClientRunInBackgroundRequested)
                EditorApplication.delayCall += PipelineServerStartup.EndClientPlaySessionIfPlayModeRefused;
            EditorApplication.isPlaying = true;
            cmd.Log.Info("Entering play mode.");
            LogRunInBackgroundNote(cmd, note);
            return ExitCode.Success;
        }

        /// <summary>Logs what <see cref="BeginRunInBackgroundForClientPlaySession"/> returned: a warning when the game pauses unfocused.</summary>
        internal static void LogRunInBackgroundNote(CommandContext cmd, string note)
        {
            if (note == RunInBackgroundOffNote)
                cmd.Log.Warning(note);
            else if (note != null)
                cmd.Log.Info(note);
        }

        internal const string RunInBackgroundOnNote =
            "Run In Background on until play mode exits; the project's value, off, is restored then and builds keep it";
        internal const string RunInBackgroundOffNote =
            "Run In Background is off: the game pauses while the Editor is unfocused";

        /// <summary>
        /// Shared by editor.playmode start and start_scenario, before they enter Play mode: turn Run In
        /// Background on for the Play session when <paramref name="runInBackground"/> asks for it and it
        /// is off. Returns what a reply should tell the client, or null when Run In Background is on
        /// and this call didn't change it. In a Play session the client didn't start, it changes
        /// nothing. Throws while scripts have compile errors, since Unity would refuse Play mode.
        /// </summary>
        internal static string BeginRunInBackgroundForClientPlaySession(bool runInBackground)
        {
            // Unity refuses Play mode while scripts have compile errors, and no exit event would then
            // restore Run In Background.
            if (RecompileCommand.s_CompilationFailedProbe())
                throw new InvalidOperationException(
                    "Cannot enter play mode while scripts have compile errors. Fix them, then run " + CommandNames.ScriptRecompile + ".");

            // Not a session this client is starting: leave Run In Background to whoever started it, but
            // still warn when the game pauses while the Editor is unfocused.
            if (EditorApplication.isPlaying)
                return Application.runInBackground ? null : RunInBackgroundOffNote;

            if (runInBackground && PipelineServerStartup.BeginClientPlaySessionRunInBackground())
                return RunInBackgroundOnNote;
            return Application.runInBackground ? null : RunInBackgroundOffNote;
        }

        /// <summary>
        /// Override the project's Run In Background Player Setting so the Play-mode player loop
        /// keeps running while the Editor is unfocused (CLI-1432). <c>editor.playmode start</c> already does
        /// this for the Play session it starts (AUTHAPI-114); this records an explicit override that lasts
        /// until reverted or until the Editor quits, and takes over from the one <c>start</c> made: <c>enabled: true</c>
        /// remembers the project's own value (unless an override is already active) and turns Run
        /// In Background on; <c>enabled: false</c> stops
        /// the override and restores the project's own value, unless the "Ensure editor runs in
        /// background in PlayMode" project setting is still forcing it on right now. A build always
        /// uses the project's own value regardless of either — see
        /// <see cref="BuildProcessors.PipelineRuntimeBuildProcessor"/>.
        /// </summary>
        [CliGroupCommand(Group,
            "Override the project's Run In Background Player Setting, recording enough state to restore it later and to keep it out of builds",
            Tags = new[] { "editor/playmode" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetRunInBackground(CommandContext cmd,
            [CliArg("enabled", "Turn the override on (true, default) or stop it (false) and restore the project's own value, unless the \"Ensure editor runs in background in PlayMode\" setting is still forcing it on right now")] bool enabled = true)
        {
            // A Multiplayer Play Mode clone shares the main project's settings file, so recording an
            // override there would replace the main Editor's. A clone can't save Player Settings either,
            // so its change is in memory only.
            if (PipelineServerStartup.s_IsVirtualProjectCloneProbe())
            {
                if (enabled)
                    Application.runInBackground = false;
                Application.runInBackground = enabled;
                cmd.Log.Info($"Run In Background is {(enabled ? "on" : "off")} in this Multiplayer Play Mode clone only, in memory: a clone can't save Player Settings, and the main Editor's override is left alone");
                cmd.Result.Write(new { runInBackground = Application.runInBackground, overridden = false, projectValue = Application.runInBackground });
                return ExitCode.Success;
            }

            var cfg = EditorPipelineConfig.Load() ?? ScriptableObject.CreateInstance<EditorPipelineConfig>();
            try
            {
                // First settle any change the user made themselves (they win and the stored override
                // ends), so this explicit request is applied on top of it instead of being swallowed
                // by it: asking again is how the override comes back.
                PipelineServerStartup.ReconcileRunInBackground(cfg, EditorApplication.isPlaying, out _);
                // An explicit choice takes over from the one editor.playmode start made for its session.
                PipelineServerStartup.EndClientPlaySession();
                cfg.SetRunInBackgroundCommandOverride(enabled);
                PipelineServerStartup.ReconcileRunInBackground(cfg, EditorApplication.isPlaying, out var message);
                // Always persisted, regardless of whether the override's net effect changed: the
                // command's own record field (RunInBackgroundCommandOverride) always needs saving,
                // even when e.g. the Play-mode setting was already forcing the same result.
                cfg.Save();

                // Stopping the override while the Play-mode setting still forces it leaves it on.
                if (!enabled && cfg.RunInBackgroundOverridden)
                    cmd.Log.Warning(message);
                else
                    cmd.Log.Info(message);

                cmd.Result.Write(new
                {
                    runInBackground = Application.runInBackground,
                    overridden = cfg.RunInBackgroundOverridden,
                    projectValue = cfg.RunInBackgroundOverridden ? cfg.RunInBackgroundOriginal : Application.runInBackground
                });
                return ExitCode.Success;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cfg);
            }
        }

        [CliGroupCommand(Group, "Exit Unity Editor play mode", Tags = new[] { "editor/playmode" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode Stop(CommandContext cmd)
        {
            if (!EditorApplication.isPlaying)
            {
                cmd.Log.Info("Already in edit mode.");
                return ExitCode.Success;
            }

            EditorApplication.isPlaying = false;
            cmd.Log.Info("Exiting play mode.");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Toggle pause state of Unity Editor play mode", Tags = new[] { "editor/playmode" }, Effect = CommandEffect.Additive)]
        internal static ExitCode Pause(CommandContext cmd)
        {
            if (!EditorApplication.isPlaying)
            {
                cmd.Log.Error($"Cannot pause: the Editor is not in play mode. Enter it first ({CommandNames.EditorPlaymodeStart}).");
                return ExitCode.Failed;
            }

            EditorApplication.isPaused = !EditorApplication.isPaused;
            cmd.Result.Write(new { paused = EditorApplication.isPaused });
            return ExitCode.Success;
        }
    }
}