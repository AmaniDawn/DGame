using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Console
{
    /// <summary>
    /// Bounded, thread-safe ring buffer of captured Unity console entries that backs the
    /// <c>console</c> command.
    ///
    /// Why thread-safe: entries arrive via <see cref="Application.logMessageReceivedThreaded"/>,
    /// which can fire from background threads, while the command reads the buffer from an HTTP
    /// request thread. All access is guarded by a single lock.
    ///
    /// Each entry gets a monotonic <see cref="ConsoleLogEntry.Seq"/> that never repeats and only
    /// increases — including across domain reloads, because the buffer (and the seq counter) is
    /// persisted via <see cref="Save"/>/<see cref="Load"/>. The seq is the cursor a "--follow"
    /// client uses to fetch only newer entries.
    /// </summary>
    public class ConsoleLogBuffer
    {
        /// <summary>Maximum number of entries retained. Older entries are evicted first.</summary>
        public const int Capacity = 2000;

        // Severity levels, ordered so a "minimum severity" filter is a simple >= comparison.
        public const int SeverityLog = 0;
        public const int SeverityWarn = 1;
        public const int SeverityError = 2;

        public const string LevelLog = "log";
        public const string LevelWarn = "warn";
        public const string LevelError = "error";

        readonly object m_Lock = new object();
        readonly Queue<StoredEntry> m_Entries = new Queue<StoredEntry>();
        long m_LastSeq;

        struct StoredEntry
        {
            public int Severity;
            public ConsoleLogEntry Entry;
        }

        /// <summary>
        /// Capture a console entry. Assigns the next sequence number and evicts the oldest entry if
        /// the buffer is full. Safe to call from any thread.
        /// </summary>
        public void Add(LogType type, string message, string stackTrace, DateTime timestampUtc)
        {
            var severity = SeverityFromLogType(type);
            lock (m_Lock)
            {
                m_LastSeq++;
                if (m_Entries.Count >= Capacity)
                    m_Entries.Dequeue();

                m_Entries.Enqueue(new StoredEntry
                {
                    Severity = severity,
                    Entry = new ConsoleLogEntry
                    {
                        Seq = m_LastSeq,
                        TimestampUtc = timestampUtc,
                        Level = LevelName(severity),
                        Message = message ?? string.Empty,
                        StackTrace = stackTrace ?? string.Empty
                    }
                });
            }
        }

        /// <summary>
        /// Query the buffer for the <c>console</c> command.
        /// </summary>
        /// <param name="since">
        /// Cursor: return only entries with <c>Seq &gt; since</c>. Pass a negative value (e.g. -1)
        /// for a snapshot of the most recent entries with no cursor filtering.
        /// </param>
        /// <param name="tail">
        /// Maximum number of entries to return (the most recent matches). Values &lt;= 0 mean "no
        /// limit" (up to <see cref="Capacity"/>).
        /// </param>
        /// <param name="minSeverity">Minimum severity to include (see the Severity* constants).</param>
        public ConsoleLogResponse Query(long since, int tail, int minSeverity)
        {
            lock (m_Lock)
            {
                var matches = new List<ConsoleLogEntry>();
                foreach (var stored in m_Entries)
                {
                    if (stored.Severity < minSeverity)
                        continue;
                    if (since >= 0 && stored.Entry.Seq <= since)
                        continue;
                    matches.Add(stored.Entry);
                }

                // Keep only the most recent `tail` matches.
                if (tail > 0 && matches.Count > tail)
                    matches.RemoveRange(0, matches.Count - tail);

                return new ConsoleLogResponse
                {
                    Entries = matches.ToArray(),
                    Cursor = m_LastSeq,
                    Returned = matches.Count,
                    Dropped = ComputeDropped(since)
                };
            }
        }

        /// <summary>
        /// True when <paramref name="since"/> refers to entries that have already been evicted, so
        /// the caller missed some output. Caller must hold <see cref="m_Lock"/>.
        /// </summary>
        bool ComputeDropped(long since)
        {
            if (since < 0)
                return false;
            if (since >= m_LastSeq)
                return false;

            // Everything after `since` was evicted (or never retained): buffer empty but seq moved on.
            if (m_Entries.Count == 0)
                return m_LastSeq > since;

            // The next entry the caller expects is since+1; if the oldest entry we still hold is
            // newer than that, the gap in between was evicted.
            var oldestSeq = m_Entries.Peek().Entry.Seq;
            return oldestSeq > since + 1;
        }

        /// <summary>Remove all entries. Does NOT reset the sequence counter (cursors stay monotonic).</summary>
        public void Clear()
        {
            lock (m_Lock)
            {
                m_Entries.Clear();
            }
        }

        /// <summary>Current number of retained entries. For diagnostics/tests.</summary>
        public int Count
        {
            get { lock (m_Lock) { return m_Entries.Count; } }
        }

        /// <summary>The highest sequence number assigned so far (the live cursor).</summary>
        public long LastSeq
        {
            get { lock (m_Lock) { return m_LastSeq; } }
        }

        #region Persistence

        [Serializable]
        class Snapshot
        {
            [JsonProperty("lastSeq")] public long LastSeq;
            [JsonProperty("entries")] public ConsoleLogEntry[] Entries;
        }

        /// <summary>
        /// Persist the buffer (entries + sequence counter) to <paramref name="path"/> so it survives
        /// a domain reload. Failures are swallowed (logging is best-effort, never fatal).
        /// </summary>
        public void Save(string path)
        {
            try
            {
                Snapshot snapshot;
                lock (m_Lock)
                {
                    var entries = new ConsoleLogEntry[m_Entries.Count];
                    var i = 0;
                    foreach (var stored in m_Entries)
                        entries[i++] = stored.Entry;
                    snapshot = new Snapshot { LastSeq = m_LastSeq, Entries = entries };
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonConvert.SerializeObject(snapshot));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pipeline] Failed to persist console log buffer: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore a buffer previously written by <see cref="Save"/>. Missing or unreadable files
        /// leave the buffer empty. Returns true if a snapshot was loaded.
        /// </summary>
        public bool Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                var snapshot = JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(path));
                if (snapshot == null)
                    return false;

                lock (m_Lock)
                {
                    m_Entries.Clear();
                    long lastRestoredSeq = 0;
                    if (snapshot.Entries != null)
                    {
                        foreach (var entry in snapshot.Entries)
                        {
                            if (entry == null)
                                continue;
                            if (m_Entries.Count >= Capacity)
                                m_Entries.Dequeue();

                            m_Entries.Enqueue(new StoredEntry
                            {
                                Severity = SeverityFromLevelName(entry.Level),
                                Entry = entry
                            });
                            lastRestoredSeq = entry.Seq;
                        }
                    }

                    // Keep the counter ahead of any restored entry so seq stays monotonic.
                    m_LastSeq = snapshot.LastSeq;
                    if (m_Entries.Count > 0)
                        m_LastSeq = Math.Max(m_LastSeq, lastRestoredSeq);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Pipeline] Failed to restore console log buffer: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Severity mapping

        /// <summary>Map a Unity <see cref="LogType"/> to an ordered severity.</summary>
        public static int SeverityFromLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return SeverityWarn;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return SeverityError;
                default:
                    return SeverityLog;
            }
        }

        /// <summary>
        /// Parse a "--level" value ("log"/"warn"/"error", plus common aliases) into a minimum
        /// severity. Unknown values fall back to <see cref="SeverityLog"/> (everything), matching the
        /// lenient behavior of the existing log command.
        /// </summary>
        public static int SeverityFromLevelName(string level)
        {
            switch ((level ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "error":
                case "err":
                case "exception":
                    return SeverityError;
                case "warn":
                case "warning":
                    return SeverityWarn;
                default:
                    return SeverityLog;
            }
        }

        /// <summary>The canonical level name for a severity.</summary>
        public static string LevelName(int severity)
        {
            switch (severity)
            {
                case SeverityError:
                    return LevelError;
                case SeverityWarn:
                    return LevelWarn;
                default:
                    return LevelLog;
            }
        }

        #endregion
    }
}
