using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Newtonsoft.Json;

namespace Unity.Pipeline.Editor.Testing
{
    /// <summary>
    /// Core test execution engine supporting both synchronous and asynchronous execution modes.
    /// Adapted from unity-tools with domain reload handling and dual mode support.
    /// </summary>
    public static class PipelineTestRunner
    {
        private const string TestRequestFile = "Temp/pipeline_test_request.json";
        private const string TestStatusFile = "Temp/pipeline_test_status.json";

        private static TestRunnerApi m_ActiveApi;
        private static TestResultCollector m_ActiveCollector;

        /// <summary>
        /// Execute tests with the given parameters
        /// </summary>
        public static async Task<TestExecutionResponse> ExecuteTestsAsync(
            string mode,
            string filter,
            string filterType,
            bool includeExplicit,
            bool asyncMode,
            int timeoutSeconds)
        {
            try
            {
                var parsedMode = ParseTestMode(mode);
                if (!parsedMode.IsValid)
                {
                    return CreateErrorResponse($"Invalid mode '{mode}'. Use 'editor', 'playmode', or 'all'");
                }

                // Validate filter type
                var normalizedFilterType = filterType?.ToLower() ?? "testname";
                if (!string.IsNullOrEmpty(filter) &&
                    normalizedFilterType != "testname" && normalizedFilterType != "assembly" && normalizedFilterType != "category")
                {
                    return CreateErrorResponse($"Invalid filterType '{filterType}'. Valid options: testName, assembly, category");
                }

                // Cancel any previous run
                InvalidatePreviousRun();

                if (parsedMode.IsAll)
                {
                    // Handle "all" mode by running both editor and playmode tests
                    return await ExecuteAllModesAsync(filter, normalizedFilterType, includeExplicit, asyncMode, timeoutSeconds);
                }
                else if (asyncMode)
                {
                    return await ExecuteAsyncMode(parsedMode.TestMode, filter, normalizedFilterType, includeExplicit, timeoutSeconds);
                }
                else if (parsedMode.TestMode == TestMode.PlayMode)
                {
                    // PlayMode entry triggers a domain reload that drops the in-flight HTTP request,
                    // so a synchronous playmode run can never return results over one call. Fail fast
                    // with guidance instead of hanging until the connection resets.
                    return CreateErrorResponse(
                        "PlayMode tests cannot run synchronously over HTTP: entering play mode triggers a " +
                        "domain reload that drops the request. Re-run asynchronously (run_tests --mode " +
                        "playmode --async_tests) and poll the test_status command for results.");
                }
                else
                {
                    return await ExecuteSyncMode(parsedMode.TestMode, filter, normalizedFilterType, includeExplicit, timeoutSeconds);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Error: {ex.Message}");
                return CreateErrorResponse($"Failed to run tests: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute tests in all modes (editor + playmode)
        /// </summary>
        private static async Task<TestExecutionResponse> ExecuteAllModesAsync(
            string filter,
            string filterType,
            bool includeExplicit,
            bool asyncMode,
            int timeoutSeconds)
        {
            if (asyncMode)
            {
                return CreateErrorResponse("Async mode is not supported for 'all' mode. Use 'editor' or 'playmode' for async execution.");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                // EditMode runs synchronously over HTTP (no domain reload), so its results are
                // returned in this response.
                Debug.Log("[PipelineTestRunner] Running editor tests (all mode)...");
                var editorResponse = await ExecuteSyncMode(TestMode.EditMode, filter, filterType, includeExplicit, timeoutSeconds);

                if (!editorResponse.Success)
                {
                    return editorResponse; // Return error immediately
                }

                // PlayMode can't run synchronously over HTTP (entering play mode triggers a domain
                // reload that drops the request), so start it asynchronously and hand back a
                // StatusPath. The caller polls the test_status command for the playmode results.
                Debug.Log("[PipelineTestRunner] Starting playmode tests asynchronously (all mode)...");
                var playmodeStart = await ExecuteAsyncMode(TestMode.PlayMode, filter, filterType, includeExplicit, timeoutSeconds);

                if (!playmodeStart.Success)
                {
                    return playmodeStart; // Return error immediately
                }

                // Build combined response: editor results inline, playmode running async.
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                return new TestExecutionResponse
                {
                    Success = true,
                    Command = "run_tests",
                    Duration = Math.Round(duration, 2),
                    Mode = "All",
                    FilterApplied = string.IsNullOrEmpty(filter) ? null : $"{filterType}: {filter}",
                    ExecutionTimeMs = (int)(duration * 1000),
                    Summary = editorResponse.Summary,
                    Results = editorResponse.Results,
                    Result = "playmode_running",
                    StatusPath = playmodeStart.StatusPath,
                    Message = $"EditMode complete: {editorResponse.Summary.Passed}/{editorResponse.Summary.Total} passed. " +
                              "PlayMode tests started asynchronously — poll the test_status command for their results."
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] All modes execution error: {ex.Message}");
                return CreateErrorResponse($"Failed to execute tests in all modes: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronous execution - blocks until completion
        /// </summary>
        private static async Task<TestExecutionResponse> ExecuteSyncMode(
            TestMode testMode,
            string filter,
            string filterType,
            bool includeExplicit,
            int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var collector = new TestResultCollector();

            m_ActiveApi = api;
            m_ActiveCollector = collector;

            // Set up timeout cancellation
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                cts.Token.Register(() =>
                {
                    Debug.Log("[PipelineTestRunner] Test execution timed out, cancelling...");
                    InvalidatePreviousRun();
                });

                try
                {
                    // For PlayMode tests, we need to handle domain reloads
                    if (testMode == TestMode.PlayMode)
                    {
                        // Save request for domain reload recovery
                        var request = new TestRequest
                        {
                            filter = filter,
                            filterType = filterType,
                            mode = testMode.ToString(),
                            includeExplicit = includeExplicit,
                            isSync = true
                        };
                        File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));
                    }

                    // Execute tests and wait for completion
                    var completionTask = ExecuteTestsInternal(api, collector, testMode, filter, filterType, includeExplicit);
                    var resultAdaptor = await completionTask;

                    // Build response
                    var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                    var response = new TestExecutionResponse
                    {
                        Success = true,
                        Command = "run_tests",
                        Duration = Math.Round(duration, 2),
                        Mode = testMode.ToString(),
                        FilterApplied = string.IsNullOrEmpty(filter) ? null : $"{filterType}: {filter}",
                        ExecutionTimeMs = (int)(duration * 1000),
                        Summary = resultAdaptor != null ? new TestSummary
                        {
                            Total = resultAdaptor.PassCount + resultAdaptor.FailCount + resultAdaptor.SkipCount + resultAdaptor.InconclusiveCount,
                            Passed = resultAdaptor.PassCount,
                            Failed = resultAdaptor.FailCount,
                            Skipped = resultAdaptor.SkipCount,
                            Inconclusive = resultAdaptor.InconclusiveCount
                        } : new TestSummary(),
                        Results = collector.Results
                    };

                    // Clean up PlayMode request file
                    CleanupRequestFile();

                    return response;
                }
                catch (OperationCanceledException)
                {
                    return CreateErrorResponse("Test execution timed out");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PipelineTestRunner] Sync execution error: {ex.Message}");
                    CleanupRequestFile();
                    return CreateErrorResponse($"Test execution failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Asynchronous execution - returns immediately
        /// </summary>
        // Not async: the work here is synchronous (file I/O + a main-thread call that returns
        // immediately). Returns a completed Task so callers can still await it.
        private static Task<TestExecutionResponse> ExecuteAsyncMode(
            TestMode testMode,
            string filter,
            string filterType,
            bool includeExplicit,
            int timeoutSeconds)
        {
            try
            {
                // Save request to disk (survives domain reloads for PlayMode)
                var request = new TestRequest
                {
                    filter = filter,
                    filterType = filterType,
                    mode = testMode.ToString(),
                    includeExplicit = includeExplicit,
                    isSync = false
                };
                File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));

                // Clear any previous status
                if (File.Exists(TestStatusFile))
                    File.Delete(TestStatusFile);

                // Start test execution now, on the main thread (run_tests is MainThreadRequired,
                // so we are inside the editor update tick that dispatched this command). Must NOT
                // run on a background thread: the test runner creates a TestRunnerApi via
                // ScriptableObject.CreateInstance, which is main-thread-only. RetrieveTestList
                // returns immediately, so the HTTP "running" response below is still sent promptly.
                StartAsyncTestExecution(testMode, filter, filterType, includeExplicit);

                return Task.FromResult(new TestExecutionResponse
                {
                    Success = true,
                    Command = "run_tests",
                    Result = "running",
                    StatusPath = TestStatusFile,
                    Mode = testMode.ToString(),
                    FilterApplied = string.IsNullOrEmpty(filter) ? null : $"{filterType}: {filter}",
                    Message = "Tests started in async mode. Poll /test-status for results."
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Async setup error: {ex.Message}");
                return Task.FromResult(CreateErrorResponse($"Failed to start async test execution: {ex.Message}"));
            }
        }

        /// <summary>
        /// Internal test execution logic
        /// </summary>
        private static async Task<ITestResultAdaptor> ExecuteTestsInternal(
            TestRunnerApi api,
            TestResultCollector collector,
            TestMode testMode,
            string filter,
            string filterType,
            bool includeExplicit)
        {
            var tcs = new TaskCompletionSource<ITestResultAdaptor>();

            // Retrieve test list and filter
            api.RetrieveTestList(testMode, (ITestAdaptor rootTest) =>
            {
                try
                {
                    var testNames = CollectTestNames(rootTest, filter, filterType, includeExplicit);

                    if (testNames.Count == 0)
                    {
                        Debug.Log("[PipelineTestRunner] No tests to run after filtering");
                        var emptyResult = CreateEmptyTestResult();
                        tcs.SetResult(emptyResult);
                        return;
                    }

                    Debug.Log($"[PipelineTestRunner] Running {testNames.Count} tests (explicit tests {(includeExplicit ? "included" : "excluded")})");

                    var testFilter = new Filter
                    {
                        testMode = testMode,
                        testNames = testNames.ToArray()
                    };

                    // Set up completion handling
                    collector.OnRunFinished = () => tcs.SetResult(collector.RootResult);

                    api.RegisterCallbacks(collector);
                    api.Execute(new ExecutionSettings(testFilter));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PipelineTestRunner] Error during test execution: {ex.Message}");
                    tcs.SetException(ex);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Start async test execution on the main thread. Registers callbacks and kicks off the
        /// run, then returns; completion is reported by the collector writing the status file.
        /// Unlike the sync path, this does NOT reuse ExecuteTestsInternal (which sets its own
        /// OnRunFinished to complete a Task, clobbering ours), and does NOT await.
        /// </summary>
        private static void StartAsyncTestExecution(TestMode testMode, string filter, string filterType, bool includeExplicit)
        {
            try
            {
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var collector = new TestResultCollector();

                m_ActiveApi = api;
                m_ActiveCollector = collector;

                // Async completion: write results for pollers. Set once, never overwritten.
                collector.OnRunFinished = () =>
                {
                    WriteCompletedResults(collector);
                    RestoreLiveServerIfDisrupted(); // B2: self-heal if a test broke the live server.
                    CleanupRequestFile();
                    m_ActiveApi = null;
                    m_ActiveCollector = null;
                };

                api.RetrieveTestList(testMode, (ITestAdaptor rootTest) =>
                {
                    try
                    {
                        var testNames = CollectTestNames(rootTest, filter, filterType, includeExplicit);

                        if (testNames.Count == 0)
                        {
                            WriteStatusFile(new
                            {
                                status = "completed",
                                duration = 0.0,
                                summary = new { total = 0, passed = 0, failed = 0, skipped = 0, inconclusive = 0 },
                                results = new object[0]
                            });
                            CleanupRequestFile();
                            m_ActiveApi = null;
                            m_ActiveCollector = null;
                            return;
                        }

                        api.RegisterCallbacks(collector);
                        api.Execute(new ExecutionSettings(new Filter
                        {
                            testMode = testMode,
                            testNames = testNames.ToArray()
                        }));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PipelineTestRunner] Async run setup error: {ex.Message}");
                        WriteStatusFile(new { status = "error", message = ex.Message });
                        CleanupRequestFile();
                        m_ActiveApi = null;
                        m_ActiveCollector = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Async execution error: {ex.Message}");
                WriteStatusFile(new { status = "error", message = ex.Message });
                CleanupRequestFile();
                m_ActiveApi = null;
                m_ActiveCollector = null;
            }
        }

        /// <summary>
        /// Re-register a result collector to capture the completion of a PlayMode run that the
        /// Unity Test Framework auto-resumes after a play-mode domain reload (see TestJobDataHolder
        /// in the test-framework package). The previous collector did not survive the reload.
        /// Deliberately does NOT call RetrieveTestList/api.Execute: the run is already in flight, so
        /// starting another one double-executes the task list (running scene tasks during play
        /// mode) and trips the runner's "too many instant steps" guard. The collector rebuilds its
        /// per-test results from the RootResult tree at RunFinished, since the incremental
        /// TestFinished callbacks were delivered to a collector from the previous domain.
        /// </summary>
        private static void ReattachResultCollector()
        {
            try
            {
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var collector = new TestResultCollector();

                m_ActiveApi = api;
                m_ActiveCollector = collector;

                collector.OnRunFinished = () =>
                {
                    WriteCompletedResults(collector);
                    RestoreLiveServerIfDisrupted();
                    CleanupRequestFile();
                    m_ActiveApi = null;
                    m_ActiveCollector = null;
                };

                api.RegisterCallbacks(collector);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to reattach result collector: {ex.Message}");
                WriteStatusFile(new { status = "error", message = ex.Message });
                CleanupRequestFile();
                m_ActiveApi = null;
                m_ActiveCollector = null;
            }
        }

        /// <summary>
        /// Called after domain reload to resume pending tests
        /// </summary>
        [InitializeOnLoad]
        public static class TestReloader
        {
            static TestReloader()
            {
                EditorApplication.delayCall += CheckForPendingTests;
            }
        }

        public static void CheckForPendingTests()
        {
            if (!File.Exists(TestRequestFile)) return;
            if (File.Exists(TestStatusFile)) return; // Already completed

            try
            {
                var json = File.ReadAllText(TestRequestFile);
                var request = JsonConvert.DeserializeObject<TestRequest>(json);

                Debug.Log("[PipelineTestRunner] Reattaching result collector after domain reload");
                if (request.isSync)
                {
                    // For sync mode, we can't really resume after domain reload
                    // The command has already timed out or failed
                    CleanupRequestFile();
                    return;
                }

                // The Unity Test Framework (TestJobDataHolder) auto-resumes the in-flight PlayMode
                // run after a play-mode domain reload. We must NOT start a new run here: re-calling
                // api.Execute double-starts the job, which re-runs the task list (executing scene
                // tasks during play mode) and trips the runner's "too many instant steps" guard.
                // We only need to re-register a collector — our previous one did not survive the
                // reload — to capture the resumed run's completion. Runs on the main thread because
                // CheckForPendingTests is invoked via EditorApplication.delayCall.
                ReattachResultCollector();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to resume tests: {ex.Message}");
                WriteStatusFile(new { status = "error", message = $"Failed to resume tests: {ex.Message}" });
                CleanupRequestFile();
            }
        }

        #region Utility Methods

        private struct ParsedTestMode
        {
            public bool IsValid { get; set; }
            public bool IsAll { get; set; }
            public TestMode TestMode { get; set; }

            public static ParsedTestMode Invalid => new ParsedTestMode { IsValid = false };
            public static ParsedTestMode All => new ParsedTestMode { IsValid = true, IsAll = true };
            public static ParsedTestMode Single(TestMode mode) => new ParsedTestMode { IsValid = true, IsAll = false, TestMode = mode };
        }

        private static ParsedTestMode ParseTestMode(string mode)
        {
            switch (mode?.ToLower())
            {
                case "editor":
                case "editmode":
                    return ParsedTestMode.Single(TestMode.EditMode);
                case "playmode":
                case "play":
                    return ParsedTestMode.Single(TestMode.PlayMode);
                case "all":
                    return ParsedTestMode.All;
                default:
                    return ParsedTestMode.Invalid;
            }
        }

        private static List<string> CollectTestNames(ITestAdaptor test, string filter, string filterType, bool includeExplicit)
        {
            var testNames = new List<string>();
            CollectTestNamesRecursive(test, testNames, filter, filterType, includeExplicit);
            return testNames;
        }

        private static void CollectTestNamesRecursive(ITestAdaptor test, List<string> testNames, string filter, string filterType, bool includeExplicit)
        {
            if (test == null) return;

            // Skip [Explicit] tests AND [Explicit] fixtures (the whole subtree) unless includeExplicit.
            // Checked at EVERY node — including suite/fixture nodes — because a class-level [Explicit]
            // is not reliably propagated to [UnityTest] leaf RunState by Unity's adaptor; honoring the
            // declaring type's attribute here is what makes class-level [Explicit] actually exclude.
            if (!includeExplicit && IsExplicit(test))
                return;

            // Only collect leaf tests (actual test methods, not fixtures/suites)
            if (!test.HasChildren && test.IsSuite == false)
            {
                // Apply user filter
                if (!ShouldIncludeTest(test, filter, filterType))
                    return;

                testNames.Add(test.FullName);
            }

            // Recurse into children
            if (test.Children != null)
            {
                foreach (var child in test.Children)
                {
                    CollectTestNamesRecursive(child, testNames, filter, filterType, includeExplicit);
                }
            }
        }

        /// <summary>
        /// True if the test, its method, or its declaring fixture type is marked [Explicit].
        /// Class-level [Explicit] must be honored here because Unity does not always propagate it
        /// to leaf RunState (notably for [UnityTest]).
        /// </summary>
        private static bool IsExplicit(ITestAdaptor test)
        {
            if (test.RunState == RunState.Explicit)
                return true;

            var method = test.Method?.MethodInfo;
            if (method != null && method.GetCustomAttributes(true).Any(a => a.GetType().Name == "ExplicitAttribute"))
                return true;

            var type = test.TypeInfo?.Type;
            if (type != null && type.GetCustomAttributes(true).Any(a => a.GetType().Name == "ExplicitAttribute"))
                return true;

            return false;
        }

        private static bool ShouldIncludeTest(ITestAdaptor test, string filter, string filterType)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            switch (filterType)
            {
                case "assembly":
                    var assemblyName = test.TypeInfo?.Assembly?.GetName()?.Name;
                    return assemblyName != null &&
                           assemblyName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                case "testname":
                    return test.FullName != null &&
                           test.FullName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                case "category":
                    return test.Categories != null &&
                           test.Categories.Any(c => c.Equals(filter, StringComparison.OrdinalIgnoreCase));

                default:
                    return true;
            }
        }

        private static void InvalidatePreviousRun()
        {
            if (m_ActiveCollector != null)
            {
                m_ActiveCollector.Cancel();

                if (m_ActiveApi != null)
                {
                    try { m_ActiveApi.UnregisterCallbacks(m_ActiveCollector); }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PipelineTestRunner] Could not unregister old collector: {ex.Message}");
                    }
                }

                Debug.Log("[PipelineTestRunner] Invalidated previous test run");
            }

            m_ActiveApi = null;
            m_ActiveCollector = null;
        }

        private static TestExecutionResponse CreateErrorResponse(string message)
        {
            return new TestExecutionResponse
            {
                Success = false,
                Command = "run_tests",
                Error = message,
                Results = new List<TestResult>(),
                Summary = new TestSummary()
            };
        }

        private static ITestResultAdaptor CreateEmptyTestResult()
        {
            // This is a bit of a hack since ITestResultAdaptor is an interface
            // In practice, this case should be rare (no tests matching filter)
            return null;
        }

        /// <summary>
        /// B2 self-heal: after a test run, if a test left the live editor server disrupted (its
        /// listener died, or its discovery descriptor was deleted/clobbered), restart it so the
        /// dogfood loop and subsequent commands keep working instead of wedging the session. No-op
        /// when the server is healthy. Runs at run completion — the watchdog is disabled during the
        /// run (PipelineWatchdogTestGuard), so this is what revives the live server afterwards.
        /// </summary>
        private static void RestoreLiveServerIfDisrupted()
        {
            try
            {
                var server = PipelineServerStartup.Server;
                var root = Path.GetDirectoryName(Application.dataPath);
                var descriptor = Unity.Pipeline.Models.InstanceDescriptor.ReadFromProjectRoot(root);

                var disrupted = server == null
                                || !server.IsRunning
                                || descriptor == null
                                || descriptor.Port != server.Port;

                if (disrupted)
                {
                    Debug.Log("[PipelineTestRunner] Live server was disrupted during the test run — restarting it.");
                    PipelineServerStartup.RestartServer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PipelineTestRunner] Live server restore check failed: {ex.Message}");
            }
        }

        private static void WriteCompletedResults(TestResultCollector collector)
        {
            try
            {
                if (!collector.IsComplete)
                {
                    WriteStatusFile(new { status = "error", message = "Tests did not complete" });
                    return;
                }

                var root = collector.RootResult;
                var result = new
                {
                    status = "completed",
                    duration = Math.Round(root.Duration, 2),
                    summary = new
                    {
                        total = root.PassCount + root.FailCount + root.SkipCount + root.InconclusiveCount,
                        passed = root.PassCount,
                        failed = root.FailCount,
                        skipped = root.SkipCount,
                        inconclusive = root.InconclusiveCount
                    },
                    results = collector.Results.ToArray()
                };

                WriteStatusFile(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to write results: {ex.Message}");
                WriteStatusFile(new { status = "error", message = ex.Message });
            }
        }

        private static void WriteStatusFile(object data)
        {
            try
            {
                File.WriteAllText(TestStatusFile, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to write status file: {ex.Message}");
            }
        }

        private static void CleanupRequestFile()
        {
            try { if (File.Exists(TestRequestFile)) File.Delete(TestRequestFile); } catch { }
        }

        /// <summary>
        /// Get current test status (for async mode polling)
        /// </summary>
        public static string GetTestStatus()
        {
            if (File.Exists(TestStatusFile))
                return File.ReadAllText(TestStatusFile);
            if (File.Exists(TestRequestFile))
                return JsonConvert.SerializeObject(new { status = "running" });
            return null;
        }

        /// <summary>
        /// Cancel running tests
        /// </summary>
        public static object CancelTests()
        {
            if (m_ActiveCollector == null && !File.Exists(TestRequestFile) && !File.Exists(TestStatusFile))
                return new { status = "no_tests", message = "No test run in progress." };

            bool wasPlayMode = false;
            try
            {
                if (File.Exists(TestRequestFile))
                {
                    var json = File.ReadAllText(TestRequestFile);
                    wasPlayMode = json.Contains("PlayMode");
                }
            }
            catch { }

            InvalidatePreviousRun();
            WriteStatusFile(new { status = "cancelled", message = "Test run cancelled." });
            CleanupRequestFile();

            if (wasPlayMode && EditorApplication.isPlaying)
            {
                Debug.Log("[PipelineTestRunner] Exiting play mode to cancel PlayMode tests");
                EditorApplication.ExitPlaymode();
            }

            Debug.Log("[PipelineTestRunner] Test run cancelled");
            return new { status = "cancelled", message = "Test run cancelled." };
        }

        #endregion

        [Serializable]
        private class TestRequest
        {
            public string filter;
            public string filterType;
            public string mode;
            public bool includeExplicit;
            public bool isSync;
        }
    }
}

