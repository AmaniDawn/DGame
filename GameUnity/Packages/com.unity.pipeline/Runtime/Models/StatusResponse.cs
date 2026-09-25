using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Response model for /api/status endpoint.
    /// Provides Editor state information for CLI tools and automation scripts.
    /// </summary>
    [Serializable]
    public class StatusResponse
    {
        /// <summary>
        /// Current Editor status: "ready", "compiling", "busy"
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Whether Unity is currently compiling scripts
        /// </summary>
        [JsonProperty("compiling")]
        public bool Compiling { get; set; }

        /// <summary>
        /// Whether a domain reload is in progress
        /// </summary>
        [JsonProperty("domainReloadInProgress")]
        public bool DomainReloadInProgress { get; set; }

        /// <summary>
        /// Current play mode state: "stopped", "playing", "paused"
        /// </summary>
        [JsonProperty("playMode")]
        public string PlayMode { get; set; }

        /// <summary>
        /// Timestamp of last heartbeat update
        /// </summary>
        [JsonProperty("lastHeartbeat")]
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Full path to the Unity project
        /// </summary>
        [JsonProperty("projectPath")]
        public string ProjectPath { get; set; }

        /// <summary>
        /// Unity Editor version string
        /// </summary>
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }
    }
}