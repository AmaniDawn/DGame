using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor.Authoring
{
    /// <summary>
    /// Tests for the create_folder reference command (CLI-190), exercised directly and via
    /// PipelineClient. Verifies the result envelope, recursive creation, Assets-relative paths,
    /// and the traversal guard.
    /// </summary>
    public class FolderCommandsTests
    {
        private const string Root = "Assets/__CLI190Test";

        [SetUp]
        public void SetUp()
        {
            // Start from the default authoring root so a leftover EditorPref (from another test or an
            // interrupted run) can't shift where bare paths resolve mid-suite.
            ProjectPaths.ResetAuthoringRoot();
        }

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
        public void CreateFolder_ExplicitAssetsPath_CreatesFoldersAndReturnsIdentity()
        {
            var result = FolderCommands.CreateFolder(Root + "/Sub/Leaf");

            Assert.IsTrue(AssetDatabase.IsValidFolder(Root + "/Sub/Leaf"), "Nested folders should exist");
            Assert.AreEqual(Root + "/Sub/Leaf", result.AssetPath);
            Assert.IsNotEmpty(result.Guid, "Created folder should have a GUID");
        }

        [Test]
        public void CreateFolder_BarePath_ResolvesUnderAssets()
        {
            // No "Assets/" prefix — should resolve under the default authoring root (Assets/).
            var result = FolderCommands.CreateFolder("__CLI190Test/Bare");

            Assert.AreEqual("Assets/__CLI190Test/Bare", result.AssetPath);
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/__CLI190Test/Bare"));
        }

        [Test]
        public void CreateFolder_Traversal_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => FolderCommands.CreateFolder("../Outside"));
            Assert.Throws<System.ArgumentException>(() => FolderCommands.CreateFolder("Assets/../Outside"));
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateFolder_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_folder", new { path = Root + "/ViaClient" });

                Assert.IsTrue(response.IsSuccess, $"create_folder should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
                Assert.IsTrue(AssetDatabase.IsValidFolder(Root + "/ViaClient"), "Folder should be created");
            }
        }

        #endregion
    }
}
