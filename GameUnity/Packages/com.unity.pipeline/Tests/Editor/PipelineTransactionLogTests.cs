using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PipelineTransactionLog"/>: the JSON-array transaction log and its
    /// session rotation. Exercises the directory-parameterized seams against an isolated temp
    /// directory so the real project Logs folder and the global SessionState gate are untouched.
    /// </summary>
    public class PipelineTransactionLogTests
    {
        private string m_LogsDir;

        private string LogPath => Path.Combine(m_LogsDir, "pipeline.log");
        private string OldLogPath => Path.Combine(m_LogsDir, "pipeline_old.log");

        [SetUp]
        public void SetUp()
        {
            m_LogsDir = Path.Combine(Path.GetTempPath(), "PipelineTxnLogTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(m_LogsDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_LogsDir))
            {
                try { Directory.Delete(m_LogsDir, true); }
                catch { /* Ignore cleanup failures */ }
            }
        }

        [Test]
        public void Append_WritesJsonArray_WithRequestResponseTimeFields()
        {
            PipelineTransactionLog.Append(m_LogsDir, "{\"command\":\"ping\"}", "{\"success\":true}");

            var array = JArray.Parse(File.ReadAllText(LogPath));
            Assert.AreEqual(1, array.Count, "Log should contain exactly one entry");

            var entry = (JObject)array[0];
            Assert.IsTrue(entry.ContainsKey("request"), "Entry must have a 'request' field");
            Assert.IsTrue(entry.ContainsKey("response"), "Entry must have a 'response' field");
            Assert.IsTrue(entry.ContainsKey("time"), "Entry must have a 'time' field");

            // time must be a parseable ISO-8601 timestamp. Use InvariantCulture so the assertion
            // is not sensitive to the host machine's locale (DateTime.Parse is culture-aware by
            // default and fails on some Mono versions when the "o" format string contains 'Z').
            Assert.DoesNotThrow(() => DateTime.Parse((string)entry["time"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind));
        }

        [Test]
        public void Append_StoresRequestResponseAsEmbeddedJson()
        {
            const string request = "{\"command\":\"eval\",\"parameters\":{\"code\":\"return 1;\"}}";
            const string response = "{\"success\":true,\"result\":1}";

            PipelineTransactionLog.Append(m_LogsDir, request, response);

            var entry = (JObject)JArray.Parse(File.ReadAllText(LogPath))[0];

            // The raw JSON is embedded as real JSON objects, not stringified blobs.
            Assert.AreEqual(JTokenType.Object, entry["request"].Type);
            Assert.AreEqual(JTokenType.Object, entry["response"].Type);
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(request), entry["request"]));
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(response), entry["response"]));
        }

        [Test]
        public void Append_NonJsonPayload_FallsBackToString_KeepsFileValid()
        {
            PipelineTransactionLog.Append(m_LogsDir, "not json", "{\"ok\":true}");

            // File must remain a valid JSON array; the non-JSON request degrades to a string value.
            var entry = (JObject)JArray.Parse(File.ReadAllText(LogPath))[0];
            Assert.AreEqual(JTokenType.String, entry["request"].Type);
            Assert.AreEqual("not json", (string)entry["request"]);
            Assert.AreEqual(JTokenType.Object, entry["response"].Type);
        }

        [Test]
        public void Append_Accumulates_PreservingOrder()
        {
            PipelineTransactionLog.Append(m_LogsDir, "{\"n\":1}", "{\"r\":1}");
            PipelineTransactionLog.Append(m_LogsDir, "{\"n\":2}", "{\"r\":2}");
            PipelineTransactionLog.Append(m_LogsDir, "{\"n\":3}", "{\"r\":3}");

            var array = JArray.Parse(File.ReadAllText(LogPath));
            Assert.AreEqual(3, array.Count);
            Assert.AreEqual(1, (int)array[0]["request"]["n"]);
            Assert.AreEqual(2, (int)array[1]["request"]["n"]);
            Assert.AreEqual(3, (int)array[2]["request"]["n"]);
        }

        [Test]
        public void Rotate_ExistingLog_MovedToOldLog_AndFreshStart()
        {
            PipelineTransactionLog.Append(m_LogsDir, "{\"n\":1}", "{\"r\":1}");
            Assert.IsTrue(File.Exists(LogPath));

            PipelineTransactionLog.Rotate(m_LogsDir);

            Assert.IsFalse(File.Exists(LogPath), "Active log should be moved away (fresh start)");
            Assert.IsTrue(File.Exists(OldLogPath), "Previous log should be backed up to pipeline_old.log");
            Assert.AreEqual(1, JArray.Parse(File.ReadAllText(OldLogPath)).Count);
        }

        [Test]
        public void Rotate_ReplacesPreviousOldLog()
        {
            // First session: one entry, then rotate -> pipeline_old.log holds it.
            PipelineTransactionLog.Append(m_LogsDir, "{\"session\":\"A\"}", "{}");
            PipelineTransactionLog.Rotate(m_LogsDir);

            // Second session: two entries, then rotate -> pipeline_old.log is replaced by these.
            PipelineTransactionLog.Append(m_LogsDir, "{\"session\":\"B\"}", "{}");
            PipelineTransactionLog.Append(m_LogsDir, "{\"session\":\"B\"}", "{}");
            PipelineTransactionLog.Rotate(m_LogsDir);

            var oldArray = JArray.Parse(File.ReadAllText(OldLogPath));
            Assert.AreEqual(2, oldArray.Count, "Old log should be replaced by the most recent session");
            Assert.AreEqual("B", (string)oldArray[0]["request"]["session"]);
        }

        [Test]
        public void Rotate_NoExistingLog_IsNoOp()
        {
            Assert.DoesNotThrow(() => PipelineTransactionLog.Rotate(m_LogsDir));
            Assert.IsFalse(File.Exists(LogPath));
            Assert.IsFalse(File.Exists(OldLogPath));
        }
    }
}
