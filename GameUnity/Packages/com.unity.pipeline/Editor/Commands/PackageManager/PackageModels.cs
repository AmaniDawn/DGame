using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Pipeline.Editor.Commands.PackageManager
{
    /// <summary>
    /// One package in a list/search/add result — a JSON-friendly projection of
    /// <c>UnityEditor.PackageManager.PackageInfo</c> (CLI-203).
    /// </summary>
    [Serializable]
    class PackageSummary
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("displayName")] public string DisplayName { get; set; }

        /// <summary>Resolved source: Registry, Embedded, Local, Git, BuiltIn, LocalTarball, Unknown.</summary>
        [JsonProperty("source")] public string Source { get; set; }

        [JsonProperty("resolvedPath")] public string ResolvedPath { get; set; }

        /// <summary>True when the package is a direct manifest dependency (vs. a transitive one).</summary>
        [JsonProperty("isDirectDependency")] public bool IsDirectDependency { get; set; }

        /// <summary>True when the package is currently installed/resolved in the project (vs. only
        /// available in the registry). Always true for an installed-scope listing.</summary>
        [JsonProperty("isInstalled")] public bool IsInstalled { get; set; }
    }

    /// <summary>
    /// Persisted status of the last async mutating operation (add / remove / resolve), written to a Temp
    /// status file that survives the domain reload an add/remove/resolve triggers (CLI-203). Read back
    /// verbatim by <c>package_status</c> so a caller can poll an async op — or validate one whose
    /// synchronous reply was lost to the reload.
    /// </summary>
    [Serializable]
    class PackageStatus
    {
        /// <summary>idle | in_progress | completed | failed.</summary>
        [JsonProperty("status")] public string Status { get; set; }

        /// <summary>add | remove | resolve.</summary>
        [JsonProperty("operation")] public string Operation { get; set; }

        /// <summary>The operation's subject (identifier/name), when applicable.</summary>
        [JsonProperty("argument")] public string Argument { get; set; }

        [JsonProperty("success")] public bool Success { get; set; }

        [JsonProperty("error")] public string Error { get; set; }

        [JsonProperty("message")] public string Message { get; set; }

        /// <summary>The operation triggers a recompile/domain reload; poll <c>recompile_status</c>.</summary>
        [JsonProperty("requiresRecompile")] public bool RequiresRecompile { get; set; }

        /// <summary>The added package (add only); null for remove/resolve.</summary>
        [JsonProperty("package")] public PackageSummary Package { get; set; }

        /// <summary>Manifest dependencies (name → version) after the operation.</summary>
        [JsonProperty("manifest")] public Dictionary<string, string> Manifest { get; set; }

        [JsonProperty("startedAt")] public string StartedAt { get; set; }
        [JsonProperty("completedAt")] public string CompletedAt { get; set; }
    }

    /// <summary>
    /// Result of a mutating package command — <c>package_add</c> / <c>package_remove</c> /
    /// <c>package_resolve</c> (CLI-203). These are dual-mode: by default the call returns
    /// <c>in_progress</c> immediately (poll <c>package_status</c>); with <c>wait=true</c> it blocks and
    /// this carries the final <c>completed</c> outcome. A refused, busy or failed operation writes none
    /// and exits non-zero, with the reason on the command's log. It reports the <c>confirm</c>/<c>dry-run</c>
    /// gate via <see cref="DryRun"/> / <see cref="Applied"/>. A successful add/remove
    /// leaves a recompile/domain reload in flight (<see cref="RequiresRecompile"/>) — poll
    /// <c>recompile_status</c> to wait for the editor to be ready.
    /// </summary>
    [Serializable]
    class PackageMutationResponse
    {
        /// <summary>add | remove | resolve.</summary>
        [JsonProperty("operation")] public string Operation { get; set; }

        /// <summary>The operation's subject (identifier/name), when applicable.</summary>
        [JsonProperty("argument")] public string Argument { get; set; }

        /// <summary>in_progress | completed | dry_run.</summary>
        [JsonProperty("status")] public string Status { get; set; }

        /// <summary>True only when the operation actually mutated the project.</summary>
        [JsonProperty("applied")] public bool Applied { get; set; }

        [JsonProperty("dryRun")] public bool DryRun { get; set; }

        /// <summary>Human-readable description of the intended/performed change.</summary>
        [JsonProperty("plan")] public string Plan { get; set; }

        /// <summary>The operation leaves a recompile/domain reload in flight; poll <c>recompile_status</c>.</summary>
        [JsonProperty("requiresRecompile")] public bool RequiresRecompile { get; set; }

        /// <summary>The added package (add only); null for remove/resolve.</summary>
        [JsonProperty("package")] public PackageSummary Package { get; set; }

        /// <summary>Manifest dependencies (name → version) after the operation.</summary>
        [JsonProperty("manifest")] public Dictionary<string, string> Manifest { get; set; }

        public static PackageMutationResponse InProgress(string operation, string argument, string plan) =>
            new PackageMutationResponse
            {
                Operation = operation,
                Argument = argument,
                Status = "in_progress",
                Applied = true,
                Plan = plan
            };

        public static PackageMutationResponse DryRunPreview(string operation, string argument, string plan) =>
            new PackageMutationResponse
            {
                Operation = operation,
                Argument = argument,
                Status = "dry_run",
                DryRun = true,
                Plan = plan
            };
    }
}
