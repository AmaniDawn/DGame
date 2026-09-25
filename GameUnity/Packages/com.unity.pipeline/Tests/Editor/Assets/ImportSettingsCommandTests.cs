using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for the CLI-212 per-type import settings + platform overrides:
    /// <c>get_import_settings</c> and the platform-aware extension of <c>set_import_settings</c>.
    ///
    /// Real importers are exercised by writing tiny generated assets, so the actual Unity read/write
    /// code paths (including per-platform overrides) are covered end to end: a 4x4 PNG (TextureImporter),
    /// a minimal OBJ (ModelImporter), and a tiny 8-bit PCM WAV (AudioImporter). The audio fixture drives
    /// a full per-platform override round-trip (<c>ApplyAudioPlatform</c> +
    /// <c>ReadAudioPlatformOverride</c>): set override, read it back, then clear it.
    /// </summary>
    public class ImportSettingsCommandTests
    {
        private const string Root = "Assets/__CLI212ImportTest";

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

        // ------------------------------------------------------------------ asset fixtures

        /// <summary>Write a tiny 4x4 PNG so Unity imports it with a TextureImporter.</summary>
        private static string CreateTexture(string name = "Tex.png")
        {
            EnsureRoot();
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 128, 64, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            var projectPath = Root + "/" + name;
            File.WriteAllBytes(ToAbsolute(projectPath), bytes);
            AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport);
            return projectPath;
        }

        /// <summary>Write a minimal single-triangle OBJ so Unity imports it with a ModelImporter.</summary>
        private static string CreateModel(string name = "Tri.obj")
        {
            EnsureRoot();
            const string obj =
                "o Tri\n" +
                "v 0 0 0\n" +
                "v 1 0 0\n" +
                "v 0 1 0\n" +
                "vn 0 0 1\n" +
                "f 1//1 2//1 3//1\n";
            var projectPath = Root + "/" + name;
            File.WriteAllText(ToAbsolute(projectPath), obj);
            AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport);
            return projectPath;
        }

        /// <summary>
        /// Write a tiny mono 8-bit PCM WAV (a handful of samples) so Unity imports it with an
        /// AudioImporter — enough to exercise the real per-platform override read/write paths.
        /// </summary>
        private static string CreateAudio(string name = "Beep.wav")
        {
            EnsureRoot();

            const int sampleRate = 8000;
            const int sampleCount = 64;
            var pcm = new byte[sampleCount];
            for (var i = 0; i < pcm.Length; i++)
                pcm[i] = (byte)(128 + (i % 2 == 0 ? 32 : -32)); // crude square wave around the 8-bit midpoint

            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                const short channels = 1;
                const short bitsPerSample = 8;
                var byteRate = sampleRate * channels * bitsPerSample / 8;
                var blockAlign = (short)(channels * bitsPerSample / 8);

                // RIFF header
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + pcm.Length);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                // fmt chunk
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);                 // PCM fmt chunk size
                w.Write((short)1);           // audio format = PCM
                w.Write(channels);
                w.Write(sampleRate);
                w.Write(byteRate);
                w.Write(blockAlign);
                w.Write(bitsPerSample);
                // data chunk
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(pcm.Length);
                w.Write(pcm);
                w.Flush();

                var projectPath = Root + "/" + name;
                File.WriteAllBytes(ToAbsolute(projectPath), stream.ToArray());
                AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport);
                return projectPath;
            }
        }

        private static void EnsureRoot()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "__CLI212ImportTest");
        }

        private static string ToAbsolute(string projectRelative) =>
            $"{ProjectPaths.ProjectRoot.Replace('\\', '/')}/{projectRelative}";

        // ------------------------------------------------------------------ texture: read

        [Test]
        public void GetImportSettings_Texture_ReturnsStructuredDefaultsAndOverrideBlock()
        {
            var path = CreateTexture();

            var result = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");

            Assert.AreEqual("TextureImporter", result.ImporterType);
            Assert.AreEqual("Android", result.Platform);
            Assert.IsNotNull(result.Settings);
            Assert.IsTrue(result.Settings.ContainsKey("textureType"));
            Assert.IsTrue(result.Settings.ContainsKey("isReadable"));
            Assert.IsTrue(result.Settings.ContainsKey("mipmapEnabled"));
            Assert.IsTrue(result.Settings.ContainsKey("wrapMode"));
            Assert.IsNotNull(result.PlatformOverride, "Texture should expose a platform override block");
            Assert.IsTrue(result.PlatformOverride.ContainsKey("overridden"));
        }

        // ------------------------------------------------------------------ texture: default round-trip

        [Test]
        public void SetImportSettings_TextureDefault_AppliesAndReadBackReflects()
        {
            var path = CreateTexture();

            var settings = new JObject
            {
                ["isReadable"] = true,
                ["textureType"] = "NormalMap"
            };
            var set = AssetImportCommands.SetImportSettings(PathRef(path), settings);

            CollectionAssert.Contains(set.Applied, "isReadable");
            CollectionAssert.Contains(set.Applied, "textureType");
            CollectionAssert.IsEmpty(set.Unknown);

            var read = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual(true, read.Settings["isReadable"]);
            Assert.AreEqual("NormalMap", read.Settings["textureType"]);
        }

        // ------------------------------------------------------------------ texture: platform override set + clear

        [Test]
        public void SetImportSettings_TextureAndroidOverride_SetsThenClears()
        {
            var path = CreateTexture();

            // Default platform maxTextureSize before the override (should be unchanged afterwards).
            var defaultBefore = AssetImportCommands.GetImportSettings(PathRef(path));
            var defaultMaxBefore = defaultBefore.Settings["maxTextureSize"];

            var set = AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["maxTextureSize"] = 1024, ["format"] = "ASTC_6x6", ["overridden"] = true },
                platform: "Android");
            CollectionAssert.Contains(set.Applied, "maxTextureSize");
            CollectionAssert.Contains(set.Applied, "format");

            var afterSet = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");
            Assert.AreEqual(true, afterSet.PlatformOverride["overridden"]);
            Assert.AreEqual(1024, afterSet.PlatformOverride["maxTextureSize"]);
            Assert.AreEqual("ASTC_6x6", afterSet.PlatformOverride["format"]);

            // Default platform unchanged by the Android override.
            var defaultAfter = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual(defaultMaxBefore, defaultAfter.Settings["maxTextureSize"],
                "Default platform settings must be unchanged by a platform override");

            // Clear the override.
            AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["overridden"] = false },
                platform: "Android");

            var afterClear = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");
            Assert.AreEqual(false, afterClear.PlatformOverride["overridden"], "Override should be cleared");
        }

        // ------------------------------------------------------------------ audio: read

        [Test]
        public void GetImportSettings_Audio_ReturnsStructuredDefaultsAndOverrideBlock()
        {
            var path = CreateAudio();

            var result = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");

            Assert.AreEqual("AudioImporter", result.ImporterType);
            Assert.AreEqual("Android", result.Platform);
            Assert.IsNotNull(result.Settings);
            Assert.IsTrue(result.Settings.ContainsKey("loadType"));
            Assert.IsTrue(result.Settings.ContainsKey("compressionFormat"));
            Assert.IsTrue(result.Settings.ContainsKey("quality"));
            Assert.IsNotNull(result.PlatformOverride, "Audio should expose a platform override block");
            Assert.IsTrue(result.PlatformOverride.ContainsKey("overridden"));
            // No override has been written yet.
            Assert.AreEqual(false, result.PlatformOverride["overridden"]);
        }

        // ------------------------------------------------------------------ audio: platform override set + clear

        [Test]
        public void SetImportSettings_AudioAndroidOverride_SetsThenClears()
        {
            var path = CreateAudio();

            // Default-platform compressionFormat before the override (must be unchanged afterwards).
            var defaultBefore = AssetImportCommands.GetImportSettings(PathRef(path));
            var defaultCompressionBefore = defaultBefore.Settings["compressionFormat"];

            var set = AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["compressionFormat"] = "Vorbis", ["loadType"] = "CompressedInMemory", ["overridden"] = true },
                platform: "Android");
            CollectionAssert.Contains(set.Applied, "compressionFormat");
            CollectionAssert.Contains(set.Applied, "loadType");
            CollectionAssert.Contains(set.Applied, "overridden");
            CollectionAssert.IsEmpty(set.Unknown);

            var afterSet = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");
            Assert.AreEqual(true, afterSet.PlatformOverride["overridden"], "Override should be present after a platform write");
            Assert.AreEqual("Vorbis", afterSet.PlatformOverride["compressionFormat"]);
            Assert.AreEqual("CompressedInMemory", afterSet.PlatformOverride["loadType"]);

            // Default platform unchanged by the Android override.
            var defaultAfter = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual(defaultCompressionBefore, defaultAfter.Settings["compressionFormat"],
                "Default platform settings must be unchanged by a platform override");

            // Clear the override.
            AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["overridden"] = false },
                platform: "Android");

            var afterClear = AssetImportCommands.GetImportSettings(PathRef(path), platform: "Android");
            Assert.AreEqual(false, afterClear.PlatformOverride["overridden"], "Override should be cleared");
        }

        // ------------------------------------------------------------------ model: read + default write

        [Test]
        public void GetImportSettings_Model_ReturnsNullPlatformOverride()
        {
            var path = CreateModel();

            var result = AssetImportCommands.GetImportSettings(PathRef(path));

            Assert.AreEqual("ModelImporter", result.ImporterType);
            Assert.IsTrue(result.Settings.ContainsKey("meshCompression"));
            Assert.IsTrue(result.Settings.ContainsKey("animationType"));
            Assert.IsTrue(result.Settings.ContainsKey("materialImportMode"));
            Assert.IsNull(result.PlatformOverride, "ModelImporter has no per-platform overrides");
        }

        [Test]
        public void SetImportSettings_ModelDefault_AppliesAndReadBack()
        {
            var path = CreateModel();

            var set = AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["meshCompression"] = "High", ["importNormals"] = "Calculate" });

            CollectionAssert.Contains(set.Applied, "meshCompression");
            CollectionAssert.Contains(set.Applied, "importNormals");

            var read = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual("High", read.Settings["meshCompression"]);
            Assert.AreEqual("Calculate", read.Settings["importNormals"]);
        }

        [Test]
        public void SetImportSettings_ModelWithPlatform_ReturnsNoPlatformOverrides_AndWritesNothing()
        {
            var path = CreateModel();
            var before = AssetImportCommands.GetImportSettings(PathRef(path));
            var meshBefore = before.Settings["meshCompression"];

            var ex = Assert.Throws<System.ArgumentException>(() =>
                AssetImportCommands.SetImportSettings(
                    PathRef(path),
                    new JObject { ["meshCompression"] = "High" },
                    platform: "iOS"));
            StringAssert.Contains("no_platform_overrides", ex.Message);

            var after = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual(meshBefore, after.Settings["meshCompression"], "A rejected platform write must change nothing");
        }

        // ------------------------------------------------------------------ dry_run

        [Test]
        public void SetImportSettings_DryRun_ReportsButDoesNotChange()
        {
            var path = CreateTexture();
            var before = AssetImportCommands.GetImportSettings(PathRef(path));
            var readableBefore = (bool)before.Settings["isReadable"];

            var result = AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["isReadable"] = !readableBefore },
                dryRun: true);

            CollectionAssert.Contains(result.Applied, "isReadable");

            var after = AssetImportCommands.GetImportSettings(PathRef(path));
            Assert.AreEqual(readableBefore, (bool)after.Settings["isReadable"], "dry_run must not change settings");
        }

        // ------------------------------------------------------------------ unknown key handling

        [Test]
        public void SetImportSettings_UnknownKey_GoesToUnknown()
        {
            var path = CreateTexture();

            var result = AssetImportCommands.SetImportSettings(
                PathRef(path),
                new JObject { ["isReadable"] = true, ["definitelyNotAMember"] = 42 });

            CollectionAssert.Contains(result.Applied, "isReadable");
            CollectionAssert.Contains(result.Unknown, "definitelyNotAMember");
        }

        [Test]
        public void SetImportSettings_AllUnknown_Throws()
        {
            var path = CreateTexture();

            Assert.Throws<System.ArgumentException>(() =>
                AssetImportCommands.SetImportSettings(
                    PathRef(path),
                    new JObject { ["definitelyNotAMember"] = 42 }));
        }

        // ------------------------------------------------------------------ platform validation

        [Test]
        public void SetImportSettings_UnknownPlatform_Throws()
        {
            var path = CreateTexture();

            var ex = Assert.Throws<System.ArgumentException>(() =>
                AssetImportCommands.SetImportSettings(
                    PathRef(path),
                    new JObject { ["maxTextureSize"] = 512 },
                    platform: "PlayStation"));
            StringAssert.Contains("unknown_platform", ex.Message);
        }

        [Test]
        public void GetImportSettings_UnknownPlatform_Throws()
        {
            var path = CreateTexture();

            Assert.Throws<System.ArgumentException>(() =>
                AssetImportCommands.GetImportSettings(PathRef(path), platform: "PlayStation"));
        }

        // ------------------------------------------------------------------ sandbox guard

        [Test]
        public void GetImportSettings_OutsideAuthoringRoot_Throws()
        {
            var path = CreateTexture();
            // Confine the authoring root to a sub-folder that does NOT contain the asset, so the asset's
            // resolved path falls outside the sandbox.
            ProjectPaths.AuthoringRoot = Root + "/Nested";

            Assert.Throws<System.ArgumentException>(() =>
                AssetImportCommands.GetImportSettings(PathRef(path)));
        }

        // ------------------------------------------------------------------ via client (wire) smoke test

        [Test]
        public void GetImportSettings_ViaClient_Succeeds()
        {
            var path = CreateTexture();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("get_import_settings", new
                {
                    asset = new { path },
                    platform = "Android"
                });

                Assert.IsTrue(response.IsSuccess, $"get_import_settings should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }
    }
}
