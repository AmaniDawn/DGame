using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.Pipeline.Editor.Testing
{
    /// <summary>
    /// Core test execution engine supporting both synchronous and asynchronous execution modes.
    /// Adapted from unity-tools with domain reload handling and dual mode support.
    /// </summary>
#if UNITY_6000_5_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    static class PipelineTestRunner
    {
        // Consts are inlined at compile time, so PipelineTestReloader can read them without
        // loading this class (whose fields need UnityEditor.TestRunner to resolve).
        internal const string TestRequestFile = "Temp/pipeline_test_request.json";
        internal const string TestStatusFile = "Temp/pipeline_test_status.json";
        /// <summary>The EditMode half's results of an 'all' run, kept on disk while its PlayMode half runs.</summary>
        internal const string TestPartialFile = "Temp/pipeline_test_partial.json";

        internal static TestResultCollector m_ActiveCollector;

        /// <summary>The Test Framework's id for the run in flight, so it can be cancelled.</summary>
        internal static string s_ActiveRunId;

        internal static readonly string SyncRunInterruptedMessage =
            $"A domain reload interrupted a synchronous {CommandNames.TestRun}, so its results could not be returned " +
            $"(the editor may still be running the tests). Tests that recompile scripts or reload the domain need " +
            $"{CommandNames.TestRun} --mode editor --async-tests true, then poll {CommandNames.TestStatus} for the results.";

        /// <summary>Stops a Test Framework run by id. Tests replace it to see which run is stopped.</summary>
        internal static Func<string, bool> s_CancelTestRun = (Func<string, bool>)Delegate.CreateDelegate(
            typeof(Func<string, bool>), typeof(TestRunnerApi), "CancelTestRun", false, false);

        /// <summary>
        /// Execute tests with the given parameters
        /// </summary>
        /// <param name="mode">"editor"/"editmode", "playmode"/"play", or "all".</param>
        /// <param name="filter">Filter value to match against, per <paramref name="filterType"/>.</param>
        /// <param name="filterType">What <paramref name="filter"/> is matched against.</param>
        /// <param name="includeExplicit">Whether to include tests/fixtures marked [Explicit].</param>
        /// <param name="asyncMode">Run asynchronously, returning immediately with a status path to poll.</param>
        /// <param name="timeout">Timeout for synchronous execution.</param>
        /// <returns>The test execution response.</returns>
        public static async Task<TestExecutionResponse> ExecuteTestsAsync(
            string mode,
            string filter,
            TestFilterType filterType,
            bool includeExplicit,
            bool asyncMode,
            TimeSpan timeout)
        {
            try
            {
                if (!Enum.IsDefined(typeof(TestFilterType), filterType))
                {
                    return CreateUsageErrorResponse($"Invalid filter-type '{filterType}'. Use TestName, Assembly, or Category.");
                }

                var parsedMode = ParseTestMode(mode);
                if (!parsedMode.IsValid)
                {
                    return CreateUsageErrorResponse($"Invalid mode '{mode}'. Use 'editor', 'playmode', or 'all'.");
                }

                if (parsedMode.IsAll && !asyncMode)
                {
                    // 'all' includes PlayMode, which can't finish over one request (see below).
                    // Refuse it rather than return half the results.
                    return CreateUsageErrorResponse(
                        "'all' includes PlayMode tests, which cannot run synchronously over HTTP: entering play mode " +
                        $"triggers a domain reload that drops the request. Re-run asynchronously ({CommandNames.TestRun} --mode " +
                        $"all --async-tests) and poll the {CommandNames.TestStatus} command for results.");
                }

                if (!parsedMode.IsAll && !asyncMode && parsedMode.TestMode == TestMode.PlayMode)
                {
                    // PlayMode entry triggers a domain reload that drops the in-flight HTTP request,
                    // so a synchronous playmode run can never return results over one call. Fail fast
                    // with guidance instead of hanging until the connection resets.
                    return CreateUsageErrorResponse(
                        "PlayMode tests cannot run synchronously over HTTP: entering play mode triggers a " +
                        $"domain reload that drops the request. Re-run asynchronously ({CommandNames.TestRun} --mode " +
                        $"playmode --async-tests) and poll the {CommandNames.TestStatus} command for results.");
                }

                var normalizedFilterType = filterType.ToString().ToLowerInvariant();

                // Cancel any previous run
                if (s_CancelTestRun == null && (m_ActiveCollector != null || File.Exists(TestRequestFile)))
                    return CreateErrorResponse("A test run is active; this Test Framework cannot cancel it. Wait for test status.");
                InvalidatePreviousRun();

                if (asyncMode)
                {
                    // An 'all' run starts with its EditMode half; FinishAsyncRun starts the PlayMode half.
                    var firstMode = parsedMode.IsAll ? TestMode.EditMode : parsedMode.TestMode;
                    return await ExecuteAsyncMode(firstMode, filter, normalizedFilterType, includeExplicit, allModes: parsedMode.IsAll);
                }
                else
                {
                    var run = await ExecuteSyncMode(parsedMode.TestMode, filter, normalizedFilterType, includeExplicit, timeout);
                    return TestRunVerdict.Judge(run, zeroTestsFail: true);
                }
            }
            catch (Exception ex) when (!(ex is PipelineTimeoutException))
            {
                Debug.LogError($"[PipelineTestRunner] Error: {ex.Message}");
                return CreateErrorResponse($"Failed to run tests: {ex.Message}");
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
            TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var collector = new TestResultCollector();

            m_ActiveCollector = collector;

            // Set up timeout cancellation
            bool keepObserving = false;
            using (var cts = new CancellationTokenSource(PipelineTimeout.StopBefore(timeout)))
            {
                try
                {
                    // Record the run so that, if a test reloads the domain (dropping this request),
                    // CheckForPendingTests can tell the caller why through the test status command.
                    if (File.Exists(TestStatusFile))
                        File.Delete(TestStatusFile);
                    var request = new TestRequest
                    {
                        filter = filter,
                        filterType = filterType,
                        mode = testMode.ToString(),
                        includeExplicit = includeExplicit,
                        isSync = true
                    };
                    File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));

                    // Execute tests and wait for completion, or for the timeout: an invalidated
                    // collector never reports the run finished, so the timeout ends the wait.
                    var completionTask = ExecuteTestsInternal(api, collector, testMode, filter, filterType, includeExplicit);
                    var resultAdaptor = await AwaitUnlessCanceled(completionTask, cts.Token);

                    // Build response
                    var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                    var response = new TestExecutionResponse
                    {
                        Success = true,
                        Command = CommandNames.TestRun,
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
                        Results = collector.Results,
                        ExecutedAt = DateTime.UtcNow
                    };

                    return response;
                }
                catch (OperationCanceledException)
                {
                    // Back on the main thread here (the timer callback is not), as CancelTestRun needs.
                    keepObserving = s_CancelTestRun == null && !string.IsNullOrEmpty(s_ActiveRunId);
                    if (keepObserving)
                        ReattachResultCollector();
                    else
                        InvalidatePreviousRun();
                    throw new PipelineTimeoutException(CommandNames.TestRun, timeout, started: true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PipelineTestRunner] Sync execution error: {ex.Message}");
                    return CreateErrorResponse($"Test execution failed: {ex.Message}");
                }
                finally
                {
                    if (!keepObserving) CleanupRequestFile();
                    UnregisterCollector(collector);
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
            bool allModes)
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
                    isSync = false,
                    allModes = allModes
                };
                File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));

                // Clear any previous status, and a previous 'all' run's EditMode results
                if (File.Exists(TestStatusFile))
                    File.Delete(TestStatusFile);
                DeletePartialFile();

                // Start test execution now, on the main thread (run_tests is MainThreadRequired,
                // so we are inside the editor update tick that dispatched this command). Must NOT
                // run on a background thread: the test runner creates a TestRunnerApi via
                // ScriptableObject.CreateInstance, which is main-thread-only. RetrieveTestList
                // returns immediately, so the HTTP "running" response below is still sent promptly.
                StartAsyncTestExecution(testMode, filter, filterType, includeExplicit);

                return Task.FromResult(new TestExecutionResponse
                {
                    Success = true,
                    Command = CommandNames.TestRun,
                    Result = "running",
                    StatusPath = TestStatusFile,
                    Mode = allModes ? "All" : testMode.ToString(),
                    FilterApplied = string.IsNullOrEmpty(filter) ? null : $"{filterType}: {filter}",
                    Message = $"Tests started asynchronously. Poll {CommandNames.TestStatus} for the results.",
                    ExecutedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Async setup error: {ex.Message}");
                return Task.FromResult(CreateErrorResponse($"Failed to start async test execution: {ex.Message}"));
            }
        }

        /// <summary>
        /// Awaits <paramref name="task"/>, or throws <see cref="OperationCanceledException"/> as soon
        /// as <paramref name="token"/> fires, whichever comes first.
        /// </summary>
        internal static async Task<T> AwaitUnlessCanceled<T>(Task<T> task, CancellationToken token)
        {
            var canceled = new TaskCompletionSource<bool>();
            using (token.Register(() => canceled.TrySetResult(true)))
            {
                if (await Task.WhenAny(task, canceled.Task) != task)
                    throw new OperationCanceledException(token);
            }
            return await task;
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
                    // Discovery finishes on a later editor update: a timeout may have abandoned the run by now.
                    if (collector.IsCancelled)
                        return;

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
                    RecordRunId(api.Execute(new ExecutionSettings(testFilter)));
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
            TestRunnerApi api = null;
            TestResultCollector collector = null;
            try
            {
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                collector = new TestResultCollector();

                m_ActiveCollector = collector;

                // Async completion: write results for pollers. Set once, never overwritten.
                collector.OnRunFinished = () =>
                {
                    RestoreLiveServerIfDisrupted(); // B2: self-heal if a test broke the live server.
                    UnregisterCollector(collector);
                    FinishAsyncRun(CompletedStatus(collector));
                };

                api.RetrieveTestList(testMode, (ITestAdaptor rootTest) =>
                {
                    try
                    {
                        // Discovery finishes on a later editor update: test cancel or a newer run may
                        // have abandoned this one, and now owns the request and status files.
                        if (collector.IsCancelled)
                            return;

                        var testNames = CollectTestNames(rootTest, filter, filterType, includeExplicit);

                        if (testNames.Count == 0)
                        {
                            UnregisterCollector(collector);
                            FinishAsyncRun(JObject.FromObject(new
                            {
                                status = "completed",
                                duration = 0.0,
                                summary = new { total = 0, passed = 0, failed = 0, skipped = 0, inconclusive = 0 },
                                results = new object[0]
                            }));
                            return;
                        }

                        api.RegisterCallbacks(collector);
                        RecordRunId(api.Execute(new ExecutionSettings(new Filter
                        {
                            testMode = testMode,
                            testNames = testNames.ToArray()
                        })));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PipelineTestRunner] Async run setup error: {ex.Message}");
                        WriteStatusFile(new { status = "error", message = ex.Message });
                        CleanupRequestFile();
                        UnregisterCollector(collector);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Async execution error: {ex.Message}");
                WriteStatusFile(new { status = "error", message = ex.Message });
                CleanupRequestFile();
                UnregisterCollector(collector);
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
            TestRunnerApi api = null;
            TestResultCollector collector = null;
            try
            {
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                collector = new TestResultCollector(attachedMidRun: true);

                m_ActiveCollector = collector;

                collector.OnRunFinished = () =>
                {
                    RestoreLiveServerIfDisrupted();
                    UnregisterCollector(collector);
                    FinishAsyncRun(CompletedStatus(collector));
                };

                api.RegisterCallbacks(collector);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to reattach result collector: {ex.Message}");
                WriteStatusFile(new { status = "error", message = ex.Message });
                CleanupRequestFile();
                UnregisterCollector(collector);
            }
        }

        /// <summary>Resume any test run that was in flight before a domain reload dropped it.
        /// Invoked by <see cref="PipelineTestReloader"/>, which checks the request files first.</summary>
        public static void CheckForPendingTests()
        {
            if (!File.Exists(TestRequestFile)) return;
            if (File.Exists(TestStatusFile)) return; // Already completed
            // The run's collector survived, so no reload interrupted it: this is a stale delayCall.
            if (m_ActiveCollector != null) return;

            try
            {
                var json = File.ReadAllText(TestRequestFile);
                var request = JsonConvert.DeserializeObject<TestRequest>(json);

                if (request.isSync)
                {
                    if (s_CancelTestRun == null && !string.IsNullOrEmpty(request.runId))
                    {
                        ReattachResultCollector();
                        return;
                    }
                    // The reload dropped the synchronous request, so its results can't be returned.
                    // Say so through the status command instead of reporting no run at all.
                    // Keep the request file: it names the run, which test cancel or the next run stops.
                    Debug.LogWarning($"[PipelineTestRunner] {SyncRunInterruptedMessage}");
                    WriteStatusFile(new { status = "error", message = SyncRunInterruptedMessage });
                    if (s_CancelTestRun == null) CleanupRequestFile();
                    return;
                }

                Debug.Log("[PipelineTestRunner] Reattaching result collector after domain reload");

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

        /// <summary>
        /// Stops the previous run: stops listening to it and asks the Test Framework to cancel it,
        /// which otherwise keeps running every remaining test. Main thread only (CancelTestRun).
        /// </summary>
        private static void InvalidatePreviousRun()
        {
            if (m_ActiveCollector != null)
            {
                m_ActiveCollector.Cancel();
                UnregisterCollector(m_ActiveCollector);
                Debug.Log("[PipelineTestRunner] Invalidated previous test run");
            }

            // After a domain reload the static is gone, but the request file still names the run.
            var runId = s_ActiveRunId ?? ReadRequest()?.runId;
            s_ActiveRunId = null;
            if (!string.IsNullOrEmpty(runId) && s_CancelTestRun?.Invoke(runId) == true)
                Debug.Log("[PipelineTestRunner] Cancelled the previous test run in the Test Framework");
        }

        /// <summary>Remembers the Test Framework's id for the run just started, in memory and in the request file.</summary>
        private static void RecordRunId(string runId)
        {
            s_ActiveRunId = runId;
            var request = ReadRequest();
            if (request == null)
                return;
            request.runId = runId;
            File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));
        }

        private static TestRequest ReadRequest()
        {
            try
            {
                return File.Exists(TestRequestFile)
                    ? JsonConvert.DeserializeObject<TestRequest>(File.ReadAllText(TestRequestFile))
                    : null;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Unregisters a collector from Unity's TestRunnerApi callback registry (process-wide, not
        /// per-instance) and clears the active-run field if it still points at it. Unregisters
        /// without the TestRunnerApi it was registered through, which a run's asset unload can destroy.
        /// </summary>
        internal static void UnregisterCollector(TestResultCollector collector)
        {
            if (collector != null)
            {
                try
                {
#if UNITY_6000_0_OR_NEWER
                    TestRunnerApi.UnregisterTestCallback(collector);
#else
                    var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                    try { api.UnregisterCallbacks(collector); }
                    finally { UnityEngine.Object.DestroyImmediate(api); }
#endif
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PipelineTestRunner] Could not unregister collector: {ex.Message}");
                }
            }

            if (ReferenceEquals(m_ActiveCollector, collector))
            {
                m_ActiveCollector = null;
            }
        }

        internal static TestExecutionResponse CreateErrorResponse(string message)
        {
            return new TestExecutionResponse
            {
                Success = false,
                Command = CommandNames.TestRun,
                Error = message,
                FailureStatusCode = 500,
                Results = new List<TestResult>(),
                Summary = new TestSummary(),
                ExecutedAt = DateTime.UtcNow
            };
        }

        private static TestExecutionResponse CreateUsageErrorResponse(string message)
        {
            var response = new TestExecutionResponse
            {
                Command = CommandNames.TestRun,
                Results = new List<TestResult>(),
                Summary = new TestSummary(),
                ExecutedAt = DateTime.UtcNow
            };
            TestRunVerdict.MarkUsageError(response, message);
            return response;
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
                var descriptor = Unity.Pipeline.Models.EditorInstanceDescriptor.ReadFromProjectRoot(root);

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

        /// <summary>A finished async run's status payload: its summary and results, or an error.</summary>
        private static JObject CompletedStatus(TestResultCollector collector)
        {
            try
            {
                if (!collector.IsComplete)
                    return JObject.FromObject(new { status = "error", message = "Tests did not complete" });

                var root = collector.RootResult;
                return JObject.FromObject(new
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
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PipelineTestRunner] Failed to collect results: {ex.Message}");
                return JObject.FromObject(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Ends one async run with <paramref name="status"/>. The EditMode half of an 'all' run that
        /// completed instead keeps its results in <see cref="TestPartialFile"/> and starts the PlayMode
        /// half, so the run stays "running"; the PlayMode half's status is merged with them. Both halves
        /// run even when EditMode tests fail, so the final status reports every failure.
        /// </summary>
        internal static void FinishAsyncRun(JObject status)
        {
            var request = ReadRequest();
            if (request != null && request.allModes && request.mode == TestMode.EditMode.ToString()
                && (string)status["status"] == "completed")
            {
                File.WriteAllText(TestPartialFile, status.ToString(Formatting.None));
                request.mode = TestMode.PlayMode.ToString();
                request.runId = null;
                File.WriteAllText(TestRequestFile, JsonConvert.SerializeObject(request));
                // Started on the next editor update, outside the finished run's callbacks.
                EditorApplication.delayCall += () =>
                {
                    // test cancel, or a newer run, may have ended this one in the meantime.
                    var current = ReadRequest();
                    if (current == null || !current.allModes || current.mode != TestMode.PlayMode.ToString()
                        || current.runId != null || File.Exists(TestStatusFile))
                        return;
                    StartAsyncTestExecution(TestMode.PlayMode, current.filter, current.filterType, current.includeExplicit);
                };
                return;
            }

            WriteStatusFile(MergeWithPartial(status));
            DeletePartialFile();
            CleanupRequestFile();
        }

        /// <summary>
        /// <paramref name="status"/> with an 'all' run's EditMode results, if any, put in front: the
        /// summaries and durations add up and the results are concatenated. An error stays an error.
        /// </summary>
        internal static JObject MergeWithPartial(JObject status)
        {
            JObject partial;
            try
            {
                if (!File.Exists(TestPartialFile))
                    return status;
                partial = JObject.Parse(File.ReadAllText(TestPartialFile));
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Debug.LogWarning($"[PipelineTestRunner] Failed to read the EditMode results: {ex.Message}");
                return status;
            }

            if ((string)status["status"] != "completed")
                return status;

            var summary = new JObject();
            foreach (var key in new[] { "total", "passed", "failed", "skipped", "inconclusive" })
                summary[key] = ((int?)partial["summary"]?[key] ?? 0) + ((int?)status["summary"]?[key] ?? 0);

            var results = new JArray();
            foreach (var r in (partial["results"] as JArray ?? new JArray()).Concat(status["results"] as JArray ?? new JArray()))
                results.Add(r);

            return new JObject
            {
                ["status"] = "completed",
                ["duration"] = Math.Round(((double?)partial["duration"] ?? 0) + ((double?)status["duration"] ?? 0), 2),
                ["summary"] = summary,
                ["results"] = results
            };
        }

        private static void DeletePartialFile()
        {
            try { if (File.Exists(TestPartialFile)) File.Delete(TestPartialFile); } catch { }
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
            s_ActiveRunId = null; // The run is over; keep the in-memory id in step with the file.
            try { if (File.Exists(TestRequestFile)) File.Delete(TestRequestFile); } catch { }
        }

        /// <summary>
        /// Get current test status (for async mode polling)
        /// </summary>
        /// <returns>The status file's JSON contents, "running" if a request is pending, or null if there's no run.</returns>
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
        /// <returns>A status payload confirming cancellation (or that there was nothing to cancel).</returns>
        public static CancelTestsResult CancelTests()
        {
            if (s_CancelTestRun == null)
                return m_ActiveCollector != null || File.Exists(TestRequestFile)
                    ? new CancelTestsResult("unsupported", "This Test Framework cannot cancel the active test run. Poll test status.")
                    : new CancelTestsResult("no_tests", "No test run in progress.");
            if (m_ActiveCollector == null && !File.Exists(TestRequestFile) && !File.Exists(TestStatusFile))
                return new CancelTestsResult("no_tests", "No test run in progress.");

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
            DeletePartialFile();

            if (wasPlayMode && EditorApplication.isPlaying)
            {
                Debug.Log("[PipelineTestRunner] Exiting play mode to cancel PlayMode tests");
                EditorApplication.ExitPlaymode();
            }

            Debug.Log("[PipelineTestRunner] Test run cancelled");
            return new CancelTestsResult("cancelled", "Test run cancelled.");
        }

        /// <summary>What <see cref="CancelTests"/> did: <c>cancelled</c>, or <c>no_tests</c> when nothing was running.</summary>
        internal readonly struct CancelTestsResult
        {
            [JsonProperty("status")] public readonly string Status;
            [JsonProperty("message")] public readonly string Message;

            public CancelTestsResult(string status, string message)
            {
                Status = status;
                Message = message;
            }
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
            /// <summary>An 'all' run: after its EditMode half, <see cref="mode"/> moves on to PlayMode.</summary>
            public bool allModes;
            /// <summary>The Test Framework's id for the run (TestRunnerApi.Execute), for cancelling it.</summary>
            public string runId;
        }
    }
}

