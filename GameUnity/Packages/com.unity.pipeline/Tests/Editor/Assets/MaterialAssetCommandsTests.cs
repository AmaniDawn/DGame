using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for create_asset's Material support (CLI-221), exercised directly and via the isolated
    /// <see cref="PipelineTestServer"/>. Verifies that a .mat is created with a real (non-null) shader,
    /// that an unknown shader name fails clearly (no null Material / null-ref), and that the created
    /// Material is loadable and assignable to a Renderer's material slot.
    /// </summary>
    public class MaterialAssetCommandsTests
    {
        private const string Root = "Assets/__CLI221_222Test";

        // A shader that is guaranteed to exist on whatever pipeline this project runs (built-in or URP).
        private string m_KnownShader;

        [SetUp]
        public void SetUp()
        {
            ProjectPaths.ResetAuthoringRoot();
            m_KnownShader = PickKnownShader();
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

        /// <summary>Pick a shader name that resolves on this project's active pipeline.</summary>
        private static string PickKnownShader()
        {
            if (Shader.Find("Standard") != null)
                return "Standard";
            if (Shader.Find("Universal Render Pipeline/Lit") != null)
                return "Universal Render Pipeline/Lit";
            // Sprites/Default ships with every pipeline as a hard fallback.
            return "Sprites/Default";
        }

        #region Direct

        [Test]
        public void CreateAsset_Material_WithShader_CreatesValidMaterial()
        {
            var path = Root + "/Mats/Bar.mat";
            var result = AssetCommands.CreateAsset(path, "Material", shader: m_KnownShader);

            Assert.AreEqual(path, result.AssetPath);

            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(loaded, "Created .mat should load as a Material");
            Assert.IsNotNull(loaded.shader, "Material's shader must not be null");
            Assert.AreEqual(Shader.Find(m_KnownShader), loaded.shader, "Material should use the requested shader");
        }

        [Test]
        public void CreateAsset_Material_DefaultShader_IsNonNull()
        {
            // No explicit shader: should fall back to the active pipeline's lit shader (or Standard).
            var path = Root + "/Mats/Default.mat";
            AssetCommands.CreateAsset(path, "Material");

            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(loaded, "Created .mat should load as a Material");
            Assert.IsNotNull(loaded.shader, "Default-shader Material must have a non-null shader");
        }

        [Test]
        public void CreateAsset_Material_InvalidShader_ThrowsClearError()
        {
            var path = Root + "/Mats/Broken.mat";

            var ex = Assert.Throws<System.ArgumentException>(
                () => AssetCommands.CreateAsset(path, "Material", shader: "No/Such/Shader__CLI221"),
                "An unknown shader name should fail with a clear ArgumentException");
            StringAssert.Contains("not found", ex.Message);

            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "No asset should be written on a shader failure");
        }

        [Test]
        public void CreateAsset_Material_DryRun_InvalidShader_ThrowsAndWritesNothing()
        {
            // dry_run is documented as "validate inputs", so an unknown shader must fail fast even in
            // dry_run (rather than reporting a would-be success) and still write nothing.
            var path = Root + "/Mats/DryRunBroken.mat";

            var ex = Assert.Throws<System.ArgumentException>(
                () => AssetCommands.CreateAsset(path, "Material", shader: "No/Such/Shader__CLI221", dryRun: true),
                "dry_run should resolve/validate the shader and fail on an unknown one");
            StringAssert.Contains("not found", ex.Message);

            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "dry_run must never write an asset");
        }

        [Test]
        public void CreateAsset_Material_DryRun_ValidShader_WritesNothing()
        {
            // A valid dry_run resolves the shader (no throw) but writes no asset.
            var path = Root + "/Mats/DryRunOk.mat";
            var result = AssetCommands.CreateAsset(path, "Material", shader: m_KnownShader, dryRun: true);

            Assert.AreEqual(path, result.AssetPath);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "dry_run must never write an asset");
        }

        [Test]
        public void CreateAsset_Material_AssignableToRendererSlot()
        {
            var path = Root + "/Mats/Assignable.mat";
            AssetCommands.CreateAsset(path, "Material", shader: m_KnownShader);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            var go = new GameObject("__CLI221_Renderer");
            try
            {
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;

                Assert.AreEqual(mat, renderer.sharedMaterial, "Created Material should be assignable to a Renderer slot");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateAsset_Material_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var path = Root + "/ViaClient/Wall.mat";
                var response = server.Execute("create_asset", new { path, type = "Material", shader = m_KnownShader });

                Assert.IsTrue(response.IsSuccess, $"create_asset (Material) should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");

                var loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(loaded, "Material should exist on disk");
                Assert.IsNotNull(loaded.shader, "Material's shader must not be null");
            }
        }

        [Test]
        public void CreateAsset_Material_ViaClient_InvalidShader_Fails()
        {
            using (var server = new PipelineTestServer())
            {
                // The server logs the failure as a Unity [Error]; expect it so the test does not fail on
                // the unexpected log. Matches the message thrown by ResolveShader ("Shader '...' not found.").
                LogAssert.Expect(LogType.Error, new Regex("not found"));

                var path = Root + "/ViaClient/Broken.mat";
                var response = server.Execute("create_asset", new { path, type = "Material", shader = "No/Such/Shader__CLI221" });

                Assert.IsFalse(response.IsSuccess, "An unknown shader should fail over the wire");
                Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "No asset should be written on a shader failure");
            }
        }

        #endregion
    }
}
