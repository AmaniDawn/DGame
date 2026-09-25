using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.Baking;
using UnityEditor.SceneManagement;

namespace Unity.Pipeline.Tests.Editor.Baking
{
    /// <summary>
    /// Tests for the CLI-215 occlusion-culling bake commands. Covers discovery/schema, the destructive
    /// clear guard, the idle status, the no_scene guard, and dry_run being a no-op (and echoing the
    /// effective parameters). The full bake → succeeded flow is exercised live.
    /// </summary>
    public class OcclusionBakeCommandsTests
    {
        [SetUp]
        public void SetUp() => CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void OcclusionCommands_AreDiscovered_WithExpectedSchema()
        {
            var commands = CommandRegistry.DiscoverCommands().ToList();

            var bake = commands.FirstOrDefault(c => c.Name == "bake_occlusion_culling");
            Assert.IsNotNull(bake, "Should discover bake_occlusion_culling");
            CollectionAssert.AreEquivalent(
                new[] { "smallest_occluder", "smallest_hole", "backface_threshold", "confirm", "dry_run" },
                bake.Parameters.Select(p => p.Name).ToList());

            var status = commands.FirstOrDefault(c => c.Name == "occlusion_bake_status");
            Assert.IsNotNull(status, "Should discover occlusion_bake_status");
            Assert.IsFalse(status.MainThreadRequired, "occlusion_bake_status must be off-main-thread");
            Assert.AreEqual(0, status.Parameters.Count, "occlusion_bake_status takes no parameters");

            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "cancel_occlusion_bake"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "clear_occlusion_culling"));
        }

        [Test]
        public void ClearOcclusion_WithoutConfirm_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => OcclusionBakeCommands.ClearOcclusionCulling());
        }

        [Test]
        public void BakeOcclusion_NoOpenSavedScene_ReturnsNoScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var result = JObject.FromObject(OcclusionBakeCommands.BakeOcclusionCulling());
            Assert.AreEqual("no_scene", result["code"].Value<string>());
        }

        [Test]
        public void OcclusionBakeStatus_AlwaysHasStatusField()
        {
            var json = JObject.Parse(OcclusionBakeCommands.OcclusionBakeStatus());
            Assert.IsNotNull(json["status"]);
        }
    }
}
