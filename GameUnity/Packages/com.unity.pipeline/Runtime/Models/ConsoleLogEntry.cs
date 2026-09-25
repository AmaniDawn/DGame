using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// A single captured Unity console entry, as returned by the <c>console</c> command.
    /// </summary>
    [Serializable]
    public class ConsoleLogEntry
    {
        /// <summary>
        /// Monotonic sequence number assigned when the entry was captured. Acts as the cursor for
        /// incremental ("--follow") retrieval: pass the highest seq seen back as the <c>since</c>
        /// parameter to get only newer entries. Never reused, and increasing for the life of the
        /// buffer (including across domain reloads, since the buffer is persisted).
        /// </summary>
        [JsonProperty("seq")]
        public long Seq { get; set; }

        /// <summary>
        /// When the entry was captured (UTC).
        /// </summary>
        [JsonProperty("timestampUtc")]
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Normalized severity: "log", "warn", or "error". Unity's Error/Exception/Assert log types
        /// all map to "error"; Warning maps to "warn"; Log maps to "log".
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// The log message text.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// The stack trace associated with the entry, if any. Empty for most plain logs.
        /// </summary>
        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; }
    }
}
