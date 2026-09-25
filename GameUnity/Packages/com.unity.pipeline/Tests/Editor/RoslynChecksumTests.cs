using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Pipeline.Editor.BuildProcessors;
using UnityEditor.Build;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the bundled-Roslyn-DLL integrity check (SHA-256 tamper detection) used by the
    /// build processor. The pure VerifyChecksums(dir, manifest) core is exercised against synthetic
    /// folders; the real package + CHECKSUMS is exercised via VerifyBundledChecksums().
    /// </summary>
    public class RoslynChecksumTests
    {
        private string m_Dir;

        [SetUp]
        public void SetUp()
        {
            m_Dir = Path.Combine(Path.GetTempPath(), "pipeline_checksums_" + Path.GetRandomFileName());
            Directory.CreateDirectory(m_Dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Dir))
                Directory.Delete(m_Dir, recursive: true);
        }

        private string WriteDll(string name, string content)
        {
            var path = Path.Combine(m_Dir, name);
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(content));
            return path;
        }

        private string WriteChecksums(string text)
        {
            var path = Path.Combine(m_Dir, "CHECKSUMS");
            File.WriteAllText(path, text);
            return path;
        }

        [Test]
        public void ComputeSha256_KnownContent_MatchesKnownHash()
        {
            var path = WriteDll("known.bin", "abc");

            // SHA-256("abc") - well-known test vector.
            Assert.AreEqual(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                PipelineRuntimeBuildProcessor.ComputeSha256(path));
        }

        [Test]
        public void VerifyChecksums_MatchingFiles_ReturnsNull()
        {
            WriteDll("a.dll", "hello");
            var hash = PipelineRuntimeBuildProcessor.ComputeSha256(Path.Combine(m_Dir, "a.dll"));
            var manifest = WriteChecksums($"# header\n{hash}  a.dll  # pkg TODO\n");

            Assert.IsNull(PipelineRuntimeBuildProcessor.VerifyChecksums(m_Dir, manifest));
        }

        [Test]
        public void VerifyChecksums_TamperedFile_ReturnsMismatch()
        {
            WriteDll("a.dll", "hello");
            // Manifest claims a hash that does not match the file's real content.
            var manifest = WriteChecksums(
                "0000000000000000000000000000000000000000000000000000000000000000  a.dll\n");

            var error = PipelineRuntimeBuildProcessor.VerifyChecksums(m_Dir, manifest);

            Assert.IsNotNull(error);
            StringAssert.Contains("hash mismatch", error);
        }

        [Test]
        public void VerifyChecksums_UnlistedDll_ReturnsError()
        {
            WriteDll("a.dll", "hello");
            WriteDll("rogue.dll", "evil"); // present on disk but not in the manifest
            var hash = PipelineRuntimeBuildProcessor.ComputeSha256(Path.Combine(m_Dir, "a.dll"));
            var manifest = WriteChecksums($"{hash}  a.dll\n");

            var error = PipelineRuntimeBuildProcessor.VerifyChecksums(m_Dir, manifest);

            Assert.IsNotNull(error);
            StringAssert.Contains("rogue.dll", error);
        }

        [Test]
        public void VerifyChecksums_MissingListedDll_ReturnsError()
        {
            // Manifest lists a DLL that does not exist on disk.
            var manifest = WriteChecksums(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad  ghost.dll\n");

            var error = PipelineRuntimeBuildProcessor.VerifyChecksums(m_Dir, manifest);

            Assert.IsNotNull(error);
            StringAssert.Contains("missing", error);
        }

        [Test]
        public void VerifyBundledChecksums_RealPackage_DoesNotThrow()
        {
            // Happy path against the actual bundled DLLs and committed CHECKSUMS in this package.
            try
            {
                PipelineRuntimeBuildProcessor.VerifyBundledChecksums();
            }
            catch (BuildFailedException ex)
            {
                Assert.Fail($"Bundled Roslyn DLLs failed integrity check: {ex.Message}");
            }
        }
    }
}
