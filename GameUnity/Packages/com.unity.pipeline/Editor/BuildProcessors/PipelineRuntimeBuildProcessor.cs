using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Unity.Pipeline.Editor.BuildProcessors
{
    /// <summary>
    /// Build processor for runtime Pipeline support. Two responsibilities:
    ///  - Validates RuntimePipelineManager components in build scenes (security settings) before
    ///    allowing builds with runtime Pipeline enabled.
    ///  - Bakes the project's hot reload scope (Assets + loaded package locations) into the manager
    ///    in each build scene. A running Player cannot resolve the project layout, so the absolute
    ///    roots it is allowed to hot reload from must be captured at build time.
    /// Components control their behavior directly without conditional compilation.
    /// </summary>
#if UNITY_6000_3_OR_NEWER
    public class PipelineRuntimeBuildProcessor : IPreprocessBuildWithContext, IProcessSceneWithReport
#else
    public class PipelineRuntimeBuildProcessor : IPreprocessBuildWithReport, IProcessSceneWithReport
#endif
    {
        public int callbackOrder => 0;

#if UNITY_6000_3_OR_NEWER
        public void OnPreprocessBuild(BuildCallbackContext ctx)
#else
        public void OnPreprocessBuild(BuildReport report)
#endif
        {
            // Integrity gate: fail the build if a bundled Roslyn DLL was swapped or modified.
            VerifyBundledChecksums();

            // Note: this opens and closes scene which invalidate all managers.
            // Find RuntimePipelineManager components in build scenes
            var managers = FindRuntimeManagersInBuildScenes();

            if (managers.Count == 0)
            {
                Debug.LogWarning("Pipeline: No RuntimePipelineManager components found in build scenes. Pipeline will be disabled in Player builds.");
                return;
            }

            if (managers.Count > 1)
            {
                var sceneNames = string.Join(", ", managers.Select(m => m.gameObject.scene.name + "/" + m.gameObject.name));
                Debug.LogWarning($"Pipeline: Multiple RuntimePipelineManager components found in build: {sceneNames}. Only the first enabled component will be used.");
            }

            // Find enabled managers
            var enabledManagers = managers.Where(m => m.enableInBuilds).ToList();

            if (enabledManagers.Count == 0)
            {
                Debug.LogWarning("Pipeline: RuntimePipelineManager components found, but all have enableInBuilds = false. Pipeline will be disabled in Player builds.");
                return;
            }

            if (enabledManagers.Count > 1)
            {
                var enabledNames = string.Join(", ", enabledManagers.Select(m => m.gameObject.scene.name + "/" + m.gameObject.name));
                Debug.LogWarning($"Pipeline: Multiple enabled RuntimePipelineManager components found: {enabledNames}. Using the first one.");
            }

            var activeManager = enabledManagers[0];

            // Validate the active manager configuration
            var validationResult = activeManager.ValidateConfiguration();

            if (!validationResult.IsValid)
            {
                throw new BuildFailedException($"Pipeline: Runtime configuration validation failed for {activeManager.gameObject.scene.name}/{activeManager.gameObject.name}: {validationResult.Message}");
            }

            if (validationResult.Level == "warning")
            {
                Debug.LogWarning($"Pipeline: Runtime configuration warning for {activeManager.gameObject.scene.name}/{activeManager.gameObject.name}: {validationResult.Message}");

                if (!EditorUserBuildSettings.development)
                {
                    Debug.LogWarning("Pipeline: Security warnings detected in release build. Consider reviewing configuration.");
                }
            }

            Debug.Log($"Pipeline: Runtime server ENABLED in build for {activeManager.gameObject.scene.name}/{activeManager.gameObject.name}");
        }

        /// <summary>
        /// Find all RuntimePipelineManager components in scenes that will be included in the build.
        /// </summary>
        private List<RuntimePipelineManager> FindRuntimeManagersInBuildScenes()
        {
            var managers = new List<RuntimePipelineManager>();

            var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();

            foreach (var buildScene in buildScenes)
            {
                var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);

                var sceneManagers = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<RuntimePipelineManager>(true))
                    .ToArray();
                managers.AddRange(sceneManagers);
            }
            return managers;
        }

        /// <summary>
        /// Bake the project's hot reload roots into the RuntimePipelineManager of each build scene.
        /// Edits the temporary build copy of the scene, so the user's saved scene is untouched.
        /// </summary>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // report is null when this runs on entering Play Mode; only bake during real builds.
            if (report == null)
            {
                return;
            }

            var roots = CollectProjectRoots();

            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var manager in go.GetComponentsInChildren<RuntimePipelineManager>(true))
                {
                    manager.SetAllowedReloadRoots(roots);
                }
            }
        }

        /// <summary>
        /// Absolute roots considered in-scope for runtime hot reload: the project's Assets folder
        /// plus the resolved location of every package loaded into the project (a local package may
        /// live anywhere on disk, not only under Packages).
        /// </summary>
        public static List<string> CollectProjectRoots()
        {
            var roots = new List<string> { Path.GetFullPath(Application.dataPath) };

            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!string.IsNullOrEmpty(package.resolvedPath))
                {
                    roots.Add(Path.GetFullPath(package.resolvedPath));
                }
            }

            return roots;
        }

        // Relative location of the bundled Roslyn DLLs + their integrity manifest within the package.
        private const string CodeAnalysisRelDir = "Runtime/Plugins/CodeAnalysis";
        private const string ChecksumsFileName = "CHECKSUMS";

        /// <summary>
        /// Verify the bundled Roslyn DLLs against the committed CHECKSUMS manifest, locating them
        /// relative to this package on disk. Throws <see cref="BuildFailedException"/> on any
        /// mismatch so a tampered/swapped DLL cannot be built into a player.
        /// </summary>
        public static void VerifyBundledChecksums()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(PipelineRuntimeBuildProcessor).Assembly);

            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                throw new BuildFailedException(
                    "Pipeline: could not locate the com.unity.pipeline package on disk to verify " +
                    "bundled Roslyn DLL integrity. Aborting build.");
            }

            var codeAnalysisDir = Path.Combine(packageInfo.resolvedPath, CodeAnalysisRelDir);
            var checksumsPath = Path.Combine(codeAnalysisDir, ChecksumsFileName);

            var error = VerifyChecksums(codeAnalysisDir, checksumsPath);
            if (error != null)
            {
                throw new BuildFailedException($"Pipeline: bundled Roslyn DLL integrity check failed. {error}");
            }
        }

        /// <summary>
        /// Core, side-effect-free integrity check (so it is directly unit-testable). Returns null
        /// when every DLL listed in <paramref name="checksumsPath"/> exists under
        /// <paramref name="codeAnalysisDir"/> with a matching SHA-256 and no unlisted DLL is present;
        /// otherwise returns a human-readable error describing the first problem found.
        /// </summary>
        public static string VerifyChecksums(string codeAnalysisDir, string checksumsPath)
        {
            if (!Directory.Exists(codeAnalysisDir))
                return $"DLL directory not found: {codeAnalysisDir}";
            if (!File.Exists(checksumsPath))
                return $"CHECKSUMS manifest not found: {checksumsPath}";

            var expected = ParseChecksums(checksumsPath);
            if (expected.Count == 0)
                return $"CHECKSUMS manifest has no entries: {checksumsPath}";

            // Every listed DLL must exist and match.
            foreach (var entry in expected)
            {
                var dllPath = Path.Combine(codeAnalysisDir, entry.Key);
                if (!File.Exists(dllPath))
                    return $"listed DLL is missing: {entry.Key}";

                var actual = ComputeSha256(dllPath);
                if (!string.Equals(actual, entry.Value, StringComparison.OrdinalIgnoreCase))
                    return $"hash mismatch for {entry.Key} (expected {entry.Value}, got {actual})";
            }

            // No unlisted DLL may sit alongside them (guards against an injected extra assembly).
            foreach (var dllPath in Directory.GetFiles(codeAnalysisDir, "*.dll"))
            {
                var name = Path.GetFileName(dllPath);
                if (!expected.ContainsKey(name))
                    return $"unexpected DLL not listed in CHECKSUMS: {name}";
            }

            return null;
        }

        /// <summary>SHA-256 of a file as a lowercase hex string.</summary>
        public static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Parse "<sha256>  <filename>  # comment" lines, skipping blanks and '#' comment lines.
        private static Dictionary<string, string> ParseChecksums(string checksumsPath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in File.ReadAllLines(checksumsPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                    continue;

                result[tokens[1]] = tokens[0];
            }

            return result;
        }
    }
}
