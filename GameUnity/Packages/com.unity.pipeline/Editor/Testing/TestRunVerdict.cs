using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Models;

namespace Unity.Pipeline.Editor.Testing
{
    /// <summary>
    /// Decides whether a test run succeeded: every test that ran passed, and at least one ran.
    /// A run that fails says so in its own envelope (<see cref="CommandExecutionResponse.FailureStatusCode"/>),
    /// so the client's exit code reports the outcome, not just that the runner worked. Clients
    /// drop the result on failure, so the message carries the failed tests' names.
    /// </summary>
    static class TestRunVerdict
    {
        internal const int MaxListedFailures = 10;

        internal const string InvalidArgsCode = "INVALID_COMMAND_ARGS";

        /// <summary>
        /// "3 of 10 tests failed: A, B, C", listing at most <see cref="MaxListedFailures"/> names. A failure
        /// outside any test, such as in a fixture's setup, has no name to list and says so instead.
        /// </summary>
        internal static string DescribeFailures(IReadOnlyList<string> failedNames, int failed, int total)
        {
            if (failedNames.Count == 0)
                return $"{failed} of {total} tests failed (no test names reported, likely a fixture setup failure)";

            var listed = string.Join(", ", failedNames.Take(MaxListedFailures));
            var more = failedNames.Count > MaxListedFailures ? $", and {failedNames.Count - MaxListedFailures} more" : "";
            return $"{failed} of {total} tests failed: {listed}{more}";
        }

        internal static string DescribeNoTests(string mode, string filterApplied)
        {
            return filterApplied == null
                ? $"No {mode} tests found."
                : $"No {mode} tests matched {filterApplied}.";
        }

        /// <summary>Marks <paramref name="response"/> as an argument error: the envelope fails as invalid usage.</summary>
        internal static void MarkUsageError(CommandExecutionResponse response, string message)
        {
            response.Success = false;
            response.Error = "Invalid Command Arguments";
            response.ErrorDetails = message;
            response.ErrorCode = InvalidArgsCode;
            response.FailureStatusCode = 400;
        }

        /// <summary>
        /// Fails a finished run that had failing tests, or that ran none when
        /// <paramref name="zeroTestsFail"/> is set. Keeps the summary and results. A run that
        /// already failed is returned unchanged.
        /// </summary>
        internal static TestExecutionResponse Judge(TestExecutionResponse run, bool zeroTestsFail)
        {
            if (!run.Success)
                return run;

            var summary = run.Summary ?? new TestSummary();
            if (summary.Failed > 0)
            {
                var failedNames = run.Results.Where(r => r.Status == "Failed").Select(r => r.FullName).ToList();
                MarkTestsFailed(run, DescribeFailures(failedNames, summary.Failed, summary.Total));
            }
            else if (summary.Total == 0 && zeroTestsFail)
            {
                MarkUsageError(run, DescribeNoTests(run.Mode, run.FilterApplied));
            }

            return run;
        }

        /// <summary>
        /// Judges a test status payload: a finished run with failing tests or none and an errored run
        /// fail, and a cancelled run ends <see cref="ExitCode.Cancelled"/>, with <paramref name="error"/>
        /// saying why. Anything else,
        /// including a run still going, succeeds.
        /// </summary>
        internal static ExitCode JudgeStatus(JToken status, out string error)
        {
            error = null;
            switch ((string)status?["status"])
            {
                case "completed":
                    var failed = (int?)status["summary"]?["failed"] ?? 0;
                    var total = (int?)status["summary"]?["total"] ?? 0;
                    if (failed > 0)
                    {
                        var failedNames = (status["results"] as JArray ?? new JArray())
                            .Where(r => (string)r["status"] == "Failed")
                            .Select(r => (string)r["fullName"])
                            .ToList();
                        error = DescribeFailures(failedNames, failed, total);
                        return ExitCode.Failed;
                    }
                    if (total == 0 && !((bool?)status["allowEmpty"] ?? false))
                    {
                        error = DescribeNoTests("selected", null);
                        return ExitCode.InvalidUsage;
                    }
                    return ExitCode.Success;

                case "error":
                    error = $"Test run failed: {(string)status["message"]}";
                    return ExitCode.Failed;

                case "cancelled":
                    error = "The test run was cancelled.";
                    return ExitCode.Cancelled;

                default:
                    return ExitCode.Success;
            }
        }

        static void MarkTestsFailed(CommandExecutionResponse response, string details)
        {
            response.Success = false;
            response.Error = "Tests Failed";
            response.ErrorDetails = details;
            response.FailureStatusCode = 400;
        }
    }
}
