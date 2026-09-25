using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Unity.Pipeline.Editor.Testing
{
    /// <summary>
    /// Collects test results from Unity TestRunner API callbacks.
    /// Adapted from unity-tools implementation with sync/async mode support.
    /// </summary>
    public class TestResultCollector : ICallbacks
    {
        public bool IsComplete { get; private set; }
        public bool IsCancelled { get; set; }
        public List<TestResult> Results { get; } = new List<TestResult>();
        public ITestResultAdaptor RootResult { get; private set; }

        // For async mode
        public Action OnRunFinished { get; set; }

        // For sync mode
        private TaskCompletionSource<ITestResultAdaptor> m_CompletionSource;

        public TestResultCollector()
        {
            m_CompletionSource = new TaskCompletionSource<ITestResultAdaptor>();
        }

        /// <summary>
        /// Wait for test completion (sync mode)
        /// </summary>
        public Task<ITestResultAdaptor> WaitForCompletionAsync()
        {
            return m_CompletionSource.Task;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            if (IsCancelled) return;

            IsComplete = false;
            Results.Clear();
            Debug.Log($"[TestResultCollector] Run started: {testsToRun.TestCaseCount} test(s)");
        }

        public void TestStarted(ITestAdaptor test)
        {
            // No action needed for test start
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (IsCancelled) return;
            if (result.Test.IsSuite) return;

            Results.Add(BuildTestResult(result));
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (IsCancelled)
            {
                Debug.Log("[TestResultCollector] Ignoring RunFinished from cancelled run");
                return;
            }

            RootResult = result;
            IsComplete = true;

            // If we attached after the tests already ran (PlayMode re-registers a fresh collector
            // after each play-mode domain reload, so the incremental TestFinished callbacks were
            // delivered to a collector from a previous domain), rebuild from the authoritative
            // result tree. The normal in-domain path leaves Results already populated.
            if (Results.Count == 0)
            {
                CollectLeafResults(result);
            }

            var total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            Debug.Log($"[TestResultCollector] Run finished: {total} total, {result.PassCount} passed, {result.FailCount} failed");

            // Notify async mode
            OnRunFinished?.Invoke();

            // Notify sync mode
            m_CompletionSource.SetResult(result);
        }

        /// <summary>
        /// Cancel the test collection and notify sync waiters
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
            if (!m_CompletionSource.Task.IsCompleted)
            {
                m_CompletionSource.SetCanceled();
            }
        }

        /// <summary>
        /// Set error state and notify sync waiters
        /// </summary>
        public void SetError(Exception exception)
        {
            if (!m_CompletionSource.Task.IsCompleted)
            {
                m_CompletionSource.SetException(exception);
            }
        }

        /// <summary>
        /// Recursively collect leaf (non-suite) test results from the result tree. Used at
        /// RunFinished to rebuild Results when this collector was registered too late to receive
        /// the incremental TestFinished callbacks (PlayMode resume after a domain reload).
        /// </summary>
        private void CollectLeafResults(ITestResultAdaptor result)
        {
            if (result == null) return;

            if (!result.Test.IsSuite && !result.Test.HasChildren)
            {
                Results.Add(BuildTestResult(result));
                return;
            }

            if (result.Children != null)
            {
                foreach (var child in result.Children)
                {
                    CollectLeafResults(child);
                }
            }
        }

        private static TestResult BuildTestResult(ITestResultAdaptor result)
        {
            return new TestResult
            {
                FullName = result.Test.FullName,
                Status = result.TestStatus.ToString(),
                Duration = result.Duration,
                Message = Truncate(result.Message, 10000),
                StackTrace = Truncate(result.StackTrace, 10000)
            };
        }

        private static string Truncate(string s, int maxLength)
        {
            if (s == null || s.Length <= maxLength) return s;
            return s.Substring(0, maxLength) + "\n... (truncated)";
        }
    }
}