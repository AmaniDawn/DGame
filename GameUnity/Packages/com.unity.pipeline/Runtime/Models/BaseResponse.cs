using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Base class for all responses. Serve also as the base class for all errors.
    /// </summary>
    [Serializable]
    public class BaseResponse
    {
        /// <summary>
        /// Response message if no error
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Error message if the command failed.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        /// Additional error details for debugging.
        /// </summary>
        [JsonProperty("errorDetails")]
        public string ErrorDetails { get; set; }

        /// <summary>
        /// When was the response created.
        /// </summary>
        [JsonProperty("executedAt")]
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Create a failed execution response.
        /// </summary>
        public static BaseResponse Failure(string error, string errorDetails)
        {
            return new BaseResponse
            {
                ExecutedAt = DateTime.UtcNow,
                Error = error,
                ErrorDetails = errorDetails
            };
        }
    }
}