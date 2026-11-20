using System;
using Newtonsoft.Json;

namespace Unity.Pipeline
{
    /// <summary>
    /// Summary statistics for test execution
    /// </summary>
    [Serializable]
    class TestSummary
    {
        /// <summary>Total number of tests run.</summary>
        [JsonProperty("total")]
        public int Total { get; set; }
        /// <summary>Number of tests that passed.</summary>
        [JsonProperty("passed")]
        public int Passed { get; set; }
        /// <summary>Number of tests that failed.</summary>
        [JsonProperty("failed")]
        public int Failed { get; set; }
        /// <summary>Number of tests skipped.</summary>
        [JsonProperty("skipped")]
        public int Skipped { get; set; }
        /// <summary>Number of tests that ran but produced no pass/fail verdict.</summary>
        [JsonProperty("inconclusive")]
        public int Inconclusive { get; set; }

        /// <summary>Create a zeroed summary.</summary>
        public TestSummary()
        {
            Total = 0;
            Passed = 0;
            Failed = 0;
            Skipped = 0;
            Inconclusive = 0;
        }
    }
}