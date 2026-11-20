using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Testing;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Commands for running Unity tests programmatically
    /// </summary>
    static class TestCommands
    {
        const string Group = "test";

        /// <summary>
        /// Execute Unity tests with filtering options.
        /// Synchronous by default: it returns once the run ends, writing one record per test that ran;
        /// failing tests, or none matching, fail the command. With --async-tests it starts the run and
        /// writes nothing; test status has the results. PlayMode and all, whose domain reload drops the
        /// request, run only asynchronously.
        /// </summary>
        [CliGroupCommand(Group, "Execute Unity tests with filtering options", MainThreadRequired = true, Tags = new[] { "tests" }, Effect = CommandEffect.Additive)]
        internal static async Task<ExitCode> Run(CommandContext cmd,
            [CliArg("mode", "Test mode: editor, playmode or all (default: editor). playmode and all need --async-tests.")] string mode = "editor",
            [CliArg("filter", "Test name filter pattern (case-insensitive partial match)")] string filter = "",
            [CliArg("filter-type", "Filter type: TestName, Assembly, Category (case-insensitive; default: TestName)")] TestFilterType filterType = TestFilterType.TestName,
            [CliArg("include-explicit", "Include tests marked with [Explicit] attribute")] bool includeExplicit = false,
            [CliArg("async-tests", "Start the run and return at once with no results; poll test status for them")] bool asyncTests = false,
            [CliArg("timeout", "Test execution timeout in seconds (default: 600)")] double timeoutInSeconds = 600)
        {
            var run = await PipelineTestRunner.ExecuteTestsAsync(
                mode,
                filter,
                filterType,
                includeExplicit,
                asyncTests,
                PipelineTimeout.FromSeconds(timeoutInSeconds));

            // The per-test results are written on failure too: they say which tests failed and why.
            if (run.Results != null)
            {
                foreach (var result in run.Results)
                    cmd.Result.WriteRecord(result);
            }

            var summary = run.Summary;
            if (summary != null && summary.Total > 0)
                cmd.Log.Info($"{run.Mode}: {summary.Passed}/{summary.Total} passed, {summary.Failed} failed, " +
                    $"{summary.Skipped} skipped, {summary.Inconclusive} inconclusive in {run.Duration}s.");
            if (!string.IsNullOrEmpty(run.Message))
                cmd.Log.Info(run.Message);

            if (run.Success)
                return ExitCode.Success;

            cmd.Log.Error(string.IsNullOrEmpty(run.ErrorDetails) ? run.Error : $"{run.Error}: {run.ErrorDetails}");
            return run.ErrorCode == TestRunVerdict.InvalidArgsCode ? ExitCode.InvalidUsage : ExitCode.Failed;
        }

        /// <summary>
        /// List all available tests without executing any of them.
        /// Enumerates the test tree via TestRunnerApi.RetrieveTestList for the requested mode(s).
        /// </summary>
        [CliGroupCommand(Group, "List all available tests (EditMode and/or PlayMode) without running them", MainThreadRequired = true, Tags = new[] { "tests" }, Effect = CommandEffect.ReadOnly)]
        internal static async Task<ExitCode> List(CommandContext cmd,
            [CliArg("mode", "Test mode: all, editor, playmode (default: all)")] string mode = "all")
        {
            if (!TryGetModes(mode, out var modes))
            {
                cmd.Log.Error($"Invalid mode '{mode}'. Use 'editor', 'playmode', or 'all'.");
                return ExitCode.InvalidUsage;
            }

            var count = 0;
            foreach (var testMode in modes)
            {
                cmd.CancellationToken.ThrowIfCancellationRequested();
                foreach (var test in await RetrieveTestsAsync(testMode))
                {
                    cmd.Result.WriteRecord(test);
                    count++;
                }
            }

            cmd.Log.Info($"Found {count} test(s).");
            return ExitCode.Success;
        }

        /// <summary>
        /// Get current test status for async test execution
        /// </summary>
        [CliGroupCommand(Group, "Get status of running async test execution", MainThreadRequired = false, Tags = new[] { "tests" }, Effect = CommandEffect.ReadOnly)]
        [ImmediateCommand]
        internal static ExitCode Status(CommandContext cmd,
            [CliArg("detail", "full (default) for every test's result; failures for only the failed tests' results; summary for the counts and no per-test results.")] string detail = null)
        {
            var keep = ResultsFilter(detail);
            if (keep == null)
            {
                cmd.Log.Error($"Invalid detail '{detail}'. Use 'full', 'failures' or 'summary'.");
                return ExitCode.InvalidUsage;
            }

            // Judged on every result, so a failed run names its failed tests whatever the detail.
            var status = RawStatus();
            var code = TestRunVerdict.JudgeStatus(status, out var error);
            if ((status as JObject)?["results"] is JArray results)
                results.ReplaceAll(results.Where(keep).ToList());
            cmd.Result.Write(status);

            if (code != ExitCode.Success)
                cmd.Log.Error(error);
            return code;
        }

        /// <summary>Which results a <c>detail</c> keeps, or null for a value it doesn't know.</summary>
        static Func<JToken, bool> ResultsFilter(string detail)
        {
            switch (detail?.ToLowerInvariant())
            {
                case null:
                case "":
                case "full":
                    return r => true;
                case "failures":
                    return r => (string)r["status"] == "Failed";
                case "summary":
                    return r => false;
                default:
                    return null;
            }
        }

        /// <summary>The asynchronous test run's status payload, unjudged: it always carries a status field.</summary>
        internal static JToken RawStatus()
        {
            var status = PipelineTestRunner.GetTestStatus();
            if (status == null)
                return new JObject { ["status"] = "no_tests", ["message"] = "No test run in progress" };

            var parsed = StatusFileReader.Parse(status);
            CamelCaseResults(parsed);
            return parsed;
        }

        /// <summary>The PascalCase result keys of a status file an earlier package wrote.</summary>
        static readonly string[] k_PascalCaseResultKeys = { "FullName", "Status", "Duration", "Message", "StackTrace" };

        /// <summary>
        /// Renames the PascalCase keys of a status file's results to camelCase, in place. A status
        /// file outlives a package update, so one written by an earlier package still names them so.
        /// </summary>
        static void CamelCaseResults(JToken status)
        {
            if (!((status as JObject)?["results"] is JArray results))
                return;
            foreach (var result in results.OfType<JObject>())
            {
                foreach (var key in k_PascalCaseResultKeys)
                {
                    var property = result.Property(key, StringComparison.Ordinal);
                    var camel = char.ToLowerInvariant(key[0]) + key.Substring(1);
                    if (property != null && result.Property(camel, StringComparison.Ordinal) == null)
                        property.Replace(new JProperty(camel, property.Value));
                }
            }
        }

        /// <summary>
        /// Cancel running test execution
        /// </summary>
        [CliGroupCommand(Group, "Cancel running test execution", MainThreadRequired = true, Tags = new[] { "tests" }, Effect = CommandEffect.Additive)]
        internal static ExitCode Cancel(CommandContext cmd)
        {
            var cancelled = PipelineTestRunner.CancelTests();
            if (cancelled.Status == "unsupported")
                return CommandFailure.Fail(cmd, ExitCode.Failed, cancelled.Message);
            cmd.Log.Info(cancelled.Message);
            cmd.Result.Write(new { status = cancelled.Status });
            return ExitCode.Success;
        }

        /// <summary>
        /// Resolve a mode string to the concrete TestMode(s) to enumerate.
        /// </summary>
        private static bool TryGetModes(string mode, out List<TestMode> modes)
        {
            switch (mode?.ToLowerInvariant())
            {
                case null:
                case "":
                case "all":
                    modes = new List<TestMode> { TestMode.EditMode, TestMode.PlayMode };
                    return true;
                case "editor":
                case "editmode":
                    modes = new List<TestMode> { TestMode.EditMode };
                    return true;
                case "playmode":
                case "play":
                    modes = new List<TestMode> { TestMode.PlayMode };
                    return true;
                default:
                    modes = null;
                    return false;
            }
        }

        /// <summary>
        /// Retrieve the test list for a single mode. RetrieveTestList enumerates the test tree (no
        /// execution) and invokes the callback on the main thread; we bridge it to a Task.
        /// </summary>
        private static Task<List<TestListItem>> RetrieveTestsAsync(TestMode testMode)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var tcs = new TaskCompletionSource<List<TestListItem>>();

            api.RetrieveTestList(testMode, root =>
            {
                try
                {
                    var items = new List<TestListItem>();
                    CollectLeafTests(root, testMode, items);
                    tcs.SetResult(items);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Walk the test tree and collect leaf tests (actual test methods, not suites/fixtures).
        /// </summary>
        private static void CollectLeafTests(ITestAdaptor test, TestMode testMode, List<TestListItem> items)
        {
            if (test == null)
                return;

            if (!test.HasChildren && !test.IsSuite)
            {
                items.Add(new TestListItem
                {
                    FullName = test.FullName,
                    Mode = testMode.ToString(),
                    Assembly = test.TypeInfo?.Assembly?.GetName()?.Name,
                    Categories = test.Categories?.ToList() ?? new List<string>(),
                    Explicit = test.RunState == RunState.Explicit
                });
                return;
            }

            if (test.Children != null)
            {
                foreach (var child in test.Children)
                    CollectLeafTests(child, testMode, items);
            }
        }
    }

    /// <summary>
    /// A single available test (no run state / outcome — this is a listing, not a result).
    /// </summary>
    [Serializable]
    class TestListItem
    {
        public string FullName { get; set; }
        public string Mode { get; set; }
        public string Assembly { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public bool Explicit { get; set; }
    }
}
