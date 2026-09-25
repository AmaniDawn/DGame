using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the shared RoslynCompilationService to verify core compilation functionality.
    /// This service is used by both EvalCodeCompiler and HotReloadCompiler.
    /// </summary>
    public class RoslynCompilationServiceTests
    {
        [Test]
        public void Compile_ValidCode_ReturnsSuccessfulResult()
        {
            // Arrange
            var request = new CompilationRequest
            {
                SourceCode = @"
using System;
using UnityEngine;

namespace TestNamespace
{
    public static class TestClass
    {
        public static object Execute()
        {
            return 42;
        }
    }
}",
                AssemblyName = "TestAssembly"
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsTrue(result.Success, "Valid code should compile successfully");
            Assert.IsNotNull(result.Assembly, "Should return compiled assembly");
            Assert.IsNotNull(result.AssemblyBytes, "Should return assembly bytes");
            Assert.IsNotEmpty(result.AssemblyBytes, "Assembly bytes should not be empty");
            Assert.IsNotNull(result.Diagnostics, "Should return diagnostics list");
        }

        [Test]
        public void Compile_InvalidSyntax_ReturnsCompilationErrors()
        {
            // Arrange
            var request = new CompilationRequest
            {
                SourceCode = @"
using System;

public static class TestClass
{
    public static object Execute()
    {
        return 42 +; // Syntax error
    }
}",
                AssemblyName = "InvalidTestAssembly"
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsFalse(result.Success, "Invalid syntax should fail compilation");
            Assert.IsNull(result.Assembly, "Should not return assembly on failure");
            Assert.IsNotNull(result.Diagnostics, "Should return diagnostics");
            Assert.Greater(result.Diagnostics.Count, 0, "Should have compilation errors");

            var errorDiagnostic = result.Diagnostics.FirstOrDefault(d => d.Severity == "error");
            Assert.IsNotNull(errorDiagnostic, "Should have at least one error diagnostic");
            Assert.IsNotEmpty(errorDiagnostic.Message, "Error should have message");
            Assert.IsNotEmpty(errorDiagnostic.Id, "Error should have Roslyn diagnostic ID");
        }

        [Test]
        public async Task CompileAsync_ValidCode_WorksAsynchronously()
        {
            // Arrange
            var request = new CompilationRequest
            {
                SourceCode = @"
using UnityEngine;

public static class AsyncTestClass
{
    public static object Execute()
    {
        return Application.unityVersion;
    }
}",
                AssemblyName = "AsyncTestAssembly"
            };

            // Act
            var result = await RoslynCompilationService.CompileAsync(request);

            // Assert
            Assert.IsTrue(result.Success, "Async compilation should succeed");
            Assert.IsNotNull(result.Assembly, "Should return compiled assembly");
            Assert.Contains("AsyncTestClass", result.Assembly.GetTypes().Select(t => t.Name).ToList(),
                "Should contain expected class");
        }

        [Test]
        public void Compile_WithLineNumberOffset_AdjustsDiagnosticLineNumbers()
        {
            // Arrange - Code with error and line offset to simulate wrapper code
            var request = new CompilationRequest
            {
                SourceCode = @"using System;
using UnityEngine;
// Some wrapper code lines...
// More wrapper lines...
// Even more wrapper lines...

public static class TestClass
{
    public static object Execute()
    {
        return undefined_variable; // Error on line ~10, but should be reported as line ~4-5 after offset
    }
}",
                AssemblyName = "LineOffsetTestAssembly",
                LineNumberOffset = 6 // Simulate 6 lines of wrapper code
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsFalse(result.Success, "Code with undefined variable should fail");
            Assert.Greater(result.Diagnostics.Count, 0, "Should have diagnostics");

            var errorDiagnostic = result.Diagnostics.FirstOrDefault(d => d.Severity == "error");
            Assert.IsNotNull(errorDiagnostic, "Should have error diagnostic");
            Assert.Less(errorDiagnostic.Line, 6, "Line number should be adjusted by offset");
        }

        [Test]
        public void GetMetadataReferences_InEditor_ReturnsAllReferences()
        {
            // Act
            var references = RoslynCompilationService.GetMetadataReferences();

            // Assert
            Assert.IsNotNull(references, "Should return references list");
            Assert.Greater(references.Count, 0, "Should have at least some references");

            // Should include Unity references
            var hasUnityEngine = references.Any(r => r.Display?.Contains("UnityEngine") == true);
            Assert.IsTrue(hasUnityEngine, "Should include UnityEngine references");

            // Should include system references
            var hasSystemReferences = references.Any(r => r.Display?.Contains("System") == true);
            Assert.IsTrue(hasSystemReferences, "Should include System references");
        }

        [Test]
        public void GetMetadataReferences_WithAdditionalPrefixes_IncludesSpecifiedAssemblies()
        {
            // Arrange
            var additionalPrefixes = new[] { "Microsoft.CodeAnalysis", "Newtonsoft.Json" };

            // Act
            var references = RoslynCompilationService.GetMetadataReferences(additionalPrefixes);

            // Assert
            Assert.IsNotNull(references, "Should return references list");
            Assert.Greater(references.Count, 0, "Should have references");

            // Should include additional prefixes if those assemblies are loaded
            var hasCodeAnalysis = references.Any(r => r.Display?.Contains("Microsoft.CodeAnalysis") == true);
            var hasNewtonsoft = references.Any(r => r.Display?.Contains("Newtonsoft.Json") == true);
        }

        [Test]
        public void Compile_WithHotReloadExample_CompilesCorrectly()
        {
            // Arrange - Test hot reload specific compilation
            var request = new CompilationRequest
            {
                SourceCode = @"
using System;
using UnityEngine;
using Unity.Pipeline.HotReload;

public static class HotReloadTestClass
{
    [HotReloadOverrideMethod(""TargetClass.TargetMethod"")]
    public static void OverrideMethod(TargetClass instance)
    {
        Debug.Log(""Hot reload method executed"");
    }
}

public class TargetClass
{
    public void TargetMethod()
    {
        Debug.Log(""Original method"");
    }
}",
                AssemblyName = "HotReloadTestAssembly",
                AdditionalAssemblyPrefixes = new[] { "Microsoft.CodeAnalysis" } // For hot reload attributes
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsTrue(result.Success, $"Hot reload code should compile. Errors: {string.Join(", ", result.Diagnostics?.Select(d => d.Message) ?? new string[0])}");
            Assert.IsNotNull(result.Assembly, "Should return compiled assembly");

            var types = result.Assembly.GetTypes();
            Assert.Contains("HotReloadTestClass", types.Select(t => t.Name).ToList(), "Should contain hot reload class");
            Assert.Contains("TargetClass", types.Select(t => t.Name).ToList(), "Should contain target class");
        }

        [Test]
        public void Compile_WithEvalExample_CompilesCorrectly()
        {
            // Arrange - Test eval-style compilation (Execute method wrapper)
            var request = new CompilationRequest
            {
                SourceCode = @"
using System;
using UnityEngine;

namespace PipelineEvaluation
{
    public static class PipelineEval_test123
    {
        public static object Execute()
        {
            var result = 2 + 2;
            Debug.Log($""Eval calculation: {result}"");
            return result;
        }
    }
}",
                AssemblyName = "EvalTestAssembly",
                LineNumberOffset = 11 // Simulate EvalCodeCompiler's wrapper offset
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsTrue(result.Success, $"Eval code should compile. Errors: {string.Join(", ", result.Diagnostics?.Select(d => d.Message) ?? new string[0])}");
            Assert.IsNotNull(result.Assembly, "Should return compiled assembly");

            var evalType = result.Assembly.GetType("PipelineEvaluation.PipelineEval_test123");
            Assert.IsNotNull(evalType, "Should find eval class");

            var executeMethod = evalType.GetMethod("Execute");
            Assert.IsNotNull(executeMethod, "Should find Execute method");
            Assert.IsTrue(executeMethod.IsStatic, "Execute method should be static");
        }

        [Test]
        public void Compile_WithEmitDebugInformation_ProducesPortablePdbAndLoadableAssembly()
        {
            // Arrange
            var request = new CompilationRequest
            {
                SourceCode = @"
public static class DebugClass
{
    public static int Execute()
    {
        return 7;
    }
}",
                AssemblyName = "PdbTestAssembly_" + System.Guid.NewGuid().ToString("N"),
                EmitDebugInformation = true,
                DocumentPath = "PdbTest.cs"
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsTrue(result.Success, $"Should compile. Errors: {string.Join(", ", result.Diagnostics?.Select(d => d.Message) ?? new string[0])}");
            Assert.IsNotNull(result.Assembly, "Should return loadable assembly");
            Assert.IsNotNull(result.PdbBytes, "Should emit a PDB when EmitDebugInformation is set");
            Assert.IsNotEmpty(result.PdbBytes, "PDB bytes should not be empty");

            // Portable PDB blobs start with the "BSJB" metadata signature.
            Assert.AreEqual((byte)'B', result.PdbBytes[0]);
            Assert.AreEqual((byte)'S', result.PdbBytes[1]);
            Assert.AreEqual((byte)'J', result.PdbBytes[2]);
            Assert.AreEqual((byte)'B', result.PdbBytes[3]);
        }

        [Test]
        public void Compile_WithoutEmitDebugInformation_ProducesNoPdb()
        {
            // Arrange
            var request = new CompilationRequest
            {
                SourceCode = @"
public static class NoDebugClass
{
    public static int Execute() { return 1; }
}",
                AssemblyName = "NoPdbTestAssembly_" + System.Guid.NewGuid().ToString("N")
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsTrue(result.Success, "Should compile");
            Assert.IsNull(result.PdbBytes, "Default path must not emit a PDB (behavior unchanged)");
        }

        [Test]
        public void Compile_ExceptionHandling_ReturnsGracefulError()
        {
            // Arrange - Request that might cause internal exception
            var request = new CompilationRequest
            {
                SourceCode = null, // This might cause internal exception
                AssemblyName = "ExceptionTestAssembly"
            };

            // Act
            var result = RoslynCompilationService.Compile(request);

            // Assert
            Assert.IsFalse(result.Success, "Null source code should fail gracefully");
            Assert.IsNotNull(result.Diagnostics, "Should return diagnostics even on exception");
            Assert.Greater(result.Diagnostics.Count, 0, "Should have error diagnostic");

            var errorDiagnostic = result.Diagnostics.First();
            Assert.AreEqual("error", errorDiagnostic.Severity, "Should be marked as error");
            Assert.That(errorDiagnostic.Message, Does.Contain("exception"), "Should mention exception");
        }
    }
}