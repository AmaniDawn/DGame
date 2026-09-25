using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// CLI commands for hot reload operations.
    /// Provides runtime compilation and management of hot reload files.
    /// Follows existing [CliCommand] patterns and integrates with Pipeline Server.
    /// </summary>
    public static class HotReloadCommands
    {
        [CliCommand("reload_file_override", "Compile and apply hot reload file changes immediately", MainThreadRequired = true)]
        internal static HotReloadResponse ReloadFileOverride(
            [CliArg("filename", "Hot reload source file to compile (e.g. PlayerTweaks.cs)", Required = true)] string filename,
            [CliArg("timeout", "Compilation timeout in milliseconds")] int timeout = 30000,
            [CliArg("assemblyDir", "Directory to save compiled assemblies to disk (optional, default is in-memory only)")] string assemblyDir = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate filename input
                if (string.IsNullOrWhiteSpace(filename))
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "Bad Request",
                        "Filename parameter is required and cannot be empty",
                        stopwatch.ElapsedMilliseconds);
                }

                // Ensure filename ends with .cs
                if (!filename.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".cs";
                }

                // Validate timeout
                if (timeout <= 0 || timeout > 300000) // Max 5 minutes
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "Bad Request",
                        "Timeout must be between 1ms and 300000ms (5 minutes)",
                        stopwatch.ElapsedMilliseconds);
                }

                Debug.Log($"HotReload: Executing reload_file_override command (helper workflow): {filename}, timeout: {timeout}ms, assemblyDir: {assemblyDir ?? "in-memory"}");

                // Resolve the override file path (absolute, or relative to the project root).
                var fullPath = Path.IsPathRooted(filename) ? filename : Path.GetFullPath(filename);

                // In a Player build, the file must be inside the project's build-baked roots. The
                // project layout cannot be resolved from a running build, so this is skipped in the
                // editor (which resolves files against the live project).
                if (!Application.isEditor)
                {
                    var scopeCheck = ValidateReloadPath(fullPath, HotReloadRegistry.AllowedReloadRoots);
                    if (!scopeCheck.IsValid)
                    {
                        stopwatch.Stop();
                        return HotReloadResponse.CmdFailure(
                            scopeCheck.Error,
                            scopeCheck.ErrorDetails,
                            stopwatch.ElapsedMilliseconds);
                    }
                }

                if (!File.Exists(fullPath))
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "File Not Found",
                        $"Override file not found: {fullPath}",
                        stopwatch.ElapsedMilliseconds);
                }

                // Up-front validation so a misconfigured override file produces a clear, actionable
                // message instead of a confusing compiler error from generated/duplicate code.
                var validation = OverrideFileValidator.Validate(File.ReadAllText(fullPath), Path.GetFileName(fullPath));
                if (!validation.IsValid)
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "Invalid Override File",
                        validation.GetFormattedErrorMessage(),
                        stopwatch.ElapsedMilliseconds,
                        validation.Errors);
                }

                // Compile and apply (helper / separate override file workflow).
                var task = ExecuteHotReloadAsync(fullPath, timeout, assemblyDir);
                task.Wait();
                var result = task.Result;

                stopwatch.Stop();

                if (result != null)
                {
                    result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                }

                Debug.Log($"HotReload: reload_file_override completed in {stopwatch.ElapsedMilliseconds}ms, success: {result?.Success}");

                return result ?? HotReloadResponse.CmdFailure(
                    "Unknown Error",
                    "Hot reload compilation returned null result",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: reload_file_override command failed: {ex.Message}");
                Debug.LogError($"HotReload: Stack trace: {ex.StackTrace}");

                return HotReloadResponse.CmdFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        [CliCommand("reload_file", "Compile and apply in-place [HotReload] edits from a source file", MainThreadRequired = true)]
        public static HotReloadResponse ReloadFile(
            [CliArg("filename", "Source file containing [HotReload] methods (e.g. Assets/Scripts/Player.cs)", Required = true)] string filename,
            [CliArg("timeout", "Compilation timeout in milliseconds")] int timeout = 30000,
            [CliArg("assemblyDir", "Directory to save compiled assemblies to disk (optional, default is in-memory only)")] string assemblyDir = null,
            [CliArg("pdb", "Emit debug symbols (portable PDB) mapped to the original source so breakpoints bind in your editor. Compiles unoptimized.")] bool pdb = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "Bad Request",
                        "Filename parameter is required and cannot be empty",
                        stopwatch.ElapsedMilliseconds);
                }

                if (!filename.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".cs";
                }

                var fullPath = ResolveSourceFilePath(filename);
                if (string.IsNullOrEmpty(fullPath))
                {
                    stopwatch.Stop();
                    return HotReloadResponse.CmdFailure(
                        "File Not Found",
                        $"Could not locate source file: {filename}",
                        stopwatch.ElapsedMilliseconds);
                }

                // In a Player build, the file must be inside the project's build-baked roots. The
                // project layout cannot be resolved from a running build, so this is skipped in the
                // editor (which resolves files against the live project).
                if (!Application.isEditor)
                {
                    var scopeCheck = ValidateReloadPath(fullPath, HotReloadRegistry.AllowedReloadRoots);
                    if (!scopeCheck.IsValid)
                    {
                        stopwatch.Stop();
                        return HotReloadResponse.CmdFailure(
                            scopeCheck.Error,
                            scopeCheck.ErrorDetails,
                            stopwatch.ElapsedMilliseconds);
                    }
                }

                Debug.Log($"HotReload: Executing reload_file command: {fullPath}, timeout: {timeout}ms, assemblyDir: {assemblyDir ?? "in-memory"}, pdb: {pdb}");

                var task = InPlaceReloadProcessor.ProcessSourceFileAsync(fullPath, assemblyDir, pdb);
                task.Wait();
                var result = task.Result;

                stopwatch.Stop();

                if (result.Success)
                {
                    return HotReloadResponse.CmdSuccess(
                        result.AssemblyName,
                        $"In-place hot reload successful: {result.AssemblyName} with {result.RegisteredMethods.Count} methods",
                        result.RegisteredMethods,
                        stopwatch.ElapsedMilliseconds);
                }

                return HotReloadResponse.CmdFailure(
                    "In-Place Reload Failed",
                    result.ErrorMessage ?? "In-place hot reload processing failed",
                    stopwatch.ElapsedMilliseconds,
                    result.CompilationDiagnostics);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: reload_file command failed: {ex.Message}");
                Debug.LogError($"HotReload: Stack trace: {ex.StackTrace}");

                return HotReloadResponse.CmdFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        [CliCommand("cleanup_hotreload", "Remove old hot reload DLL versions and clear registry", MainThreadRequired = true, RuntimeOnly = true)]
        public static HotReloadResponse CleanupHotReload(
            [CliArg("assemblyDir", "Directory containing assemblies to cleanup", Required = true)] string assemblyDir,
            [CliArg("force_domain_reload", "Force Unity domain reload after cleanup")] bool forceDomainReload = true)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Debug.Log($"HotReload: Executing cleanup_hotreload command for directory: {assemblyDir}");

                // Execute cleanup
                var cleanupResult = HotReloadCompiler.CleanupHotReloadDlls(assemblyDir);

                stopwatch.Stop();

                if (cleanupResult.Success)
                {
                    var message = $"Cleanup successful: {cleanupResult.Message}";
                    if (cleanupResult.DeletedFiles.Count > 0)
                    {
                        message += $" Files removed: {string.Join(", ", cleanupResult.DeletedFiles)}";
                    }

                    // Force domain reload if requested (interrupts gameplay but ensures clean state)
                    if (forceDomainReload && Application.isEditor)
                    {
#if UNITY_EDITOR
                        Debug.Log("HotReload: Requesting Unity domain reload to clean memory state");
                        UnityEditor.EditorUtility.RequestScriptReload();
                        message += " Unity domain reload requested.";
#endif
                    }

                    Debug.Log($"HotReload: cleanup_hotreload completed successfully in {stopwatch.ElapsedMilliseconds}ms");

                    return HotReloadResponse.CmdSuccess(
                        "cleanup_completed",
                        message,
                        cleanupResult.DeletedFiles,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    return HotReloadResponse.CmdFailure(
                        "Cleanup Failed",
                        cleanupResult.Message,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: cleanup_hotreload command failed: {ex.Message}");

                return HotReloadResponse.CmdFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        [CliCommand("hotreload_status", "Show current hot reload registry status and statistics", MainThreadRequired = true, RuntimeOnly = true)]
        public static HotReloadResponse HotReloadStatus()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Get current registry stats
                var stats = HotReloadRegistry.GetStats();

                stopwatch.Stop();

                var statusMessage = $"Hot Reload Status - " +
                    $"Reloadable Methods: {stats.ReloadableMethodCount}, " +
                    $"Active Overrides: {stats.ActiveOverrideCount}, " +
                    $"Loaded Types: {stats.LoadedTypeCount}";

                Debug.Log($"HotReload: {statusMessage}");

                return HotReloadResponse.CmdSuccess(
                    "status_retrieved",
                    statusMessage,
                    stats.ActiveOverrideIds,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: hotreload_status command failed: {ex.Message}");

                return HotReloadResponse.CmdFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Validate that a resolved file path lives inside one of the allowed project roots and
        /// exists on disk. "In scope" means under the Assets folder or a loaded package's location.
        /// The path is normalized (collapsing any <c>..</c> segments) before the scope check, so a
        /// path that escapes the allowed roots via traversal is rejected. A null or empty root set
        /// matches nothing, so the path is rejected as out of scope. This is a security boundary —
        /// it prevents files outside the project from being compiled and injected into the assembly.
        /// </summary>
        public static PathValidationResult ValidateReloadPath(string resolvedPath, System.Collections.Generic.IReadOnlyCollection<string> allowedRoots)
        {
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return PathValidationResult.Invalid("Bad Request", "Resolved file path is empty.");
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(resolvedPath);
            }
            catch (Exception ex)
            {
                return PathValidationResult.Invalid("Bad Request", $"Invalid path: {ex.Message}");
            }

            var underAnyRoot = false;
            if (allowedRoots != null)
            {
                foreach (var root in allowedRoots)
                {
                    if (string.IsNullOrWhiteSpace(root))
                    {
                        continue;
                    }

                    string fullRoot;
                    try
                    {
                        fullRoot = Path.GetFullPath(root);
                    }
                    catch
                    {
                        continue;
                    }

                    if (IsUnderDirectory(fullPath, fullRoot))
                    {
                        underAnyRoot = true;
                        break;
                    }
                }
            }

            if (!underAnyRoot)
            {
                return PathValidationResult.Invalid(
                    "Out Of Project Scope",
                    $"Hot reload only accepts files inside the project's baked roots (Assets/ or a loaded " +
                    $"package). '{fullPath}' is outside the project scope and will not be compiled.");
            }

            if (!File.Exists(fullPath))
            {
                return PathValidationResult.Invalid("File Not Found", $"Source file not found: {fullPath}");
            }

            return PathValidationResult.Valid();
        }

        /// <summary>
        /// True when <paramref name="path"/> is contained within <paramref name="directory"/>.
        /// Both are expected to be normalized absolute paths. A trailing separator is appended to
        /// the directory so that a sibling such as "AssetsExtra" is not treated as being under "Assets".
        /// </summary>
        private static bool IsUnderDirectory(string path, string directory)
        {
            var prefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            // Windows file systems are case-insensitive; Unix file systems are case-sensitive.
            var comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return path.StartsWith(prefix, comparison);
        }

        /// <summary>
        /// Resolve source file path from various possible locations (used by the in-place command).
        /// </summary>
        private static string ResolveSourceFilePath(string filename)
        {
            if (Path.IsPathRooted(filename) && File.Exists(filename))
            {
                return filename;
            }

            var potentialPaths = new[]
            {
                Path.Combine("Assets", filename),
                Path.Combine("Assets", "Scripts", filename),
                filename // Project root
            };

            foreach (var path in potentialPaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }

        /// <summary>
        /// Execute hot reload compilation and application asynchronously (for separate override files).
        /// </summary>
        private static async Task<HotReloadResponse> ExecuteHotReloadAsync(string filename, int timeoutMs, string assemblyDir)
        {
            try
            {
                // Use the HotReloadCompiler to compile and apply the changes
                var compileResult = await HotReloadCompiler.CompileAndApplyAsync(filename, assemblyDir);

                if (!compileResult.IsSuccess)
                {
                    return HotReloadResponse.CmdFailure(
                        compileResult.Error ?? "Compilation Failed",
                        compileResult.ErrorDetails ?? "Hot reload compilation failed",
                        compileResult.ExecutionTimeMs,
                        compileResult.Diagnostics);
                }

                // Compiled, but no override actually bound (e.g. the target is not [HotReloadWithOverrides],
                // or the component is not in the scene / not in play mode). Report it rather than
                // claiming success when nothing changed.
                if (compileResult.RegisteredMethods.Count == 0)
                {
                    return HotReloadResponse.CmdFailure(
                        "No Overrides Applied",
                        compileResult.Diagnostics.Count > 0
                            ? "No hot reload overrides were applied:\n- " + string.Join("\n- ", compileResult.Diagnostics)
                            : "No [HotReloadOverrideMethod] overrides were applied. Ensure the target component is in the scene and in play mode.",
                        compileResult.ExecutionTimeMs,
                        compileResult.Diagnostics);
                }

                var message = $"Hot reload successful: {compileResult.AssemblyName} with {compileResult.RegisteredMethods.Count} methods";
                var response = HotReloadResponse.CmdSuccess(
                    compileResult.AssemblyName,
                    message,
                    compileResult.RegisteredMethods,
                    compileResult.ExecutionTimeMs);
                response.Diagnostics = compileResult.Diagnostics; // surface any partially-skipped overrides
                return response;
            }
            catch (TimeoutException)
            {
                return HotReloadResponse.CmdFailure(
                    "Timeout",
                    $"Hot reload compilation exceeded {timeoutMs}ms timeout",
                    timeoutMs);
            }
            catch (Exception ex)
            {
                return HotReloadResponse.CmdFailure(
                    "Execution Failed",
                    ex.ToString(),
                    0);
            }
        }
    }

    /// <summary>
    /// Response model for hot reload CLI commands.
    /// Provides consistent response format for all hot reload operations.
    /// </summary>
    public class HotReloadResponse : CommandExecutionResponse
    {
        /// <summary>
        /// Assembly name or operation identifier for successful operations.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// List of registered method IDs or files processed.
        /// </summary>
        public System.Collections.Generic.List<string> Items { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Compilation or processing diagnostics.
        /// </summary>
        public System.Collections.Generic.List<string> Diagnostics { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Create a successful hot reload response.
        /// </summary>
        public static HotReloadResponse CmdSuccess(string assemblyName, string message, System.Collections.Generic.List<string> items, long executionTimeMs)
        {
            return new HotReloadResponse
            {
                Success = true,
                AssemblyName = assemblyName,
                Message = message,
                Items = items ?? new System.Collections.Generic.List<string>(),
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Create a failed hot reload response.
        /// </summary>
        public static HotReloadResponse CmdFailure(string error, string errorDetails, long executionTimeMs, System.Collections.Generic.List<string> diagnostics = null)
        {
            return new HotReloadResponse
            {
                Success = false,
                Error = error,
                ErrorDetails = errorDetails,
                ExecutionTimeMs = executionTimeMs,
                Diagnostics = diagnostics ?? new System.Collections.Generic.List<string>()
            };
        }
    }

    /// <summary>
    /// Result of validating a hot reload source path. <see cref="Error"/> is a short category and
    /// <see cref="ErrorDetails"/> the human-readable explanation, matching the shape consumed by
    /// <see cref="HotReloadResponse.CmdFailure"/>.
    /// </summary>
    public class PathValidationResult
    {
        public bool IsValid { get; private set; }
        public string Error { get; private set; }
        public string ErrorDetails { get; private set; }

        public static PathValidationResult Valid()
        {
            return new PathValidationResult { IsValid = true };
        }

        public static PathValidationResult Invalid(string error, string errorDetails)
        {
            return new PathValidationResult { IsValid = false, Error = error, ErrorDetails = errorDetails };
        }
    }
}