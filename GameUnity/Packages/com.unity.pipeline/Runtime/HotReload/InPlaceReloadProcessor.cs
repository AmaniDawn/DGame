using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Pipeline.Compilation;
using UnityEngine;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Main orchestrator for in-place hot reload processing.
    /// Handles the complete pipeline: source parsing -> validation -> transformation -> compilation.
    /// </summary>
    public static class InPlaceReloadProcessor
    {
        /// <summary>
        /// Process a source file for in-place hot reload.
        /// Extracts [HotReloadWithOverrides] methods, validates accessibility, transforms to static overrides, and compiles.
        /// DEADLOCK FIX: Detects main thread and uses synchronous processing when needed.
        /// </summary>
        /// <param name="sourceFilePath">Path to source file containing [HotReloadWithOverrides] methods</param>
        /// <param name="assemblyDir">Optional directory to save compiled assembly</param>
        /// <param name="pdb">Emit debug symbols mapped to the original source so breakpoints bind.</param>
        /// <returns>Compilation result with success/failure and diagnostic information</returns>
        public static Task<InPlaceReloadResult> ProcessSourceFileAsync(string sourceFilePath, string assemblyDir = null, bool pdb = false)
        {
            // Runs synchronously on the calling (main) thread; returns a completed Task.
            return Task.FromResult(ProcessSourceFileOnMainThread(sourceFilePath, assemblyDir, pdb));
        }

        /// <summary>
        /// Process source file on main thread synchronously to avoid deadlocks.
        /// </summary>
        public static InPlaceReloadResult ProcessSourceFileOnMainThread(string sourceFilePath, string assemblyDir = null, bool pdb = false)
        {
            var result = new InPlaceReloadResult
            {
                SourceFilePath = sourceFilePath,
                Success = false
            };

            try
            {
                Debug.Log($"HotReload: Processing source file synchronously on main thread: {sourceFilePath}");

                // 1. Read and parse the source file (synchronous)
                var sourceCode = ReadSourceFile(sourceFilePath);
                if (string.IsNullOrEmpty(sourceCode))
                {
                    result.ErrorMessage = $"Could not read source file: {sourceFilePath}";
                    return result;
                }

                // 2. Extract [HotReloadWithOverrides] methods
                var extractionResult = ExtractHotReloadableMethods(sourceCode);
                if (!extractionResult.HasMethods)
                {
                    result.ErrorMessage = $"No [HotReload] methods found in {sourceFilePath}";
                    return result;
                }

                result.OriginalTypeName = extractionResult.TypeName;
                result.ExtractedMethods = extractionResult.Methods.Keys.ToList();

                Debug.Log($"HotReload: Extracted {extractionResult.Methods.Count} [HotReloadWithOverrides] methods from {extractionResult.TypeName}");

                // 3. Validate accessibility (public members only)
                var validationResult = AccessibilityValidator.ValidatePublicAccess(
                    sourceCode,
                    extractionResult.Methods,
                    extractionResult.TypeName);

                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = validationResult.GetFormattedErrorMessage();
                    result.ValidationViolations = validationResult.Violations;
                    Debug.LogWarning($"HotReload: Accessibility validation failed: {result.ErrorMessage}");
                    return result;
                }

                Debug.Log($"HotReload: Accessibility validation passed for {extractionResult.TypeName}");

                // 4. Transform method bodies to static overrides
                var originalSource = File.ReadAllText(sourceFilePath);
                var transformedCode = SourceCodeTransformer.TransformMethodBodies(
                    extractionResult.Methods,
                    extractionResult.TypeName,
                    extractionResult.MethodSignatures,
                    originalSource,
                    emitLineDirectives: pdb,
                    originalFilePath: sourceFilePath);

                result.TransformedCode = transformedCode;

                Debug.Log($"HotReload: Code transformation completed for {extractionResult.TypeName}");

                // 5. Compile the transformed code (synchronous)
                var compilationResult = CompileTransformedCode(
                    transformedCode,
                    extractionResult.TypeName,
                    assemblyDir,
                    pdb,
                    sourceFilePath);

                result.Success = compilationResult.IsSuccess;
                result.AssemblyName = compilationResult.AssemblyName;
                result.RegisteredMethods = compilationResult.RegisteredMethods;
                result.CompilationDiagnostics = compilationResult.Diagnostics;

                if (result.Success)
                {
                    Debug.Log($"HotReload: In-place reload successful for {sourceFilePath} - {result.RegisteredMethods.Count} methods registered");
                }
                else
                {
                    result.ErrorMessage = compilationResult.ErrorDetails ?? "Compilation failed";
                    Debug.LogError($"HotReload: In-place reload compilation failed: {result.ErrorMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error processing source file {sourceFilePath}: {ex.Message}");
                Debug.LogError($"HotReload: Stack trace: {ex.StackTrace}");

                result.ErrorMessage = $"Processing error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Internal async processing implementation for background threads.
        /// </summary>
        private static async Task<InPlaceReloadResult> ProcessSourceFileInternalAsync(string sourceFilePath, string assemblyDir = null)
        {
            var result = new InPlaceReloadResult
            {
                SourceFilePath = sourceFilePath,
                Success = false
            };

            try
            {
                Debug.Log($"HotReload: Processing source file asynchronously: {sourceFilePath}");

                // 1. Read and parse the source file (async)
                var sourceCode = await ReadSourceFileAsync(sourceFilePath);
                if (string.IsNullOrEmpty(sourceCode))
                {
                    result.ErrorMessage = $"Could not read source file: {sourceFilePath}";
                    return result;
                }

                // 2-4. Same processing as main thread version
                var extractionResult = ExtractHotReloadableMethods(sourceCode);
                if (!extractionResult.HasMethods)
                {
                    result.ErrorMessage = $"No [HotReload] methods found in {sourceFilePath}";
                    return result;
                }

                result.OriginalTypeName = extractionResult.TypeName;
                result.ExtractedMethods = extractionResult.Methods.Keys.ToList();

                var validationResult = AccessibilityValidator.ValidatePublicAccess(
                    sourceCode,
                    extractionResult.Methods,
                    extractionResult.TypeName);

                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = validationResult.GetFormattedErrorMessage();
                    result.ValidationViolations = validationResult.Violations;
                    return result;
                }

                var originalSource = File.ReadAllText(sourceFilePath);
                var transformedCode = SourceCodeTransformer.TransformMethodBodies(
                    extractionResult.Methods,
                    extractionResult.TypeName,
                    extractionResult.MethodSignatures,
                    originalSource);

                result.TransformedCode = transformedCode;

                // 5. Compile the transformed code (async)
                var compilationResult = await CompileTransformedCodeAsync(
                    transformedCode,
                    extractionResult.TypeName,
                    assemblyDir);

                result.Success = compilationResult.IsSuccess;
                result.AssemblyName = compilationResult.AssemblyName;
                result.RegisteredMethods = compilationResult.RegisteredMethods;
                result.CompilationDiagnostics = compilationResult.Diagnostics;

                if (!result.Success)
                {
                    result.ErrorMessage = compilationResult.ErrorDetails ?? "Compilation failed";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Processing error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Check if a source file contains [HotReload] methods.
        /// Simple synchronous check to avoid async deadlocks in tests.
        /// </summary>
        /// <param name="sourceFilePath">Path to source file to check</param>
        /// <returns>True if file contains [HotReload] methods</returns>
        public static Task<bool> ContainsHotReloadableMethodsAsync(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    return Task.FromResult(false);
                }

                // Use synchronous read for simple attribute check to avoid deadlocks
                var sourceCode = File.ReadAllText(sourceFilePath);
                if (string.IsNullOrEmpty(sourceCode))
                {
                    return Task.FromResult(false);
                }

                // Quick check for [HotReload] attribute
                var result = sourceCode.Contains("[HotReload]");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error checking for [HotReload] methods in {sourceFilePath}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Read source file content synchronously (for main thread).
        /// </summary>
        private static string ReadSourceFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"HotReload: Source file not found: {filePath}");
                    return null;
                }

                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error reading source file {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Read source file content asynchronously (for background threads).
        /// </summary>
        private static Task<string> ReadSourceFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"HotReload: Source file not found: {filePath}");
                    return Task.FromResult<string>(null);
                }

                // Use synchronous read wrapped in Task.FromResult to avoid deadlock issues
                var content = File.ReadAllText(filePath);
                return Task.FromResult(content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error reading source file {filePath}: {ex.Message}");
                return Task.FromResult<string>(null);
            }
        }

        /// <summary>
        /// Extract [HotReloadWithOverrides] methods from source code.
        /// </summary>
        private static HotReloadableExtractionResult ExtractHotReloadableMethods(string sourceCode)
        {
            var result = new HotReloadableExtractionResult();

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                // Find the class containing [HotReload] methods
                var classDeclaration = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Any(m => HasHotReloadAttribute(m)));

                if (classDeclaration == null)
                {
                    return result;
                }

                result.TypeName = classDeclaration.Identifier.ValueText;

                // Extract all [HotReload] methods
                var hotReloadableMethods = classDeclaration.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => HasHotReloadAttribute(m));

                foreach (var method in hotReloadableMethods)
                {
                    var methodName = method.Identifier.ValueText;
                    var methodBody = ExtractMethodBody(method);
                    var signature = ExtractMethodSignature(method);

                    if (!string.IsNullOrEmpty(methodBody))
                    {
                        result.Methods[methodName] = methodBody;
                        result.MethodSignatures[methodName] = signature;
                    }
                }

                Debug.Log($"HotReload: Extracted {result.Methods.Count} [HotReload] methods from class {result.TypeName}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error extracting [HotReload] methods: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Check if method has [HotReload] attribute.
        /// </summary>
        private static bool HasHotReloadAttribute(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().EndsWith("HotReload") || a.Name.ToString().EndsWith("HotReloadAttribute"));
        }

        /// <summary>
        /// Extract method body content (excluding braces).
        /// </summary>
        private static string ExtractMethodBody(MethodDeclarationSyntax method)
        {
            if (method.Body != null)
            {
                var bodyText = method.Body.ToString().Trim();
                // Remove outer braces
                if (bodyText.StartsWith("{") && bodyText.EndsWith("}"))
                {
                    bodyText = bodyText.Substring(1, bodyText.Length - 2).Trim();
                }
                return bodyText;
            }

            return "";
        }

        /// <summary>
        /// Extract method signature information.
        /// </summary>
        private static MethodSignatureInfo ExtractMethodSignature(MethodDeclarationSyntax method)
        {
            var signature = new MethodSignatureInfo
            {
                ReturnType = method.ReturnType.ToString()
            };

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var paramInfo = new ParameterInfo
                {
                    Type = parameter.Type?.ToString() ?? "object",
                    Name = parameter.Identifier.ValueText,
                    HasDefaultValue = parameter.Default != null,
                    DefaultValue = parameter.Default?.Value?.ToString()
                };

                signature.Parameters.Add(paramInfo);
            }

            return signature;
        }

        /// <summary>
        /// Compile transformed code synchronously (for main thread).
        /// </summary>
        private static HotReloadCompilationResult CompileTransformedCode(
            string transformedCode,
            string originalTypeName,
            string assemblyDir,
            bool emitPdb = false,
            string documentPath = null)
        {
            try
            {
                // Generate a temporary file name for the transformed code
                var tempFileName = $"InPlace_{originalTypeName}_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Use HotReloadCompiler synchronous method
                var compileResult = HotReloadCompiler.CompileSourceCodeOnMainThread(
                    transformedCode,
                    tempFileName,
                    assemblyDir,
                    emitPdb,
                    documentPath);

                return compileResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error compiling transformed code for {originalTypeName}: {ex.Message}");

                return HotReloadCompilationResult.Failure(
                    "Compilation Error",
                    ex.Message,
                    0,
                    new List<string> { ex.ToString() });
            }
        }

        /// <summary>
        /// Compile transformed code asynchronously (for background threads).
        /// </summary>
        private static async Task<HotReloadCompilationResult> CompileTransformedCodeAsync(
            string transformedCode,
            string originalTypeName,
            string assemblyDir)
        {
            try
            {
                // Generate a temporary file name for the transformed code
                var tempFileName = $"InPlace_{originalTypeName}_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Use HotReloadCompiler to compile the transformed source code
                var compileResult = await HotReloadCompiler.CompileSourceCodeAsync(
                    transformedCode,
                    tempFileName,
                    assemblyDir);

                return compileResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error compiling transformed code for {originalTypeName}: {ex.Message}");

                return HotReloadCompilationResult.Failure(
                    "Compilation Error",
                    ex.Message,
                    0,
                    new List<string> { ex.ToString() });
            }
        }

        /// <summary>
        /// Result of extracting [HotReloadWithOverrides] methods from source code.
        /// </summary>
        private class HotReloadableExtractionResult
        {
            /// <summary>
            /// Name of the class containing [HotReloadWithOverrides] methods.
            /// </summary>
            public string TypeName { get; set; }

            /// <summary>
            /// Dictionary of method names to their extracted body code.
            /// </summary>
            public Dictionary<string, string> Methods { get; set; } = new Dictionary<string, string>();

            /// <summary>
            /// Dictionary of method names to their signature information.
            /// </summary>
            public Dictionary<string, MethodSignatureInfo> MethodSignatures { get; set; } = new Dictionary<string, MethodSignatureInfo>();

            /// <summary>
            /// Whether any [HotReloadWithOverrides] methods were found.
            /// </summary>
            public bool HasMethods => Methods.Count > 0;
        }
    }

    /// <summary>
    /// Result of in-place hot reload processing.
    /// </summary>
    public class InPlaceReloadResult
    {
        /// <summary>
        /// Path to the source file that was processed.
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// Whether the processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Name of the original type containing [HotReloadWithOverrides] methods.
        /// </summary>
        public string OriginalTypeName { get; set; }

        /// <summary>
        /// List of method names that were extracted and processed.
        /// </summary>
        public List<string> ExtractedMethods { get; set; } = new List<string>();

        /// <summary>
        /// Generated transformed code for hot reload assembly.
        /// </summary>
        public string TransformedCode { get; set; }

        /// <summary>
        /// Name of the compiled assembly (if successful).
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// List of registered method IDs (if successful).
        /// </summary>
        public List<string> RegisteredMethods { get; set; } = new List<string>();

        /// <summary>
        /// Error message if processing failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Accessibility validation violations (if any).
        /// </summary>
        public List<AccessibilityViolation> ValidationViolations { get; set; } = new List<AccessibilityViolation>();

        /// <summary>
        /// Compilation diagnostics (warnings, errors).
        /// </summary>
        public List<string> CompilationDiagnostics { get; set; } = new List<string>();

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}