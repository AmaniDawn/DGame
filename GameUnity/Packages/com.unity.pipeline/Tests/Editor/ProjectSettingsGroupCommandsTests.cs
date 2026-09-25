using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.ProjectSettings;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Wiring tests for the per-group project-settings commands (CLI-202, step 3). These are
    /// deliberately non-mutating: they verify discovery, nested-object schema exposure, the read path,
    /// and the confirm/dry-run gate — without committing a change to the test project's real settings.
    /// End-to-end mutate-and-read-back coverage per group belongs to the dedicated test step.
    /// </summary>
    public class ProjectSettingsGroupCommandsTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }

        [Test]
        public void AllSettingsGroups_ExposeGetAndSetCommands()
        {
            var names = CommandRegistry.DiscoverCommands().Select(c => c.Name).ToList();

            foreach (var group in new[] { "player", "quality", "graphics", "physics", "time", "input", "audio" })
            {
                Assert.Contains($"get_{group}_settings", names, $"missing get for {group}");
                Assert.Contains($"set_{group}_settings", names, $"missing set for {group}");
            }

            // Tags & layers uses its own (non "_settings") naming.
            Assert.Contains("get_tags_layers", names);
            Assert.Contains("set_tags_layers", names);
        }

        [Test]
        public void SetPlayerSettings_ExposesNestedObjectSchema_WithGateArgs()
        {
            var command = CommandRegistry.DiscoverCommands().First(c => c.Name == "set_player_settings");
            var schema = JObject.Parse(JsonSchemaGenerator.GenerateCommandSchema(command));
            var properties = (JObject)schema["properties"];

            // The structured 'settings' parameter is a real nested object (from CAT-2508)...
            var settings = (JObject)properties["settings"];
            Assert.AreEqual("object", settings["type"]?.ToString());

            var settingsProps = (JObject)settings["properties"];
            Assert.AreEqual("string", settingsProps["companyName"]?["type"]?.ToString());

            // ...including the enum-typed scripting backend rendered with its allowed values.
            var backend = settingsProps["scriptingBackend"];
            Assert.AreEqual("string", backend?["type"]?.ToString());
            Assert.IsNotNull(backend?["enum"], "scriptingBackend should expose its enum values");

            // ...and the shared confirm / dry_run gate args.
            Assert.AreEqual("boolean", properties["confirm"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["dry_run"]?["type"]?.ToString());
        }

        [Test]
        public void GetTimeSettings_ReadsCurrentValues()
        {
            var response = TimeSettingsCommands.Get();

            Assert.IsTrue(response.Success);
            Assert.AreEqual("time", response.Group);
            Assert.IsFalse(response.Applied);
            Assert.IsTrue(response.Values.ContainsKey("fixedDeltaTime"));
        }

        [Test]
        public void SetTimeSettings_DryRun_PreviewsWithoutMutating()
        {
            var before = Time.timeScale;

            var response = TimeSettingsCommands.Set(
                new TimeSettingsInput { TimeScale = before + 5f },
                confirm: true,
                dryRun: true);

            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.DryRun);
            Assert.IsFalse(response.Applied);
            Assert.AreEqual(before, Time.timeScale, "dry run must not change the real setting");
        }

        [Test]
        public void SetTimeSettings_WithoutConfirm_IsRejectedWithoutMutating()
        {
            var before = Time.timeScale;

            var response = TimeSettingsCommands.Set(
                new TimeSettingsInput { TimeScale = before + 5f },
                confirm: false,
                dryRun: false);

            Assert.IsFalse(response.Success);
            Assert.IsFalse(response.Applied);
            Assert.AreEqual(before, Time.timeScale, "an unconfirmed change must not mutate the setting");
            StringAssert.Contains("confirm=true", response.Message);
        }
    }
}
