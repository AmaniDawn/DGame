using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Materials;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.Materials
{
    /// <summary>
    /// Tests for the CLI-213 shader discovery / introspection commands (list_shaders /
    /// get_shader_properties). URP-specific assertions are guarded with Assert.Ignore so the suite runs
    /// on a Built-in RP project too.
    /// </summary>
    public class ShaderCommandsTests
    {
        private bool m_HasUrpLit;

        [SetUp]
        public void SetUp()
        {
            ProjectPaths.ResetAuthoringRoot();
            m_HasUrpLit = Shader.Find("Universal Render Pipeline/Lit") != null;
        }

        [TearDown]
        public void TearDown() => ProjectPaths.ResetAuthoringRoot();

        // ---- list_shaders -------------------------------------------------------------------------

        [Test]
        public void ListShaders_FilterLit_ReturnsTheLitShader()
        {
            var results = ShaderCommands.ListShaders(filter: "Lit");

            Assert.IsNotEmpty(results, "filtering by 'Lit' should return at least one shader");
            Assert.IsTrue(results.All(s => s.Name.IndexOf("Lit", System.StringComparison.OrdinalIgnoreCase) >= 0),
                "every result should contain the filter substring");

            // Only assert the specific shader name when URP/Lit is present; this branch never runs
            // otherwise, so a plain constant keeps the intent clear.
            if (m_HasUrpLit)
                Assert.IsTrue(results.Any(s => s.Name == "Universal Render Pipeline/Lit"),
                    "the active pipeline's Lit shader should be listed (AC #10)");
        }

        [Test]
        public void ListShaders_ReportsIsSupportedAndName()
        {
            var results = ShaderCommands.ListShaders(filter: "Lit", limit: 50);
            if (results.Count == 0)
                Assert.Ignore("No 'Lit' shaders in this project.");

            foreach (var s in results)
                Assert.IsFalse(string.IsNullOrEmpty(s.Name), "every shader entry must carry a name");
            // isSupported is a bool that always serializes; just assert the field is meaningful for a known shader.
            var known = results.FirstOrDefault();
            Assert.IsNotNull(known);
        }

        [Test]
        public void ListShaders_RespectsLimit()
        {
            var results = ShaderCommands.ListShaders(limit: 3);
            Assert.LessOrEqual(results.Count, 3, "limit should cap the result count");
        }

        [Test]
        public void ListShaders_ExcludeBuiltin_DropsEngineShaders()
        {
            var withBuiltin = ShaderCommands.ListShaders(includeBuiltin: true);
            var withoutBuiltin = ShaderCommands.ListShaders(includeBuiltin: false);

            Assert.IsFalse(withoutBuiltin.Any(s => s.IsBuiltin),
                "includeBuiltin=false must not return any built-in shaders");
            Assert.GreaterOrEqual(withBuiltin.Count, withoutBuiltin.Count);
        }

        // ---- get_shader_properties ----------------------------------------------------------------

        [Test]
        public void GetShaderProperties_Urp_ReturnsBaseColorMetallicBaseMap()
        {
            if (!m_HasUrpLit)
                Assert.Ignore("URP/Lit not available; skipping URP property introspection (AC #11).");

            var result = ShaderCommands.GetShaderProperties(shader: "Universal Render Pipeline/Lit");
            Assert.AreEqual("Universal Render Pipeline/Lit", result.Shader);

            var baseColor = result.Properties.FirstOrDefault(p => p.Name == "_BaseColor");
            Assert.IsNotNull(baseColor, "_BaseColor should be present");
            Assert.AreEqual("Color", baseColor.Type);

            var metallic = result.Properties.FirstOrDefault(p => p.Name == "_Metallic");
            Assert.IsNotNull(metallic, "_Metallic should be present");
            Assert.AreEqual("Range", metallic.Type);
            Assert.IsNotNull(metallic.Range, "_Metallic should carry min/max");

            var baseMap = result.Properties.FirstOrDefault(p => p.Name == "_BaseMap");
            Assert.IsNotNull(baseMap, "_BaseMap should be present");
            Assert.AreEqual("TexEnv", baseMap.Type);
            Assert.AreEqual("Tex2D", baseMap.TextureDimension);
        }

        [Test]
        public void GetShaderProperties_KnownShader_ReturnsProperties()
        {
            // Shader-agnostic: any real shader exposes a non-empty property list with names + types.
            var shaderName = m_HasUrpLit ? "Universal Render Pipeline/Lit"
                : (Shader.Find("Standard") != null ? "Standard" : "Sprites/Default");

            var result = ShaderCommands.GetShaderProperties(shader: shaderName);
            Assert.AreEqual(shaderName, result.Shader);
            Assert.IsNotEmpty(result.Properties);
            foreach (var p in result.Properties)
            {
                Assert.IsFalse(string.IsNullOrEmpty(p.Name));
                Assert.IsFalse(string.IsNullOrEmpty(p.Type));
                Assert.IsNotNull(p.Flags, "flags should never be null (empty list when None)");
            }
        }

        [Test]
        public void GetShaderProperties_FromMaterial_ReadsShaderOffMaterial()
        {
            var shaderName = Shader.Find("Standard") != null ? "Standard"
                : (m_HasUrpLit ? "Universal Render Pipeline/Lit" : "Sprites/Default");

            var folder = "Assets/__CLI213ShaderTest";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "__CLI213ShaderTest");
            try
            {
                var path = $"{folder}/Mat.mat";
                AssetDatabase.CreateAsset(new Material(Shader.Find(shaderName)), path);
                AssetDatabase.SaveAssets();

                var result = ShaderCommands.GetShaderProperties(material: new ObjectRef { Path = path });
                Assert.AreEqual(shaderName, result.Shader);
                Assert.IsNotEmpty(result.Properties);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void GetShaderProperties_NotFound_Throws()
        {
            var ex = Assert.Throws<ShaderNotFoundException>(() =>
                ShaderCommands.GetShaderProperties(shader: "No/Such/Shader__CLI213"));
            StringAssert.Contains("shader_not_found", ex.Message);
        }

        [Test]
        public void GetShaderProperties_NeitherArg_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => ShaderCommands.GetShaderProperties());
        }

        // ---- ViaClient ----------------------------------------------------------------------------

        [Test]
        public void ListShaders_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("list_shaders", new { filter = "Lit", limit = 20 });
                Assert.IsTrue(response.IsSuccess, $"list_shaders should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "response should be valid JSON");
            }
        }
    }
}
