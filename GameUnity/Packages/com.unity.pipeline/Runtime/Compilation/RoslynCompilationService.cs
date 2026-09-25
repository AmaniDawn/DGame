using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Compilation
{
    /// <summary>
    /// Shared Roslyn compilation service for both code evaluation and hot reload systems.
    /// Eliminates code duplication by providing common compilation infrastructure.
    /// Thread-safe and can be called from any thread.
    /// </summary>
    public static class RoslynCompilationService
    {
#if UNITY_EDITOR || (UNITY_STANDALONE && DEBUG)

        /// <summary>
        /// Compile source code to assembly with customizable options.
        /// Thread-safe compilation that can run on any thread.
        /// </summary>
        public static CompilationResult Compile(CompilationRequest request)
        {
            try
            {
                // Parse syntax tree. When emitting debug info, the tree needs a document path and
                // UTF-8 encoding so the portable PDB can reference a source document.
                var syntaxTree = request.EmitDebugInformation
                    ? CSharpSyntaxTree.ParseText(
                        SourceText.From(request.SourceCode, Encoding.UTF8),
                        path: request.DocumentPath ?? request.AssemblyName + ".cs")
                    : CSharpSyntaxTree.ParseText(request.SourceCode);

                // Get metadata references with optional additional prefixes
                var references = GetMetadataReferences(request.AdditionalAssemblyPrefixes);

                // Create compilation. Disable optimizations when emitting debug info so the JIT
                // keeps full sequence points (otherwise breakpoints can't bind).
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                if (request.EmitDebugInformation)
                    options = options.WithOptimizationLevel(OptimizationLevel.Debug);

                var compilation = CSharpCompilation.Create(
                    request.AssemblyName,
                    new[] { syntaxTree },
                    references,
                    options);

                // Get diagnostics
                var diagnostics = compilation.GetDiagnostics();
                var diagnosticInfos = ConvertDiagnostics(diagnostics, request.LineNumberOffset);

                // Check for compilation errors
                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (errors.Any())
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Diagnostics = diagnosticInfos
                    };
                }

                // Compile to memory
                using (var peStream = new MemoryStream())
                {
                    byte[] pdbBytes = null;
                    EmitResult emitResult;

                    if (request.EmitDebugInformation)
                    {
                        using (var pdbStream = new MemoryStream())
                        {
                            emitResult = compilation.Emit(
                                peStream,
                                pdbStream,
                                options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

                            if (emitResult.Success)
                                pdbBytes = pdbStream.ToArray();
                        }
                    }
                    else
                    {
                        emitResult = compilation.Emit(peStream);
                    }

                    if (!emitResult.Success)
                    {
                        var emitDiagnostics = ConvertDiagnostics(emitResult.Diagnostics, request.LineNumberOffset);
                        return new CompilationResult
                        {
                            Success = false,
                            Diagnostics = diagnosticInfos.Concat(emitDiagnostics).ToList()
                        };
                    }

                    // Load assembly from memory. When symbols are present, load them alongside the
                    // assembly so an attached debugger can bind breakpoints into the emitted code.
                    var assemblyBytes = peStream.ToArray();
                    var assembly = pdbBytes != null
                        ? PipelineUtils.LoadFromBytes(assemblyBytes, pdbBytes)
                        : PipelineUtils.LoadFromBytes(assemblyBytes);

                    return new CompilationResult
                    {
                        Success = true,
                        Assembly = assembly,
                        AssemblyBytes = assemblyBytes,
                        PdbBytes = pdbBytes,
                        Diagnostics = diagnosticInfos
                    };
                }
            }
            catch (Exception ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Diagnostics = new List<DiagnosticInfo>
                    {
                        new DiagnosticInfo
                        {
                            Severity = "error",
                            Message = $"Compilation exception: {ex.Message}",
                            Line = 0,
                            Column = 0,
                            Id = "ROSLYN001"
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Async wrapper for compilation that runs on background thread.
        /// Use when you need to avoid blocking the calling thread.
        /// </summary>
        public static Task<CompilationResult> CompileAsync(CompilationRequest request)
        {
            return Task.Run(() => Compile(request));
        }

        /// <summary>
        /// Get metadata references for compilation with optional additional assembly prefixes.
        /// Handles both Editor and Runtime scenarios with appropriate filtering.
        /// </summary>
        public static List<MetadataReference> GetMetadataReferences(string[] additionalPrefixes = null)
        {
            var references = new List<MetadataReference>();
            var assemblies = PipelineUtils.GetLoadedAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(PipelineUtils.GetLoadedAssemblyPath(a)));

            if (Application.isEditor)
            {
                // Editor: Include all loaded assemblies for maximum compatibility
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(PipelineUtils.GetLoadedAssemblyPath(assembly)));
                    }
                    catch
                    {
                        // Skip problematic assemblies
                    }
                }
            }
            else
            {
                // Runtime: Use curated filtering for Unity + user assemblies
                var allowedPrefixes = new List<string>
                {
                    "UnityEngine",
                    "Assembly-CSharp",
                    "netstandard",
                    "mscorlib",
                    "System.",
                    "Unity.Pipeline"
                };

                // Add any additional prefixes (e.g., for hot reload: "Microsoft.CodeAnalysis")
                if (additionalPrefixes != null)
                {
                    allowedPrefixes.AddRange(additionalPrefixes);
                }

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var assemblyName = assembly.GetName().Name;
                        if (allowedPrefixes.Any(prefix => assemblyName.StartsWith(prefix)))
                        {
                            references.Add(MetadataReference.CreateFromFile(PipelineUtils.GetLoadedAssemblyPath(assembly)));
                        }
                    }
                    catch
                    {
                        // Skip problematic assemblies
                    }
                }
            }

            return references;
        }

        /// <summary>
        /// Convert Roslyn diagnostics to DiagnosticInfo list with optional line number adjustment.
        /// Handles line number offset for wrapped code scenarios.
        /// </summary>
        private static List<DiagnosticInfo> ConvertDiagnostics(IEnumerable<Diagnostic> diagnostics, int lineOffset = 0)
        {
            var result = new List<DiagnosticInfo>();

            foreach (var diagnostic in diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning))
            {
                var location = diagnostic.Location.GetLineSpan();
                var line = Math.Max(0, location.StartLinePosition.Line - lineOffset);
                var column = location.StartLinePosition.Character;

                result.Add(new DiagnosticInfo
                {
                    Severity = diagnostic.Severity.ToString().ToLower(),
                    Message = diagnostic.GetMessage(),
                    Line = line,
                    Column = column,
                    Id = diagnostic.Id
                });
            }

            return result;
        }

