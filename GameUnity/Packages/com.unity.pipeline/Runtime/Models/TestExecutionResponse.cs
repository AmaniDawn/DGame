using System;
using System.Collections.Generic;
using Unity.Pipeline.Models;

namespace Unity.Pipeline
{
    /// <summary>
    /// Response for test execution commands, containing summary and detailed results
    /// </summary>
    [Serializable]
    public class TestExecutionResponse : CommandExecutionResponse
    {
        public TestSummary Summary { get; set; }
        public List<TestResult> Results { get; set; }
        public double Duration { get; set; }
        public string StatusPath { get; set; } // For async mode
        public string Mode { get; set; } // EditMode, PlayMode, or All
        public string FilterApplied { get; set; } // What filter was used, if any

        public TestExecutionResponse()
        {
            Results = new List<TestResult>();
            Summary = new TestSummary();
        }
    }
}