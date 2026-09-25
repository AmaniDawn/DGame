using System.IO;
using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Unit tests for HotReloadCommands.ValidateReloadPath - the scope guard that ensures only files
    /// under the build-baked allowed roots (Assets/, or a loaded package's location) are compiled and
    /// injected at runtime.
    ///
    /// Tests build a throwaway "project root" under the temp folder so they never depend on (or
    /// touch) the real test project's Assets.
    /// </summary>
    public class ReloadPathValidationTests
    {
        private string m_Root;
        private string m_Assets;

        [SetUp]
        public void SetUp()
        {
            m_Root = Path.Combine(Path.GetTempPath(), "pipeline_pathval_" + Path.GetRandomFileName());
            m_Assets = Path.Combine(m_Root, "Assets");
            Directory.CreateDirectory(m_Assets);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Root))
            {
                Directory.Delete(m_Root, recursive: true);
            }
        }

        private string CreateFile(string relativePath)
        {
            var full = Path.Combine(m_Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, "// test");
            return full;
        }

        [Test]
        public void Validate_FileUnderAllowedRoot_IsValid()
        {
            var path = CreateFile(Path.Combine("Assets", "Scripts", "Player.cs"));

            var result = HotReloadCommands.ValidateReloadPath(path, new[] { m_Assets });

            Assert.IsTrue(result.IsValid, result.ErrorDetails);
        }

        [Test]
        public void Validate_FileUnderExternalLoadedPackage_IsValid()
        {
            // A local package loaded from outside the project root (e.g. a file: package). Its
            // resolved path is one of the baked roots, so files under it are in scope.
            var externalPkg = Path.Combine(Path.GetTempPath(), "pipeline_extpkg_" + Path.GetRandomFileName());
            Directory.CreateDirectory(externalPkg);
            try
            {
                var path = Path.Combine(externalPkg, "Runtime", "Gameplay.cs");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "// test");

                var result = HotReloadCommands.ValidateReloadPath(path, new[] { m_Assets, externalPkg });

                Assert.IsTrue(result.IsValid, result.ErrorDetails);
            }
            finally
            {
                Directory.Delete(externalPkg, recursive: true);
            }
        }

        [Test]
        public void Validate_FileUnderRootButMissing_IsFileNotFound()
        {
            var path = Path.Combine(m_Assets, "Scripts", "Ghost.cs");

            var result = HotReloadCommands.ValidateReloadPath(path, new[] { m_Assets });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("File Not Found", result.Error);
        }

        [Test]
        public void Validate_FileOutsideAllowedRoots_IsOutOfScope()
        {
            var outside = Path.Combine(Path.GetTempPath(), "pipeline_outside_" + Path.GetRandomFileName());
            Directory.CreateDirectory(outside);
            try
            {
                var path = Path.Combine(outside, "Evil.cs");
                File.WriteAllText(path, "// test");

                var result = HotReloadCommands.ValidateReloadPath(path, new[] { m_Assets });

                Assert.IsFalse(result.IsValid);
                Assert.AreEqual("Out Of Project Scope", result.Error);
            }
            finally
            {
                Directory.Delete(outside, recursive: true);
            }
        }

        [Test]
        public void Validate_TraversalEscapeFromRoot_IsOutOfScope()
        {
            var sibling = Path.Combine(m_Root, "Secrets");
            Directory.CreateDirectory(sibling);
            File.WriteAllText(Path.Combine(sibling, "Leak.cs"), "// test");

            var traversal = Path.Combine(m_Assets, "..", "Secrets", "Leak.cs");

            var result = HotReloadCommands.ValidateReloadPath(traversal, new[] { m_Assets });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Out Of Project Scope", result.Error);
        }

        [Test]
        public void Validate_SiblingPrefixFolder_IsOutOfScope()
        {
            // "AssetsExtra" shares a prefix with "Assets" but is not under it.
            var path = CreateFile(Path.Combine("AssetsExtra", "Sneaky.cs"));

            var result = HotReloadCommands.ValidateReloadPath(path, new[] { m_Assets });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Out Of Project Scope", result.Error);
        }

        [Test]
        public void Validate_NullRoots_IsOutOfScope()
        {
            var path = CreateFile(Path.Combine("Assets", "Player.cs"));

            var result = HotReloadCommands.ValidateReloadPath(path, null);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Out Of Project Scope", result.Error);
        }

        [Test]
        public void Validate_EmptyPath_IsBadRequest()
        {
            var result = HotReloadCommands.ValidateReloadPath("", new[] { m_Assets });

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Bad Request", result.Error);
        }
    }
}
