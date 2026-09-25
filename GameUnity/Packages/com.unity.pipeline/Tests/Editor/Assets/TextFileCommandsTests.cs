using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for read_text_file / write_text_file (CLI-191), direct and via the isolated
    /// <see cref="PipelineTestServer"/>. Covers round-tripping content, the overwrite-confirm guard, and
    /// the sandbox guard (out-of-project writes rejected).
    /// </summary>
    public class TextFileCommandsTests
    {
        private const string Root = "Assets/__CLI191TextTest";

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
                AssetDatabase.Refresh();
            }
        }

        #region Direct

        [Test]
        public void WriteThenRead_RoundTrips()
        {
            var path = Root + "/Notes/hello.txt";
            const string content = "hello cli-191";

            var write = TextFileCommands.WriteTextFile(path, content);
            Assert.AreEqual(path, write.AssetPath);

            var read = TextFileCommands.ReadTextFile(path);
            Assert.AreEqual(content, read.Contents);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetByteCount(content), read.Bytes);
        }

        [Test]
        public void WriteTextFile_OverwriteWithoutConfirm_Throws()
        {
            var path = Root + "/Notes/dup.txt";
            TextFileCommands.WriteTextFile(path, "first");

            Assert.Throws<System.ArgumentException>(
                () => TextFileCommands.WriteTextFile(path, "second"),
                "Overwriting an existing file without confirm should be rejected");

            var ok = TextFileCommands.WriteTextFile(path, "second", confirm: true);
            Assert.AreEqual(path, ok.AssetPath);
            Assert.AreEqual("second", TextFileCommands.ReadTextFile(path).Contents);
        }

        [Test]
        public void WriteTextFile_OutOfProject_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.WriteTextFile("../escape.txt", "x"));
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.WriteTextFile("Assets/../escape.txt", "x"));
        }

        [Test]
        public void ReadTextFile_Missing_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.ReadTextFile(Root + "/does-not-exist.txt"));
        }

        #endregion

        #region ViaClient

        [Test]
        public void WriteTextFile_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var path = Root + "/ViaClient/wire.txt";
                var response = server.Execute("write_text_file", new { path, contents = "over the wire" });

                Assert.IsTrue(response.IsSuccess, $"write_text_file should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.AreEqual("over the wire", TextFileCommands.ReadTextFile(path).Contents);
            }
        }

        [Test]
        public void ReadTextFile_ViaClient_Succeeds()
        {
            var path = Root + "/ViaClient/readable.txt";
            TextFileCommands.WriteTextFile(path, "readable content");

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("read_text_file", new { path });

                Assert.IsTrue(response.IsSuccess, $"read_text_file should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