#else
        /// <summary>
        /// Runtime compilation not supported on this platform.
        /// Desktop development builds only (Windows/Mac/Linux).
        /// </summary>
        public static CompilationResult Compile(CompilationRequest request)
        {
            return new CompilationResult
            {
                Success = false,
                Diagnostics = new List<DiagnosticInfo>
                {
                    new DiagnosticInfo
                    {
                        Severity = "error",
                        Message = "Runtime code compilation only supported on Desktop development builds",
                        Line = 0,
                        Column = 0,
                        Id = "PLATFORM001"
                    }
                }
            };
        }

        /// <summary>
        /// Runtime compilation not supported on this platform.
        /// </summary>
        public static Task<CompilationResult> CompileAsync(CompilationRequest request)
        {
            return Task.FromResult(Compile(request));
        }

        /// <summary>
        /// Runtime compilation not supported on this platform.
        /// </summary>
        public static List<MetadataReference> GetMetadataReferences(string[] additionalPrefixes = null)
        {
            return new List<MetadataReference>();
        }
#endif
    }

    /// <summary>
    /// Request for Roslyn compilation with customizable options.
    /// </summary>
    public class CompilationRequest
    {
        /// <summary>
        /// Source code to compile.
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// Name for the generated assembly (should be unique).
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Additional assembly prefixes to include in metadata references.
        /// Useful for hot reload scenarios that need "Microsoft.CodeAnalysis" etc.
        /// </summary>
        public string[] AdditionalAssemblyPrefixes { get; set; }

        /// <summary>
        /// Line number offset to subtract from diagnostic line numbers.
        /// Used when source code has wrapper code that should be hidden from diagnostics.
        /// </summary>
        public int LineNumberOffset { get; set; }

        /// <summary>
        /// When true, emit a portable PDB and load the assembly with its symbols so an attached
        /// managed debugger can bind breakpoints. Compiles unoptimized. Default false (no symbols).
        /// </summary>
        public bool EmitDebugInformation { get; set; }

        /// <summary>
        /// Document path recorded in the syntax tree when <see cref="EmitDebugInformation"/> is set.
        /// Source that is not covered by explicit <c>#line</c> directives maps to this path.
        /// </summary>
        public string DocumentPath { get; set; }
    }

    /// <summary>
    /// Result from Roslyn compilation.
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Whether compilation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Compiled assembly (only available if Success = true).
        /// </summary>
        public Assembly Assembly { get; set; }

        /// <summary>
        /// Raw assembly bytes (for potential disk saving or caching).
        /// </summary>
        public byte[] AssemblyBytes { get; set; }

        /// <summary>
        /// Portable PDB bytes when <see cref="CompilationRequest.EmitDebugInformation"/> was set;
        /// null otherwise.
        /// </summary>
        public byte[] PdbBytes { get; set; }

        /// <summary>
        /// Compilation diagnostics (errors, warnings, info).
        /// </summary>
        public List<DiagnosticInfo> Diagnostics { get; set; } = new List<DiagnosticInfo>();
    }
}