using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using Unity.Pipeline.Models;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for set_import_settings (CLI-191). Uses the importer's <c>userData</c> property — present
    /// on every AssetImporter — so the test is independent of any specific importer kind. Verifies the
    /// applied/unknown bookkeeping and that the setting actually persists on the importer.
    /// </summary>
    public class AssetImportCommandsTests
    {
        private const string Root = "Assets/__CLI191ImportTest";

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

        private static string CreateSampleAsset()
        {
            var path = Root + "/Imported.asset";
            AssetCommands.CreateAsset(path, typeof(CLI191SampleAsset).FullName);
            return path;
        }

        #region Direct

        [Test]
        public void SetImportSettings_AppliesKnownMember()
        {
            var path = CreateSampleAsset();
            var settings = new JObject { ["userData"] = "cli191" };

            var result = AssetImportCommands.SetImportSettings(PathRef(path), settings);

            CollectionAssert.Contains(result.Applied, "userData");
            CollectionAssert.IsEmpty(result.Unknown);
            Assert.AreEqual("cli191", AssetImporter.GetAtPath(path).userData, "Setting should persist on the importer");
        }

        [Test]
        public void SetImportSettings_UnknownOnly_Throws()
        {
            var path = CreateSampleAsset();
            var settings = new JObject { ["definitelyNotAMember"] = 42 };

            Assert.Throws<System.ArgumentException>(
                () => AssetImportCommands.SetImportSettings(PathRef(path), settings),
                "When no supplied setting exists on the importer, the command should fail");
        }

        [Test]
        public void SetImportSettings_DryRun_DoesNotPersist()
        {
            var path = CreateSampleAsset();
            var settings = new JObject { ["userData"] = "dryrun" };

            var result = AssetImportCommands.SetImportSettings(PathRef(path), settings, dryRun: true);

            // dry_run reports the member as applicable but never mutates the importer.
            CollectionAssert.Contains(result.Applied, "userData");
            Assert.AreNotEqual("dryrun", AssetImporter.GetAtPath(path).userData, "dry_run should not persist the change");
        }

        #endregion

        #region ViaClient

        [Test]
        public void SetImportSettings_ViaClient_Succeeds()
        {
            var path = CreateSampleAsset();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_import_settings", new
                {
                    asset = new { path },
                    settings = new { userData = "viaclient" }
                });

                Assert.IsTrue(response.IsSuccess, $"set_import_settings should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.AreEqual("viaclient", AssetImporter.GetAtPath(path).userData);
            }
        }

        #endregion
    }
}
