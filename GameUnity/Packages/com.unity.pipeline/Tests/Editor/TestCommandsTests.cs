using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the test commands (run_tests, test_status, cancel_tests): input validation and
    /// registration/discovery. Does not execute a real test run (that would be self-referential).
    /// </summary>
    public class TestCommandsTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }

        #region Behavior

        [Test]
        public async Task RunTests_InvalidMode_ReturnsError()
        {
            // RunTests returns a structured failure (Success=false + Error) rather than throwing,
            // so the client receives the full response.
            var result = await TestCommands.RunTests(mode: "invalid_mode");
            Assert.IsFalse(result.Success, "Invalid mode should fail");
            Assert.IsTrue(result.Error.Contains("Invalid mode"), $"Error should mention invalid mode: {result.Error}");
        }

        [Test]
        public async Task RunTests_InvalidFilterType_ReturnsError()
        {
            var result = await TestCommands.RunTests(filter: "x", filterType: "invalid_filter_type");
            Assert.IsFalse(result.Success, "Invalid filterType should fail");
            Assert.IsTrue(result.Error.Contains("Invalid filterType"), $"Error should mention invalid filterType: {result.Error}");
        }

        [Test]
        public void GetTestStatus_ReturnsJsonWithStatus()
        {
            var status = TestCommands.GetTestStatus();
            Assert.IsNotNull(status);
            Assert.IsTrue(status.Contains("status"));
        }

        #endregion

        #region list_tests

        [UnityTest]
        public IEnumerator ListTests_Editor_ReturnsTestsWithoutRunning()
        {
            var task = TestCommands.ListTests("editor");
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.IsNull(task.Exception, task.Exception?.ToString());
            var response = task.Result;

            Assert.IsTrue(response.Success, response.Error);
            Assert.Greater(response.Count, 0, "Should list at least one EditMode test");
            Assert.AreEqual(response.Count, response.Tests.Count);
            Assert.IsTrue(response.Tests.All(t => t.Mode == "EditMode"), "Editor mode should only list EditMode tests");
            Assert.IsTrue(response.Tests.All(t => !string.IsNullOrEmpty(t.FullName)), "Every entry should have a full name");

            // This very test class is an EditMode test, so it must appear in the listing.
            Assert.IsTrue(response.Tests.Any(t => t.FullName.Contains("TestCommandsTests")),
                "Listing should include this test class");
        }

        [UnityTest]
        public IEnumerator ListTests_All_IsSupersetOfEditorAndIncludesEditMode()
        {
            var editorTask = TestCommands.ListTests("editor");
            yield return new WaitUntil(() => editorTask.IsCompleted);

            var allTask = TestCommands.ListTests("all");
            yield return new WaitUntil(() => allTask.IsCompleted);

            Assert.IsNull(allTask.Exception, allTask.Exception?.ToString());
            var all = allTask.Result;

            Assert.IsTrue(all.Success, all.Error);
            Assert.AreEqual("All", all.Mode);
            Assert.GreaterOrEqual(all.Count, editorTask.Result.Count, "'all' should include at least the EditMode tests");
            Assert.IsTrue(all.Tests.Any(t => t.Mode == "EditMode"), "'all' should include EditMode tests");
        }

        [UnityTest]
        public IEnumerator ListTests_InvalidMode_ReturnsError()
        {
            var task = TestCommands.ListTests("bogus");
            yield return new WaitUntil(() => task.IsCompleted);

            var response = task.Result;
            Assert.IsFalse(response.Success);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(0, response.Tests.Count);
        }

        #endregion

        #region Registration

        [Test]
        public void TestCommands_AreDiscovered_WithCorrectThreadingFlags()
        {
            var cmds = CommandRegistry.DiscoverCommands();

            var run = cmds.FirstOrDefault(c => c.Name == "run_tests");
            var status = cmds.FirstOrDefault(c => c.Name == "test_status");
            var cancel = cmds.FirstOrDefault(c => c.Name == "cancel_tests");
            var list = cmds.FirstOrDefault(c => c.Name == "list_tests");

            Assert.IsNotNull(run, "run_tests should be discovered");
            Assert.IsTrue(run.MainThreadRequired, "run_tests requires main thread");
            Assert.IsNotNull(status, "test_status should be discovered");
            Assert.IsFalse(status.MainThreadRequired, "test_status does not require main thread");
            Assert.IsNotNull(cancel, "cancel_tests should be discovered");
            Assert.IsTrue(cancel.MainThreadRequired, "cancel_tests requires main thread");
            Assert.IsNotNull(list, "list_tests should be discovered");
            Assert.IsTrue(list.MainThreadRequired, "list_tests requires main thread");
        }

        [Test]
        public void RunTests_HasExpectedParametersAndDefaults()
        {
            var run = CommandRegistry.DiscoverCommands().FirstOrDefault(c => c.Name == "run_tests");
            Assert.IsNotNull(run);

            var names = run.Parameters.Select(p => p.Name).ToList();
            foreach (var n in new[] { "mode", "filter", "filter_type", "include_explicit", "async_tests", "timeout" })
                Assert.Contains(n, names, $"run_tests should have '{n}' parameter");

            Assert.AreEqual("all", run.Parameters.First(p => p.Name == "mode").DefaultValue);
            Assert.AreEqual(300, run.Parameters.First(p => p.Name == "timeout").DefaultValue);
        }

        #endregion
    }
}
