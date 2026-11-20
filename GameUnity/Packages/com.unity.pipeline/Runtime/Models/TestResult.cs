using System;
using Newtonsoft.Json;

namespace Unity.Pipeline
{
    /// <summary>
    /// Individual test result information
    /// </summary>
    [Serializable]
    class TestResult
    {
        /// <summary>Fully-qualified test name.</summary>
        [JsonProperty("fullName")]
        public string FullName { get; set; }
        /// <summary>Outcome: Passed, Failed, Skipped, or Inconclusive.</summary>
        [JsonProperty("status")]
        public string Status { get; set; } // Passed, Failed, Skipped, Inconclusive
        /// <summary>How long the test took, in seconds.</summary>
        [JsonProperty("duration")]
        public double Duration { get; set; }
        /// <summary>Failure message, if any.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
        /// <summary>Failure stack trace, if any.</summary>
        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; }
    }
}