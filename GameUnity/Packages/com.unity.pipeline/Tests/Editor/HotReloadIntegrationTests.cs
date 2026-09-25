using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Integration tests for hot reload end-to-end functionality.
    /// Tests the complete workflow: register methods → create overrides → compile → verify runtime behavior.
    /// </summary>
    public class HotReloadIntegrationTests
    {
        /// <summary>
        /// Test component that properly integrates hot reload pattern.
        /// </summary>
        public class IntegratedTestComponent : MonoBehaviour
        {
            public int lastValue;
            public string lastMessage;
            public Vector3 lastMovement;

            [HotReloadWithOverrides]
            public void ProcessValue(int input)
            {
                HotReloadHelper.ExecuteWithHotReload(this, "ProcessValue",
                    () => OriginalProcessValue(input), input);
            }

            [HotReloadWithOverrides(Id = "CustomMovement")]
            public void Move(Vector3 direction)
            {
                HotReloadHelper.ExecuteWithHotReloadCustomId(this, "CustomMovement",
                    () => OriginalMove(direction), direction);
            }

            private void OriginalProcessValue(int input)
            {
                lastValue = input * 2;
                lastMessage = $"Original: {input} * 2 = {lastValue}";
            }

            private void OriginalMove(Vector3 direction)
            {
                lastMovement = direction * 1.5f;
            }
        }

        /// <summary>
        /// Mock hot reload override methods for testing.
        /// This simulates what would be in a HotReload/TestFile.cs.
        /// </summary>
        public static class MockHotReloadOverrides
        {
            [HotReloadOverrideMethod("IntegratedTestComponent.ProcessValue")]
            public static void ProcessValue(IntegratedTestComponent instance, int input)
            {
                instance.lastValue = input * 3; // Different calculation
                instance.lastMessage = $"HotReload: {input} * 3 = {instance.lastValue}";
            }

            [HotReloadOverrideMethod("CustomMovement")]
            public static void Move(IntegratedTestComponent instance, Vector3 direction)
            {
                instance.lastMovement = direction * 2.5f; // Different multiplier
            }
        }

        private IntegratedTestComponent testComponent;
        private GameObject testObject;

        [SetUp]
        public void SetUp()
        {
            // Create test component
            testObject = new GameObject("IntegrationTestObject");
            testComponent = testObject.AddComponent<IntegratedTestComponent>();

            // Clear hot reload state (including reloadable methods for test isolation)
            HotReloadRegistry.ClearAllForTesting();

            // Register the test methods as hot reloadable
            var processMethod = typeof(IntegratedTestComponent).GetMethod("ProcessValue");
            var moveMethod = typeof(IntegratedTestComponent).GetMethod("Move");

            HotReloadRegistry.RegisterReloadableMethod(processMethod, new HotReloadWithOverridesAttribute());
            HotReloadRegistry.RegisterReloadableMethod(moveMethod, new HotReloadWithOverridesAttribute { Id = "CustomMovement" });
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }

            HotReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void IntegratedComponent_OriginalBehavior_WorksCorrectly()
        {
            // Act - call methods without hot reload overrides
            testComponent.ProcessValue(5);
            testComponent.Move(Vector3.right);

            // Assert - should use original implementations
            Assert.That(testComponent.lastValue, Is.EqualTo(10)); // 5 * 2
            Assert.That(testComponent.lastMessage, Is.EqualTo("Original: 5 * 2 = 10"));
            Assert.That(testComponent.lastMovement, Is.EqualTo(Vector3.right * 1.5f));
        }

        [Test]
        public void IntegratedComponent_WithHotReloadOverride_UsesOverrideLogic()
        {
            // Arrange - register hot reload overrides
            var processOverride = typeof(MockHotReloadOverrides).GetMethod("ProcessValue");
            var moveOverride = typeof(MockHotReloadOverrides).GetMethod("Move");

            HotReloadRegistry.RegisterMethodOverride(processOverride,
                new HotReloadOverrideMethodAttribute("IntegratedTestComponent.ProcessValue"),
                typeof(MockHotReloadOverrides));

            HotReloadRegistry.RegisterMethodOverride(moveOverride,
                new HotReloadOverrideMethodAttribute("CustomMovement"),
                typeof(MockHotReloadOverrides));

            // Act - call methods with hot reload overrides active
            testComponent.ProcessValue(5);
            testComponent.Move(Vector3.right);

            // Assert - should use hot reload implementations
            Assert.That(testComponent.lastValue, Is.EqualTo(15)); // 5 * 3 (hot reload version)
            Assert.That(testComponent.lastMessage, Is.EqualTo("HotReload: 5 * 3 = 15"));
            Assert.That(testComponent.lastMovement, Is.EqualTo(Vector3.right * 2.5f)); // hot reload version
        }

        [Test]
        public void HotReloadHelper_IsHotReloadActive_ReturnsCorrectStatus()
        {
            // Arrange - no overrides initially
            Assert.That(HotReloadHelper.IsHotReloadActive<IntegratedTestComponent>("ProcessValue"), Is.False);
            Assert.That(HotReloadHelper.IsHotReloadActive("CustomMovement"), Is.False);

            // Act - register override
            var processOverride = typeof(MockHotReloadOverrides).GetMethod("ProcessValue");
            HotReloadRegistry.RegisterMethodOverride(processOverride,
                new HotReloadOverrideMethodAttribute("IntegratedTestComponent.ProcessValue"),
                typeof(MockHotReloadOverrides));

            // Assert - now should be active
            Assert.That(HotReloadHelper.IsHotReloadActive<IntegratedTestComponent>("ProcessValue"), Is.True);
            Assert.That(HotReloadHelper.IsHotReloadActive("CustomMovement"), Is.False);
        }

        [Test]
        public void HotReloadRegistry_Stats_ReflectCurrentState()
        {
            // Arrange - register override
            var processOverride = typeof(MockHotReloadOverrides).GetMethod("ProcessValue");
            HotReloadRegistry.RegisterMethodOverride(processOverride,
                new HotReloadOverrideMethodAttribute("IntegratedTestComponent.ProcessValue"),
                typeof(MockHotReloadOverrides));

            // Act
            var stats = HotReloadRegistry.GetStats();

            // Assert
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(2)); // ProcessValue + Move
            Assert.That(stats.ActiveOverrideCount, Is.EqualTo(1)); // Only ProcessValue override
            Assert.That(stats.ActiveOverrideIds.Contains("IntegratedTestComponent.ProcessValue"), Is.True);
            Assert.That(stats.ReloadableMethodIds.Contains("IntegratedTestComponent.ProcessValue"), Is.True);
            Assert.That(stats.ReloadableMethodIds.Contains("CustomMovement"), Is.True);
        }

        [Test]
        public void HotReloadRegistry_ClearOverrides_RestoresOriginalBehavior()
        {
            // Arrange - register and test override
            var processOverride = typeof(MockHotReloadOverrides).GetMethod("ProcessValue");
            HotReloadRegistry.RegisterMethodOverride(processOverride,
                new HotReloadOverrideMethodAttribute("IntegratedTestComponent.ProcessValue"),
                typeof(MockHotReloadOverrides));

            testComponent.ProcessValue(5);
            Assert.That(testComponent.lastValue, Is.EqualTo(15)); // Hot reload behavior

            // Act - clear overrides (but keep reloadable method registrations)
            HotReloadRegistry.ClearAllOverrides();

            // Assert - should revert to original behavior
            testComponent.ProcessValue(5);
            Assert.That(testComponent.lastValue, Is.EqualTo(10)); // Original behavior
            Assert.That(testComponent.lastMessage, Is.EqualTo("Original: 5 * 2 = 10"));
        }

        [UnityTest]
        public IEnumerator HotReloadIntegration_MainThreadDispatch_WorksCorrectly()
        {
            // Arrange
            var processOverride = typeof(MockHotReloadOverrides).GetMethod("ProcessValue");
            HotReloadRegistry.RegisterMethodOverride(processOverride,
                new HotReloadOverrideMethodAttribute("IntegratedTestComponent.ProcessValue"),
                typeof(MockHotReloadOverrides));

            // Act - invoke from main thread
            testComponent.ProcessValue(7);

            // Yield to allow any async operations
            yield return null;

            // Assert - hot reload should work on main thread
            Assert.That(testComponent.lastValue, Is.EqualTo(21)); // 7 * 3
            Assert.That(testComponent.lastMessage, Is.EqualTo("HotReload: 7 * 3 = 21"));
        }
    }
}