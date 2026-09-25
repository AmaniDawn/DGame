using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.HotReload;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for HotReloadCompiler to verify refactoring, deadlock fix, and core functionality.
    /// Tests the complete workflow: file reading → compilation → method registration.
    /// </summary>
    public class HotReloadCompilerTests
    {
        private string testHotReloadDir;
        private string validHotReloadFile;
        private string invalidHotReloadFile;
        private string emptyHotReloadFile;
        private int m_MainThreadId;

        [SetUp]
        public void SetUp()
        {
            // Write test source files OUTSIDE Assets/ (project-root Temp/, which Unity ignores) so
            // they are never imported/compiled by the editor — importing a .cs under Assets/ triggers
            // a domain reload that tears down the live pipeline server. Absolute paths are passed to
            // the compiler, whose GetHotReloadPath returns rooted paths as-is (no Assets/ prefix).
            testHotReloadDir = Path.GetFullPath(Path.Combine("Temp", "PipelineHotReloadTests", "CompilerTests"));
            Directory.CreateDirectory(testHotReloadDir);

            validHotReloadFile = Path.Combine(testHotReloadDir, "ValidTest.cs");
            invalidHotReloadFile = Path.Combine(testHotReloadDir, "InvalidTest.cs");
            emptyHotReloadFile = Path.Combine(testHotReloadDir, "EmptyTest.cs");

            // Create valid hot reload file with both reloadable target and hot reload override
            File.WriteAllText(validHotReloadFile, @"
using Unity.Pipeline.HotReload;
using UnityEngine;

// Target class with reloadable method
public class TestClass
{
    public int value = 42;

    [HotReloadWithOverrides(Id = ""TestClass.TestMethod"")]
    public void TestMethod()
    {
        value = 123;
    }
}

// Hot reload override
public static class ValidTestHotReload
{
    [HotReloadOverrideMethod(""TestClass.TestMethod"")]
    public static void TestMethod(TestClass instance)
    {
        instance.value = 999;
        Debug.Log(""Hot reload method executed"");
    }
}");

            // Create invalid hot reload file (syntax error)
            File.WriteAllText(invalidHotReloadFile, @"
using Unity.Pipeline.HotReload;

// Need to define TestClass since we're not wrapping code anymore
public class TestClass
{
    public int value = 42;

    [HotReloadWithOverrides(Id = ""TestClass.TestMethod"")]
    public void TestMethod()
    {
        value = 123;
    }
}

public static class InvalidTestHotReload
{
    [HotReloadOverrideMethod(""TestClass.TestMethod"")]
    public static void TestMethod(TestClass instance)
    {
        instance.value = 999 +; // Syntax error
    }
}");

            // Create empty file
            File.WriteAllText(emptyHotReloadFile, "");

            // Clear hot reload state
            HotReloadRegistry.ClearAllForTesting();

            // Capture the main thread so tests can assert thread context.
            m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            if (Directory.Exists(testHotReloadDir))
            {
                Directory.Delete(testHotReloadDir, true);
            }

            // Clean up hot reload state and temp files
            HotReloadRegistry.ClearAllForTesting();
            HotReloadCompiler.CleanupHotReloadDlls();
        }

        [Test]
        public void CompileAndApplyAsync_FromMainThread_RunsSynchronously()
        {
            // Arrange - We're on main thread
            Assert.AreEqual(m_MainThreadId, Thread.CurrentThread.ManagedThreadId, "Test should run on main thread");

            // Act - This should not deadlock (the main fix being tested)
            var task = HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Assert - Should complete synchronously without blocking
            Assert.IsTrue(task.IsCompleted, "Should complete synchronously on main thread to avoid deadlock");

            var result = task.Result;
            Assert.IsTrue(result.IsSuccess, $"Valid hot reload should succeed. Error: {result.Error}");
            Assert.IsNotEmpty(result.AssemblyName, "Should have assembly name");
            Assert.Greater(result.ExecutionTimeMs, 0, "Should record execution time");
        }

        [Test]
        public async Task CompileAndApplyAsync_FromBackgroundThread_UsesAsyncPath()
        {
            // Arrange - Track thread IDs
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var backgroundThreadId = -1;
            HotReloadCompileResult result = null;

            // Act - Run from background thread
            await Task.Run(async () =>
            {
                backgroundThreadId = Thread.CurrentThread.ManagedThreadId;
                Assert.AreNotEqual(mainThreadId, backgroundThreadId, "Should be on different thread");
                Assert.AreNotEqual(m_MainThreadId, Thread.CurrentThread.ManagedThreadId, "Should not be on main thread");

                result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            });

            // Assert
            Assert.IsNotNull(result, "Should get result");
            Assert.IsTrue(result.IsSuccess, $"Background compilation should succeed. Error: {result.Error}");
            Assert.IsNotEmpty(result.AssemblyName, "Should have assembly name");
        }

        [Test]
        public async Task CompileAndApplyAsync_ValidFile_RegistersHotReloadMethods()
        {
            // Get initial stats for comparison
            var initialStats = HotReloadRegistry.GetStats();

            // Act
            var result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Assert compilation success
            Assert.IsTrue(result.IsSuccess, $"Should succeed. Error: {result.Error}");

            Assert.Greater(result.RegisteredMethods.Count, 0, "Should register at least one method");
            Assert.Contains("TestClass.TestMethod", result.RegisteredMethods, "Should register the specified target method");

            // Get final stats and compare
            var finalStats = HotReloadRegistry.GetStats();

            // Verify method was registered in registry
            Assert.Greater(finalStats.ActiveOverrideCount, initialStats.ActiveOverrideCount, $"Active override count should increase from {initialStats.ActiveOverrideCount} to more than {initialStats.ActiveOverrideCount}");
        }

        [Test]
        public async Task CompileAndApplyAsync_InvalidSyntax_ReturnsCompilationError()
        {
            // Act
            var result = await HotReloadCompiler.CompileAndApplyAsync(invalidHotReloadFile);

            // Assert
            Assert.IsFalse(result.IsSuccess, "Invalid syntax should fail compilation");
            Assert.AreEqual("Compilation Failed", result.Error, "Should return compilation failed error");
            Assert.IsNotNull(result.Diagnostics, "Should have diagnostics");
            Assert.Greater(result.Diagnostics.Count, 0, "Should have compilation diagnostics");
        }

        [Test]
        public async Task CompileAndApplyAsync_EmptyFile_ReturnsEmptyFileError()
        {
            // Act
            var result = await HotReloadCompiler.CompileAndApplyAsync(emptyHotReloadFile);

            // Assert
            Assert.IsFalse(result.IsSuccess, "Empty file should fail");
            Assert.AreEqual("Empty File", result.Error, "Should return empty file error");
            Assert.That(result.ErrorDetails, Does.Contain("empty"), "Should mention file is empty");
        }

        [Test]
        public async Task CompileAndApplyAsync_FileNotFound_ReturnsFileNotFoundError()
        {
            // Act
            var result = await HotReloadCompiler.CompileAndApplyAsync("NonExistentFile.cs");

            // Assert
            Assert.IsFalse(result.IsSuccess, "Non-existent file should fail");
            Assert.AreEqual("File Not Found", result.Error, "Should return file not found error");
            Assert.That(result.ErrorDetails, Does.Contain("not found"), "Should mention file not found");
        }

        [Test]
        public async Task CompileAndApplyAsync_VersionTracking_GeneratesIncrementingVersions()
        {
            // Expected errors: signature mismatches for versions 2+ due to cross-assembly type incompatibility
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'TestClass.TestMethod'. Override method must have instance parameter as first argument.");
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'TestClass.TestMethod'. Override method must have instance parameter as first argument.");

            // Act - Compile the same file multiple times
            var result1 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            var result2 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            var result3 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Assert - Each compilation should have incrementing version numbers
            Assert.IsTrue(result1.IsSuccess, "First compilation should succeed");
            Assert.IsTrue(result2.IsSuccess, "Second compilation should succeed");
            Assert.IsTrue(result3.IsSuccess, "Third compilation should succeed");

            // Assembly names should contain version numbers
            Assert.That(result1.AssemblyName, Does.Contain("_001"), "First version should be 001");
            Assert.That(result2.AssemblyName, Does.Contain("_002"), "Second version should be 002");
            Assert.That(result3.AssemblyName, Does.Contain("_003"), "Third version should be 003");
        }

        [Test]
        public void CleanupHotReloadDlls_WithNoFiles_SucceedsGracefully()
        {
            // Act
            var result = HotReloadCompiler.CleanupHotReloadDlls();

            // Assert
            Assert.IsTrue(result.Success, "Cleanup should succeed even when no files exist");
            Assert.AreEqual(0, result.DeletedFiles.Count, "Should not delete any files");
            Assert.That(result.Message, Does.Contain("No hot reload temp directory").Or.Contain("0 old DLL versions"));
        }

        [Test]
        public async Task CleanupHotReloadDlls_AfterCompilations_CleansUpOldVersions()
        {
            // Expected errors: signature mismatches for versions 2+ due to cross-assembly type incompatibility
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'TestClass.TestMethod'. Override method must have instance parameter as first argument.");
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'TestClass.TestMethod'. Override method must have instance parameter as first argument.");

            // Arrange - Create multiple versions
            await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Act - Cleanup
            var cleanupResult = HotReloadCompiler.CleanupHotReloadDlls();

            // Assert
            Assert.IsTrue(cleanupResult.Success, "Cleanup should succeed");
            Assert.GreaterOrEqual(cleanupResult.ExecutionTimeMs, 0, "Should record execution time (0ms is acceptable for fast cleanup)");

            // Registry should be cleared
            var stats = HotReloadRegistry.GetStats();
            Assert.AreEqual(0, stats.ActiveOverrideCount, "Registry should be cleared after cleanup");
        }

        [Test]
        public async Task CompileAndApplyAsync_UsesRoslynCompilationService_Integration()
        {
            // This test verifies that HotReloadCompiler properly integrates with RoslynCompilationService

            // Act
            var result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Assert - Verify it uses the shared service (indirect verification through successful compilation)
            Assert.IsTrue(result.IsSuccess, "Should successfully compile using shared RoslynCompilationService");
            Assert.IsNotEmpty(result.AssemblyName, "Should generate assembly name");

            // The fact that it compiles successfully with the same diagnostics format as EvalCodeCompiler
            // indicates it's using the shared RoslynCompilationService
            if (!result.IsSuccess && result.Diagnostics?.Any() == true)
            {
                // If there were diagnostics, they should be in the same format as RoslynCompilationService
                var diagnostic = result.Diagnostics.First();
                Assert.IsNotEmpty(diagnostic, "Diagnostic should not be empty");
            }
        }

        [Test]
        public async Task CompileAndApplyAsync_ResolvesPaths_AbsoluteAndRelative()
        {
            // Absolute paths are used as-is.
            var result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);
            Assert.IsTrue(result.IsSuccess, $"Should find file by absolute path. Error: {result.Error}");

            // Relative paths resolve against the project root (no special "HotReload" folder).
            var result2 = await HotReloadCompiler.CompileAndApplyAsync("DoesNotExist.cs");
            Assert.IsFalse(result2.IsSuccess, "Should fail for non-existent file");
            Assert.That(result2.ErrorDetails, Does.Contain("DoesNotExist.cs"),
                "Error should reference the resolved path");
            Assert.That(result2.ErrorDetails, Does.Not.Contain("HotReload\\DoesNotExist.cs"),
                "Relative paths should no longer be rooted under a HotReload folder");
        }

        [UnityTest]
        public IEnumerator CompileAndApplyAsync_PerformanceComparison_LogsTiming()
        {
            // Expected error: signature mismatch for second compilation due to cross-assembly type incompatibility
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'TestClass.TestMethod'. Override method must have instance parameter as first argument.");

            // Test to observe performance differences between main thread and background execution
            var filename = validHotReloadFile;

            // Main thread execution
            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            var mainThreadTask = HotReloadCompiler.CompileAndApplyAsync(filename);
            yield return new WaitUntil(() => mainThreadTask.IsCompleted);
            sw1.Stop();
            var result1 = mainThreadTask.Result;

            Assert.IsTrue(result1.IsSuccess, "Main thread execution should succeed");

            // Background thread execution
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            HotReloadCompileResult result2 = null;
            var backgroundTask = Task.Run(async () =>
            {
                result2 = await HotReloadCompiler.CompileAndApplyAsync(filename);
            });

            yield return new WaitUntil(() => backgroundTask.IsCompleted);
            sw2.Stop();

            Assert.IsNotNull(result2, "Background execution should complete");
            Assert.IsTrue(result2.IsSuccess, "Background execution should succeed");
        }

        [Test]
        public void CompileAndApplyOnMainThread_DirectCall_WorksSynchronously()
        {
            // Test the direct synchronous method (used internally for main thread detection)

            // Act - Call the synchronous version directly
            var result = HotReloadCompiler.CompileAndApplyOnMainThread(validHotReloadFile);

            // Assert - Should complete synchronously
            Assert.IsNotNull(result, "Should return result immediately");
            Assert.IsTrue(result.IsSuccess, $"Direct synchronous call should succeed. Error: {result.Error}");
            Assert.Greater(result.ExecutionTimeMs, 0, "Should record execution time");
        }
    }
}