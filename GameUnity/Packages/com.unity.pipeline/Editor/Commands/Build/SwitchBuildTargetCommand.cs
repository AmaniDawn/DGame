using System;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.Build
{
    /// <summary>
    /// Switches the active build target (CLI-204). <see cref="BuildPipeline.SwitchActiveBuildTarget"/> is
    /// synchronous, blocks the main thread, and triggers a full asset reimport + domain reload for the new
    /// platform — minutes on a large project. So this follows the trigger-then-poll pattern: the command
    /// validates and queues the switch, returns <c>switching</c> immediately, and an
    /// <see cref="EditorApplication.update"/> tick performs the switch. The switch is confirm-gated
    /// (destructive and long-running) and is not part of Unity's Undo.
    ///
    /// The status is persisted to a Temp file that survives the domain reload the switch causes; on the
    /// next load <see cref="RecoverInterruptedOperation"/> reconciles a left-over <c>switching</c> record
    /// against the now-active target. <c>switch_build_target_status</c> reads the file off the main thread,
    /// so it keeps answering while the switch holds the main thread.
    /// </summary>
    [InitializeOnLoad]
    public static class SwitchBuildTargetCommand
    {
        const string StatusFile = "Temp/pipeline_switch_target_status.json";

        static readonly object s_Lock = new object();
        static bool s_Pending;
        static bool s_Switching;
        static BuildTarget s_PendingTarget;
        static BuildTarget s_FromTarget;

        static SwitchBuildTargetCommand()
        {
            EditorApplication.update += OnUpdate;
            RecoverInterruptedOperation();
        }

        [CliCommand("switch_build_target",
            "Switch the active build target (destructive, long-running: triggers a full reimport + domain " +
            "reload). Requires confirm=true. Returns immediately; poll switch_build_target_status.",
            MainThreadRequired = false)]
        public static object SwitchBuildTarget(
            [CliArg("target", "BuildTarget name to switch to (must be installed; see list_build_targets).", Required = true)] string target = "",
            [CliArg("confirm", "Apply the switch. Without it the call is refused.")] bool confirm = false)
        {
            return RunOnMain<object>(() => ValidateAndQueue(target, confirm));
        }

        static object ValidateAndQueue(string targetArg, bool confirm)
        {
            if (string.IsNullOrWhiteSpace(targetArg))
                return new { status = "error", success = false, message = "A 'target' is required." };

            if (!TryParseTarget(targetArg, out var target))
                return new { status = "error", success = false, message = $"Unknown BuildTarget '{targetArg}'. Use a name from list_build_targets." };

            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                return new { status = "error", success = false, message = $"Build target '{target}' is not installed (isInstalled=false in list_build_targets)." };

            var from = EditorUserBuildSettings.activeBuildTarget;
            if (target == from)
                return new { status = "completed", success = true, activeBuildTarget = from.ToString(), message = $"Already on build target '{from}'." };

            lock (s_Lock)
            {
                if (s_Pending || s_Switching)
                    return new { status = "busy", success = false, message = "A target switch is already queued or in progress. Poll switch_build_target_status." };
            }

            // Confirm gate. The queue below only sets pending state; the switch runs on the next tick.
            if (!confirm)
                return new
                {
                    status = "error",
                    success = false,
                    message = "Refused: switching the build target triggers a full reimport + domain reload. Pass confirm=true to apply."
                };

            lock (s_Lock)
            {
                s_FromTarget = from;
                s_PendingTarget = target;
                s_Pending = true;
            }
            WriteStatusJson(new
            {
                status = "switching",
                fromTarget = from.ToString(),
                toTarget = target.ToString(),
                startedAt = DateTime.UtcNow.ToString("o")
            });

            return new { status = "switching", fromTarget = from.ToString(), toTarget = target.ToString() };
        }

        [CliCommand("switch_build_target_status",
            "Status of the last target switch: idle | switching | completed (with success + activeBuildTarget).",
            MainThreadRequired = false)]
        public static string SwitchBuildTargetStatus()
        {
            if (File.Exists(StatusFile))
                return File.ReadAllText(StatusFile);
            return "{\"status\":\"idle\"}";
        }

        /// <summary>Main-thread pump: performs the queued switch. Blocks the main thread until it returns.</summary>
        static void OnUpdate()
        {
            BuildTarget target;
            BuildTarget from;
            lock (s_Lock)
            {
                if (!s_Pending || s_Switching)
                    return;
                s_Pending = false;
                s_Switching = true;
                target = s_PendingTarget;
                from = s_FromTarget;
            }

            try
            {
                var group = BuildPipeline.GetBuildTargetGroup(target);

                // Synchronous and may trigger a domain reload on success — in which case the code below
                // never runs and RecoverInterruptedOperation finalizes the status after the reload.
                // (SwitchActiveBuildTarget moved from BuildPipeline to EditorUserBuildSettings in Unity 6.)
                var ok = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

                if (ok)
                    WriteStatusJson(new
                    {
                        status = "completed",
                        success = true,
                        activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString()
                    });
                else
                    WriteStatusJson(new
                    {
                        status = "completed",
                        success = false,
                        target = target.ToString(),
                        errors = new[] { new { message = $"Failed to switch build target to '{target}'." } }
                    });
            }
            catch (Exception ex)
            {
                WriteStatusJson(new
                {
                    status = "completed",
                    success = false,
                    target = target.ToString(),
                    errors = new[] { new { message = $"Switch to '{target}' threw: {ex.Message}" } }
                });
            }
            finally
            {
                lock (s_Lock) { s_Switching = false; }
            }
        }

        /// <summary>
        /// On load, settle a status file left at "switching" by the domain reload a successful switch
        /// causes: if the active target now matches the requested one, the switch completed; otherwise it
        /// did not take effect.
        /// </summary>
        static void RecoverInterruptedOperation()
        {
            try
            {
                if (!File.Exists(StatusFile))
                    return;

                var doc = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(StatusFile));
                if ((string)doc["status"] != "switching")
                    return;

                var toTarget = (string)doc["toTarget"];
                var active = EditorUserBuildSettings.activeBuildTarget.ToString();

                if (string.Equals(active, toTarget, StringComparison.Ordinal))
                    WriteStatusJson(new { status = "completed", success = true, activeBuildTarget = active });
                else
                    WriteStatusJson(new
                    {
                        status = "completed",
                        success = false,
                        target = toTarget,
                        errors = new[] { new { message = $"Switch did not take effect; active build target is '{active}'." } }
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[pipeline] switch_build_target status recovery failed: {ex.Message}");
            }
        }

        // ---- Helpers ---------------------------------------------------------------------------

        static bool TryParseTarget(string name, out BuildTarget target)
        {
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), name.Trim(), ignoreCase: true);
                return Enum.IsDefined(typeof(BuildTarget), target);
            }
            catch
            {
                target = EditorUserBuildSettings.activeBuildTarget;
                return false;
            }
        }

        static T RunOnMain<T>(Func<T> fn)
        {
            var dispatcher = PipelineServerStartup.Server?.Dispatcher;
            if (dispatcher != null && dispatcher.IsInitialized && !dispatcher.IsMainThread())
                return dispatcher.Invoke(fn);
            return fn();
        }

        static void WriteStatusJson(object status)
        {
            try
            {
                File.WriteAllText(StatusFile, JsonConvert.SerializeObject(status));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SwitchBuildTarget] Failed to write status file: {ex.Message}");
            }
        }
    }
}
