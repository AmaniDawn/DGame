using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Response from code evaluation commands containing results, output, and diagnostics.
    /// </summary>
    [Serializable]
    public class EvalResponse :CommandExecutionResponse
    {
        /// <summary>
        /// Console output captured during execution.
        /// </summary>
        [JsonProperty("output")]
        public string Output { get; set; }

        /// <summary>
        /// Compilation diagnostics (errors, warnings) from Roslyn.
        /// </summary>
        [JsonProperty("diagnostics")]
        public List<DiagnosticInfo> Diagnostics { get; set; } = new List<DiagnosticInfo>();

        /// <summary>
        /// Create a successful evaluation response.
        /// </summary>
        public static EvalResponse EvalSuccess(object result, string output = null, long executionTimeMs = 0, List<DiagnosticInfo> diagnostics = null)
        {
            return new EvalResponse
            {
                Success = true,
                Result = result,
                Output = output,
                ExecutionTimeMs = executionTimeMs,
                Diagnostics = diagnostics ?? new List<DiagnosticInfo>()
            };
        }

        /// <summary>
        /// Create a failed evaluation response.
        /// </summary>
        public static EvalResponse EvalFailure(string error, string errorDetails = null, long executionTimeMs = 0, List<DiagnosticInfo> diagnostics = null)
        {
            return new EvalResponse
            {
                Success = false,
                Error = error,
                ErrorDetails = errorDetails,
                ExecutionTimeMs = executionTimeMs,
                Diagnostics = diagnostics ?? new List<DiagnosticInfo>()
            };
        }
    }

    /// <summary>
    /// Compilation diagnostic information from Roslyn.
    /// </summary>
    [Serializable]
    public class DiagnosticInfo
    {
        /// <summary>
        /// Severity level: "error", "warning", "info".
        /// </summary>
        [JsonProperty("severity")]
        public string Severity { get; set; }

        /// <summary>
        /// Diagnostic message text.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Line number (0-based).
        /// </summary>
        [JsonProperty("line")]
        public int Line { get; set; }

        /// <summary>
        /// Column number (0-based).
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; }

        /// <summary>
        /// Diagnostic identifier (e.g., "CS1002").
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}