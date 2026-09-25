using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.Build;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the build-configuration commands (CLI-204): <c>list_build_targets</c>,
    /// <c>get_build_settings</c>, <c>set_build_settings</c> (dry run only — no real mutation),
    /// <c>list_build_profiles</c>, and the <c>switch_build_target</c> command surface. The read-only
    /// commands are executed directly on the test (main) thread; nothing here triggers a build or switch.
    /// </summary>
    public class BuildConfigCommandsTests
    {
        [SetUp]
        public void SetUp() => CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

        [Test]
        public void NewCommands_AreDiscovered()
        {
            var names = CommandRegistry.DiscoverCommands().Select(c => c.Name).ToList();

            foreach (var expected in new[]
            {
                "list_build_targets", "get_build_settings", "set_build_settings",
                "list_build_profiles", "switch_build_target", "switch_build_target_status"
            })
            {
                CollectionAssert.Contains(names, expected, $"Should discover the {expected} command");
            }
        }

        [Test]
        public void SwitchBuildTarget_RequiresTarget_AndConfirm()
        {
            var switchCmd = CommandRegistry.DiscoverCommands().First(c => c.Name == "switch_build_target");

            var target = switchCmd.Parameters.First(p => p.Name == "target");
            Assert.IsTrue(target.Required, "switch_build_target 'target' must be required");

            // Missing confirm must be refused without attempting the switch.
            var result = JObjectOf(SwitchBuildTargetCommand.SwitchBuildTarget(
                EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64
                    ? "StandaloneLinux64"
                    : "StandaloneWindows64",
                confirm: false));

            Assert.AreEqual("error", (string)result["status"]);
            Assert.AreEqual(false, (bool)result["success"]);
        }

        [Test]
        public void ListBuildTargets_IncludesActiveTarget_AsInstalled()
        {
            var targets = (List<BuildTargetInfo>)BuildConfigCommands.ListBuildTargets();
            Assert.IsNotNull(targets);
            Assert.Greater(targets.Count, 0, "should enumerate at least one build target");

            var active = EditorUserBuildSettings.activeBuildTarget.ToString();
            var entry = targets.FirstOrDefault(t => t.Name == active);
            Assert.IsNotNull(entry, "the active build target should appear in the list");
            Assert.IsTrue(entry.IsInstalled, "the active build target must be installed");
        }

        [Test]
        public void GetBuildSettings_ReportsActiveTargetAndScenes()
        {
            var settings = (BuildSettingsResult)BuildConfigCommands.GetBuildSettings();
            Assert.IsNotNull(settings);
            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget.ToString(), settings.ActiveBuildTarget);
            Assert.IsNotNull(settings.Scenes, "scene list should be present (may be empty)");
            Assert.IsNotNull(settings.Il2CppCodeGeneration);
        }

        [Test]
        public void SetBuildSettings_DryRun_ReportsPlan_WithoutMutating()
        {
            var before = EditorUserBuildSettings.development;

            var input = new SetBuildSettingsInput { DevelopmentBuild = !before };
            var result = (SetBuildSettingsResult)BuildConfigCommands.SetBuildSettings(input, dryRun: true);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.DryRun);
            Assert.IsTrue(result.Applied.ContainsKey("developmentBuild"),
                "the flipped field should appear in the dry-run 'applied' plan");
            Assert.AreEqual(before, EditorUserBuildSettings.development,
                "a dry run must not change EditorUserBuildSettings");
        }

        [Test]
        public void SetBuildSettings_NoSettings_Fails()
        {
            var result = (SetBuildSettingsResult)BuildConfigCommands.SetBuildSettings(null, dryRun: false);
            Assert.IsFalse(result.Success);
        }

        static Newtonsoft.Json.Linq.JObject JObjectOf(object value) =>
            Newtonsoft.Json.Linq.JObject.FromObject(value);
    }
}
