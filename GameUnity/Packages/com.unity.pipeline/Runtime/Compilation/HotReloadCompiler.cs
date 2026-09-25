using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Models;
using Unity.Pipeline.Threading;
using UnityEngine;

namespace Unity.Pipeline.Compilation
{
    /// <summary>
    /// Compiles hot reload source files using shared RoslynCompilationService.
    /// Creates versioned DLLs and manages hot reload assembly lifecycle.
    /// Fixed deadlock issue by adding main thread detection like EvalCodeCompiler.
    /// </summary>
    public static class HotReloadCompiler
    {
#if UNITY_EDITOR || (UNITY_STANDALONE && DEBUG)

        private static readonly Dictionary<string, int> _versionTracker = new();
        private const string HotReloadTempDir = "Temp/HotReload";

        /// <summary>
        /// Compile a hot reload source file and apply the overrides immediately.
        /// Fixed deadlock by detecting main thread and running synchronously when needed.
        /// </summary>
        /// <param name="filename">Hot reload file to compile</param>
        /// <param name="assemblyDir">Optional directory to save compiled assembly to disk. If null, assembly stays in memory only.</param>
        public static Task<HotReloadCompileResult> CompileAndApplyAsync(string filename, string assemblyDir = null)
        {
            // Hot reload commands are MainThreadRequired, so we are already on the main thread.
            // Run synchronously (no background thread / dispatcher). Returns a completed Task for
            // signature compatibility.
            return Task.FromResult(CompileAndApplyOnMainThread(filename, assemblyDir));
        }

        public static string GetHotReloadPath(string filePath)
        {
            if (Path.IsPathRooted(filePath))
                return filePath;

            // Relative paths are resolved against the current working directory (the Unity
            // project root). Override files can live in any folder; there is no special
            // "HotReload" location.
            return Path.GetFullPath(filePath);
        }

        /// <summary>
        /// Compile and apply on main thread synchronously to avoid deadlocks.
        /// </summary>
        /// <param name="filename">Hot reload file to compile</param>
        /// <param name="assemblyDir">Optional directory to save compiled assembly to disk. If null, assembly stays in memory only.</param>
        public static HotReloadCompileResult CompileAndApplyOnMainThread(string filename, string assemblyDir = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Resolve the override file path (absolute, or relative to the project root).
                var hotReloadPath = GetHotReloadPath(filename);
                if (!File.Exists(hotReloadPath))
                {
                    return HotReloadCompileResult.Failure(
                        "File Not Found",
                        $"Hot reload file not found: {hotReloadPath}",
                        stopwatch.ElapsedMilliseconds);
                }

                Debug.Log($"HotReload: Starting synchronous compilation of {filename}");

                // Read source code
                var sourceCode = File.ReadAllText(hotReloadPath);
                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    return HotReloadCompileResult.Failure(
                        "Empty File",
                        $"Hot reload file is empty: {hotReloadPath}",
                        stopwatch.ElapsedMilliseconds);
                }

                // Generate versioned assembly name
                var baseFilename = Path.GetFileNameWithoutExtension(filename);
                var nextVersion = GetNextVersion(baseFilename);
                var assemblyName = $"{baseFilename}_{nextVersion:D3}";

                // Ensure temp directory exists
                Directory.CreateDirectory(HotReloadTempDir);

                // Compile using shared RoslynCompilationService
                var compilationResult = CompileHotReloadAssembly(sourceCode, assemblyName);

                if (!compilationResult.Success)
                {
                    return HotReloadCompileResult.Failure(
                        "Compilation Failed",
                        "Hot reload file compilation failed",
                        stopwatch.ElapsedMilliseconds,
                        compilationResult.Diagnostics.Select(d => d.Message).ToList());
                }

                // Register hot reload methods from compiled assembly
                var registeredMethods = RegisterHotReloadMethods(compilationResult.Assembly, assemblyName, out var skippedOverrides);

