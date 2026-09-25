using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Materials;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.Materials
{
    /// <summary>
    /// Tests for the CLI-213 material authoring commands (get_material_properties /
    /// set_material_properties). All assets are generated in-test under a temp folder and torn down
    /// afterwards. URP-specific assertions (e.g. _BaseColor / _Metallic Range / _BaseMap) are guarded
    /// so the suite still runs (with Assert.Ignore) on a Built-in RP project that lacks URP/Lit.
    /// </summary>
    public class MaterialCommandsTests
    {
        private const string Root = "Assets/__CLI213MaterialTest";

        private string m_KnownShader;       // a shader guaranteed to resolve on this project
        private bool m_HasUrpLit;

        [SetUp]
        public void SetUp()
        {
            ProjectPaths.ResetAuthoringRoot();
            m_HasUrpLit = Shader.Find("Universal Render Pipeline/Lit") != null;
            m_KnownShader = PickKnownShader();
            EnsureFolder(Root);
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

        private static string PickKnownShader()
        {
            if (Shader.Find("Standard") != null)
                return "Standard";
            if (Shader.Find("Universal Render Pipeline/Lit") != null)
                return "Universal Render Pipeline/Lit";
            return "Sprites/Default";
        }

        /// <summary>The Color property to exercise: URP/Lit uses _BaseColor; built-in Standard uses _Color.</summary>
        private string ColorProp => m_HasUrpLit ? "_BaseColor" : "_Color";

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
        }

        /// <summary>Create a .mat asset with the given shader and return a path-based handle to it.</summary>
        private ObjectRef CreateMaterial(string name, string shaderName = null)
        {
            var shader = Shader.Find(shaderName ?? m_KnownShader);
            Assert.IsNotNull(shader, $"Test prerequisite: shader '{shaderName ?? m_KnownShader}' must resolve");
            var mat = new Material(shader);
            var path = $"{Root}/{name}.mat";
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return new ObjectRef { Path = path };
        }

        /// <summary>Create a tiny 2x2 texture asset on disk and return a handle to it.</summary>
        private ObjectRef CreateTexture(string name)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            var path = $"{Root}/{name}.png";
            File.WriteAllBytes(Path.Combine(ProjectPaths.ProjectRoot, path), bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return new ObjectRef { Path = path };
        }

        private static MaterialPropertyValue Prop(MaterialPropertiesResult r, string name)
            => r.Properties.FirstOrDefault(p => p.Name == name);

        /// <summary>
        /// Extract the float components of a Color/Vector property value. The direct (in-process) path
        /// returns a <c>float[]</c>; tolerate any IEnumerable (e.g. a JArray) so the helper also works
        /// if the value was JSON round-tripped.
        /// </summary>
        private static float[] Components(object value)
        {
            Assert.IsNotNull(value, "property value should not be null");
            return ((System.Collections.IEnumerable)value)
                .Cast<object>()
                .Select(o => System.Convert.ToSingle(o is JToken t ? (object)t.ToObject<float>() : o))
                .ToArray();
        }

        // ---- get_material_properties --------------------------------------------------------------

        [Test]
        public void Get_ReturnsShaderRenderQueueKeywordsAndProperties()
        {
            var handle = CreateMaterial("Get");
            var result = MaterialCommands.GetMaterialProperties(handle);

            Assert.AreEqual(Shader.Find(m_KnownShader).name, result.Shader);
            Assert.IsNotNull(result.EnabledKeywords);
            Assert.IsNotEmpty(result.Properties, "A real shader should expose at least one property");

            var color = Prop(result, ColorProp);
            Assert.IsNotNull(color, $"{ColorProp} should be present");
            Assert.AreEqual("Color", color.Type);
        }

        [Test]
        public void Get_Urp_BaseColorIsColor_MetallicIsRangeWithLimits()
        {
            if (!m_HasUrpLit)
                Assert.Ignore("URP/Lit not available in this project; skipping URP-specific assertion (AC #1).");

            var handle = CreateMaterial("UrpGet", "Universal Render Pipeline/Lit");
            var result = MaterialCommands.GetMaterialProperties(handle);

            var baseColor = Prop(result, "_BaseColor");
            Assert.IsNotNull(baseColor, "_BaseColor should be present");
            Assert.AreEqual("Color", baseColor.Type);

            var metallic = Prop(result, "_Metallic");
            Assert.IsNotNull(metallic, "_Metallic should be present");
            Assert.AreEqual("Range", metallic.Type);
            Assert.IsNotNull(metallic.Range, "_Metallic (Range) should carry range limits");
            Assert.LessOrEqual(metallic.Range.Min, metallic.Range.Max);
        }

        // ---- set_material_properties: color -------------------------------------------------------

        [Test]
        public void Set_BaseColor_ViaArray_AppliesRed()
        {
            var handle = CreateMaterial("SetColorArray");
            var props = new JObject { [ColorProp] = new JArray(1, 0, 0, 1) };

            var result = MaterialCommands.SetMaterialProperties(handle, properties: props);

            CollectionAssert.Contains(result.Applied, ColorProp);
            var read = MaterialCommands.GetMaterialProperties(handle);
            var c = Components(Prop(read, ColorProp).Value);
            Assert.AreEqual(1f, c[0], 1e-4f);
            Assert.AreEqual(0f, c[1], 1e-4f);
            Assert.AreEqual(0f, c[2], 1e-4f);
            Assert.AreEqual(1f, c[3], 1e-4f);
        }

        [Test]
        public void Set_BaseColor_ViaHex_AppliesGreen()
        {
            var handle = CreateMaterial("SetColorHex");
            var props = new JObject { [ColorProp] = "#00FF00" };

            MaterialCommands.SetMaterialProperties(handle, properties: props);

            var read = MaterialCommands.GetMaterialProperties(handle);
            var c = Components(Prop(read, ColorProp).Value);
            Assert.AreEqual(0f, c[0], 1e-3f);
            Assert.AreEqual(1f, c[1], 1e-3f);
            Assert.AreEqual(0f, c[2], 1e-3f);
            Assert.AreEqual(1f, c[3], 1e-3f, "hex without alpha should default alpha to 1.0");
        }

        // ---- set_material_properties: texture -----------------------------------------------------

        [Test]
        public void Set_Texture_AssignsThenClears()
        {
            if (!m_HasUrpLit)
                Assert.Ignore("URP/Lit not available; skipping _BaseMap texture test (AC #4).");

            var handle = CreateMaterial("SetTex", "Universal Render Pipeline/Lit");
            var tex = CreateTexture("Albedo");

            var set = MaterialCommands.SetMaterialProperties(handle,
                properties: new JObject { ["_BaseMap"] = JToken.FromObject(tex) });
            CollectionAssert.Contains(set.Applied, "_BaseMap");

            var loadedTex = AssetDatabase.LoadAssetAtPath<Texture>(tex.Path);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(handle.Path);
            Assert.AreEqual(loadedTex, mat.GetTexture("_BaseMap"), "texture should be assigned");

            // Clear with null.
            MaterialCommands.SetMaterialProperties(handle,
                properties: new JObject { ["_BaseMap"] = JValue.CreateNull() });
            mat = AssetDatabase.LoadAssetAtPath<Material>(handle.Path);
            Assert.IsNull(mat.GetTexture("_BaseMap"), "passing null should clear the texture");
        }

        [Test]
        public void Set_Texture_OutsideAuthoringRoot_IsRejected()
        {
            if (!m_HasUrpLit)
                Assert.Ignore("URP/Lit not available; skipping _BaseMap sandbox test (AC #13).");

            // Confine the authoring root to a subfolder, then reference a texture created OUTSIDE it.
            var outsideTex = CreateTexture("OutsideAlbedo"); // lives directly under Root
            var sub = $"{Root}/Sub";
            if (!AssetDatabase.IsValidFolder(sub))
                AssetDatabase.CreateFolder(Root, "Sub");
            var handle = CreateMaterial("Sub/SetTexSandbox", "Universal Render Pipeline/Lit");

            ProjectPaths.AuthoringRoot = sub; // now Root/OutsideAlbedo.png is outside the root

            var ex = Assert.Throws<System.ArgumentException>(() =>
                MaterialCommands.SetMaterialProperties(handle,
                    properties: new JObject { ["_BaseMap"] = JToken.FromObject(outsideTex) }));
            StringAssert.Contains("outside the authoring root", ex.Message);
        }

        // ---- shader reassignment ------------------------------------------------------------------

        [Test]
        public void Set_Shader_Reassigns()
        {
            if (Shader.Find("Standard") == null)
                Assert.Ignore("Built-in Standard shader not available; skipping shader-reassign test (AC #5).");

            var handle = CreateMaterial("Reassign", m_KnownShader);
            var result = MaterialCommands.SetMaterialProperties(handle, shader: "Standard");

            Assert.AreEqual("Standard", result.Shader);
            var read = MaterialCommands.GetMaterialProperties(handle);
            Assert.AreEqual("Standard", read.Shader);
        }

        [Test]
        public void Set_Shader_NotFound_ThrowsAndWritesNothing()
        {
            var handle = CreateMaterial("BadShader");
            var before = MaterialCommands.GetMaterialProperties(handle).Shader;

            var ex = Assert.Throws<ShaderNotFoundException>(() =>
                MaterialCommands.SetMaterialProperties(handle, shader: "No/Such/Shader__CLI213",
                    properties: new JObject { [ColorProp] = new JArray(1, 0, 0, 1) }));
            StringAssert.Contains("shader_not_found", ex.Message);
            Assert.AreEqual("shader_not_found", ex.Code);

            var after = MaterialCommands.GetMaterialProperties(handle).Shader;
            Assert.AreEqual(before, after, "a shader_not_found must write nothing");
        }

        // ---- keywords -----------------------------------------------------------------------------

        [Test]
        public void Set_Keyword_EnableThenDisable()
        {
            var handle = CreateMaterial("Keyword");
            const string keyword = "_EMISSION";

            MaterialCommands.SetMaterialProperties(handle, enableKeywords: new[] { keyword });
            var enabled = MaterialCommands.GetMaterialProperties(handle).EnabledKeywords;
            CollectionAssert.Contains(enabled, keyword);

            MaterialCommands.SetMaterialProperties(handle, disableKeywords: new[] { keyword });
            var disabled = MaterialCommands.GetMaterialProperties(handle).EnabledKeywords;
            CollectionAssert.DoesNotContain(disabled, keyword);
        }

        // ---- render queue -------------------------------------------------------------------------

        [Test]
        public void Set_RenderQueue_ExplicitThenInherit()
        {
            var handle = CreateMaterial("Queue");

            MaterialCommands.SetMaterialProperties(handle, renderQueue: 3000);
            Assert.AreEqual(3000, MaterialCommands.GetMaterialProperties(handle).RenderQueue,
                "an explicit renderQueue must be reflected by the command's own read-back");

            MaterialCommands.SetMaterialProperties(handle, renderQueue: -1);
            // Validate inheritance via rawRenderQueue == -1 rather than the effective queue: the shader
            // default can legitimately equal the prior override (e.g. 3000), so comparing the effective
            // value would be flaky. rawRenderQueue is -1 exactly when the material inherits the shader's.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(handle.Path);
            Assert.AreEqual(-1, mat.rawRenderQueue, "renderQueue -1 should clear the override and inherit from the shader");
        }

        // ---- unknown / mismatch -------------------------------------------------------------------

        [Test]
        public void Set_UnknownProperty_GoesToUnknown_StillAppliesOthers()
        {
            var handle = CreateMaterial("Unknown");
            var props = new JObject
            {
                [ColorProp] = new JArray(0, 0, 1, 1),
                ["_DefinitelyNotARealProp_CLI213"] = 5,
            };

            var result = MaterialCommands.SetMaterialProperties(handle, properties: props);

            CollectionAssert.Contains(result.Applied, ColorProp);
            Assert.IsTrue(result.Unknown.Any(u => u.Contains("_DefinitelyNotARealProp_CLI213")),
                "unknown property should be reported in unknown[]");
        }

        [Test]
        public void Set_TypeMismatch_GoesToUnknownWithReason()
        {
            if (!m_HasUrpLit)
                Assert.Ignore("URP/Lit not available; skipping _Metallic type-mismatch test.");

            var handle = CreateMaterial("Mismatch", "Universal Render Pipeline/Lit");
            // _Metallic is a Range (number); send an array to force a mismatch.
            var props = new JObject
            {
                ["_BaseColor"] = new JArray(1, 1, 1, 1),
                ["_Metallic"] = new JArray(1, 2, 3),
            };

            var result = MaterialCommands.SetMaterialProperties(handle, properties: props);

            CollectionAssert.Contains(result.Applied, "_BaseColor");
            Assert.IsTrue(result.Unknown.Any(u => u.Contains("_Metallic")),
                "a type mismatch should be reported in unknown[] with a reason");
        }

        // ---- dry_run ------------------------------------------------------------------------------

        [Test]
        public void Set_DryRun_ReportsButDoesNotWrite()
        {
            var handle = CreateMaterial("DryRun");
            var beforeColor = Components(Prop(MaterialCommands.GetMaterialProperties(handle), ColorProp).Value);

            var props = new JObject { [ColorProp] = new JArray(1, 0, 0, 1) };
            var result = MaterialCommands.SetMaterialProperties(handle, properties: props, dryRun: true);

            CollectionAssert.Contains(result.Applied, ColorProp);

            var afterColor = Components(Prop(MaterialCommands.GetMaterialProperties(handle), ColorProp).Value);
            CollectionAssert.AreEqual(beforeColor, afterColor, "dry_run must not change the material");
        }

        // ---- ViaClient ----------------------------------------------------------------------------

        [Test]
        public void Set_ViaClient_AppliesColor()
        {
            var handle = CreateMaterial("ViaClient");
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_material_properties", new
                {
                    material = new { path = handle.Path },
                    properties = new JObject { [ColorProp] = new JArray(1, 0, 0, 1) },
                });

                Assert.IsTrue(response.IsSuccess, $"set_material_properties should succeed: {response.Error}");

                var read = MaterialCommands.GetMaterialProperties(handle);
                var c = Components(Prop(read, ColorProp).Value);
                Assert.AreEqual(1f, c[0], 1e-4f);
            }
        }
    }
}
