using System;

namespace Unity.Pipeline
{
    /// <summary>
    /// Individual test result information
    /// </summary>
    [Serializable]
    public class TestResult
    {
        public string FullName { get; set; }
        public string Status { get; set; } // Passed, Failed, Skipped, Inconclusive
        public double Duration { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}