                // Save assembly to disk if assemblyDir is specified
                string actualAssemblyPath = null;
                if (!string.IsNullOrWhiteSpace(assemblyDir))
                {
                    Directory.CreateDirectory(assemblyDir);
                    actualAssemblyPath = Path.Combine(assemblyDir, $"{assemblyName}.dll");
                    File.WriteAllBytes(actualAssemblyPath, compilationResult.AssemblyBytes);
                    Debug.Log($"HotReload: Assembly saved to disk: {actualAssemblyPath}");
                }
                else
                {
                    actualAssemblyPath = Path.Combine(HotReloadTempDir, $"{assemblyName}.dll"); // For compatibility (in-memory)
                }

                stopwatch.Stop();

                Debug.Log($"HotReload: Successfully compiled {filename} -> {assemblyName}.dll with {registeredMethods.Count} methods in {stopwatch.ElapsedMilliseconds}ms");

                var compileResult = HotReloadCompileResult.Success(
                    assemblyName,
                    actualAssemblyPath,
                    registeredMethods,
                    stopwatch.ElapsedMilliseconds);
                compileResult.Diagnostics = skippedOverrides;
                return compileResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: Synchronous compilation failed for {filename}: {ex.Message}");
                return HotReloadCompileResult.Failure(
                    "Exception",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Internal async compilation implementation for background threads.
        /// </summary>
        /// <param name="filename">Hot reload file to compile</param>
        /// <param name="assemblyDir">Optional directory to save compiled assembly to disk. If null, assembly stays in memory only.</param>
        private static async Task<HotReloadCompileResult> CompileAndApplyInternalAsync(string filename, string assemblyDir = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Resolve the override file path (absolute, or relative to the project root).
                var hotReloadPath = GetHotReloadPath(filename);
                if (!File.Exists(hotReloadPath))
                {
                    return HotReloadCompileResult.Failure(
                        "File Not Found",
                        $"Hot reload file not found: {hotReloadPath}",
                        stopwatch.ElapsedMilliseconds);
                }

                Debug.Log($"HotReload: Starting async compilation of {filename}");

                // Read source code
                var sourceCode = File.ReadAllText(hotReloadPath);
                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    return HotReloadCompileResult.Failure(
                        "Empty File",
                        $"Hot reload file is empty: {hotReloadPath}",
                        stopwatch.ElapsedMilliseconds);
                }

                // Generate versioned assembly name
                var baseFilename = Path.GetFileNameWithoutExtension(filename);
                var nextVersion = GetNextVersion(baseFilename);
                var assemblyName = $"{baseFilename}_{nextVersion:D3}";

                // Ensure temp directory exists
                Directory.CreateDirectory(HotReloadTempDir);

                // Compile using shared RoslynCompilationService async
                var compilationResult = await CompileHotReloadAssemblyAsync(sourceCode, assemblyName);

                if (!compilationResult.Success)
                {
                    return HotReloadCompileResult.Failure(
                        "Compilation Failed",
                        "Hot reload file compilation failed",
                        stopwatch.ElapsedMilliseconds,
                        compilationResult.Diagnostics.Select(d => d.Message).ToList());
                }

                // Register hot reload methods from compiled assembly
                var registeredMethods = RegisterHotReloadMethods(compilationResult.Assembly, assemblyName, out var skippedOverrides);

                // Save assembly to disk if assemblyDir is specified
                string actualAssemblyPath = null;
                if (!string.IsNullOrWhiteSpace(assemblyDir))
                {
                    Directory.CreateDirectory(assemblyDir);
                    actualAssemblyPath = Path.Combine(assemblyDir, $"{assemblyName}.dll");
                    File.WriteAllBytes(actualAssemblyPath, compilationResult.AssemblyBytes);
                    Debug.Log($"HotReload: Assembly saved to disk: {actualAssemblyPath}");
                }
                else
                {
                    actualAssemblyPath = Path.Combine(HotReloadTempDir, $"{assemblyName}.dll"); // For compatibility (in-memory)
                }

                stopwatch.Stop();

                Debug.Log($"HotReload: Successfully compiled {filename} -> {assemblyName}.dll with {registeredMethods.Count} methods in {stopwatch.ElapsedMilliseconds}ms");

                var compileResult = HotReloadCompileResult.Success(
                    assemblyName,
                    actualAssemblyPath,
                    registeredMethods,
                    stopwatch.ElapsedMilliseconds);
                compileResult.Diagnostics = skippedOverrides;
                return compileResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: Async compilation failed for {filename}: {ex.Message}");
                return HotReloadCompileResult.Failure(
                    "Exception",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Compile hot reload source code directly (for in-place editing workflow).
        /// </summary>
        /// <param name="sourceCode">Hot reload source code to compile</param>
        /// <param name="baseFileName">Base filename for versioned assembly naming</param>
        /// <param name="assemblyDir">Optional directory to save compiled assembly to disk</param>
        public static Task<HotReloadCompilationResult> CompileSourceCodeAsync(string sourceCode, string baseFileName, string assemblyDir = null, bool emitPdb = false, string documentPath = null)
        {
            // Runs synchronously on the calling (main) thread; returns a completed Task.
            return Task.FromResult(CompileSourceCodeOnMainThread(sourceCode, baseFileName, assemblyDir, emitPdb, documentPath));
        }

        /// <summary>
        /// Compile source code on main thread synchronously to avoid deadlocks.
        /// </summary>
        /// <param name="emitPdb">Emit a portable PDB and load symbols so breakpoints can bind.</param>
        /// <param name="documentPath">Source document path recorded in the PDB (the original .cs file).</param>
        public static HotReloadCompilationResult CompileSourceCodeOnMainThread(string sourceCode, string baseFileName, string assemblyDir = null, bool emitPdb = false, string documentPath = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Debug.Log($"HotReload: Starting synchronous source code compilation for {baseFileName}");

                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    return HotReloadCompilationResult.Failure(
                        "Empty Source Code",
                        "Source code cannot be empty",
                        stopwatch.ElapsedMilliseconds);
                }

                // Generate versioned assembly name
                var nextVersion = GetNextVersion(baseFileName);
                var assemblyName = $"{baseFileName}_{nextVersion:D3}";

                // Ensure temp directory exists
                Directory.CreateDirectory(HotReloadTempDir);

                // Compile using shared RoslynCompilationService
                var compilationResult = CompileHotReloadAssembly(sourceCode, assemblyName, emitPdb, documentPath);

                if (!compilationResult.Success)
                {
                    return HotReloadCompilationResult.Failure(
                        "Compilation Failed",
                        "Hot reload source code compilation failed",
                        stopwatch.ElapsedMilliseconds,
                        compilationResult.Diagnostics.Select(d => d.Message).ToList());
                }

                // Register hot reload methods from compiled assembly
                var registeredMethods = RegisterHotReloadMethods(compilationResult.Assembly, assemblyName, out var skippedOverrides);

                // Save assembly to disk if assemblyDir is specified
                string actualAssemblyPath = null;
                if (!string.IsNullOrWhiteSpace(assemblyDir))
                {
                    Directory.CreateDirectory(assemblyDir);
                    actualAssemblyPath = Path.Combine(assemblyDir, $"{assemblyName}.dll");
                    File.WriteAllBytes(actualAssemblyPath, compilationResult.AssemblyBytes);
                    Debug.Log($"HotReload: Assembly saved to disk: {actualAssemblyPath}");

                    // Symbols are loaded in-memory; also write the .pdb next to the .dll for convenience.
                    if (compilationResult.PdbBytes != null)
                    {
                        var pdbPath = Path.Combine(assemblyDir, $"{assemblyName}.pdb");
                        File.WriteAllBytes(pdbPath, compilationResult.PdbBytes);
                        Debug.Log($"HotReload: Symbols saved to disk: {pdbPath}");
                    }
                }
                else
                {
                    actualAssemblyPath = Path.Combine(HotReloadTempDir, $"{assemblyName}.dll"); // For compatibility (in-memory)
                }

                stopwatch.Stop();

                Debug.Log($"HotReload: Successfully compiled source code -> {assemblyName}.dll with {registeredMethods.Count} methods in {stopwatch.ElapsedMilliseconds}ms");

                var sourceCompileResult = HotReloadCompilationResult.Success(
                    assemblyName,
                    actualAssemblyPath,
                    registeredMethods,
                    stopwatch.ElapsedMilliseconds);
                sourceCompileResult.Diagnostics = skippedOverrides;
                return sourceCompileResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: Synchronous source code compilation failed for {baseFileName}: {ex.Message}");
                return HotReloadCompilationResult.Failure(
                    "Exception",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Internal async source code compilation implementation for background threads.
        /// </summary>
        private static async Task<HotReloadCompilationResult> CompileSourceCodeInternalAsync(string sourceCode, string baseFileName, string assemblyDir = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Debug.Log($"HotReload: Starting async source code compilation for {baseFileName}");

                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    return HotReloadCompilationResult.Failure(
                        "Empty Source Code",
                        "Source code cannot be empty",
                        stopwatch.ElapsedMilliseconds);
                }

                // Generate versioned assembly name
                var nextVersion = GetNextVersion(baseFileName);
                var assemblyName = $"{baseFileName}_{nextVersion:D3}";

                // Ensure temp directory exists
                Directory.CreateDirectory(HotReloadTempDir);

                // Compile using shared RoslynCompilationService async
                var compilationResult = await CompileHotReloadAssemblyAsync(sourceCode, assemblyName);

                if (!compilationResult.Success)
                {
                    return HotReloadCompilationResult.Failure(
                        "Compilation Failed",
                        "Hot reload source code compilation failed",
                        stopwatch.ElapsedMilliseconds,
                        compilationResult.Diagnostics.Select(d => d.Message).ToList());
                }

                // Register hot reload methods from compiled assembly
                var registeredMethods = RegisterHotReloadMethods(compilationResult.Assembly, assemblyName, out var skippedOverrides);

                // Save assembly to disk if assemblyDir is specified
                string actualAssemblyPath = null;
                if (!string.IsNullOrWhiteSpace(assemblyDir))
                {
                    Directory.CreateDirectory(assemblyDir);
                    actualAssemblyPath = Path.Combine(assemblyDir, $"{assemblyName}.dll");
                    File.WriteAllBytes(actualAssemblyPath, compilationResult.AssemblyBytes);
                    Debug.Log($"HotReload: Assembly saved to disk: {actualAssemblyPath}");
                }
                else
                {
                    actualAssemblyPath = Path.Combine(HotReloadTempDir, $"{assemblyName}.dll"); // For compatibility (in-memory)
                }

                stopwatch.Stop();

                Debug.Log($"HotReload: Successfully compiled source code -> {assemblyName}.dll with {registeredMethods.Count} methods in {stopwatch.ElapsedMilliseconds}ms");

                var sourceCompileResult = HotReloadCompilationResult.Success(
                    assemblyName,
                    actualAssemblyPath,
                    registeredMethods,
                    stopwatch.ElapsedMilliseconds);
                sourceCompileResult.Diagnostics = skippedOverrides;
                return sourceCompileResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: Async source code compilation failed for {baseFileName}: {ex.Message}");
                return HotReloadCompilationResult.Failure(
                    "Exception",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Clean up old hot reload DLL versions, keeping only the latest for each base filename.
        /// </summary>
        public static HotReloadCleanupResult CleanupHotReloadDlls(string assemblyDir = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var deletedFiles = new List<string>();

            try
            {
                // Use specified assemblyDir or default to HotReloadTempDir
                var targetDir = !string.IsNullOrWhiteSpace(assemblyDir) ? assemblyDir : HotReloadTempDir;

                if (!Directory.Exists(targetDir))
                {
                    return new HotReloadCleanupResult
                    {
                        Success = true,
                        DeletedFiles = deletedFiles,
                        Message = $"No hot reload directory found: {targetDir}",
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                var dllFiles = Directory.GetFiles(targetDir, "*.dll");
                var groupedFiles = dllFiles
                    .Select(f => new
                    {
                        FilePath = f,
                        FileName = Path.GetFileNameWithoutExtension(f),
                        BaseFilename = ExtractBaseFilename(Path.GetFileNameWithoutExtension(f)),
                        Version = ExtractVersion(Path.GetFileNameWithoutExtension(f))
                    })
                    .Where(f => f.Version > 0) // Only versioned hot reload DLLs
                    .GroupBy(f => f.BaseFilename)
                    .ToList();

                foreach (var group in groupedFiles)
                {
                    var sortedFiles = group.OrderByDescending(f => f.Version).ToList();

                    // Keep the latest version, delete older ones
                    for (int i = 1; i < sortedFiles.Count; i++)
                    {
                        var fileToDelete = sortedFiles[i];
                        File.Delete(fileToDelete.FilePath);
                        deletedFiles.Add(fileToDelete.FileName + ".dll");
                        Debug.Log($"HotReload: Deleted old version {fileToDelete.FileName}.dll");
                    }
                }

                // Clear registry and reset version tracker
                HotReloadRegistry.ClearAllOverrides();
                _versionTracker.Clear();

                stopwatch.Stop();

                Debug.Log($"HotReload: Cleanup completed - deleted {deletedFiles.Count} files in {stopwatch.ElapsedMilliseconds}ms");

                return new HotReloadCleanupResult
                {
                    Success = true,
                    DeletedFiles = deletedFiles,
                    Message = $"Deleted {deletedFiles.Count} old DLL versions",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"HotReload: Cleanup failed: {ex.Message}");

                return new HotReloadCleanupResult
                {
                    Success = false,
                    DeletedFiles = deletedFiles,
                    Message = $"Cleanup failed: {ex.Message}",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Get the next version number for a base filename.
        /// </summary>
        private static int GetNextVersion(string baseFilename)
        {
            if (!_versionTracker.ContainsKey(baseFilename))
            {
                _versionTracker[baseFilename] = 0;
            }

            return ++_versionTracker[baseFilename];
        }

        /// <summary>
        /// Wrap hot reload user code with proper using statements and namespace.
        /// </summary>

        /// <summary>
        /// Compile hot reload source code using shared RoslynCompilationService.
        /// Synchronous compilation for main thread use.
        /// </summary>
        private static CompilationResult CompileHotReloadAssembly(string sourceCode, string assemblyName, bool emitPdb = false, string documentPath = null)
        {
            var request = new CompilationRequest
            {
                SourceCode = sourceCode,
                AssemblyName = assemblyName,
                // Ensure Unity.Pipeline assemblies are included (contains HotReloadOverrideMethodAttribute)
                // Also include test assemblies for testing scenarios
                AdditionalAssemblyPrefixes = new[] { "Unity.Pipeline", "Unity.Pipeline.Tests" },
                EmitDebugInformation = emitPdb,
                DocumentPath = documentPath
            };

            return RoslynCompilationService.Compile(request);
        }

        /// <summary>
        /// Compile hot reload source code using shared RoslynCompilationService.
        /// Async compilation for background thread use.
        /// </summary>
        private static async Task<CompilationResult> CompileHotReloadAssemblyAsync(string sourceCode, string assemblyName)
        {
            var request = new CompilationRequest
            {
                SourceCode = sourceCode,
                AssemblyName = assemblyName,
                // Ensure Unity.Pipeline assemblies are included (contains HotReloadOverrideMethodAttribute)
                // Also include test assemblies for testing scenarios
                AdditionalAssemblyPrefixes = new[] { "Unity.Pipeline", "Unity.Pipeline.Tests" }
            };

            return await RoslynCompilationService.CompileAsync(request);
        }

        /// <summary>
        /// Register hot reload methods from compiled assembly with the registry.
        /// Also registers any [HotReloadWithOverrides] target methods found in the assembly.
        /// Only overrides that actually bind are returned; overrides that were skipped (e.g. the
        /// target is not currently [HotReloadWithOverrides], or a signature mismatch) are reported via
        /// <paramref name="skipped"/> with a user-facing reason.
        /// </summary>
        private static List<string> RegisterHotReloadMethods(Assembly assembly, string assemblyId, out List<string> skipped)
        {
            var registeredMethods = new List<string>();
            skipped = new List<string>();

            try
            {
                Debug.Log($"HotReload: Registering methods from assembly {assemblyId} with {assembly.GetTypes().Length} types");

                // Register assembly types for discovery
                foreach (var type in assembly.GetTypes())
                {
                    Debug.Log($"HotReload: Processing type {type.Name} in {type.Namespace}");
                    HotReloadRegistry.RegisterHotReloadType(type, assemblyId);

                    // First, scan for [HotReloadWithOverrides] methods to register as targets
                    var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in allMethods)
                    {
                        var reloadableAttr = method.GetCustomAttribute<HotReloadWithOverridesAttribute>();
                        if (reloadableAttr != null)
                        {
                            Debug.Log($"HotReload: Found reloadable method {method.Name} with ID {reloadableAttr.Id}");
                            HotReloadRegistry.RegisterReloadableMethod(method, reloadableAttr);
                        }
                    }

                    // Then, find methods with [HotReloadOverrideMethod] attribute for overrides
                    var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    Debug.Log($"HotReload: Found {staticMethods.Length} static methods in {type.Name}");

                    foreach (var method in staticMethods)
                    {
                        var customAttributes = method.GetCustomAttributes(true);
                        Debug.Log($"HotReload: Method {method.Name} has {customAttributes.Length} custom attributes: {string.Join(", ", customAttributes.Select(a => a.GetType().Name))}");

                        // Try multiple ways to find the attribute
                        var hotReloadAttr = method.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
                        if (hotReloadAttr == null)
                        {
                            // Try by name in case of type loading issues
                            var attrByName = customAttributes.FirstOrDefault(a => a.GetType().Name == "HotReloadOverrideMethodAttribute");
                            if (attrByName != null)
                            {
                                Debug.Log($"HotReload: Found HotReloadOverrideMethodAttribute by name on {method.Name}");
                                // Cast to the attribute type
                                hotReloadAttr = attrByName as HotReloadOverrideMethodAttribute;
                            }
                        }

                        if (hotReloadAttr != null)
                        {
                            Debug.Log($"HotReload: Registering method override {method.Name} -> {hotReloadAttr.TargetMethodId}");
                            if (HotReloadRegistry.RegisterMethodOverride(method, hotReloadAttr, type, out var skipReason))
                            {
                                registeredMethods.Add(hotReloadAttr.TargetMethodId);
                            }
                            else
                            {
                                skipped.Add($"{method.Name} -> {hotReloadAttr.TargetMethodId}: {skipReason}");
                            }
                        }
                    }
                }

                Debug.Log($"HotReload: Registered {registeredMethods.Count} override methods: {string.Join(", ", registeredMethods)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error registering methods from assembly {assemblyId}: {ex.Message}");
                Debug.LogError($"HotReload: Stack trace: {ex.StackTrace}");
            }

            return registeredMethods;
        }

        /// <summary>
        /// Extract base filename from versioned filename (e.g., "PlayerMovement_001" -> "PlayerMovement").
        /// </summary>
        private static string ExtractBaseFilename(string versionedFilename)
        {
            var lastUnderscoreIndex = versionedFilename.LastIndexOf('_');
            if (lastUnderscoreIndex > 0)
            {
                var potentialVersion = versionedFilename.Substring(lastUnderscoreIndex + 1);
                if (int.TryParse(potentialVersion, out _))
                {
                    return versionedFilename.Substring(0, lastUnderscoreIndex);
                }
            }
            return versionedFilename;
        }

        /// <summary>
        /// Extract version number from versioned filename (e.g., "PlayerMovement_001" -> 1).
        /// </summary>
        private static int ExtractVersion(string versionedFilename)
        {
            var lastUnderscoreIndex = versionedFilename.LastIndexOf('_');
            if (lastUnderscoreIndex > 0)
            {
                var potentialVersion = versionedFilename.Substring(lastUnderscoreIndex + 1);
                if (int.TryParse(potentialVersion, out var version))
                {
                    return version;
                }
            }
            return 0;
        }

#else
        // Hot reload requires Roslyn compilation, which is only available in the Editor and in
        // Desktop development builds. In all other builds the methods below are compiled instead,
        // matching the public surface used by callers (HotReloadCommands, InPlaceReloadProcessor)
        // so the project still builds, and returning a clear "not supported" failure at runtime.
        const string NotSupportedMessage =
            "Hot reload compilation is only supported on Desktop development builds (Windows/Mac/Linux).";

        /// <summary>
        /// Hot reload compilation not supported on this build.
        /// </summary>
        public static Task<HotReloadCompileResult> CompileAndApplyAsync(string filename, string assemblyDir = null)
        {
            return Task.FromResult(HotReloadCompileResult.Failure("Platform Not Supported", NotSupportedMessage));
        }

        /// <summary>
        /// Hot reload source compilation (in-place editing) not supported on this build.
        /// </summary>
        public static HotReloadCompilationResult CompileSourceCodeOnMainThread(string sourceCode, string baseFileName, string assemblyDir = null, bool emitPdb = false, string documentPath = null)
        {
            return HotReloadCompilationResult.Failure("Platform Not Supported", NotSupportedMessage);
        }

        /// <summary>
        /// Hot reload source compilation (in-place editing) not supported on this build.
        /// </summary>
        public static Task<HotReloadCompilationResult> CompileSourceCodeAsync(string sourceCode, string baseFileName, string assemblyDir = null, bool emitPdb = false, string documentPath = null)
        {
            return Task.FromResult(HotReloadCompilationResult.Failure("Platform Not Supported", NotSupportedMessage));
        }

        /// <summary>
        /// Hot reload cleanup not supported on this build.
        /// </summary>
        public static HotReloadCleanupResult CleanupHotReloadDlls(string assemblyDir = null)
        {
            return new HotReloadCleanupResult
            {
                Success = false,
                Message = NotSupportedMessage,
                ExecutionTimeMs = 0
            };
        }
#endif
    }

    /// <summary>
    /// Result from hot reload compilation operation.
    /// </summary>
    public class HotReloadCompileResult
    {
        public bool IsSuccess { get; set; }
        public string AssemblyName { get; set; }
        public string OutputPath { get; set; }
        public List<string> RegisteredMethods { get; set; } = new List<string>();
        public long ExecutionTimeMs { get; set; }
        public string Error { get; set; }
        public string ErrorDetails { get; set; }
        public List<string> Diagnostics { get; set; } = new List<string>();

        public static HotReloadCompileResult Success(string assemblyName, string outputPath, List<string> registeredMethods, long executionTimeMs)
        {
            return new HotReloadCompileResult
            {
                IsSuccess = true,
                AssemblyName = assemblyName,
                OutputPath = outputPath,
                RegisteredMethods = registeredMethods,
                ExecutionTimeMs = executionTimeMs
            };
        }

        public static HotReloadCompileResult Failure(string error, string errorDetails, long executionTimeMs = 0, List<string> diagnostics = null)
        {
            return new HotReloadCompileResult
            {
                IsSuccess = false,
                Error = error,
                ErrorDetails = errorDetails,
                ExecutionTimeMs = executionTimeMs,
                Diagnostics = diagnostics ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// Result from hot reload cleanup operation.
    /// </summary>
    public class HotReloadCleanupResult
    {
        public bool Success { get; set; }
        public List<string> DeletedFiles { get; set; } = new List<string>();
        public string Message { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Result from hot reload source code compilation operation (for in-place editing).
    /// </summary>
    public class HotReloadCompilationResult
    {
        public bool IsSuccess { get; set; }
        public string AssemblyName { get; set; }
        public string OutputPath { get; set; }
        public List<string> RegisteredMethods { get; set; } = new List<string>();
        public long ExecutionTimeMs { get; set; }
        public string Error { get; set; }
        public string ErrorDetails { get; set; }
        public List<string> Diagnostics { get; set; } = new List<string>();

        public static HotReloadCompilationResult Success(string assemblyName, string outputPath, List<string> registeredMethods, long executionTimeMs)
        {
            return new HotReloadCompilationResult
            {
                IsSuccess = true,
                AssemblyName = assemblyName,
                OutputPath = outputPath,
                RegisteredMethods = registeredMethods,
                ExecutionTimeMs = executionTimeMs
            };
        }

        public static HotReloadCompilationResult Failure(string error, string errorDetails, long executionTimeMs = 0, List<string> diagnostics = null)
        {
            return new HotReloadCompilationResult
            {
                IsSuccess = false,
                Error = error,
                ErrorDetails = errorDetails,
                ExecutionTimeMs = executionTimeMs,
                Diagnostics = diagnostics ?? new List<string>()
            };
        }
    }
}