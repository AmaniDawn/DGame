using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.Pipeline.Editor.Commands.Build
{
    /// <summary>
    /// Triggers a Player build and reports the full <see cref="BuildReport"/> (CLI-204). Fills out the
    /// original <c>build</c>/<c>build_status</c> stub (CLI-127) with the complete param contract and the
    /// structured report shape so build failures can be diagnosed without the Editor UI.
    ///
    /// Why this is async (trigger-then-poll, mirroring <see cref="RecompileCommand"/> /
    /// <see cref="PackageManager.PackageManagerCommand"/>): <see cref="BuildPipeline.BuildPlayer(BuildPlayerOptions)"/>
    /// runs synchronously and freezes the main thread for the whole build (seconds to minutes). If
    /// <c>build</c> ran it inline it would block the server's (serial) request loop and the call could
    /// never return. Instead <c>build</c> validates + queues and returns <c>queued</c> immediately; an
    /// <see cref="EditorApplication.update"/> hook runs the build on the next tick. Poll <c>build_status</c>
    /// until <c>status == "completed"</c>.
    ///
    /// <c>build_status</c> is deliberately <c>MainThreadRequired = false</c>: while the build holds the
    /// main thread, any main-thread command (e.g. editor_status) would hang, but build_status only reads
    /// a Temp status file off-thread, so polling keeps working throughout the build. The status file also
    /// survives the domain reload a build can incur, so the last report is retained until the next build.
    /// </summary>
    [InitializeOnLoad]
#if UNITY_6000_5_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    static class BuildCommand
    {
        const string Group = "build";
        const string StatusFile = "Temp/pipeline_build_status.json";

        // BuildOptions that callers may pass by name (the supported set from the ticket). Parsed
        // case-insensitively; anything outside this set is a validation error rather than silently
        // tolerated, so a typo surfaces instead of changing the build shape.
        static readonly Dictionary<string, BuildOptions> s_SupportedOptions =
            new Dictionary<string, BuildOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Development"] = BuildOptions.Development,
                ["AllowDebugging"] = BuildOptions.AllowDebugging,
                ["ConnectWithProfiler"] = BuildOptions.ConnectWithProfiler,
                // EnableHeadlessMode is obsolete in Unity 6 (superseded by StandaloneBuildSubtarget.Server)
                // but still functions and is part of this command's documented option set (CLI-204), so we
                // keep accepting it and suppress only the deprecation warning here.
#pragma warning disable 618
                ["EnableHeadlessMode"] = BuildOptions.EnableHeadlessMode,
#pragma warning restore 618
                ["SymlinkSources"] = BuildOptions.SymlinkSources,
                ["BuildAdditionalStreamedScenes"] = BuildOptions.BuildAdditionalStreamedScenes,
                ["CleanBuildCache"] = BuildOptions.CleanBuildCache,
                ["DetailedBuildReport"] = BuildOptions.DetailedBuildReport,
            };

        static readonly object s_Lock = new object();
        static bool s_Pending;
        static bool s_Building;

        // Queued request, captured under s_Lock by build() and consumed by the update pump.
        static BuildTarget s_PendingTarget;
        static string s_PendingOutput;
        static BuildOptions s_PendingOptions;

        /// <summary>
        /// BuildOptions of the scripted build currently inside BuildPipeline.BuildPlayer, or null
        /// when no `build` command is running. Build processors read it because a scripted build
        /// with BuildOptions.Development is a development build even when the Build Settings
        /// toggle (EditorUserBuildSettings.development) is off.
        /// </summary>
        internal static BuildOptions? ActiveScriptedBuildOptions { get; private set; }
        static string[] s_PendingScenes;
        static string s_PendingProfile;
        static string s_PendingBuildId;

        static BuildCommand()
        {
            EditorApplication.update += OnUpdate;
        }

        [CliGroupCommand(Group,
            "Trigger an async Player build and report the full BuildReport. Returns immediately (queued); " +
            "poll " + CommandNames.BuildStatus + " until status is 'completed'. DetailedBuildReport is included by default unless " +
            "'options' is supplied. Use dry-run to validate without building.",
            MainThreadRequired = false,
            Tags = new[] { "build" },
            Effect = CommandEffect.Destructive)]
        internal static ExitCode Run(CommandContext cmd,
            [CliArg("target", "BuildTarget name (e.g. StandaloneWindows64). Defaults to the active target. Must be installed.")] string target = "",
            [CliArg("output-path", "Output path (absolute, or relative to the project root). Defaults to the last/auto path.")] string outputPath = "",
            [CliArg("profile-name", "Build Profile to activate and build with (Unity 6 only). Must be on the active build target; without scenes, its scene list is built.")] string profileName = "",
            [CliArg("options", "BuildOptions names. Omit to get just DetailedBuildReport; supplying any disables that default.")] string[] options = null,
            [CliArg("scenes", "Scene asset paths to build (e.g. Assets/Scenes/Main.unity). Defaults to the profile's scene list, or EditorBuildSettings.")] string[] scenes = null,
            [CliArg("confirm", "Acknowledge and run the build; without it the call is refused. Use dry-run to validate only.")] bool confirm = false,
            [CliArg("dry-run", "Validate target/output-path/profile-name/scenes without building; reports the target and scenes the build would use.")] bool dryRun = false,
            [CliArg("timeout", "Seconds to wait for the build to be validated and queued (the build itself is polled via " + CommandNames.BuildStatus + ").")] double timeoutInSeconds = 60)
        {
            // Validation + queueing both touch Unity APIs, so run them on the main thread. When invoked
            // off-thread (the normal HTTP path) this marshals through the server dispatcher; in-process
            // (tests, no server) it runs inline.
            return RunOnMain(() => ValidateAndQueue(cmd, target, outputPath, profileName, options, scenes, confirm, dryRun), PipelineTimeout.FromSeconds(timeoutInSeconds));
        }

        static ExitCode ValidateAndQueue(CommandContext cmd, string targetArg, string outputArg, string profileName,
            string[] optionArgs, string[] sceneArgs, bool confirm, bool dryRun)
        {
            lock (s_Lock)
            {
                if (!dryRun && (s_Building || s_Pending))
                {
                    cmd.Log.Error("A build is already queued or in progress. Poll " + CommandNames.BuildStatus + ".");
                    return ExitCode.Failed;
                }
            }

            var errors = new List<string>();

            // ---- target ----
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (!string.IsNullOrWhiteSpace(targetArg))
            {
                if (!TryParseTarget(targetArg, out target))
                    errors.Add(Invalid("target", $"Unknown BuildTarget '{targetArg}'. Use a name from {CommandNames.BuildTargetList}."));
                else if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                    errors.Add(Invalid("target", $"Build target '{target}' is not installed (isInstalled=false in {CommandNames.BuildTargetList})."));
            }

            // ---- options ----
            var options = BuildOptions.None;
            if (optionArgs != null && optionArgs.Length > 0)
            {
                foreach (var name in optionArgs)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    if (s_SupportedOptions.TryGetValue(name.Trim(), out var opt))
                        options |= opt;
                    else
                        errors.Add(Invalid("options", $"Unsupported BuildOptions value '{name}'."));
                }
            }
            else
            {
                // Strongly recommended: without it BuildReport.packedAssets is empty.
                options = BuildOptions.DetailedBuildReport;
            }

            // ---- profile ----
            // Resolved here, not when the build runs: the profile decides the scene list and must not switch
            // the target, and a dry run has to report both.
            var explicitScenes = sceneArgs != null && sceneArgs.Length > 0;
            string[] profileScenes = null;
            if (!string.IsNullOrWhiteSpace(profileName))
                profileScenes = ValidateProfile(profileName.Trim(), targetArg, target, needScenes: !explicitScenes, errors);

            // ---- scenes ----
            var scenes = explicitScenes
                ? sceneArgs
                : profileScenes ?? EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            if (sceneArgs != null)
            {
                foreach (var scene in sceneArgs)
                {
                    if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scene)))
                        errors.Add(Invalid("scenes", $"No scene asset at '{scene}'."));
                }
            }

            // ---- output path ----
            // Non-mutating writability check: resolve the path and confirm an existing ancestor
            // directory exists (so the build's output folder can be created). BuildPipeline.BuildPlayer
            // creates the leaf directory itself, so we never create anything here — a dry run stays pure.
            string outputPath = null;
            try
            {
                outputPath = ResolveOutput(outputArg, target);
                if (!HasExistingAncestor(outputPath))
                    errors.Add(Invalid("output-path", $"No existing parent directory for '{outputPath}'."));
            }
            catch (Exception ex)
            {
                errors.Add(Invalid("output-path", $"Output path is invalid: {ex.Message}"));
            }

            if (errors.Count > 0)
            {
                // Fail immediately — do not queue (acceptance criteria 5).
                cmd.Log.Error((dryRun ? "Build configuration is invalid: " : "Build configuration is invalid; nothing was queued: ") +
                              string.Join(" ", errors));
                return ExitCode.InvalidUsage;
            }

            if (dryRun)
            {
                cmd.Log.Info("Build configuration is valid.");
                cmd.Result.Write(new { status = "dry_run", valid = true, target = target.ToString(), scenes });
                return ExitCode.Success;
            }

            if (!confirm)
            {
                cmd.Log.Error("Refused: this triggers a player build. Pass confirm=true to build, or dry-run=true to validate without building.");
                return ExitCode.InvalidUsage;
            }

            var buildId = NewBuildId();

            lock (s_Lock)
            {
                s_PendingTarget = target;
                s_PendingOutput = outputPath;
                s_PendingOptions = options;
                s_PendingScenes = scenes;
                s_PendingProfile = profileName;
                s_PendingBuildId = buildId;
                s_Pending = true;
            }

            WriteStatusJson(new { status = "queued", buildId, message = "Build queued. Poll " + CommandNames.BuildStatus + " until status is 'completed'." });

            cmd.Log.Info("Build queued. Poll " + CommandNames.BuildStatus + " until status is 'completed'.");
            cmd.Result.Write(new { status = "queued", buildId });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Status of the current/most recent build: idle | queued | building | completed, with the full " +
            "BuildReport (files, packedAssets, buildSteps, errors, warnings) once completed. Retained until the next build.",
            MainThreadRequired = false,
            Tags = new[] { "build" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Status(CommandContext cmd)
        {
            cmd.Result.Write(ReadStatus());
            return ExitCode.Success;
        }

        /// <summary>The status <see cref="Status"/> writes: the status file, with a live elapsedMs while building.</summary>
        internal static JToken ReadStatus()
        {
            var status = StatusFileReader.ReadOrIdle(StatusFile);

            // While building, recompute elapsedMs at read time so polling shows live progress. A file
            // that is not a JSON object cannot carry a status, so it passes through untouched.
            var doc = status as JObject;
            if (doc != null && (string)doc["status"] == "building")
            {
                long elapsed = 0;
                var startedStr = (string)doc["buildStartedAt"];
                if (!string.IsNullOrEmpty(startedStr) &&
                    DateTime.TryParse(startedStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var started))
                {
                    elapsed = (long)Math.Max(0, (DateTime.UtcNow - started.ToUniversalTime()).TotalMilliseconds);
                }

                return new JObject
                {
                    ["status"] = "building",
                    ["buildId"] = (string)doc["buildId"],
                    ["elapsedMs"] = elapsed
                };
            }

            return status;
        }

        /// <summary>
        /// Main-thread pump: picks up a queued request and runs the (blocking) build. Guarded so a build
        /// runs at most once at a time.
        /// </summary>
        static void OnUpdate()
        {
            BuildTarget target;
            string output;
            BuildOptions options;
            string[] scenes;
            string profile;
            string buildId;

            lock (s_Lock)
            {
                if (!s_Pending || s_Building)
                    return;
                s_Pending = false;
                s_Building = true;
                target = s_PendingTarget;
                output = s_PendingOutput;
                options = s_PendingOptions;
                scenes = s_PendingScenes;
                profile = s_PendingProfile;
                buildId = s_PendingBuildId;
            }

            try
            {
                RunBuild(buildId, target, output, options, scenes, profile);
            }
            catch (Exception ex)
            {
                WriteStatusJson(new
                {
                    status = "completed",
                    buildId,
                    result = "Failed",
                    totalSizeBytes = 0,
                    errors = new[] { new { message = $"Build failed to start: {ex.Message}" } },
                    warnings = Array.Empty<object>()
                });
            }
            finally
            {
                lock (s_Lock) { s_Building = false; }
            }
        }

        static void RunBuild(string buildId, BuildTarget target, string outputPath, BuildOptions options,
            string[] scenes, string profileName)
        {
            var startedAt = DateTime.UtcNow;
            WriteStatusJson(new
            {
                status = "building",
                buildId,
                buildStartedAt = startedAt.ToString("o"),
                platform = target.ToString(),
                outputPath
            });

            // Validated when queued; a profile that cannot be activated now fails the build rather than
            // building with whatever configuration happens to be active.
            if (!string.IsNullOrWhiteSpace(profileName))
                ActivateBuildProfile(profileName.Trim());

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = options
            };

            ActiveScriptedBuildOptions = options;
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(buildOptions); }
            finally { ActiveScriptedBuildOptions = null; }
            WriteStatusJson(Project(buildId, report));
        }

        // ---- BuildReport projection ------------------------------------------------------------

        static BuildReportResult Project(string buildId, BuildReport report)
        {
            var summary = report.summary;
            var success = summary.result == BuildResult.Succeeded;
            var started = summary.buildStartedAt;
            var ended = started + summary.totalTime;

            var result = new BuildReportResult
            {
                Status = "completed",
                BuildId = buildId,
                Result = summary.result.ToString(),
                Platform = summary.platform.ToString(),
                OutputPath = summary.outputPath,
                TotalSizeBytes = (long)summary.totalSize,
                BuildTimeMs = (long)summary.totalTime.TotalMilliseconds,
                BuildStartedAt = started.ToUniversalTime().ToString("o"),
                BuildEndedAt = ended.ToUniversalTime().ToString("o"),
                TotalWarnings = summary.totalWarnings,
                TotalErrors = summary.totalErrors,
                BuildSteps = ProjectSteps(report, out var errors, out var warnings),
                Errors = errors,
                Warnings = warnings
            };

            // Files / packed assets are only meaningful on success (and packedAssets needs DetailedBuildReport).
            if (success)
            {
                result.Files = ProjectFiles(report);
                result.PackedAssets = ProjectPackedAssets(report);
            }

            return result;
        }

        static List<BuildFileEntry> ProjectFiles(BuildReport report)
        {
            var files = new List<BuildFileEntry>();
            try
            {
                foreach (var f in report.GetFiles())
                    files.Add(new BuildFileEntry { Path = f.path, Role = f.role, SizeBytes = (long)f.size });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[pipeline] build file projection failed: {ex.Message}");
            }
            return files;
        }

        static List<PackedAssetEntry> ProjectPackedAssets(BuildReport report)
        {
            var packed = new List<PackedAssetEntry>();
            if (report.packedAssets == null)
                return packed;

            foreach (var bundle in report.packedAssets)
            {
                var entry = new PackedAssetEntry
                {
                    BundlePath = bundle.shortPath,
                    OverheadBytes = (long)bundle.overhead,
                    Contents = new List<PackedAssetContent>()
                };

                if (bundle.contents != null)
                {
                    foreach (var c in bundle.contents)
                    {
                        entry.Contents.Add(new PackedAssetContent
                        {
                            AssetPath = c.sourceAssetPath,
                            Type = c.type != null ? c.type.Name : null,
                            Guid = c.sourceAssetGUID.ToString(),
                            SizeBytes = (long)c.packedSize
                        });
                    }
                }

                packed.Add(entry);
            }
            return packed;
        }

        static List<BuildStepEntry> ProjectSteps(BuildReport report,
            out List<BuildIssue> errors, out List<BuildIssue> warnings)
        {
            errors = new List<BuildIssue>();
            warnings = new List<BuildIssue>();

            var steps = new List<BuildStepEntry>();
            if (report.steps == null)
                return steps;

            foreach (var step in report.steps)
            {
                var entry = new BuildStepEntry
                {
                    Name = step.name,
                    DurationMs = (long)step.duration.TotalMilliseconds,
                    Depth = (int)step.depth,
                    Messages = new List<BuildStepMessageEntry>()
                };

                if (step.messages != null)
                {
                    foreach (var m in step.messages)
                    {
                        entry.Messages.Add(new BuildStepMessageEntry { Type = MapLogType(m.type), Content = m.content });

                        if (m.type == LogType.Error || m.type == LogType.Assert || m.type == LogType.Exception)
                            errors.Add(BuildIssue.From(m.content));
                        else if (m.type == LogType.Warning)
                            warnings.Add(BuildIssue.From(m.content));
                    }
                }

                steps.Add(entry);
            }
            return steps;
        }

        static string MapLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return "Warning";
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception: return "Error";
                default: return "Log";
            }
        }

        // ---- Helpers ---------------------------------------------------------------------------

        /// <summary>Case-insensitive parse of a <see cref="BuildTarget"/> enum name.</summary>
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

        static string Invalid(string field, string message) => $"{field}: {message}";

        static string NewBuildId() => "build_" + Guid.NewGuid().ToString("N").Substring(0, 12);

        static string ResolveOutput(string output, BuildTarget target)
        {
            var projectRoot = ProjectPaths.ProjectRoot;

            if (!string.IsNullOrWhiteSpace(output))
                return ProjectPaths.ResolveFile(output);

            var product = string.IsNullOrEmpty(PlayerSettings.productName) ? "Player" : PlayerSettings.productName;
            return Path.Combine(projectRoot, "Builds", target.ToString(), product + ExtensionFor(target));
        }

        /// <summary>True if some ancestor directory of <paramref name="path"/> already exists — a cheap,
        /// non-mutating check that the build output folder is creatable.</summary>
        static bool HasExistingAncestor(string path)
        {
            var dir = Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(dir))
                    return true;
                dir = Path.GetDirectoryName(dir);
            }
            return false;
        }

        static string ExtensionFor(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.Android:
                    return ".apk";
                default:
                    return string.Empty; // Linux standalone, dedicated server, etc.
            }
        }

        /// <summary>
        /// Validate <c>profile-name</c> when the build is queued, adding any problem to <paramref name="errors"/>,
        /// and return the enabled scenes the profile builds (its own list when it overrides the global one).
        /// Refuses a profile that is missing, that disagrees with an explicit <c>target</c>, or whose
        /// activation would switch the build target or subtarget — a reimport and domain reload inside the
        /// build, which Unity disallows in batch mode (the same rule as <c>create_build_profile --activate</c>).
        /// </summary>
        static string[] ValidateProfile(string name, string targetArg, BuildTarget target, bool needScenes, List<string> errors)
        {
            if (!BuildProfiles.IsSupported)
            {
                errors.Add(Invalid("profile-name", "Build Profiles require Unity 6 (6000.0) or later."));
                return null;
            }

            var info = BuildProfiles.FindByName(name);
            if (info == null)
            {
                errors.Add(Invalid("profile-name", $"No Build Profile named '{name}'. See {CommandNames.BuildListProfiles}."));
                return null;
            }

            if (!string.IsNullOrWhiteSpace(targetArg) && !string.Equals(info.Platform, target.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Omitting target is enough only when the profile's platform is already active; otherwise
                // the next attempt would hit the target-switch refusal below, so name the switch now.
                var profileOnActiveTarget = string.Equals(info.Platform, EditorUserBuildSettings.activeBuildTarget.ToString(), StringComparison.OrdinalIgnoreCase);
                errors.Add(Invalid("profile-name",
                    $"Build Profile '{info.Name}' is for {info.Platform}, but target is {target}. " +
                    (profileOnActiveTarget
                        ? "Omit target to build for the profile's platform."
                        : $"To build for the profile's platform, run {CommandNames.BuildTargetSwitch} with target={info.Platform} and confirm=true, " +
                          $"wait for {CommandNames.BuildTargetSwitchStatus} to report completion, then build without target.")));
            }
            else
            {
                var platform = PlatformOf(info);
                var change = BuildProfileCommands.ActivationSwitch(platform, out var targetDiffers);
                if (change != null)
                    errors.Add(Invalid("profile-name",
                        $"Activating Build Profile '{info.Name}' would switch {change}, a full reimport and domain reload that a build cannot run. " +
                        (targetDiffers
                            ? $"Run {CommandNames.BuildTargetSwitch} with target={platform.BuildTarget} and confirm=true first, and wait for " +
                              CommandNames.BuildTargetSwitchStatus + " to report completion."
                            : "Switch the subtarget from the Build Profiles window first.")));
                // Without a readable platform id the subtarget is unknown, so on a standalone target a
                // Server profile cannot be told from a Player one. Refuse rather than risk switching the
                // subtarget inside the build. An already-active profile switches nothing when activated.
                else if (platform.Subtarget == null && !info.IsActive &&
                         BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Standalone)
                    errors.Add(Invalid("profile-name",
                        $"This editor cannot tell whether Build Profile '{info.Name}' is a Player or a Server profile, so activating it " +
                        "could switch the standalone subtarget inside the build. Activate it from the Build Profiles window first."));
            }

            if (!needScenes)
                return null;

            var scenes = BuildProfiles.GetScenesForBuild(info);
            if (scenes == null)
            {
                errors.Add(Invalid("profile-name", $"This editor cannot read Build Profile '{info.Name}''s scene list. Pass scenes explicitly."));
                return null;
            }
            return scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }

        /// <summary>
        /// The profile's platform, by its platform id where readable so the standalone subtarget is known;
        /// otherwise just its build target.
        /// </summary>
        static BuildPlatformInfo PlatformOf(BuildProfileInfo info)
        {
            var id = BuildProfiles.ReadPlatformId(BuildProfiles.Load(info));
            if (id != null && BuildProfilePlatforms.IsSupported && BuildProfilePlatforms.TryResolve(id, "", out var platform, out _))
                return platform;
            return new BuildPlatformInfo { BuildTarget = info.Platform };
        }

        /// <summary>
        /// Activate the profile validated when the build was queued. Throws if it is gone or cannot be
        /// activated, which fails the build: the caller asked for this profile, not the current settings.
        /// </summary>
        static void ActivateBuildProfile(string profileName)
        {
            var info = BuildProfiles.FindByName(profileName)
                ?? throw new InvalidOperationException($"Build Profile '{profileName}' no longer exists.");
            if (!BuildProfiles.TrySetActive(info))
                throw new InvalidOperationException($"Build Profile '{profileName}' could not be activated.");
        }

        /// <summary>
        /// Run <paramref name="fn"/> on the Unity main thread. Off-thread (the HTTP path) it marshals
        /// through the live server's dispatcher; on the main thread (a direct in-process/test call, or no
        /// server running) it runs inline. Mirrors <see cref="PackageManager.PackageManagerCommand"/>.
        /// </summary>
        static T RunOnMain<T>(Func<T> fn, TimeSpan timeout)
        {
            var dispatcher = PipelineServerStartup.Server?.Dispatcher;
            if (dispatcher != null && dispatcher.IsInitialized && !dispatcher.IsMainThread())
                return dispatcher.Invoke(fn, PipelineTimeout.StopBefore(timeout));
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
                Debug.LogError($"[Build] Failed to write status file: {ex.Message}");
            }
        }
    }
}
