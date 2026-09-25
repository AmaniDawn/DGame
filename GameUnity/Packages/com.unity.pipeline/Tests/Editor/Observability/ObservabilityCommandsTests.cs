using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Editor.Commands.Observability;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor.Observability
{
    /// <summary>
    /// Tests for the observability commands (CLI-198): get_console_logs, clear_console, and
    /// get_performance_stats, exercised directly and via PipelineClient. Console capture is verified
    /// against a unique marker logged in-test; Application.logMessageReceivedThreaded fires
    /// synchronously on Debug.Log, so the entry is in the buffer immediately.
    /// </summary>
    public class ObservabilityCommandsTests
    {
        #region Direct

        [Test]
        public void GetConsoleLogs_CapturesWarningMarker()
        {
            const string Marker = "CLI198_UNIQUE_marker";

            // The test runner treats unexpected warnings as failures; declare the one we emit.
            LogAssert.Expect(LogType.Warning, new Regex(".*" + Marker + ".*"));
            Debug.LogWarning(Marker);

            var result = ConsoleCommands.GetConsoleLogs("warning", 50);
            var logs = ReadLogs(result);

            var match = logs.FirstOrDefault(e => e.Message != null && e.Message.Contains(Marker));
            Assert.IsNotNull(match, "Warning marker should be captured by the console buffer");
            Assert.AreEqual("Warning", match.Type, "Captured marker should carry the Warning type");
        }

        [Test]
        public void GetPerformanceStats_ReturnsMemory()
        {
            var stats = PerformanceCommands.GetPerformanceStats();

            Assert.IsNotNull(stats, "Performance stats should not be null");
            Assert.IsNotNull(stats.Memory, "Memory stats should be present");
            Assert.Greater(stats.Memory.TotalAllocatedBytes, 0,
                "Total allocated memory should always be positive at runtime");
            Assert.IsNotNull(stats.Render, "Render stats should be present");
            Assert.IsNotNull(stats.FrameTiming, "Frame timing block should be present");
        }

        [Test]
        public void ClearConsole_RemovesSeededMarker()
        {
            // Assert against a unique marker rather than the overall Count: the editor/test runner can
            // emit unrelated logs immediately after the clear, so the buffer may be non-empty even when
            // the clear succeeded. The marker we seeded must be gone regardless of that noise.
            var marker = "CLI198_CLEAR_marker_" + System.Guid.NewGuid().ToString("N");
            Debug.Log(marker);

            var beforeClear = ConsoleLogBuffer.Snapshot();
            Assert.IsTrue(beforeClear.Any(e => e.Message != null && e.Message.Contains(marker)),
                "Seeded marker should be present in the buffer before clear");

            var result = ConsoleCommands.ClearConsole();

            Assert.IsNotNull(result);
            var afterClear = ConsoleLogBuffer.Snapshot();
            Assert.IsFalse(afterClear.Any(e => e.Message != null && e.Message.Contains(marker)),
                "Seeded marker should no longer be in the buffer after clear_console");
        }

        #endregion

        #region ViaClient

        [Test]
        public void GetConsoleLogs_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("get_console_logs", new { severity = "all", limit = 10 });

                Assert.IsTrue(response.IsSuccess, $"get_console_logs should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        [Test]
        public void GetPerformanceStats_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("get_performance_stats", null);

                Assert.IsTrue(response.IsSuccess, $"get_performance_stats should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        [Test]
        public void ClearConsole_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("clear_console");

                Assert.IsTrue(response.IsSuccess, $"clear_console should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion

        /// <summary>
        /// get_console_logs returns an anonymous object whose <c>logs</c> field is the typed entry
        /// list; reflect it out so direct tests can assert on entry fields.
        /// </summary>
        private static System.Collections.Generic.List<ConsoleLogEntryDto> ReadLogs(object result)
        {
            var logsProp = result.GetType().GetProperty("logs");
            Assert.IsNotNull(logsProp, "Result should expose a 'logs' property");
            return (System.Collections.Generic.List<ConsoleLogEntryDto>)logsProp.GetValue(result);
        }
    }
}
