using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for create_asset's PhysicsMaterial support (CLI-222), exercised directly and via the
    /// isolated <see cref="PipelineTestServer"/>. Verifies that BOTH the Unity 6 type name
    /// "PhysicsMaterial" and the legacy "PhysicMaterial" resolve to <see cref="PhysicsMaterial"/>, that
    /// the created asset carries the documented defaults (bounciness 0, frictions 0.6), is discoverable
    /// via find_assets, and is assignable to a Collider's material slot.
    ///
    /// NOTE: Unity 6 renamed the class to PhysicsMaterial but the on-disk asset extension is still
    /// ".physicMaterial" (AssetDatabase.CreateAsset rejects ".physicsMaterial"). create_asset normalizes
    /// a ".physicsMaterial" request to ".physicMaterial", so the created path is asserted via the
    /// command result rather than the requested path.
    /// </summary>
    public class PhysicsMaterialAssetCommandsTests
    {
        private const string Root = "Assets/__CLI221_222Test";

        [SetUp]
        public void SetUp()
        {
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

        private static void AssertDefaults(PhysicsMaterial pm)
        {
            Assert.IsNotNull(pm, "Created physics material should load as a PhysicsMaterial");
            Assert.AreEqual(0f, pm.bounciness, 1e-4f, "bounciness default should be 0");
            Assert.AreEqual(0.6f, pm.dynamicFriction, 1e-4f, "dynamicFriction default should be 0.6");
            Assert.AreEqual(0.6f, pm.staticFriction, 1e-4f, "staticFriction default should be 0.6");
        }

        #region Direct

        [Test]
        public void CreateAsset_PhysicsMaterial_CreatesWithDefaults()
        {
            var result = AssetCommands.CreateAsset(Root + "/Phys/Ball.physicMaterial", "PhysicsMaterial");

            StringAssert.EndsWith(".physicMaterial", result.AssetPath);
            AssertDefaults(AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(result.AssetPath));
        }

        [Test]
        public void CreateAsset_PhysicsMaterial_NormalizesModernExtension()
        {
            // CLI-222: an agent may pass the modern ".physicsMaterial" extension; it must succeed,
            // normalized to the ".physicMaterial" file Unity actually writes.
            var result = AssetCommands.CreateAsset(Root + "/Phys/Modern.physicsMaterial", "PhysicsMaterial");

            StringAssert.EndsWith(".physicMaterial", result.AssetPath);
            AssertDefaults(AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(result.AssetPath));
        }

        [Test]
        public void CreateAsset_PhysicsMaterial_LegacyTypeName_ResolvesToSameType()
        {
            // CLI-222: the legacy "PhysicMaterial" spelling must resolve to UnityEngine.PhysicsMaterial.
            var result = AssetCommands.CreateAsset(Root + "/Phys/Legacy.physicMaterial", "PhysicMaterial");

            var loaded = AssetDatabase.LoadMainAssetAtPath(result.AssetPath);
            Assert.IsInstanceOf<PhysicsMaterial>(loaded, "Legacy 'PhysicMaterial' type name should resolve to PhysicsMaterial");
            AssertDefaults(loaded as PhysicsMaterial);
        }

        [Test]
        public void CreateAsset_PhysicsMaterial_FoundByFindAssets()
        {
            var result = AssetCommands.CreateAsset(Root + "/Phys/Findable.physicMaterial", "PhysicsMaterial");

            var found = AssetCommands.FindAssets(type: "PhysicsMaterial", searchIn: Root);

            Assert.GreaterOrEqual(found.Count, 1, "Should find the created PhysicsMaterial by type");
            CollectionAssert.Contains(
                System.Linq.Enumerable.Select(found.Assets, a => a.AssetPath),
                result.AssetPath);
        }

        [Test]
        public void CreateAsset_PhysicsMaterial_AssignableToColliderSlot()
        {
            var result = AssetCommands.CreateAsset(Root + "/Phys/Assignable.physicMaterial", "PhysicsMaterial");
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(result.AssetPath);

            var go = new GameObject("__CLI222_Collider");
            try
            {
                var collider = go.AddComponent<BoxCollider>();
                collider.sharedMaterial = pm;

                Assert.AreEqual(pm, collider.sharedMaterial, "Created PhysicsMaterial should be assignable to a Collider slot");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateAsset_PhysicsMaterial_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_asset",
                    new { path = Root + "/ViaClient/Ball.physicMaterial", type = "PhysicsMaterial" });

                Assert.IsTrue(response.IsSuccess, $"create_asset (PhysicsMaterial) should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");

                var created = (string)response.JsonResponse["result"]["assetPath"];
                AssertDefaults(AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(created));
            }
        }

        [Test]
        public void CreateAsset_PhysicsMaterial_ViaClient_LegacyName_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_asset",
                    new { path = Root + "/ViaClient/Legacy.physicMaterial", type = "PhysicMaterial" });

                Assert.IsTrue(response.IsSuccess, $"create_asset (legacy PhysicMaterial) should succeed: {response.Error}");

                var created = (string)response.JsonResponse["result"]["assetPath"];
                Assert.IsInstanceOf<PhysicsMaterial>(AssetDatabase.LoadMainAssetAtPath(created),
                    "Legacy 'PhysicMaterial' should resolve to PhysicsMaterial over the wire");
            }
        }

        #endregion
    }
}
