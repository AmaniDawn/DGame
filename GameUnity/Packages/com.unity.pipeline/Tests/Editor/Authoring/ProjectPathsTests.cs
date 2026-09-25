using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;

namespace Unity.Pipeline.Tests.Editor.Authoring
{
    /// <summary>
    /// Tests for ProjectPaths (CLI-190): Assets-relative resolution, the traversal/out-of-root
    /// guards, and the configurable authoring root.
    /// </summary>
    public class ProjectPathsTests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset to the default root before each test so a leftover EditorPref can't leak in.
            ProjectPaths.ResetAuthoringRoot();
        }

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();
        }

        [Test]
        public void DefaultRoot_IsAssets()
        {
            ProjectPaths.ResetAuthoringRoot();
            Assert.AreEqual("Assets", ProjectPaths.AuthoringRoot);
        }

        [Test]
        public void Resolve_BarePath_PrependsAssets()
        {
            var resolved = ProjectPaths.Resolve("Gameplay/Enemies", out var error);
            Assert.IsNull(error, error);
            Assert.AreEqual("Assets/Gameplay/Enemies", resolved);
        }

        [Test]
        public void Resolve_ExplicitAssetsPath_Unchanged()
        {
            var resolved = ProjectPaths.Resolve("Assets/Gameplay", out var error);
            Assert.IsNull(error, error);
            Assert.AreEqual("Assets/Gameplay", resolved);
        }

        [Test]
        public void Resolve_Traversal_Fails()
        {
            Assert.IsNull(ProjectPaths.Resolve("../Outside", out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Resolve_EmptyPath_Fails()
        {
            Assert.IsNull(ProjectPaths.Resolve("", out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void ConfigurableRoot_ConfinesBareAndRejectsOutside()
        {
            ProjectPaths.AuthoringRoot = "Assets/__CLI190Root";

            // Bare path resolves under the configured root.
            var inside = ProjectPaths.Resolve("Foo/Bar", out var insideError);
            Assert.IsNull(insideError, insideError);
            Assert.AreEqual("Assets/__CLI190Root/Foo/Bar", inside);

            // Explicit path outside the configured root is rejected (confinement).
            Assert.IsNull(ProjectPaths.Resolve("Assets/Other", out var outsideError));
            Assert.IsNotEmpty(outsideError);
        }

        [Test]
        public void SetAuthoringRoot_OutsideAssets_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => ProjectPaths.AuthoringRoot = "Outside");
            Assert.Throws<System.ArgumentException>(() => ProjectPaths.AuthoringRoot = "Assets/../Escape");
        }
    }
}
