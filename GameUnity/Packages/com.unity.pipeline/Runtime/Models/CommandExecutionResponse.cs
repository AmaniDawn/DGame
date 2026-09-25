using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Response model for /api/exec endpoint.
    /// Contains execution result and metadata for remote command execution.
    /// </summary>
    [Serializable]
    public class CommandExecutionResponse : BaseResponse
    {
        /// <summary>
        /// Whether the command executed successfully.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Name of the command that was executed.
        /// </summary>
        [JsonProperty("command")]
        public string Command { get; set; }


        /// <summary>
        /// Result returned by the command (if any).
        /// </summary>
        [JsonProperty("result")]
        public object Result { get; set; }

        /// <summary>
        /// Execution duration in milliseconds.
        /// </summary>
        [JsonProperty("executionTimeMs")]
        public long? ExecutionTimeMs { get; set; }

        /// <summary>
        /// Create a successful execution response.
        /// </summary>
        public static CommandExecutionResponse CmdSuccess(string command, object result = null, long? executionTimeMs = null)
        {
            return new CommandExecutionResponse
            {
                Success = true,
                Command = command,
                ExecutedAt = DateTime.UtcNow,
                Result = result,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Create a failed execution response.
        /// </summary>
        public static CommandExecutionResponse CmdFailure(string cmd, string error, string errorDetails)
        {
            return new CommandExecutionResponse
            {
                Command = cmd,
                Success = false,
                ExecutedAt = DateTime.UtcNow,
                Error = error,
                ErrorDetails = errorDetails
            };
        }
    }
}