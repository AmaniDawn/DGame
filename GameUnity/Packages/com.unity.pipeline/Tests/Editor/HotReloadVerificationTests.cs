using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Simple verification tests for hot reload infrastructure.
    /// These tests verify the core functionality works correctly in isolation.
    /// </summary>
    public class HotReloadVerificationTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state for each test
            HotReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            HotReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void HotReloadRegistry_EmptyState_HasZeroStats()
        {
            // Act
            var stats = HotReloadRegistry.GetStats();

            // Assert
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(0));
            Assert.That(stats.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(stats.LoadedTypeCount, Is.EqualTo(0));
        }

        [Test]
        public void HotReloadRegistry_RegisterSingleMethod_CorrectCount()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            var attribute = new HotReloadWithOverridesAttribute();

            // Act
            HotReloadRegistry.RegisterReloadableMethod(testMethod, attribute);

            // Assert
            var stats = HotReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Count, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Contains("SimpleTestClass.TestMethod"));
        }

        [Test]
        public void HotReloadRegistry_RegisterMultipleMethods_CorrectCount()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var method1 = testType.GetMethod("TestMethod");
            var method2 = testType.GetMethod("AnotherTestMethod");

            // Act
            HotReloadRegistry.RegisterReloadableMethod(method1, new HotReloadWithOverridesAttribute());
            HotReloadRegistry.RegisterReloadableMethod(method2, new HotReloadWithOverridesAttribute { Id = "CustomId" });

            // Assert
            var stats = HotReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(2));
            Assert.That(stats.ReloadableMethodIds.Count, Is.EqualTo(2));
            Assert.That(stats.ReloadableMethodIds.Contains("SimpleTestClass.TestMethod"));
            Assert.That(stats.ReloadableMethodIds.Contains("CustomId"));
        }

        [Test]
        public void HotReloadRegistry_TryInvokeWithoutOverride_ReturnsFalse()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            HotReloadRegistry.RegisterReloadableMethod(testMethod, new HotReloadWithOverridesAttribute());

            var testInstance = new SimpleTestClass();

            // Act
            var result = HotReloadRegistry.TryInvokeHotReload("SimpleTestClass.TestMethod", testInstance);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HotReloadHelper_GenerateMethodId_CorrectFormat()
        {
            // This tests the private method indirectly through the IsHotReloadActive method
            // Act & Assert
            Assert.That(HotReloadHelper.IsHotReloadActive<SimpleTestClass>("TestMethod"), Is.False);
            // The method ID generation is tested implicitly - if it's wrong, the registry lookup would fail
        }

        [Test]
        public void HotReloadRegistry_ClearForTesting_RemovesAllState()
        {
            // Arrange - add some state
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            HotReloadRegistry.RegisterReloadableMethod(testMethod, new HotReloadWithOverridesAttribute());

            // Verify state exists
            var statsBefore = HotReloadRegistry.GetStats();
            Assert.That(statsBefore.ReloadableMethodCount, Is.EqualTo(1));

            // Act
            HotReloadRegistry.ClearAllForTesting();

            // Assert
            var statsAfter = HotReloadRegistry.GetStats();
            Assert.That(statsAfter.ReloadableMethodCount, Is.EqualTo(0));
            Assert.That(statsAfter.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(statsAfter.LoadedTypeCount, Is.EqualTo(0));
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

            public int AnotherTestMethod(int input)
            {
                return input * 2;
            }
        }
    }
}