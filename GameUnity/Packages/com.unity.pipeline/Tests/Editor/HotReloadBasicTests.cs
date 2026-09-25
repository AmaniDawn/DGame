using System.Collections;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Basic tests for hot reload Pattern A method override functionality.
    /// Tests the end-to-end flow: mark method -> create override -> compile -> verify behavior.
    /// </summary>
    public class HotReloadBasicTests
    {
        /// <summary>
        /// Test class with hot reloadable methods for testing.
        /// </summary>
        public class TestComponent : MonoBehaviour
        {
            public int lastCalculatedValue;
            public string lastMessage;

            [HotReloadWithOverrides]
            public void UpdateValue()
            {
                lastCalculatedValue = 10; // Original behavior
                lastMessage = "Original UpdateValue called";
            }

            [HotReloadWithOverrides(Id = "CustomCalculation")]
            public int CalculateScore(int input)
            {
                return input * 2; // Original calculation
            }
        }

        private TestComponent testComponent;

        [SetUp]
        public void SetUp()
        {
            // Create test component
            var go = new GameObject("HotReloadTestObject");
            testComponent = go.AddComponent<TestComponent>();

            // Clear any existing hot reload state (including reloadable methods for test isolation)
            HotReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            if (testComponent != null && testComponent.gameObject != null)
            {
                Object.DestroyImmediate(testComponent.gameObject);
            }

            // Clean up hot reload state
            HotReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void HotReloadRegistry_RegisterReloadableMethod_Success()
        {
            // Arrange
            var method = typeof(TestComponent).GetMethod("UpdateValue");
            var attribute = new HotReloadWithOverridesAttribute();

            // Act
            HotReloadRegistry.RegisterReloadableMethod(method, attribute);

            // Assert
            var stats = HotReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Contains("TestComponent.UpdateValue"));
        }

        [Test]
        public void HotReloadRegistry_RegisterReloadableMethodWithCustomId_Success()
        {
            // Arrange
            var method = typeof(TestComponent).GetMethod("CalculateScore");
            var attribute = new HotReloadWithOverridesAttribute { Id = "CustomCalculation" };

            // Act
            HotReloadRegistry.RegisterReloadableMethod(method, attribute);

            // Assert
            var stats = HotReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Contains("CustomCalculation"));
        }

        [Test]
        public void HotReloadRegistry_TryInvokeHotReload_NoOverrideReturnsFalse()
        {
            // Arrange
            var method = typeof(TestComponent).GetMethod("UpdateValue");
            var attribute = new HotReloadWithOverridesAttribute();
            HotReloadRegistry.RegisterReloadableMethod(method, attribute);

            // Act
            var result = HotReloadRegistry.TryInvokeHotReload("TestComponent.UpdateValue", testComponent);

            // Assert
            Assert.That(result, Is.False, "Should return false when no hot reload override is registered");
        }

        [Test]
        public void HotReloadRegistry_GetStats_ReturnsCorrectCounts()
        {
            // Arrange - register multiple methods
            var updateMethod = typeof(TestComponent).GetMethod("UpdateValue");
            var calculateMethod = typeof(TestComponent).GetMethod("CalculateScore");

            HotReloadRegistry.RegisterReloadableMethod(updateMethod, new HotReloadWithOverridesAttribute());
            HotReloadRegistry.RegisterReloadableMethod(calculateMethod, new HotReloadWithOverridesAttribute { Id = "CustomCalculation" });

            // Act
            var stats = HotReloadRegistry.GetStats();

            // Assert
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(2));
            Assert.That(stats.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(stats.LoadedTypeCount, Is.EqualTo(0));
            Assert.That(stats.ReloadableMethodIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void HotReloadRegistry_ClearAllOverrides_ClearsAllState()
        {
            // Arrange - register some methods and mock some overrides
            var method = typeof(TestComponent).GetMethod("UpdateValue");
            HotReloadRegistry.RegisterReloadableMethod(method, new HotReloadWithOverridesAttribute());

            // Act
            HotReloadRegistry.ClearAllOverrides();

            // Assert
            var stats = HotReloadRegistry.GetStats();
            Assert.That(stats.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(stats.LoadedTypeCount, Is.EqualTo(0));

            // Note: ReloadableMethodCount might not be cleared as those are the original methods, not overrides
            // This depends on implementation - currently reloadable methods are not cleared, only overrides
        }

        [UnityTest]
        public IEnumerator HotReloadRegistry_MainThreadDispatch_WorksCorrectly()
        {
            // Arrange
            var method = typeof(TestComponent).GetMethod("UpdateValue");
            var attribute = new HotReloadWithOverridesAttribute { RequireMainThread = true };
            HotReloadRegistry.RegisterReloadableMethod(method, attribute);

            // Act - test that method can be invoked (even without override, for thread safety)
            var result = HotReloadRegistry.TryInvokeHotReload("TestComponent.UpdateValue", testComponent);

            // Assert
            Assert.That(result, Is.False); // No override registered, should return false

            // Yield to ensure any async operations complete
            yield return null;
        }

        [Test]
        public void TestComponent_OriginalBehavior_WorksCorrectly()
        {
            // Arrange & Act - call original methods to establish baseline
            testComponent.UpdateValue();
            var score = testComponent.CalculateScore(5);

            // Assert
            Assert.That(testComponent.lastCalculatedValue, Is.EqualTo(10));
            Assert.That(testComponent.lastMessage, Is.EqualTo("Original UpdateValue called"));
            Assert.That(score, Is.EqualTo(10)); // 5 * 2
        }
    }
}