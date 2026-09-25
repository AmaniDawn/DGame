using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for HotReloadRegistry to verify method registration and statistics.
    /// Helps diagnose issues with hot reload method registration.
    /// </summary>
    public class HotReloadRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            HotReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            HotReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void RegisterMethodOverride_DirectCall_UpdatesStats()
        {
            // Test direct registration to verify registry behavior

            var method = typeof(TestRegistryClass).GetMethod("TestMethod");
            var attribute = method.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            var type = typeof(TestRegistryClass);

            Assert.IsNotNull(method, "Test method should exist");
            Assert.IsNotNull(attribute, "Test attribute should exist");

            // Get initial stats
            var initialStats = HotReloadRegistry.GetStats();

            // CRITICAL: First register the target method as reloadable (this was missing!)
            // Create a mock target method that matches the attribute's TargetMethodId
            var targetMethod = typeof(MockTargetClass1).GetMethod("TargetMethod1");
            var reloadableAttr = new HotReloadWithOverridesAttribute { Id = "TargetClass1.TargetMethod1" };
            HotReloadRegistry.RegisterReloadableMethod(targetMethod, reloadableAttr);

            // Register the type and method override
            HotReloadRegistry.RegisterHotReloadType(type, "TestAssembly");
            HotReloadRegistry.RegisterMethodOverride(method, attribute, type);

            // Get final stats
            var finalStats = HotReloadRegistry.GetStats();

            // Verify stats changed
            Assert.Greater(finalStats.ActiveOverrideCount, initialStats.ActiveOverrideCount, "Active override count should increase");
            Assert.Greater(finalStats.LoadedTypeCount, initialStats.LoadedTypeCount, "Loaded type count should increase");
            Assert.Greater(finalStats.ReloadableMethodCount, initialStats.ReloadableMethodCount, "Reloadable method count should increase");
        }

        [Test]
        public void GetStats_AfterRegistration_ReturnsCorrectCounts()
        {
            // Test that GetStats returns accurate counts

            var stats1 = HotReloadRegistry.GetStats();
            Assert.AreEqual(0, stats1.ActiveOverrideCount, "Should start with 0 active overrides");

            // Register target methods as reloadable first (this was missing!)
            var targetMethod1 = typeof(MockTargetClass1).GetMethod("TargetMethod1");
            var reloadableAttr1 = new HotReloadWithOverridesAttribute { Id = "TargetClass1.TargetMethod1" };
            HotReloadRegistry.RegisterReloadableMethod(targetMethod1, reloadableAttr1);

            var targetMethod2 = typeof(MockTargetClass2).GetMethod("TargetMethod2");
            var reloadableAttr2 = new HotReloadWithOverridesAttribute { Id = "TargetClass2.TargetMethod2" };
            HotReloadRegistry.RegisterReloadableMethod(targetMethod2, reloadableAttr2);

            // Register multiple methods
            var method1 = typeof(TestRegistryClass).GetMethod("TestMethod");
            var attribute1 = method1.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            HotReloadRegistry.RegisterHotReloadType(typeof(TestRegistryClass), "TestAssembly1");
            HotReloadRegistry.RegisterMethodOverride(method1, attribute1, typeof(TestRegistryClass));

            var method2 = typeof(TestRegistryClass2).GetMethod("AnotherTestMethod");
            var attribute2 = method2.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            HotReloadRegistry.RegisterHotReloadType(typeof(TestRegistryClass2), "TestAssembly2");
            HotReloadRegistry.RegisterMethodOverride(method2, attribute2, typeof(TestRegistryClass2));

            var finalStats = HotReloadRegistry.GetStats();

            Assert.AreEqual(2, finalStats.ActiveOverrideCount, "Should have 2 active overrides");
            Assert.AreEqual(2, finalStats.LoadedTypeCount, "Should have 2 loaded types");
            Assert.AreEqual(2, finalStats.ReloadableMethodCount, "Should have 2 reloadable methods");
        }

        [Test]
        public void ClearAllForTesting_ResetsStats()
        {
            // Register target method as reloadable first (this was missing!)
            var targetMethod = typeof(MockTargetClass1).GetMethod("TargetMethod1");
            var reloadableAttr = new HotReloadWithOverridesAttribute { Id = "TargetClass1.TargetMethod1" };
            HotReloadRegistry.RegisterReloadableMethod(targetMethod, reloadableAttr);

            // Register something first
            var method = typeof(TestRegistryClass).GetMethod("TestMethod");
            var attribute = method.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            HotReloadRegistry.RegisterHotReloadType(typeof(TestRegistryClass), "TestAssembly");
            HotReloadRegistry.RegisterMethodOverride(method, attribute, typeof(TestRegistryClass));

            var statsBeforeClear = HotReloadRegistry.GetStats();
            Assert.Greater(statsBeforeClear.ActiveOverrideCount, 0, "Should have active overrides before clear");
            Assert.Greater(statsBeforeClear.ReloadableMethodCount, 0, "Should have reloadable methods before clear");

            // Clear and verify
            HotReloadRegistry.ClearAllForTesting();

            var statsAfterClear = HotReloadRegistry.GetStats();
            Assert.AreEqual(0, statsAfterClear.ActiveOverrideCount, "Should have 0 active overrides after clear");
            Assert.AreEqual(0, statsAfterClear.LoadedTypeCount, "Should have 0 loaded types after clear");
            Assert.AreEqual(0, statsAfterClear.ReloadableMethodCount, "Should have 0 reloadable methods after clear");
        }

        /// <summary>
        /// Test class with HotReloadMethod attribute for registry testing.
        /// </summary>
        public static class TestRegistryClass
        {
            [HotReloadOverrideMethod("TargetClass1.TargetMethod1")]
            public static void TestMethod(MockTargetClass1 instance)
            {
            }
        }

        /// <summary>
        /// Second test class for multi-registration testing.
        /// </summary>
        public static class TestRegistryClass2
        {
            [HotReloadOverrideMethod("TargetClass2.TargetMethod2")]
            public static void AnotherTestMethod(MockTargetClass2 instance)
            {
            }
        }

        /// <summary>
        /// Mock target class 1 for testing reloadable method registration.
        /// </summary>
        public class MockTargetClass1
        {
            public int value = 42;

            [HotReloadWithOverrides(Id = "TargetClass1.TargetMethod1")]
            public void TargetMethod1()
            {
                value = 123;
            }
        }

        /// <summary>
        /// Mock target class 2 for testing reloadable method registration.
        /// </summary>
        public class MockTargetClass2
        {
            public string text = "original";

            [HotReloadWithOverrides(Id = "TargetClass2.TargetMethod2")]
            public void TargetMethod2()
            {
                text = "modified";
            }
        }
    }
}