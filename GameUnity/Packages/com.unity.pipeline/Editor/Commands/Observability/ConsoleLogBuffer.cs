using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.Observability
{
    /// <summary>
    /// A single captured Editor console entry. Returned (most-recent-first) by the
    /// <c>get_console_logs</c> command so an agent can read structured log output without scraping
    /// the Editor UI. Mirrors the fields Unity surfaces on <see cref="Application.logMessageReceivedThreaded"/>.
    /// </summary>
    [Serializable]
    public class ConsoleLogEntryDto
    {
        /// <summary>Unity <see cref="LogType"/> name, e.g. "Log", "Warning", "Error", "Exception", "Assert".</summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>The logged message text.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>Captured stack trace, if Unity provided one for the entry.</summary>
        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; }

        /// <summary>Capture time in UTC, ISO-8601 round-trip ("o") format.</summary>
        [JsonProperty("timestampUtc")]
        public string TimestampUtc { get; set; }
    }

    /// <summary>
    /// Captures Editor console output into a bounded, thread-safe ring buffer so it can be read back
    /// structurally over the pipeline (CLI-198). Subscribes to
    /// <see cref="Application.logMessageReceivedThreaded"/> on load — the threaded variant is used
    /// because Unity may raise log callbacks off the main thread, and the handler is fully lock-guarded.
    ///
    /// The buffer holds at most <see cref="MaxEntries"/> entries; the oldest is dropped when full.
    /// </summary>
    public static class ConsoleLogBuffer
    {
        /// <summary>Maximum number of entries retained; oldest entries are dropped past this.</summary>
        public const int MaxEntries = 1000;

        private static readonly object m_Lock = new object();
        private static readonly Queue<ConsoleLogEntryDto> m_Entries = new Queue<ConsoleLogEntryDto>(MaxEntries);

        /// <summary>
        /// Subscribe to Unity's threaded log callback as soon as the Editor domain loads, so capture
        /// is live before any command runs. Idempotent across domain reloads (handler is removed
        /// first to avoid double-subscription if Init somehow runs twice).
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageThreaded;
            Application.logMessageReceivedThreaded += OnLogMessageThreaded;
        }

        private static void OnLogMessageThreaded(string condition, string stackTrace, LogType type)
        {
            var entry = new ConsoleLogEntryDto
            {
                Type = type.ToString(),
                Message = condition,
                StackTrace = stackTrace,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };

            lock (m_Lock)
            {
                if (m_Entries.Count >= MaxEntries)
                    m_Entries.Dequeue();
                m_Entries.Enqueue(entry);
            }
        }

        /// <summary>
        /// Take a point-in-time copy of the buffer in chronological (oldest-first) order. The copy is
        /// detached from the live buffer, so callers can filter/reverse it without holding the lock.
        /// </summary>
        public static IReadOnlyList<ConsoleLogEntryDto> Snapshot()
        {
            lock (m_Lock)
            {
                return new List<ConsoleLogEntryDto>(m_Entries);
            }
        }

        /// <summary>Number of entries currently retained in the buffer.</summary>
        public static int Count
        {
            get
            {
                lock (m_Lock)
                {
                    return m_Entries.Count;
                }
            }
        }

        /// <summary>Drop all captured entries.</summary>
        public static void Clear()
        {
            lock (m_Lock)
            {
                m_Entries.Clear();
            }
        }
    }
}
