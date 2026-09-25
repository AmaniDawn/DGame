using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.HotReload;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests specifically for HotReloadMethod attribute compilation and reflection.
    /// Helps diagnose issues with attribute detection in compiled assemblies.
    /// </summary>
    public class HotReloadAttributeTests
    {
        [Test]
        public void HotReloadOverrideMethodAttribute_DirectUsage_CanBeReflected()
        {
            // Test that the attribute can be found via reflection on a directly compiled class

            var method = typeof(TestHotReloadClass).GetMethod("TestOverrideMethod");
            Assert.IsNotNull(method, "Test method should exist");

            var attribute = method.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            Assert.IsNotNull(attribute, "HotReloadMethod attribute should be found");
            Assert.AreEqual("TestTarget.TestMethod", attribute.TargetMethodId, "Target method ID should match");
        }

        [Test]
        public void RoslynCompilationService_WithHotReloadAttribute_CompilesAndReflects()
        {
            // Test that RoslynCompilationService can compile code with HotReloadMethod attribute
            // and the attribute can be reflected from the compiled assembly

            var sourceCode = @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public static class CompiledHotReloadClass
{
    [HotReloadOverrideMethod(""CompiledTarget.CompiledMethod"")]
    public static void CompiledOverrideMethod(object instance)
    {
        Debug.Log(""Compiled hot reload method"");
    }
}";

            var request = new CompilationRequest
            {
                SourceCode = sourceCode,
                AssemblyName = "HotReloadAttributeTest",
                AdditionalAssemblyPrefixes = new[] { "Unity.Pipeline" }
            };

            var result = RoslynCompilationService.Compile(request);

            Assert.IsTrue(result.Success, $"Compilation should succeed. Errors: {string.Join(", ", result.Diagnostics?.Select(d => d.Message) ?? new string[0])}");
            Assert.IsNotNull(result.Assembly, "Should return compiled assembly");

            // Find the compiled class and method
            var compiledType = result.Assembly.GetType("CompiledHotReloadClass");
            Assert.IsNotNull(compiledType, "Compiled class should exist");

            var compiledMethod = compiledType.GetMethod("CompiledOverrideMethod");
            Assert.IsNotNull(compiledMethod, "Compiled method should exist");

            // Check if attribute can be found via reflection
            var customAttributes = compiledMethod.GetCustomAttributes(true);

            var hotReloadAttribute = compiledMethod.GetCustomAttribute<HotReloadOverrideMethodAttribute>();
            if (hotReloadAttribute == null)
            {
                // Try to find by name
                var attributeByName = customAttributes.FirstOrDefault(a => a.GetType().Name == "HotReloadOverrideMethodAttribute");

                if (attributeByName != null)
                {
                    hotReloadAttribute = attributeByName as HotReloadOverrideMethodAttribute;
                }
            }

            Assert.IsNotNull(hotReloadAttribute, "HotReloadMethod attribute should be found on compiled method");
            Assert.AreEqual("CompiledTarget.CompiledMethod", hotReloadAttribute.TargetMethodId, "Target method ID should match");
        }

        [Test]
        public void MetadataReferences_IncludeUnityPipeline_CanFindHotReloadTypes()
        {
            // Verify that Unity.Pipeline assemblies are included in metadata references

            var references = RoslynCompilationService.GetMetadataReferences(new[] { "Unity.Pipeline" });

            var pipelineReferences = references.Where(r => r.Display?.Contains("Unity.Pipeline") == true).ToList();

            Assert.Greater(pipelineReferences.Count, 0, "Should find Unity.Pipeline assembly references");
        }

        /// <summary>
        /// Test class with HotReloadMethod attribute for direct testing.
        /// </summary>
        public static class TestHotReloadClass
        {
            [HotReloadOverrideMethod("TestTarget.TestMethod")]
            public static void TestOverrideMethod(object instance)
            {
            }
        }
    }
}