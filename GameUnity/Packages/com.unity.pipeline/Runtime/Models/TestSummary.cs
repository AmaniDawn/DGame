using System;

namespace Unity.Pipeline
{
    /// <summary>
    /// Summary statistics for test execution
    /// </summary>
    [Serializable]
    public class TestSummary
    {
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Inconclusive { get; set; }

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