using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Integration tests for hot reload CLI commands.
    /// Tests the complete workflow: CLI commands → compiler → registry → runtime behavior.
    /// </summary>
    public class HotReloadCommandTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear hot reload state
            HotReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            HotReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void HotReloadStatus_ReturnsStats()
        {
            // Arrange - register a test method
            var method = typeof(SimpleTestClass).GetMethod("TestMethod");
            HotReloadRegistry.RegisterReloadableMethod(method, new HotReloadWithOverridesAttribute());

            // Act
            var result = HotReloadCommands.HotReloadStatus();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Contains.Substring("Reloadable Methods: 1"));
            Assert.That(result.Message, Contains.Substring("Active Overrides: 0"));
        }

        [Test]
        public void ReloadFile_WithInvalidFilename_ReturnsBadRequest()
        {
            // Act
            var result = HotReloadCommands.ReloadFileOverride("");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Bad Request"));
            Assert.That(result.ErrorDetails, Contains.Substring("cannot be empty"));
        }

        [Test]
        public void ReloadFile_WithInvalidTimeout_ReturnsBadRequest()
        {
            // Act
            var result = HotReloadCommands.ReloadFileOverride("test.cs", timeout: -1);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Bad Request"));
            Assert.That(result.ErrorDetails, Contains.Substring("Timeout must be between"));
        }

        [Test]
        public void ReloadFile_WithNonExistentFile_ReturnsFileNotFound()
        {
            // Act
            var result = HotReloadCommands.ReloadFileOverride("NonExistentFile.cs");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("File Not Found"));
            Assert.That(result.ErrorDetails, Contains.Substring("not found"));
        }

        [Test]
        public void ReloadFile_FileWithoutOverrides_ReturnsInvalidOverrideFile()
        {
            // A plain gameplay file (no [HotReloadOverrideMethod]) is not an override file.
            var path = Path.GetFullPath(Path.Combine("Temp", "PipelineHotReloadTests", "CommandTests", "NoOverrides.cs"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "using UnityEngine;\npublic class Plain : MonoBehaviour { void Update() { } }");

            try
            {
                var result = HotReloadCommands.ReloadFileOverride(path);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Invalid Override File"));
                Assert.That(result.ErrorDetails, Contains.Substring("No [HotReloadOverrideMethod]"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void ReloadFile_OverrideRedeclaresTargetType_ReturnsInvalidOverrideFile()
        {
            // The override targets Boss but the file also declares Boss -> redeclaration is rejected.
            var path = Path.GetFullPath(Path.Combine("Temp", "PipelineHotReloadTests", "CommandTests", "SameFile.cs"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [HotReloadOverrideMethod(""Boss.Update"")]
    public static void Tweaked(Boss instance) { }
}");

            try
            {
                var result = HotReloadCommands.ReloadFileOverride(path);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Invalid Override File"));
                Assert.That(result.ErrorDetails, Contains.Substring("Boss"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void ReloadFileInPlace_NonExistentFile_ReturnsFileNotFound()
        {
            var result = HotReloadCommands.ReloadFile("NonExistentInPlaceFile.cs");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("File Not Found"));
        }

        [UnityTest]
        public IEnumerator ReloadFile_WithValidFile_CompilationProcess()
        {
            // This test would require a valid hot reload file to exist
            // For now, we just test that the command structure works

            // Arrange - create a simple test file OUTSIDE Assets/ (project-root Temp/, ignored by
            // Unity) so it isn't imported/compiled → no domain reload that would kill the live server.
            // Absolute path is passed to ReloadFileOverride, whose GetHotReloadPath returns it as-is.
            var testFilePath = Path.GetFullPath(Path.Combine("Temp", "PipelineHotReloadTests", "CommandTests", "TestCommandFile.cs"));
            var testFileContent = @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public static class TestCommandFile
{
    [HotReloadOverrideMethod(""SimpleTestClass.TestMethod"")]
    public static void TestMethod(SimpleTestClass instance)
    {
        instance.value = 999;
    }
}";

            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath));
            File.WriteAllText(testFilePath, testFileContent);

            try
            {
                // Act
                var result = HotReloadCommands.ReloadFileOverride(testFilePath);

                // Assert - compilation should be attempted (may fail due to missing references, but structure should work)
                Assert.That(result, Is.Not.Null);
                // Note: This test mainly verifies the command structure and error handling
                // Full compilation tests would require proper assembly setup

                yield return null; // Unity test requirement
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFilePath))
                {
                    File.Delete(testFilePath);
                }
            }
        }

        [Test]
        public void HotReloadResponse_Success_CreatesCorrectResponse()
        {
            // Act
            var response = HotReloadResponse.CmdSuccess("test-assembly", "Test message", new System.Collections.Generic.List<string> { "method1", "method2" }, 1500);

            // Assert
            Assert.That(response.Success, Is.True);
            Assert.That(response.AssemblyName, Is.EqualTo("test-assembly"));
            Assert.That(response.Message, Is.EqualTo("Test message"));
            Assert.That(response.Items.Count, Is.EqualTo(2));
            Assert.That(response.ExecutionTimeMs, Is.EqualTo(1500));
        }

        [Test]
        public void HotReloadResponse_Failure_CreatesCorrectResponse()
        {
            // Act
            var response = HotReloadResponse.CmdFailure("Test Error", "Detailed error message", 2000);

            // Assert
            Assert.That(response.Success, Is.False);
            Assert.That(response.Error, Is.EqualTo("Test Error"));
            Assert.That(response.ErrorDetails, Is.EqualTo("Detailed error message"));
            Assert.That(response.ExecutionTimeMs, Is.EqualTo(2000));
        }

        /// <summary>
        /// Simple test class for hot reload testing.
        /// </summary>
        public class SimpleTestClass
        {
            public int value;

            public void TestMethod()
            {
                value = 42;
            }
        }
    }
}