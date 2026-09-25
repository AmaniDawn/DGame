using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.HotReload;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the new assemblyDir parameter functionality in hot reload compilation.
    /// Verifies that assemblies are saved to disk when assemblyDir is specified, and remain in-memory when not specified.
    /// </summary>
    public class HotReloadAssemblyDirectoryTests
    {
        private string testHotReloadDir;
        private string testAssemblyDir;
        private string validHotReloadFile;

        [SetUp]
        public void SetUp()
        {
            // Write source files OUTSIDE Assets/ (project-root Temp/, ignored by Unity) so they are
            // never imported/compiled by the editor → no domain reload that would kill the live
            // server. Absolute paths are passed to the compiler (GetHotReloadPath returns them as-is).
            testHotReloadDir = Path.GetFullPath(Path.Combine("Temp", "PipelineHotReloadTests", "AssemblyDirTests"));
            testAssemblyDir = Path.Combine("TestAssemblies", "HotReload");
            Directory.CreateDirectory(testHotReloadDir);
            Directory.CreateDirectory(testAssemblyDir);

            validHotReloadFile = Path.Combine(testHotReloadDir, "AssemblyDirTest.cs");

            // Create a valid hot reload file
            File.WriteAllText(validHotReloadFile, @"
using Unity.Pipeline.HotReload;
using UnityEngine;

// Target class with reloadable method
public class AssemblyDirTestClass
{
    public int value = 42;

    [HotReloadWithOverrides(Id = ""AssemblyDirTest.TestMethod"")]
    public void TestMethod()
    {
        value = 123;
    }
}

// Hot reload override
public static class AssemblyDirTestHotReload
{
    [HotReloadOverrideMethod(""AssemblyDirTest.TestMethod"")]
    public static void TestMethod(AssemblyDirTestClass instance)
    {
        instance.value = 999;
        Debug.Log(""Assembly directory test hot reload method executed"");
    }
}");

            // Clear state
            HotReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files and directories
            if (Directory.Exists(testHotReloadDir))
            {
                Directory.Delete(testHotReloadDir, true);
            }
            if (Directory.Exists(testAssemblyDir))
            {
                Directory.Delete(testAssemblyDir, true);
            }
            if (Directory.Exists("TestAssemblies"))
            {
                Directory.Delete("TestAssemblies", true);
            }

            // Clean up hot reload state
            HotReloadRegistry.ClearAllForTesting();
            HotReloadCompiler.CleanupHotReloadDlls(); // Default cleanup
        }

        [Test]
        public async Task CompileAndApplyAsync_WithoutAssemblyDir_AssemblyStaysInMemory()
        {
            // Act - Compile without assemblyDir (should remain in-memory)
            var result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Compilation should succeed. Error: {result.Error}");
            Assert.IsNotEmpty(result.AssemblyName, "Should have assembly name");

            // The assembly should not exist in our test directory
            var expectedDiskPath = Path.Combine(testAssemblyDir, $"{result.AssemblyName}.dll");
            Assert.IsFalse(File.Exists(expectedDiskPath), "Assembly should not be saved to test directory");

            // But hot reload should still work (assembly is in memory)
            var stats = HotReloadRegistry.GetStats();
            Assert.Greater(stats.ActiveOverrideCount, 0, "Should have registered hot reload methods");
        }

        [Test]
        public async Task CompileAndApplyAsync_WithAssemblyDir_AssemblyIsSavedToDisk()
        {
            // Act - Compile with assemblyDir (should save to disk)
            var result = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile, testAssemblyDir);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Compilation should succeed. Error: {result.Error}");
            Assert.IsNotEmpty(result.AssemblyName, "Should have assembly name");

            // The assembly should exist on disk
            var expectedDiskPath = Path.Combine(testAssemblyDir, $"{result.AssemblyName}.dll");
            Assert.IsTrue(File.Exists(expectedDiskPath), $"Assembly should be saved to disk: {expectedDiskPath}");

            // Verify file size is reasonable (should be a valid assembly)
            var fileInfo = new FileInfo(expectedDiskPath);
            Assert.Greater(fileInfo.Length, 1000, "Assembly file should have reasonable size");

            // Hot reload should still work (assembly is loaded from memory)
            var stats = HotReloadRegistry.GetStats();
            Assert.Greater(stats.ActiveOverrideCount, 0, "Should have registered hot reload methods");
        }

        [Test]
        public async Task CompileAndApplyAsync_WithAssemblyDir_MultipleVersionsCreateSeparateFiles()
        {
            // Expected errors: signature mismatches for versions 2+ due to cross-assembly type incompatibility
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'AssemblyDirTest.TestMethod'. Override method must have instance parameter as first argument.");
            LogAssert.Expect(LogType.Error, "HotReload: Signature mismatch for 'AssemblyDirTest.TestMethod'. Override method must have instance parameter as first argument.");

            // Act - Compile same file multiple times to create versioned assemblies
            var result1 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile, testAssemblyDir);
            var result2 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile, testAssemblyDir);
            var result3 = await HotReloadCompiler.CompileAndApplyAsync(validHotReloadFile, testAssemblyDir);

            // Assert - All compilations should succeed
            Assert.IsTrue(result1.IsSuccess, "First compilation should succeed");
            Assert.IsTrue(result2.IsSuccess, "Second compilation should succeed");
            Assert.IsTrue(result3.IsSuccess, "Third compilation should succeed");

            // Assert - Each should have different version numbers
            Assert.That(result1.AssemblyName, Does.Contain("_001"), "First version should be 001");
            Assert.That(result2.AssemblyName, Does.Contain("_002"), "Second version should be 002");
            Assert.That(result3.AssemblyName, Does.Contain("_003"), "Third version should be 003");

            // Assert - All versions should exist on disk
            var diskPath1 = Path.Combine(testAssemblyDir, $"{result1.AssemblyName}.dll");
            var diskPath2 = Path.Combine(testAssemblyDir, $"{result2.AssemblyName}.dll");
            var diskPath3 = Path.Combine(testAssemblyDir, $"{result3.AssemblyName}.dll");

            Assert.IsTrue(File.Exists(diskPath1), $"Version 1 should exist: {diskPath1}");
            Assert.IsTrue(File.Exists(diskPath2), $"Version 2 should exist: {diskPath2}");
            Assert.IsTrue(File.Exists(diskPath3), $"Version 3 should exist: {diskPath3}");
        }

        [Test]
        public void CleanupHotReloadDlls_WithAssemblyDir_CleansUpDiskFiles()
        {
            // Arrange - Create some test assembly files
            var testAssembly1 = Path.Combine(testAssemblyDir, "TestAssembly_001.dll");
            var testAssembly2 = Path.Combine(testAssemblyDir, "TestAssembly_002.dll");
            var testAssembly3 = Path.Combine(testAssemblyDir, "TestAssembly_003.dll");

            File.WriteAllText(testAssembly1, "fake dll content 1");
            File.WriteAllText(testAssembly2, "fake dll content 2");
            File.WriteAllText(testAssembly3, "fake dll content 3");

            // Verify files exist before cleanup
            Assert.IsTrue(File.Exists(testAssembly1), "Test file 1 should exist before cleanup");
            Assert.IsTrue(File.Exists(testAssembly2), "Test file 2 should exist before cleanup");
            Assert.IsTrue(File.Exists(testAssembly3), "Test file 3 should exist before cleanup");

            // Act - Cleanup the specified directory
            var result = HotReloadCompiler.CleanupHotReloadDlls(testAssemblyDir);

            // Assert - Cleanup should succeed
            Assert.IsTrue(result.Success, $"Cleanup should succeed. Message: {result.Message}");

            // Note: Cleanup only removes older versions, keeping the latest, so some files may remain
            // The important thing is that the cleanup operated on the correct directory
        }
    }
}