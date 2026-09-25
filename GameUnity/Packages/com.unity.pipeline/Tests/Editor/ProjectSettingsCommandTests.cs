using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Pipeline.Editor.Commands.ProjectSettings;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the shared project-settings plumbing (CLI-202, step 2): the get/apply helpers that
    /// per-group commands wrap. Uses an in-memory "setting" as the backing store so the policy wiring
    /// (read-back, dry-run preview, confirm gate, reload signalling) can be tested without touching
    /// real Unity project settings.
    /// </summary>
    public class ProjectSettingsCommandTests
    {
        [Test]
        public void Get_ReturnsValues_AndGroup()
        {
            var response = ProjectSettingsCommand.Get("sample", () => new Dictionary<string, object>
            {
                ["foo"] = 1,
                ["bar"] = "baz"
            });

            Assert.IsTrue(response.Success);
            Assert.AreEqual("sample", response.Group);
            Assert.IsFalse(response.Applied);
            Assert.AreEqual(1, response.Values["foo"]);
            Assert.AreEqual("baz", response.Values["bar"]);
        }

        [Test]
        public void Get_WhenReadThrows_ReturnsFailure()
        {
            var response = ProjectSettingsCommand.Get("sample",
                () => throw new InvalidOperationException("read boom"));

            Assert.IsFalse(response.Success);
            Assert.AreEqual("sample", response.Group);
            StringAssert.Contains("read boom", response.Message);
        }

        [Test]
        public void Apply_DryRun_DoesNotMutate_ButReportsReloadSignal()
        {
            var setting = 10;

            var response = ProjectSettingsCommand.Apply(
                group: "sample",
                confirm: true,           // irrelevant for a dry run
                dryRun: true,
                auditParams: new { value = 20 },
                plan: () => "set value 10 -> 20",
                apply: () => setting = 20,
                readValues: () => new Dictionary<string, object> { ["value"] = setting },
                requiresDomainReload: true);

            Assert.AreEqual(10, setting, "dry run must not mutate the setting");
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.DryRun);
            Assert.IsFalse(response.Applied);
            Assert.IsTrue(response.RequiresDomainReload, "dry run should still report the reload signal");
            Assert.AreEqual(10, response.Values["value"], "values should reflect the unchanged state");
        }

        [Test]
        public void Apply_MissingConfirm_IsRejected_DoesNotMutate()
        {
            var setting = 10;

            var response = ProjectSettingsCommand.Apply(
                group: "sample",
                confirm: false,
                dryRun: false,
                auditParams: null,
                plan: () => "set value 10 -> 20",
                apply: () => setting = 20,
                readValues: () => new Dictionary<string, object> { ["value"] = setting },
                requiresDomainReload: true);

            Assert.AreEqual(10, setting, "an unconfirmed write must not mutate");
            Assert.IsFalse(response.Success);
            Assert.IsFalse(response.Applied);
            Assert.IsFalse(response.RequiresDomainReload, "refused operations should not report side-effect signals");
            StringAssert.Contains("confirm=true", response.Message);
        }

        [Test]
        public void Apply_Confirmed_Mutates_ReadBackReflectsChange()
        {
            var setting = 10;

            var response = ProjectSettingsCommand.Apply(
                group: "sample",
                confirm: true,
                dryRun: false,
                auditParams: new { value = 20 },
                plan: () => "set value 10 -> 20",
                apply: () => setting = 20,
                readValues: () => new Dictionary<string, object> { ["value"] = setting },
                requiresDomainReload: true);

            Assert.AreEqual(20, setting, "a confirmed write must mutate");
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Applied);
            Assert.IsFalse(response.DryRun);
            Assert.IsTrue(response.RequiresDomainReload);
            Assert.AreEqual(20, response.Values["value"], "read-back should reflect the applied change");
        }
    }
}
