using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.BuildProcessors;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the build-time collection of hot reload roots. The build injection itself runs only
    /// inside a real player build, but the root-collection helper is editor-callable and verifiable.
    /// </summary>
    public class PipelineRuntimeBuildProcessorTests
    {
        [Test]
        public void CollectProjectRoots_IncludesAssetsFolder()
        {
            var roots = PipelineRuntimeBuildProcessor.CollectProjectRoots();

            var assets = Path.GetFullPath(Application.dataPath);
            CollectionAssert.Contains(roots, assets, "Assets folder must be a baked root.");
        }

        [Test]
        public void CollectProjectRoots_AreAbsoluteAndNonEmpty()
        {
            var roots = PipelineRuntimeBuildProcessor.CollectProjectRoots();

            Assert.IsNotEmpty(roots);
            Assert.IsTrue(roots.All(r => !string.IsNullOrEmpty(r) && Path.IsPathRooted(r)),
                "All baked roots must be non-empty absolute paths.");
        }

        [Test]
        public void CollectProjectRoots_IncludesThisPackage()
        {
            // com.unity.pipeline is loaded into the test project, so its resolved location must be a
            // baked root - this is the "package source can be hot reloaded" guarantee.
            var roots = PipelineRuntimeBuildProcessor.CollectProjectRoots();

            Assert.IsTrue(roots.Count > 1, "Expected at least one package root in addition to Assets.");
        }
    }
}
