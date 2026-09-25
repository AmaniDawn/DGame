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
    /// Tests for the CLI-215 legacy NavMesh bake commands. Covers discovery/schema, the settings
    /// get/set round-trip, the destructive clear guard, the idle status, the no_scene guard, dry_run
    /// being a no-op, and the AI-Navigation stub returning package_not_found when the package is
    /// absent. The full bake → succeeded flow is exercised live.
    /// </summary>
    public class NavMeshBakeCommandsTests
    {
        [SetUp]
        public void SetUp() => CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void NavMeshCommands_AreDiscovered_WithExpectedSchema()
        {
            var commands = CommandRegistry.DiscoverCommands().ToList();

            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "bake_navmesh"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "cancel_navmesh_bake"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "clear_navmesh"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "get_navmesh_settings"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.Name == "set_navmesh_settings"));

            var status = commands.FirstOrDefault(c => c.Name == "navmesh_bake_status");
            Assert.IsNotNull(status, "Should discover navmesh_bake_status");
            Assert.IsFalse(status.MainThreadRequired, "navmesh_bake_status must be off-main-thread");
            Assert.AreEqual(0, status.Parameters.Count, "navmesh_bake_status takes no parameters");
        }

        [Test]
        public void GetNavMeshSettings_ReturnsAgentFields()
        {
            var settings = NavMeshBakeCommands.GetNavMeshSettings();
            Assert.IsTrue(settings.Available, "Legacy NavMesh settings should be available");
            Assert.Greater(settings.AgentRadius, 0f, "agentRadius should be a sensible positive default");
            Assert.Greater(settings.AgentHeight, 0f, "agentHeight should be a sensible positive default");
        }

        [Test]
        public void SetNavMeshSettings_RoundTrips_AgentRadius()
        {
            var before = NavMeshBakeCommands.GetNavMeshSettings();
            var newRadius = before.AgentRadius >= 0.5f ? 0.35f : 0.75f;

            NavMeshBakeCommands.SetNavMeshSettings(new JObject { ["agentRadius"] = newRadius });

            var after = NavMeshBakeCommands.GetNavMeshSettings();
            Assert.AreEqual(newRadius, after.AgentRadius, 0.001f, "agentRadius should round-trip");

            // Restore the prior value so the suite leaves no residue in the host project's settings.
            NavMeshBakeCommands.SetNavMeshSettings(new JObject { ["agentRadius"] = before.AgentRadius });
        }

        [Test]
        public void SetNavMeshSettings_ReportsUnknownKeys()
        {
            var result = JObject.FromObject(
                NavMeshBakeCommands.SetNavMeshSettings(new JObject { ["agentRadius"] = 0.5f, ["nope"] = 1 }, dryRun: true));

            var applied = result["applied"].Select(t => t.Value<string>()).ToList();
            var unknown = result["unknown"].Select(t => t.Value<string>()).ToList();
            CollectionAssert.Contains(applied, "agentRadius");
            CollectionAssert.Contains(unknown, "nope");
        }

        [Test]
        public void SetNavMeshSettings_DryRun_DoesNotChangeAnything()
        {
            var before = NavMeshBakeCommands.GetNavMeshSettings();
            var changed = before.AgentRadius >= 0.5f ? 0.2f : 0.9f;

            NavMeshBakeCommands.SetNavMeshSettings(new JObject { ["agentRadius"] = changed }, dryRun: true);

            var after = NavMeshBakeCommands.GetNavMeshSettings();
            Assert.AreEqual(before.AgentRadius, after.AgentRadius, 0.001f, "dry_run must not mutate settings");
        }

        [Test]
        public void ClearNavMesh_WithoutConfirm_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => NavMeshBakeCommands.ClearNavMesh());
        }

        [Test]
        public void BakeNavMesh_NoOpenSavedScene_ReturnsNoScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var result = JObject.FromObject(NavMeshBakeCommands.BakeNavMesh());
            Assert.AreEqual("no_scene", result["code"].Value<string>());
        }

        [Test]
        public void NavMeshBakeStatus_AlwaysHasStatusField()
        {
            var json = JObject.Parse(NavMeshBakeCommands.NavMeshBakeStatus());
            Assert.IsNotNull(json["status"]);
        }

        [Test]
        public void BakeNavMeshSurfaces_WithoutPackage_ReturnsPackageNotFound()
        {
            // This repo does not reference com.unity.ai.navigation, so the stub should report the
            // package is absent (or, if the package is present in the host project, not_implemented).
            var result = JObject.FromObject(NavMeshBakeCommands.BakeNavMeshSurfaces());
            var code = result["code"].Value<string>();
            Assert.That(code, Is.EqualTo("package_not_found").Or.EqualTo("not_implemented"));
        }
    }
}
