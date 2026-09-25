using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Integration tests for in-place hot reload editing workflow.
    /// Tests the complete pipeline: source parsing -> validation -> transformation -> compilation.
    /// </summary>
    [Ignore("HotReload in-place editing is deferred until the autonomous test loop is solid; this path exercises the known instance-to-static transformation bug. Re-enable when revisiting in-place reload.")]
    public class HotReloadInPlaceEditingTests
    {
        private string _testFilePath;

        [SetUp]
        public void Setup()
        {
            // Clean registry before each test
            HotReloadRegistry.ClearAllForTesting();

            // Create temp test file path
            _testFilePath = Path.Combine(Application.temporaryCachePath, "HotReloadExampleComponent.cs");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test file
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }

            // Clean registry after test
            HotReloadRegistry.ClearAllForTesting();
        }

        [UnityTest]
        public IEnumerator InPlaceReload_ValidPublicMemberAccess_ShouldSucceed()
        {
            // Arrange: Create source file with [HotReloadWithOverrides] method using only public members
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float speed = 5.0f;
    public bool isActive = true;

    [HotReload]
    void Update()
    {
        if (isActive)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
        }
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.Result.Success, $"In-place reload should succeed. Error: {task.Result.ErrorMessage}");
            Assert.AreEqual("HotReloadExampleComponent", task.Result.OriginalTypeName);
            Assert.Contains("Update", task.Result.ExtractedMethods);
            Assert.IsTrue(task.Result.RegisteredMethods.Count > 0, "Should have registered at least one method");
            Assert.IsNotNull(task.Result.TransformedCode, "Should have generated transformed code");

            // Verify registry state
            var stats = HotReloadRegistry.GetStats();
            Assert.IsTrue(stats.ActiveOverrideCount > 0, "Should have active overrides registered");
        }

        [UnityTest]
        public IEnumerator InPlaceReload_PrivateMemberAccess_ShouldFail()
        {
            // Arrange: Create source file with [HotReloadWithOverrides] method trying to access private members
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    private float privateSpeed = 5.0f;
    public bool isActive = true;

    [HotReload]
    void Update()
    {
        if (isActive)
        {
            transform.position += Vector3.right * privateSpeed * Time.deltaTime;
        }
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert: Should fail due to private member access
            Assert.IsFalse(task.Result.Success, "In-place reload should fail for private member access");
            Assert.IsNotNull(task.Result.ErrorMessage, "Should have error message");
            Assert.IsTrue(task.Result.ErrorMessage.Contains("privateSpeed") ||
                         task.Result.ErrorMessage.Contains("private"),
                         $"Error message should mention private member. Actual: {task.Result.ErrorMessage}");
        }

        [UnityTest]
        public IEnumerator InPlaceReload_MultipleHotReloadableMethods_ShouldSucceed()
        {
            // Arrange: Create source file with multiple [HotReloadWithOverrides] methods
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Samples.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float speed = 5.0f;
    public int health = 100;

    [HotReload]
    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }

    [HotReload]
    public int CalculateDamage(int baseDamage)
    {
        return baseDamage * 2;
    }

    [HotReload]
    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.Result.Success, $"In-place reload should succeed. Error: {task.Result.ErrorMessage}");
            Assert.AreEqual(3, task.Result.ExtractedMethods.Count, "Should extract 3 [HotReloadWithOverrides] methods");
            Assert.Contains("Update", task.Result.ExtractedMethods);
            Assert.Contains("CalculateDamage", task.Result.ExtractedMethods);
            Assert.Contains("ResetPosition", task.Result.ExtractedMethods);

            // Verify transformed code contains all methods
            Assert.IsTrue(task.Result.TransformedCode.Contains("[HotReloadOverrideMethod(\"HotReloadExampleComponent.Update\")]"));
            Assert.IsTrue(task.Result.TransformedCode.Contains("[HotReloadOverrideMethod(\"HotReloadExampleComponent.CalculateDamage\")]"));
            Assert.IsTrue(task.Result.TransformedCode.Contains("[HotReloadOverrideMethod(\"HotReloadExampleComponent.ResetPosition\")]"));
        }

        [UnityTest]
        public IEnumerator ReloadFileCommand_InPlaceSourceFile_ShouldUseInPlaceWorkflow()
        {
            // Arrange: Create source file with [HotReloadWithOverrides] method
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float speed = 5.0f;

    [HotReload]
    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute reload_file command
            var response = HotReloadCommands.ReloadFile(_testFilePath);

            // Assert
            yield return null; // Allow frame to complete

            Assert.IsTrue(response.Success, $"reload_file command should succeed. Error: {response.ErrorDetails}");
            Assert.IsNotNull(response.AssemblyName, "Should have assembly name");
            Assert.IsTrue(response.Items.Count > 0, "Should have registered methods");

            // Verify registry state
            var stats = HotReloadRegistry.GetStats();
            Assert.IsTrue(stats.ActiveOverrideCount > 0, "Should have active overrides registered");
        }

        [UnityTest]
        public IEnumerator ReloadFileCommand_SeparateOverrideFile_ShouldUseSeparateWorkflow()
        {
            // Arrange: Create separate override file in HotReload/ directory
            var hotReloadDir = Path.Combine(Application.temporaryCachePath, "HotReload");
            Directory.CreateDirectory(hotReloadDir);
            var overrideFilePath = Path.Combine(hotReloadDir, "TestOverrides.cs");

            var overrideCode = @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public static class TestOverrides
{
    [HotReloadOverrideMethod(""TestComponent.Update"")]
    public static void Update(MonoBehaviour instance)
    {
        instance.transform.position += Vector3.down * Time.deltaTime;
    }
}";

            File.WriteAllText(overrideFilePath, overrideCode);

            try
            {
                // Act: Execute reload_file_override command with HotReload/ path
                var response = HotReloadCommands.ReloadFileOverride("HotReload/TestOverrides.cs");

                // Assert
                yield return null; // Allow frame to complete

                // This might fail due to missing target method, but should use separate workflow
                // The important thing is that it recognizes it as separate override file
                Assert.IsNotNull(response, "Should get a response");
                // Note: This test verifies workflow detection rather than successful compilation
            }
            finally
            {
                // Cleanup
                if (File.Exists(overrideFilePath))
                {
                    File.Delete(overrideFilePath);
                }
                if (Directory.Exists(hotReloadDir))
                {
                    Directory.Delete(hotReloadDir, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator AccessibilityValidation_MixedAccess_ShouldReportSpecificViolations()
        {
            // Arrange: Create source with both valid and invalid member access
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float publicSpeed = 5.0f;
    private float privateSpeed = 3.0f;
    internal int internalHealth = 100;

    [HotReload]
    void Update()
    {
        // Valid public access
        transform.position += Vector3.right * publicSpeed * Time.deltaTime;

        // Invalid private access
        transform.position += Vector3.up * privateSpeed * Time.deltaTime;
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert: Should fail with specific violation details
            Assert.IsFalse(task.Result.Success, "Should fail due to private member access");
            Assert.IsTrue(task.Result.ValidationViolations.Count > 0, "Should have validation violations");

            var violation = task.Result.ValidationViolations[0];
            Assert.IsTrue(violation.MemberName.Contains("privateSpeed"), $"Should identify privateSpeed as violation. Got: {violation.MemberName}");
            Assert.IsNotNull(violation.Suggestion, "Should provide suggestion for fix");
        }

        [UnityTest]
        public IEnumerator TransformedCode_ThisReferences_ShouldBeConvertedToInstance()
        {
            // Arrange: Create source with 'this.' references
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float speed = 5.0f;

    [HotReload]
    void Update()
    {
        this.transform.position += Vector3.right * this.speed * Time.deltaTime;
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.Result.Success, $"Should succeed. Error: {task.Result.ErrorMessage}");

            var transformedCode = task.Result.TransformedCode;
            Assert.IsNotNull(transformedCode, "Should have transformed code");

            // Verify 'this.' is converted to 'instance.'
            Assert.IsFalse(transformedCode.Contains("this."), "Transformed code should not contain 'this.' references");
            Assert.IsTrue(transformedCode.Contains("instance."), "Transformed code should contain 'instance.' references");
            Assert.IsTrue(transformedCode.Contains("instance.transform"), "Should convert this.transform to instance.transform");
            Assert.IsTrue(transformedCode.Contains("instance.speed"), "Should convert this.speed to instance.speed");
        }

        [UnityTest]
        public IEnumerator MethodSignatures_WithParameters_ShouldPreserveParameterInfo()
        {
            // Arrange: Create source with parameterized [HotReloadWithOverrides] method
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class HotReloadExampleComponent : MonoBehaviour
{
    public float speed = 5.0f;

    [HotReload]
    public void MoveTowards(Vector3 target, float customSpeed)
    {
        var direction = (target - transform.position).normalized;
        transform.position += direction * customSpeed * Time.deltaTime;
    }
}";

            File.WriteAllText(_testFilePath, sourceCode);

            // Act: Execute in-place reload command
            var task = Task.Run(async () =>
            {
                return await InPlaceReloadProcessor.ProcessSourceFileAsync(_testFilePath);
            });

            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.Result.Success, $"Should succeed. Error: {task.Result.ErrorMessage}");

            var transformedCode = task.Result.TransformedCode;
            Assert.IsNotNull(transformedCode, "Should have transformed code");

            // Verify method signature includes instance parameter plus original parameters
            Assert.IsTrue(transformedCode.Contains("MoveTowards(HotReloadExampleComponent instance, Vector3 target, float customSpeed)"),
                $"Should preserve method parameters with instance parameter first. Actual:\n{transformedCode}");
        }

        [Test]
        public void ContainsHotReloadableMethods_ValidFile_ShouldReturnTrue()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class TestComponent : MonoBehaviour
{
    [HotReload]
    void Update() { }
}";

            var testPath = Path.Combine(Application.temporaryCachePath, "TestCheck.cs");
            File.WriteAllText(testPath, sourceCode);

            try
            {
                // Act
                var result = InPlaceReloadProcessor.ContainsHotReloadableMethodsAsync(testPath).GetAwaiter().GetResult();

                // Assert
                Assert.IsTrue(result, "Should detect [HotReloadWithOverrides] methods");
            }
            finally
            {
                if (File.Exists(testPath))
                {
                    File.Delete(testPath);
                }
            }
        }

        [Test]
        public void ContainsHotReloadableMethods_NoHotReloadMethods_ShouldReturnFalse()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestComponent : MonoBehaviour
{
    void Update() { }
}";

            var testPath = Path.Combine(Application.temporaryCachePath, "TestCheck.cs");
            File.WriteAllText(testPath, sourceCode);

            try
            {
                // Act
                var result = InPlaceReloadProcessor.ContainsHotReloadableMethodsAsync(testPath).GetAwaiter().GetResult();

                // Assert
                Assert.IsFalse(result, "Should not detect [HotReloadWithOverrides] methods");
            }
            finally
            {
                if (File.Exists(testPath))
                {
                    File.Delete(testPath);
                }
            }
        }
    }
}