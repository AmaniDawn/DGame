using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// A trivial ScriptableObject used by the asset-command tests to exercise create / find / move /
    /// delete with a real, project-resolvable type.
    /// </summary>
    public class CLI191SampleAsset : ScriptableObject
    {
        public int Value;
    }

    /// <summary>
    /// Tests for the CLI-191 asset lifecycle commands, exercised both directly (static method) and via
    /// the isolated <see cref="PipelineTestServer"/>. Covers the acceptance flow (create folder tree →
    /// create ScriptableObject → find by type → move → delete), the sandbox guard (out-of-project
    /// writes rejected), and the destructive guard (delete without confirm rejected).
    /// </summary>
    public class AssetCommandsTests
    {
        private const string Root = "Assets/__CLI191Test";

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

        private static ObjectRef PathRef(string path) => new ObjectRef { Path = path };

        #region Direct

        [Test]
        public void CreateAsset_ScriptableObject_CreatesAssetAndReturnsIdentity()
        {
            var path = Root + "/Data/Sample.asset";
            var result = AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            Assert.AreEqual(path, result.AssetPath);
            Assert.IsNotEmpty(result.Guid, "Created asset should have a GUID");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<CLI191SampleAsset>(path), "Asset should exist on disk");
        }

        [Test]
        public void CreateAsset_OverwriteWithoutConfirm_Throws()
        {
            var path = Root + "/Data/Dup.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            Assert.Throws<System.ArgumentException>(
                () => AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName),
                "Overwriting an existing asset without confirm should be rejected");

            // With confirm it succeeds.
            var result = AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName, confirm: true);
            Assert.AreEqual(path, result.AssetPath);
        }

        [Test]
        public void CreateAsset_OutOfProject_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => AssetCommands.CreateAsset("../Outside.asset", typeof(CLI191SampleAsset).FullName));
            Assert.Throws<System.ArgumentException>(
                () => AssetCommands.CreateAsset("Assets/../Outside.asset", typeof(CLI191SampleAsset).FullName));
        }

        [Test]
        public void FindAssets_ByType_FindsCreatedAsset()
        {
            var path = Root + "/Find/Target.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            var found = AssetCommands.FindAssets(type: nameof(CLI191SampleAsset), searchIn: Root);

            Assert.GreaterOrEqual(found.Count, 1, "Should find the created asset by type");
            CollectionAssert.Contains(
                System.Linq.Enumerable.Select(found.Assets, a => a.AssetPath),
                path);
        }

        [Test]
        public void FindAssets_NoFilter_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => AssetCommands.FindAssets());
        }

        [Test]
        public void MoveAsset_RelocatesAndPreservesGuid()
        {
            var src = Root + "/Move/Src.asset";
            AssetCommands.CreateAsset(src, typeof(CLI191SampleAsset).FullName);
            var srcGuid = AssetDatabase.AssetPathToGUID(src);

            var dest = Root + "/Move/Dest/Moved.asset";
            var result = AssetCommands.MoveAsset(PathRef(src), dest);

            Assert.AreEqual(dest, result.AssetPath);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(src), "Source should no longer exist");
            Assert.AreEqual(srcGuid, AssetDatabase.AssetPathToGUID(dest), "GUID should be preserved across a move");
        }

        [Test]
        public void CopyAsset_CreatesDistinctAsset()
        {
            var src = Root + "/Copy/Src.asset";
            AssetCommands.CreateAsset(src, typeof(CLI191SampleAsset).FullName);

            var dest = Root + "/Copy/Copy.asset";
            var result = AssetCommands.CopyAsset(PathRef(src), dest);

            Assert.AreEqual(dest, result.AssetPath);
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(src), "Source should still exist after copy");
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(src), AssetDatabase.AssetPathToGUID(dest),
                "A copy should get a fresh GUID");
        }

        [Test]
        public void RenameAsset_RenamesInPlace()
        {
            var src = Root + "/Rename/Old.asset";
            AssetCommands.CreateAsset(src, typeof(CLI191SampleAsset).FullName);

            var result = AssetCommands.RenameAsset(PathRef(src), "New");

            Assert.AreEqual(Root + "/Rename/New.asset", result.AssetPath);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(src));
        }

        [Test]
        public void DeleteAsset_WithoutConfirm_Throws()
        {
            var path = Root + "/Delete/Doomed.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            Assert.Throws<System.ArgumentException>(() => AssetCommands.DeleteAsset(PathRef(path)),
                "delete_asset without confirm should be rejected");
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(path), "Asset should survive a refused delete");
        }

        [Test]
        public void DeleteAsset_WithConfirm_Deletes()
        {
            var path = Root + "/Delete/Doomed2.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            var result = AssetCommands.DeleteAsset(PathRef(path), confirm: true);

            Assert.AreEqual(path, result.AssetPath);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "Asset should be gone after a confirmed delete");
        }

        [Test]
        public void AcceptanceFlow_CreateFindMoveDelete()
        {
            // create folder tree
            FolderCommands.CreateFolder(Root + "/Flow/Configs");
            Assert.IsTrue(AssetDatabase.IsValidFolder(Root + "/Flow/Configs"));

            // create ScriptableObject asset
            var path = Root + "/Flow/Configs/Cfg.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            // find by type
            var found = AssetCommands.FindAssets(type: nameof(CLI191SampleAsset), searchIn: Root + "/Flow");
            Assert.GreaterOrEqual(found.Count, 1);

            // move it
            var moved = AssetCommands.MoveAsset(PathRef(path), Root + "/Flow/Moved.asset");
            Assert.AreEqual(Root + "/Flow/Moved.asset", moved.AssetPath);

            // delete it (with confirm)
            AssetCommands.DeleteAsset(PathRef(moved.AssetPath), confirm: true);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(moved.AssetPath));
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateAsset_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var path = Root + "/ViaClient/Made.asset";
                var response = server.Execute("create_asset", new { path, type = typeof(CLI191SampleAsset).FullName });

                Assert.IsTrue(response.IsSuccess, $"create_asset should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<CLI191SampleAsset>(path), "Asset should exist");
            }
        }

        [Test]
        public void DeleteAsset_ViaClient_WithoutConfirm_Fails()
        {
            var path = Root + "/ViaClient/ToDelete.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            using (var server = new PipelineTestServer())
            {
                // The server logs the refusal as a Unity [Error]; expect it so the test does not fail on
                // the unexpected log. Matches the message thrown by DeleteAsset ("Refusing to delete ...").
                LogAssert.Expect(LogType.Error, new Regex("Refusing to delete"));

                var response = server.Execute("delete_asset", new { asset = new { path } });

                Assert.IsFalse(response.IsSuccess, "delete_asset without confirm should fail over the wire");
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(path), "Asset should survive a refused delete");
            }
        }

        [Test]
        public void FindAssets_ViaClient_ReturnsResults()
        {
            var path = Root + "/ViaClient/Findable.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("find_assets", new { type = nameof(CLI191SampleAsset), search_in = Root });

                Assert.IsTrue(response.IsSuccess, $"find_assets should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
