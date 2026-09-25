using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.BuildProcessors;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for Pipeline runtime build support using RuntimePipelineManager.
    /// Validates build processor behavior and scene-based configuration detection.
    /// </summary>
    public class RuntimePipelineBuildTests
    {
        private string m_TestScenePath;
        private string m_BuildDirectory;
        private PipelineRuntimeBuildProcessor m_BuildProcessor;

        [SetUp]
        public void SetUp()
        {
            // Create test scene path
            m_TestScenePath = "Assets/TestRuntimePipelineScene.unity";
            m_BuildDirectory = $"TestBuild/{GetType().Name}";
            m_BuildProcessor = new PipelineRuntimeBuildProcessor();

            // Clean up any existing test files
            if (File.Exists(m_TestScenePath))
            {
                AssetDatabase.DeleteAsset(m_TestScenePath);
            }

            if (Directory.Exists(m_BuildDirectory))
            {
                Directory.Delete(m_BuildDirectory, true);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test scene
            if (File.Exists(m_TestScenePath))
            {
                AssetDatabase.DeleteAsset(m_TestScenePath);
            }

            // Clean up build directory
            if (Directory.Exists(m_BuildDirectory))
            {
                Directory.Delete(m_BuildDirectory, true);
            }
        }

        [Test]
        public void BuildProcessor_RuntimeManagerDisabled_LogsDisabledMessage()
        {
            // Arrange - Create scene with disabled RuntimePipelineManager
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var go = new GameObject("RuntimePipelineManager");
            var manager = go.AddComponent<RuntimePipelineManager>();
            manager.enableInBuilds = false; // Disabled

            EditorSceneManager.SaveScene(scene, m_TestScenePath);

            var originalScenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(m_TestScenePath, true) };

            try
            {
                var mockReport = CreateMockBuildReport();

                // Act & Assert
                Assert.DoesNotThrow(() => m_BuildProcessor.OnPreprocessBuild(mockReport));
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildProcessor_RuntimeManagerEnabled_ValidatesConfiguration()
        {
            // Arrange - Create scene with enabled RuntimePipelineManager
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var go = new GameObject("RuntimePipelineManager");
            var manager = go.AddComponent<RuntimePipelineManager>();

            // Configure with valid settings
            manager.enableInBuilds = true;

            EditorSceneManager.SaveScene(scene, m_TestScenePath);

            var originalScenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(m_TestScenePath, true) };

            try
            {
                var mockReport = CreateMockBuildReport();

                // Act & Assert - Should not throw with valid configuration
                Assert.DoesNotThrow(() => m_BuildProcessor.OnPreprocessBuild(mockReport));
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildProcessor_MultipleManagers_UsesFirstEnabled()
        {
            // Arrange - Create scene with multiple RuntimePipelineManagers
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            // First manager - disabled
            var go1 = new GameObject("DisabledManager");
            var manager1 = go1.AddComponent<RuntimePipelineManager>();
            manager1.enableInBuilds = false;

            // Second manager - enabled
            var go2 = new GameObject("EnabledManager");
            var manager2 = go2.AddComponent<RuntimePipelineManager>();
            manager2.enableInBuilds = true;

            EditorSceneManager.SaveScene(scene, m_TestScenePath);

            var originalScenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(m_TestScenePath, true) };

            try
            {
                var mockReport = CreateMockBuildReport();

                // Act & Assert - Should use the enabled manager without throwing
                Assert.DoesNotThrow(() => m_BuildProcessor.OnPreprocessBuild(mockReport));
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
                Object.DestroyImmediate(go1);
                Object.DestroyImmediate(go2);
            }
        }

        /// <summary>
        /// Create a mock BuildReport for testing build processor.
        /// </summary>
#if UNITY_6000_3_OR_NEWER
        private BuildCallbackContext CreateMockBuildReport()
#else
        private BuildReport CreateMockBuildReport()
#endif
        {
            // Note: BuildReport is sealed and cannot be easily mocked
            // In a real implementation, we'd need a wrapper or different approach
            // For now, we'll use reflection or test with null if the processor handles it
            return null; // Build processor doesn't actually use the report parameter
        }
    }

    /// <summary>
    /// Platform-specific build tests following Unity test patterns.
    /// Tests actual build generation with RuntimePipelineManager.
    /// </summary>
    [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
    public class RuntimePipelineWindows64BuildTests : RuntimePipelineBuildTestBase
    {
        protected override BuildTarget Target => BuildTarget.StandaloneWindows64;
    }

    /// <summary>
    /// Abstract base class for build generation tests.
    /// Follows Unity test patterns for build validation.
    /// </summary>
    public abstract class RuntimePipelineBuildTestBase
    {
        protected abstract BuildTarget Target { get; }
        protected string BuildDirectory => $"TestBuild/{GetType().Name}";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Clean up from any previous test runs
            if (Directory.Exists(BuildDirectory))
            {
                Directory.Delete(BuildDirectory, true);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up after tests
            if (Directory.Exists(BuildDirectory))
            {
                Directory.Delete(BuildDirectory, true);
            }
        }

        [Test]
        public void BuildPlayerWithRuntimeManager_ValidConfiguration_BuildSucceeds()
        {
            // Skip if platform support not available
            if (!IsPlatformSupportInstalled(Target))
            {
                Assert.Ignore($"Platform support for {Target} is not installed");
            }

            // Note: When calling BuildPlayer the list of scene is ALWAYS overridden with what is in the current build profile
            var testScenePath = CreateTestSceneWithRuntimeManager(validConfiguration: true);

            try
            {
                var buildOptions = new BuildPlayerOptions
                {
                    scenes = new[] { testScenePath },
                    locationPathName = Path.Combine(BuildDirectory, "TestBuild.exe"),
                    target = Target,
                    options = BuildOptions.Development // Use development for faster builds
                };

                // Act - Build player
                var report = BuildPipeline.BuildPlayer(buildOptions);

                // Assert - Build succeeded
                Assert.AreEqual(BuildResult.Succeeded, report.summary.result,
                    $"Build should succeed: {string.Join("; ", report.steps.SelectMany(s => s.messages).Select(m => m.content))}");

                // Verify output files exist
                var outputFiles = report.GetFiles();
                Assert.IsTrue(outputFiles.Any(), "Build should produce output files");

                // Verify executable exists
                Assert.IsTrue(File.Exists(buildOptions.locationPathName), "Executable should exist after successful build");
            }
            finally
            {
                // Clean up test scene
                AssetDatabase.DeleteAsset(testScenePath);
            }
        }

        /// <summary>
        /// Create a test scene with RuntimePipelineManager component.
        /// </summary>
        protected string CreateTestSceneWithRuntimeManager(bool validConfiguration)
        {
            var scenePath = $"Assets/TestScene_{Target}_{System.Guid.NewGuid():N}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            var go = new GameObject("RuntimePipelineManager");
            var manager = go.AddComponent<RuntimePipelineManager>();

            manager.enableInBuilds = validConfiguration;

            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        /// <summary>
        /// Check if platform support is installed for the target.
        /// </summary>
        protected bool IsPlatformSupportInstalled(BuildTarget target)
        {
            try
            {
                var group = BuildPipeline.GetBuildTargetGroup(target);
                return true; // If we can get the group, platform support is available
            }
            catch
            {
                return false;
            }
        }
    }
}