using System.IO;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Prefabs;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.Prefabs
{
    /// <summary>
    /// Tests for the prefab authoring commands (CLI-194), exercised directly and via
    /// <see cref="PipelineTestServer"/>. Coverage:
    /// - a create -> instantiate -> variant -> override apply/revert round-trip, and
    /// - a nested-prefab edit integrity check through <c>save_prefab_contents</c> (the prefab stage).
    ///
    /// Source GameObjects are created directly with UnityEditor/UnityEngine APIs (CLI-192's GameObject
    /// commands are not available here). Prefabs are written under a temp folder beneath the authoring
    /// root and cleaned up in teardown along with any scene objects created.
    /// </summary>
    public class PrefabCommandsTests
    {
        private const string Root = "Assets/__CLI194Test";

        // Resolve a project-relative asset path to an absolute disk path. File.Exists with a bare
        // "Assets/..." path only works when the process CWD is the project root, which isn't guaranteed
        // across Unity test runners/CI; ProjectPaths.ProjectRoot makes the check environment-independent.
        private static string AbsPath(string assetPath) => Path.Combine(ProjectPaths.ProjectRoot, assetPath);

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();

            // Remove any scene objects created by the tests so the editor scene is left clean.
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name.StartsWith("CLI194_"))
                        Object.DestroyImmediate(go);
                }
            }

            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateSourceGameObject(string name)
        {
            var go = new GameObject("CLI194_" + name);
            // Give it a configured component so override behaviour is observable.
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = false;
            go.transform.localScale = Vector3.one;
            return go;
        }

        #region Direct round-trip

        [Test]
        public void RoundTrip_Create_Instantiate_Variant_ApplyRevert()
        {
            // 1. Create a prefab from a configured source GameObject.
            var source = CreateSourceGameObject("Base");
            var baseResult = PrefabCommands.CreatePrefab(
                new ObjectRef { InstanceId = PipelineUtils.GetObjectId(source) },
                Root + "/Base");

            var basePath = Root + "/Base.prefab";
            Assert.AreEqual(basePath, baseResult.AssetPath, "Create should report the .prefab path");
            Assert.IsNotEmpty(baseResult.Guid, "Created prefab should have a GUID");
            Assert.IsTrue(File.Exists(AbsPath(basePath)), "Prefab asset file should exist on disk");
            // Source should now be a connected instance of the new prefab.
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(source), "Source should become a connected prefab instance");

            // 2. Instantiate the prefab into the active scene.
            var instResult = PrefabCommands.InstantiatePrefab(
                new ObjectRef { Path = basePath },
                name: "CLI194_Instance");
            Assert.IsTrue(instResult.InstanceId.HasValue, "Instantiate should return a scene instanceId");
            var instance = (GameObject)PipelineUtils.IdToObject(instResult.InstanceId.Value);
            Assert.IsNotNull(instance, "Instance should resolve");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance), "Instantiated object should be a prefab instance");

            // 3. Create a variant of the base prefab.
            var variantResult = PrefabCommands.CreatePrefabVariant(
                new ObjectRef { Path = basePath },
                Root + "/BaseVariant");
            var variantPath = Root + "/BaseVariant.prefab";
            Assert.AreEqual(variantPath, variantResult.AssetPath);
            var variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.IsNotNull(variantAsset, "Variant asset should load");
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variantAsset),
                "Saved asset should be a prefab variant");

            // 4. Override a property on the instance, apply, and confirm it propagates to the base asset.
            var collider = instance.GetComponent<BoxCollider>();
            Assert.IsNotNull(collider);
            collider.isTrigger = true; // an instance override
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);

            PrefabCommands.ApplyPrefabOverrides(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(instance) });

            var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            Assert.IsTrue(baseAsset.GetComponent<BoxCollider>().isTrigger,
                "Applied override should be written to the base prefab asset");

            // 5. Override again then revert, and confirm the instance matches the asset (no overrides).
            collider.isTrigger = false; // new local override
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            Assert.IsTrue(PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false),
                "Instance should have a pending override before revert");

            PrefabCommands.RevertPrefabOverrides(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(instance) });

            Assert.IsTrue(instance.GetComponent<BoxCollider>().isTrigger,
                "After revert the instance should match the asset (isTrigger=true)");
            Assert.IsFalse(PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false),
                "Revert should clear instance overrides");
        }

        [Test]
        public void UnpackPrefab_OutermostRoot_RemovesInstanceConnection()
        {
            var source = CreateSourceGameObject("Unpackable");
            PrefabCommands.CreatePrefab(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(source) }, Root + "/Unpackable");

            var instResult = PrefabCommands.InstantiatePrefab(
                new ObjectRef { Path = Root + "/Unpackable.prefab" }, name: "CLI194_Unpacked");
            var instance = (GameObject)PipelineUtils.IdToObject(instResult.InstanceId.Value);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance));

            PrefabCommands.UnpackPrefab(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(instance) }, completely: true);

            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(instance),
                "After unpack the object should no longer be a prefab instance");
        }

        #endregion

        #region Nested-prefab integrity

        [Test]
        public void SavePrefabContents_NestedPrefabEdit_PreservesNestedInstanceLink()
        {
            // Build an inner prefab and an outer prefab that nests an instance of the inner one.
            var inner = CreateSourceGameObject("Inner");
            PrefabCommands.CreatePrefab(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(inner) }, Root + "/Inner");
            var innerPath = Root + "/Inner.prefab";
            var innerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(innerPath);

            var outer = new GameObject("CLI194_Outer");
            var nested = (GameObject)PrefabUtility.InstantiatePrefab(innerAsset);
            nested.transform.SetParent(outer.transform);
            nested.name = "NestedChild";
            PrefabCommands.CreatePrefab(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(outer) }, Root + "/Outer");
            var outerPath = Root + "/Outer.prefab";

            // Sanity: the saved outer prefab nests a real instance of the inner prefab.
            using (var check = new PrefabStageScope(outerPath))
            {
                var nestedChild = check.Root.transform.Find("NestedChild");
                Assert.IsNotNull(nestedChild, "Outer prefab should contain the nested child");
                Assert.AreEqual(innerPath,
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(nestedChild.gameObject),
                    "Nested child should be an instance of the inner prefab before editing");
            }

            // Edit the outer prefab through the stage (rename the nested child).
            var editResult = PrefabCommands.SavePrefabContents(
                new ObjectRef { Path = outerPath },
                renameChild: "NestedChild",
                newName: "RenamedNested");
            Assert.AreEqual(outerPath, editResult.AssetPath);

            // Integrity check: the rename applied AND the nested-prefab link is intact (not flattened).
            using (var verify = new PrefabStageScope(outerPath))
            {
                var renamed = verify.Root.transform.Find("RenamedNested");
                Assert.IsNotNull(renamed, "Renamed nested child should exist after save_prefab_contents");
                Assert.AreEqual(innerPath,
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renamed.gameObject),
                    "Nested-prefab link must be preserved through the edit (not flattened)");
            }
        }

        /// <summary>Loads prefab contents for read-only verification and unloads on dispose.</summary>
        private sealed class PrefabStageScope : System.IDisposable
        {
            public GameObject Root { get; }

            public PrefabStageScope(string assetPath)
            {
                Root = PrefabUtility.LoadPrefabContents(assetPath);
            }

            public void Dispose()
            {
                if (Root != null)
                    PrefabUtility.UnloadPrefabContents(Root);
            }
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreatePrefab_ViaClient_Succeeds()
        {
            var source = CreateSourceGameObject("ClientSource");

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_prefab", new
                {
                    source = new { instanceId = PipelineUtils.GetObjectId(source) },
                    path = Root + "/ViaClient"
                });

                Assert.IsTrue(response.IsSuccess, $"create_prefab should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");

                var assetPath = response.JsonResponse["result"]?["assetPath"]?.ToString();
                Assert.AreEqual(Root + "/ViaClient.prefab", assetPath, "Result should report the prefab path");
                Assert.IsTrue(File.Exists(AbsPath(Root + "/ViaClient.prefab")), "Prefab asset should be created on disk");
            }
        }

        [Test]
        public void InstantiatePrefab_ViaClient_Succeeds()
        {
            var source = CreateSourceGameObject("ClientInstSource");
            PrefabCommands.CreatePrefab(new ObjectRef { InstanceId = PipelineUtils.GetObjectId(source) }, Root + "/ClientInst");
            var prefabPath = Root + "/ClientInst.prefab";

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("instantiate_prefab", new
                {
                    prefab = new { path = prefabPath },
                    name = "CLI194_ClientInstance"
                });

                Assert.IsTrue(response.IsSuccess, $"instantiate_prefab should succeed: {response.Error}");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");

                var instanceId = response.JsonResponse["result"]?["instanceId"]?.ToObject<ObjectId?>();
                Assert.IsTrue(instanceId.HasValue, "Result should include a scene instanceId");
                var instance = (GameObject)PipelineUtils.IdToObject(instanceId.Value);
                Assert.IsNotNull(instance, "Instantiated object should resolve");
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance), "Created object should be a prefab instance");
            }
        }

        #endregion
    }
}
