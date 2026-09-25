using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor.Scenes
{
    /// <summary>
    /// Tests for the scene-management commands (CLI-193), exercised directly and via
    /// <see cref="PipelineTestServer"/>. Covers the create -> populate -> save -> reopen round-trip,
    /// build-list add/remove, the hierarchy snapshot, and the play-mode guard.
    ///
    /// All scenes are created under a dedicated temp folder beneath the authoring root and deleted in
    /// teardown, and any build-list entries added by the tests are removed, so the suite leaves no
    /// residue in the host project.
    /// </summary>
    public class SceneCommandsTests
    {
        private const string TestRoot = "Assets/__CLI193Test";
        private const string ScenePath = TestRoot + "/RoundTrip.unity";

        // CLI-226: create_scene --template. Kept under its own root so the template tests are
        // self-contained and cleaned up independently of the CLI-193 round-trip fixtures.
        private const string TemplateTestRoot = "Assets/__CLI226Test";

        [SetUp]
        public void SetUp()
        {
            // ViaClient tests need discovery wired up for the isolated test server.
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();

            // Make sure no test scene is left open (an open, unsaved scene would block deletion).
            EnsureNoTestSceneOpen();

            // Drop any build-list entries that point into either temp folder.
            var scenes = EditorBuildSettings.scenes
                .Where(s => !IsTestScenePath(s.path))
                .ToArray();
            if (scenes.Length != EditorBuildSettings.scenes.Length)
                EditorBuildSettings.scenes = scenes;

            var deletedAny = false;
            foreach (var root in new[] { TestRoot, TemplateTestRoot })
            {
                if (AssetDatabase.IsValidFolder(root))
                {
                    AssetDatabase.DeleteAsset(root);
                    deletedAny = true;
                }
            }
            if (deletedAny)
                AssetDatabase.Refresh();
        }

        private static bool IsTestScenePath(string path) =>
            !string.IsNullOrEmpty(path) &&
            (path.StartsWith(TestRoot, System.StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith(TemplateTestRoot, System.StringComparison.OrdinalIgnoreCase));

        private static void EnsureNoTestSceneOpen()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (IsTestScenePath(scene.path))
                {
                    // Open a fresh empty scene if this is the last one, otherwise just close it.
                    if (SceneManager.sceneCount == 1)
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    else
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        #region Direct

        [Test]
        public void CreateScene_CreatesAssetAndReturnsIdentity()
        {
            var result = SceneCommands.CreateScene(ScenePath);

            Assert.AreEqual(ScenePath, result.AssetPath);
            Assert.IsNotEmpty(result.Guid, "Created scene should have a GUID");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), "Scene asset should exist on disk");
        }

        [Test]
        public void CreateScene_AppendsUnityExtension_WhenOmitted()
        {
            var result = SceneCommands.CreateScene(TestRoot + "/NoExt");
            Assert.AreEqual(TestRoot + "/NoExt.unity", result.AssetPath);
        }

        [Test]
        public void CreatePopulateSaveReopen_RoundTrip_PreservesContent()
        {
            // Create (single mode replaces open scenes, leaving our scene active).
            SceneCommands.CreateScene(ScenePath);
            var scene = EditorSceneManager.GetActiveScene();
            Assert.AreEqual(ScenePath, scene.path, "New scene should be the active scene");

            // Populate: add a GameObject with a component.
            var go = new GameObject("Marker");
            go.AddComponent<BoxCollider>();
            SceneManager.MoveGameObjectToScene(go, scene);
            EditorSceneManager.MarkSceneDirty(scene);

            // Save.
            SceneCommands.SaveScene(ScenePath);
            Assert.IsFalse(EditorSceneManager.GetActiveScene().isDirty, "Scene should be clean after save");

            // Reopen from a clean slate (replace with empty, then open our saved scene).
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneCommands.OpenScene(ScenePath);

            var reopened = EditorSceneManager.GetActiveScene();
            Assert.AreEqual(ScenePath, reopened.path);
            var marker = reopened.GetRootGameObjects().FirstOrDefault(g => g.name == "Marker");
            Assert.IsNotNull(marker, "Saved GameObject should survive the round-trip");
            Assert.IsNotNull(marker.GetComponent<BoxCollider>(), "Saved component should survive the round-trip");
        }

        [Test]
        public void GetSceneHierarchy_ReturnsTreeUsableByObjectResolver()
        {
            SceneCommands.CreateScene(ScenePath);
            var scene = EditorSceneManager.GetActiveScene();

            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            SceneManager.MoveGameObjectToScene(parent, scene);

            var hierarchy = SceneCommands.GetSceneHierarchy(ScenePath);

            Assert.IsTrue(hierarchy.IsActive, "Snapshotted scene should be flagged active");
            var parentNode = hierarchy.Roots.FirstOrDefault(n => n.Name == "Parent");
            Assert.IsNotNull(parentNode, "Parent should appear as a root node");
            Assert.Contains("Transform", parentNode.Components, "Every GameObject has a Transform");
            Assert.AreEqual(1, parentNode.Children.Count, "Parent should have one child");

            var childNode = parentNode.Children[0];
            Assert.AreEqual("Child", childNode.Name);
            Assert.AreEqual("/Parent/Child", childNode.HierarchyPath);

            // The node identity must be usable as an ObjectRef handle for GameObject commands.
            var byInstance = new Unity.Pipeline.Models.ObjectRef { InstanceId = childNode.InstanceId };
            Assert.IsTrue(ObjectResolver.TryResolve(byInstance, out var resolvedById, out _),
                "instanceId from the tree should resolve");
            Assert.AreEqual(child, resolvedById);

            var byPath = new Unity.Pipeline.Models.ObjectRef { HierarchyPath = childNode.HierarchyPath };
            Assert.IsTrue(ObjectResolver.TryResolve(byPath, out var resolvedByPath, out _),
                "hierarchyPath from the tree should resolve");
            Assert.AreEqual(child, resolvedByPath);
        }

        [Test]
        public void ListOpenScenes_IncludesTheActiveScene()
        {
            SceneCommands.CreateScene(ScenePath);

            var json = Newtonsoft.Json.Linq.JObject.FromObject(SceneCommands.ListOpenScenes());
            var scenes = (Newtonsoft.Json.Linq.JArray)json["scenes"];
            var active = scenes.FirstOrDefault(s => (bool)s["isActive"]);

            Assert.IsNotNull(active, "There should be an active scene");
            Assert.AreEqual(ScenePath, (string)active["path"]);
        }

        [Test]
        public void AddAndRemoveSceneFromBuild_UpdatesBuildList()
        {
            SceneCommands.CreateScene(ScenePath);

            // Add.
            SceneCommands.AddSceneToBuild(ScenePath);
            Assert.IsTrue(
                EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled),
                "Scene should be present and enabled in the build list");

            // Idempotent add reconciling the enabled flag.
            SceneCommands.AddSceneToBuild(ScenePath, enabled: false);
            var entries = EditorBuildSettings.scenes.Where(s => s.path == ScenePath).ToArray();
            Assert.AreEqual(1, entries.Length, "Adding twice must not duplicate the entry");
            Assert.IsFalse(entries[0].enabled, "Re-add should reconcile the enabled flag");

            // Remove.
            SceneCommands.RemoveSceneFromBuild(ScenePath);
            Assert.IsFalse(
                EditorBuildSettings.scenes.Any(s => s.path == ScenePath),
                "Scene should be gone from the build list");
        }

        [Test]
        public void SetActiveScene_UnopenedScene_ThrowsRecoverable()
        {
            SceneCommands.CreateScene(ScenePath);
            // A path that isn't open should produce a clear, recoverable error.
            Assert.Throws<System.InvalidOperationException>(
                () => SceneCommands.SetActiveScene(TestRoot + "/NotOpen.unity"));
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateAndAddToBuild_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var create = server.Execute("create_scene", new { path = ScenePath });
                Assert.IsTrue(create.IsSuccess, $"create_scene should succeed: {create.Error}");
                Assert.IsTrue(create.HasValidJson, "create_scene response should be valid JSON");
                Assert.IsTrue(create.JsonResponse.ContainsKey("result"), "Should have a result field");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), "Scene asset should exist");

                var add = server.Execute("add_scene_to_build", new { path = ScenePath });
                Assert.IsTrue(add.IsSuccess, $"add_scene_to_build should succeed: {add.Error}");
                Assert.IsTrue(
                    EditorBuildSettings.scenes.Any(s => s.path == ScenePath),
                    "Scene should be in the build list after the ViaClient add");
            }
        }

        [Test]
        public void GetSceneHierarchy_ViaClient_ReturnsTree()
        {
            SceneCommands.CreateScene(ScenePath);
            var go = new GameObject("ViaClientRoot");
            SceneManager.MoveGameObjectToScene(go, EditorSceneManager.GetActiveScene());

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("get_scene_hierarchy", new { path = ScenePath });

                Assert.IsTrue(response.IsSuccess, $"get_scene_hierarchy should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should be valid JSON");
                var roots = (Newtonsoft.Json.Linq.JArray)response.JsonResponse["result"]["roots"];
                Assert.IsTrue(
                    roots.Any(n => (string)n["name"] == "ViaClientRoot"),
                    "Tree should include the created root GameObject");
            }
        }

        #endregion

        #region Template (CLI-226)

        // Unity's built-in "3D" template positions. Used to assert --template default reproduces it.
        private static readonly Vector3 MainCameraPosition = new Vector3(0f, 1f, -10f);
        private static readonly Vector3 DirectionalLightEuler = new Vector3(50f, -30f, 0f);

        [Test]
        public void CreateScene_TemplateDefault_SeedsCameraAndLight()
        {
            var scenePath = TemplateTestRoot + "/Default.unity";
            SceneCommands.CreateScene(scenePath, template: "default");

            var scene = EditorSceneManager.GetActiveScene();
            Assert.AreEqual(scenePath, scene.path, "New scene should be the active scene");

            AssertDefaultTemplateContents(scene);
        }

        [Test]
        public void CreateScene_TemplateDefault_SurvivesReopen()
        {
            // The seeded objects must be saved into the .unity asset, not just live in the open scene.
            var scenePath = TemplateTestRoot + "/DefaultReopen.unity";
            SceneCommands.CreateScene(scenePath, template: "default");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneCommands.OpenScene(scenePath);

            AssertDefaultTemplateContents(EditorSceneManager.GetActiveScene());
        }

        [Test]
        public void CreateScene_TemplateEmpty_SeedsNothing()
        {
            var scenePath = TemplateTestRoot + "/Empty.unity";
            SceneCommands.CreateScene(scenePath, template: "empty");

            AssertEmptyTemplateContents(EditorSceneManager.GetActiveScene());
        }

        [Test]
        public void CreateScene_TemplateOmitted_DefaultsToEmpty_NoRegression()
        {
            // Regression guard: the historical behavior (no template arg) must remain a blank scene.
            var scenePath = TemplateTestRoot + "/Omitted.unity";
            SceneCommands.CreateScene(scenePath);

            AssertEmptyTemplateContents(EditorSceneManager.GetActiveScene());
        }

        [Test]
        public void CreateScene_UnknownTemplate_ThrowsArgumentException()
        {
            var scenePath = TemplateTestRoot + "/Bogus.unity";
            var ex = Assert.Throws<System.ArgumentException>(
                () => SceneCommands.CreateScene(scenePath, template: "isometric"));

            // The error must enumerate the valid values so the caller can recover.
            StringAssert.Contains("empty", ex.Message);
            StringAssert.Contains("default", ex.Message);

            // Nothing should have been written when validation rejected the value.
            Assert.IsNull(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath),
                "No scene asset should be created when the template is invalid");
        }

        [Test]
        public void CreateScene_TemplateDefault_ViaClient_SeedsCameraAndLight()
        {
            var scenePath = TemplateTestRoot + "/DefaultClient.unity";
            using (var server = new PipelineTestServer())
            {
                var create = server.Execute("create_scene", new { path = scenePath, template = "default" });
                Assert.IsTrue(create.IsSuccess, $"create_scene should succeed: {create.Error}");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), "Scene asset should exist");
            }

            // Inspect the freshly-created (and now active) scene's contents.
            AssertDefaultTemplateContents(EditorSceneManager.GetActiveScene());
        }

        [Test]
        public void CreateScene_TemplateEmpty_ViaClient_SeedsNothing()
        {
            var scenePath = TemplateTestRoot + "/EmptyClient.unity";
            using (var server = new PipelineTestServer())
            {
                var create = server.Execute("create_scene", new { path = scenePath, template = "empty" });
                Assert.IsTrue(create.IsSuccess, $"create_scene should succeed: {create.Error}");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), "Scene asset should exist");
            }

            AssertEmptyTemplateContents(EditorSceneManager.GetActiveScene());
        }

        [Test]
        public void CreateScene_UnknownTemplate_ViaClient_Fails()
        {
            var scenePath = TemplateTestRoot + "/BogusClient.unity";
            using (var server = new PipelineTestServer())
            {
                // The server surfaces the command failure via Debug.LogError; expect it so the test
                // runner's unhandled-log check doesn't fail the test on the (expected) error message.
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                    new System.Text.RegularExpressions.Regex("Unknown template"));

                var create = server.Execute("create_scene", new { path = scenePath, template = "isometric" });

                Assert.IsFalse(create.IsSuccess, "create_scene with an unknown template should fail");
                Assert.IsNull(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath),
                    "No scene asset should be created when the template is invalid");
            }
        }

        /// <summary>Assert a scene matches Unity's built-in 3D template: a tagged Main Camera + a Directional Light.</summary>
        private static void AssertDefaultTemplateContents(Scene scene)
        {
            var roots = scene.GetRootGameObjects();

            var camera = roots.Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            Assert.IsNotNull(camera, "Default template should seed a Camera");
            Assert.AreEqual("MainCamera", camera.gameObject.tag, "Seeded camera should be tagged MainCamera");
            AssertApproximately(MainCameraPosition, camera.transform.position, "Main Camera position");
            Assert.AreEqual(Quaternion.identity, camera.transform.rotation, "Main Camera rotation should be identity");

            var light = roots.Select(g => g.GetComponent<Light>()).FirstOrDefault(l => l != null);
            Assert.IsNotNull(light, "Default template should seed a Light");
            Assert.AreEqual(LightType.Directional, light.type, "Seeded light should be Directional");
            Assert.AreEqual(1f, light.intensity, 0.001f, "Directional Light intensity should be 1");

            // Compare rotations as quaternions: eulerAngles round-trips to a different-but-equivalent
            // decomposition (normalized to [0,360)), so a component-wise euler compare is fragile.
            var expectedLightRotation = Quaternion.Euler(DirectionalLightEuler);
            Assert.LessOrEqual(
                Quaternion.Angle(expectedLightRotation, light.transform.rotation), 0.1f,
                "Directional Light rotation should match Unity's 3D template (euler 50,-30,0)");
        }

        /// <summary>Assert a scene was seeded with no default camera or light (the empty-template contract).</summary>
        private static void AssertEmptyTemplateContents(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            Assert.AreEqual(0, roots.Length, "Empty template should produce a scene with no root GameObjects");
            Assert.IsFalse(roots.Any(g => g.GetComponent<Camera>() != null), "Empty template should not seed a Camera");
            Assert.IsFalse(roots.Any(g => g.GetComponent<Light>() != null), "Empty template should not seed a Light");
        }

        private static void AssertApproximately(Vector3 expected, Vector3 actual, string label, float tolerance = 0.001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, $"{label} X");
            Assert.AreEqual(expected.y, actual.y, tolerance, $"{label} Y");
            Assert.AreEqual(expected.z, actual.z, tolerance, $"{label} Z");
        }

        #endregion

        #region Play-mode guard

        /// <summary>
        /// Verifies a mutating scene op refuses in play mode with a clear, recoverable error and does
        /// not corrupt scene state. Explicit + ServerLifecycle because entering play mode triggers a
        /// domain reload that tears down the live editor server; run it deliberately from the Test
        /// Runner window, like the play-mode command tests.
        /// </summary>
        [UnityTest]
        [Explicit("Enters play mode (domain reload tears down the live server). Run manually from the Test Runner window.")]
        [Category("ServerLifecycle")]
        public IEnumerator MutatingCommand_InPlayMode_FailsRecoverably()
        {
            EditorApplication.isPlaying = true;
            yield return null;
            while (!EditorApplication.isPlaying)
                yield return null;

            var threw = false;
            try
            {
                SceneCommands.CreateScene(ScenePath);
            }
            catch (System.InvalidOperationException)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "create_scene should refuse with InvalidOperationException in play mode");
            Assert.IsNull(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                "No scene asset should be created when the guard rejects the op");

            EditorApplication.isPlaying = false;
            yield return null;
            while (EditorApplication.isPlaying)
                yield return null;
        }

        #endregion
    }
}
