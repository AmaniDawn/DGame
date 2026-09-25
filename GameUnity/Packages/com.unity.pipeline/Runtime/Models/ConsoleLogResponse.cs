using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Response model for the <c>console</c> command: a page of captured console entries plus
    /// the cursor needed to fetch the next page.
    /// </summary>
    [Serializable]
    public class ConsoleLogResponse
    {
        /// <summary>
        /// The matching entries, oldest first, after applying the level filter, <c>since</c> cursor,
        /// and <c>tail</c> limit.
        /// </summary>
        [JsonProperty("entries")]
        public ConsoleLogEntry[] Entries { get; set; }

        /// <summary>
        /// The highest <see cref="ConsoleLogEntry.Seq"/> currently held in the buffer (regardless of
        /// the level filter). A "--follow" client passes this back as <c>since</c> on the next poll
        /// so it resumes exactly where it left off, even if the only new entries were filtered out.
        /// </summary>
        [JsonProperty("cursor")]
        public long Cursor { get; set; }

        /// <summary>
        /// Number of entries returned in <see cref="Entries"/>.
        /// </summary>
        [JsonProperty("returned")]
        public int Returned { get; set; }

        /// <summary>
        /// True when the requested <c>since</c> cursor pointed at entries that have already been
        /// evicted from the bounded buffer, meaning the consumer missed some entries. Lets a
        /// "--follow" client warn about a gap instead of silently skipping output.
        /// </summary>
        [JsonProperty("dropped")]
        public bool Dropped { get; set; }
    }
}
