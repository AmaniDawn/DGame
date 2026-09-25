using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Pipeline.Console;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="ConsoleLogBuffer"/> — the ring buffer behind the console command.
    /// Exercised directly (no Unity log capture) for determinism.
    /// </summary>
    public class ConsoleLogBufferTests
    {
        static readonly DateTime k_Ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static ConsoleLogBuffer NewBuffer() => new ConsoleLogBuffer();

        [Test]
        public void Add_AssignsIncreasingSequenceNumbers()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "a", null, k_Ts);
            buffer.Add(LogType.Log, "b", null, k_Ts);
            buffer.Add(LogType.Log, "c", null, k_Ts);

            var entries = buffer.Query(-1, 100, ConsoleLogBuffer.SeverityLog).Entries;

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, entries.Select(e => e.Message));
            CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, entries.Select(e => e.Seq));
        }

        [Test]
        public void Query_Snapshot_ReturnsOldestFirst()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "first", null, k_Ts);
            buffer.Add(LogType.Log, "second", null, k_Ts);

            var response = buffer.Query(-1, 100, ConsoleLogBuffer.SeverityLog);

            Assert.AreEqual(2, response.Returned);
            Assert.AreEqual("first", response.Entries[0].Message);
            Assert.AreEqual("second", response.Entries[1].Message);
            Assert.AreEqual(2, response.Cursor);
            Assert.IsFalse(response.Dropped);
        }

        [Test]
        public void Query_LevelThreshold_IsMinimumSeverity()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "log", null, k_Ts);
            buffer.Add(LogType.Warning, "warn", null, k_Ts);
            buffer.Add(LogType.Error, "error", null, k_Ts);
            buffer.Add(LogType.Exception, "exception", null, k_Ts);

            var warnAndUp = buffer.Query(-1, 100, ConsoleLogBuffer.SeverityWarn).Entries;
            CollectionAssert.AreEqual(new[] { "warn", "error", "exception" }, warnAndUp.Select(e => e.Message));

            var errorOnly = buffer.Query(-1, 100, ConsoleLogBuffer.SeverityError).Entries;
            CollectionAssert.AreEqual(new[] { "error", "exception" }, errorOnly.Select(e => e.Message));

            var all = buffer.Query(-1, 100, ConsoleLogBuffer.SeverityLog).Entries;
            Assert.AreEqual(4, all.Length);
        }

        [Test]
        public void Query_Tail_ReturnsMostRecentMatches()
        {
            var buffer = NewBuffer();
            for (int i = 0; i < 10; i++)
                buffer.Add(LogType.Log, $"m{i}", null, k_Ts);

            var entries = buffer.Query(-1, 3, ConsoleLogBuffer.SeverityLog).Entries;

            CollectionAssert.AreEqual(new[] { "m7", "m8", "m9" }, entries.Select(e => e.Message));
        }

        [Test]
        public void Query_Tail_AppliesAfterLevelFilter()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Error, "e0", null, k_Ts);
            buffer.Add(LogType.Log, "l0", null, k_Ts);
            buffer.Add(LogType.Error, "e1", null, k_Ts);
            buffer.Add(LogType.Log, "l1", null, k_Ts);
            buffer.Add(LogType.Error, "e2", null, k_Ts);

            // tail=2 over error-only matches => last two errors, not last two of all entries.
            var entries = buffer.Query(-1, 2, ConsoleLogBuffer.SeverityError).Entries;

            CollectionAssert.AreEqual(new[] { "e1", "e2" }, entries.Select(e => e.Message));
        }

        [Test]
        public void Query_Since_ReturnsOnlyNewerEntries()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "a", null, k_Ts); // seq 1
            buffer.Add(LogType.Log, "b", null, k_Ts); // seq 2
            buffer.Add(LogType.Log, "c", null, k_Ts); // seq 3

            var response = buffer.Query(2, 100, ConsoleLogBuffer.SeverityLog);

            CollectionAssert.AreEqual(new[] { "c" }, response.Entries.Select(e => e.Message));
            Assert.AreEqual(3, response.Cursor);
            Assert.IsFalse(response.Dropped);
        }

        [Test]
        public void Query_SinceAtCursor_ReturnsEmptyButReportsCursor()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "a", null, k_Ts);
            buffer.Add(LogType.Log, "b", null, k_Ts);

            var response = buffer.Query(2, 100, ConsoleLogBuffer.SeverityLog);

            Assert.AreEqual(0, response.Returned);
            Assert.AreEqual(2, response.Cursor, "Cursor must still advance so a follow client resumes correctly");
            Assert.IsFalse(response.Dropped);
        }

        [Test]
        public void Query_SinceBeyondCursor_DoesNotOverflowDroppedCheck()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "a", null, k_Ts);

            var response = buffer.Query(long.MaxValue, 100, ConsoleLogBuffer.SeverityLog);

            Assert.AreEqual(0, response.Returned);
            Assert.IsFalse(response.Dropped);
        }

        [Test]
        public void Query_CursorReflectsLatestEvenWhenFilteredOut()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Error, "boom", null, k_Ts); // seq 1
            buffer.Add(LogType.Log, "noise", null, k_Ts);  // seq 2, filtered out by error level

            var response = buffer.Query(1, 100, ConsoleLogBuffer.SeverityError);

            Assert.AreEqual(0, response.Returned, "The only new entry is below the level threshold");
            Assert.AreEqual(2, response.Cursor, "Cursor advances past the filtered entry to avoid re-scanning it");
        }

        [Test]
        public void Eviction_DropsOldestAndFlagsDropped()
        {
            var buffer = NewBuffer();
            int total = ConsoleLogBuffer.Capacity + 50;
            for (int i = 0; i < total; i++)
                buffer.Add(LogType.Log, $"m{i}", null, k_Ts);

            Assert.AreEqual(ConsoleLogBuffer.Capacity, buffer.Count, "Buffer should be capped at Capacity");

            // since=1 points before the evicted window, so the consumer missed entries.
            var response = buffer.Query(1, ConsoleLogBuffer.Capacity, ConsoleLogBuffer.SeverityLog);
            Assert.IsTrue(response.Dropped, "Querying from an evicted cursor should set Dropped");

            // The most recent entry is always present.
            var latest = buffer.Query(-1, 1, ConsoleLogBuffer.SeverityLog).Entries.Single();
            Assert.AreEqual($"m{total - 1}", latest.Message);
        }

        [Test]
        public void Dropped_FalseWhenSinceWithinRetainedWindow()
        {
            var buffer = NewBuffer();
            for (int i = 0; i < 10; i++)
                buffer.Add(LogType.Log, $"m{i}", null, k_Ts);

            var response = buffer.Query(5, 100, ConsoleLogBuffer.SeverityLog);
            Assert.IsFalse(response.Dropped);
        }

        [Test]
        public void SaveAndLoad_RoundTripsEntriesAndCursor()
        {
            var path = Path.Combine("Temp", $"pipeline_console_test_{Guid.NewGuid():N}.json");
            try
            {
                var original = NewBuffer();
                original.Add(LogType.Log, "log", null, k_Ts);
                original.Add(LogType.Warning, "warn", "trace", k_Ts);
                original.Add(LogType.Error, "error", null, k_Ts);
                original.Save(path);

                var restored = NewBuffer();
                Assert.IsTrue(restored.Load(path));

                var entries = restored.Query(-1, 100, ConsoleLogBuffer.SeverityLog).Entries;
                CollectionAssert.AreEqual(new[] { "log", "warn", "error" }, entries.Select(e => e.Message));
                Assert.AreEqual(ConsoleLogBuffer.LevelWarn, entries[1].Level, "Severity must survive the round trip");
                Assert.AreEqual("trace", entries[1].StackTrace);

                // Sequence continues monotonically after a restore.
                restored.Add(LogType.Log, "after", null, k_Ts);
                Assert.AreEqual(4, restored.Query(3, 100, ConsoleLogBuffer.SeverityLog).Entries.Single().Seq);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Load_MissingFile_ReturnsFalseAndLeavesBufferEmpty()
        {
            var buffer = NewBuffer();
            Assert.IsFalse(buffer.Load(Path.Combine("Temp", $"does_not_exist_{Guid.NewGuid():N}.json")));
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void Load_ClampsRestoredEntriesToCapacityKeepingMostRecent()
        {
            var path = Path.Combine("Temp", $"pipeline_console_oversized_{Guid.NewGuid():N}.json");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Temp");

                var entries = Enumerable.Range(1, ConsoleLogBuffer.Capacity + 5)
                    .Select(i => new ConsoleLogEntry
                    {
                        Seq = i,
                        TimestampUtc = k_Ts,
                        Level = ConsoleLogBuffer.LevelLog,
                        Message = $"m{i}",
                        StackTrace = string.Empty
                    })
                    .ToArray();

                File.WriteAllText(path, JsonConvert.SerializeObject(new
                {
                    lastSeq = (long)(ConsoleLogBuffer.Capacity + 5),
                    entries
                }));

                var buffer = NewBuffer();
                Assert.IsTrue(buffer.Load(path));
                Assert.AreEqual(ConsoleLogBuffer.Capacity, buffer.Count);

                var restored = buffer.Query(-1, ConsoleLogBuffer.Capacity, ConsoleLogBuffer.SeverityLog).Entries;
                Assert.AreEqual("m6", restored.First().Message);
                Assert.AreEqual($"m{ConsoleLogBuffer.Capacity + 5}", restored.Last().Message);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Clear_EmptiesEntriesButKeepsSequenceMonotonic()
        {
            var buffer = NewBuffer();
            buffer.Add(LogType.Log, "a", null, k_Ts);
            buffer.Add(LogType.Log, "b", null, k_Ts);

            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);

            buffer.Add(LogType.Log, "c", null, k_Ts);
            Assert.AreEqual(3, buffer.LastSeq, "Sequence must not reset on Clear");
        }

        [Test]
        public void Add_IsThreadSafe()
        {
            var buffer = NewBuffer();
            const int threads = 8;
            const int perThread = 500;

            var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (int i = 0; i < perThread; i++)
                    buffer.Add(LogType.Log, $"t{t}-{i}", null, k_Ts);
            })).ToArray();
            Task.WaitAll(tasks);

            Assert.AreEqual(threads * perThread, buffer.LastSeq, "Every add must get a unique sequence number");
            Assert.AreEqual(ConsoleLogBuffer.Capacity, buffer.Count);

            // All retained seqs are unique.
            var seqs = buffer.Query(-1, ConsoleLogBuffer.Capacity, ConsoleLogBuffer.SeverityLog)
                .Entries.Select(e => e.Seq).ToArray();
            Assert.AreEqual(seqs.Length, seqs.Distinct().Count(), "Sequence numbers must be unique");
        }

        [Test]
        public void SeverityFromLevelName_ParsesNamesAndAliases()
        {
            Assert.AreEqual(ConsoleLogBuffer.SeverityError, ConsoleLogBuffer.SeverityFromLevelName("error"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityError, ConsoleLogBuffer.SeverityFromLevelName("ERR"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityWarn, ConsoleLogBuffer.SeverityFromLevelName("warn"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityWarn, ConsoleLogBuffer.SeverityFromLevelName("Warning"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityLog, ConsoleLogBuffer.SeverityFromLevelName("log"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityLog, ConsoleLogBuffer.SeverityFromLevelName("nonsense"));
            Assert.AreEqual(ConsoleLogBuffer.SeverityLog, ConsoleLogBuffer.SeverityFromLevelName(null));
        }
    }
}
