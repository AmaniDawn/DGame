using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Request model for /api/exec endpoint.
    /// Contains command name and parameters for remote command execution.
    /// </summary>
    [Serializable]
    public class CommandExecutionRequest
    {
        /// <summary>
        /// Name of the command to execute.
        /// Must match a registered [CliCommand] name.
        /// </summary>
        [JsonProperty("command", Required = Required.Always)]
        public string Command { get; set; }

        /// <summary>
        /// Parameters for the command execution.
        /// Contains key-value pairs matching command parameter names.
        /// </summary>
        [JsonProperty("parameters")]
        public JObject Parameters { get; set; }

        /// <summary>
        /// Optional timeout for command execution in milliseconds.
        /// Default: 60000 (60 seconds)
        /// </summary>
        [JsonProperty("timeout")]
        public int? Timeout { get; set; }

        /// <summary>
        /// Create a new command execution request.
        /// </summary>
        public CommandExecutionRequest()
        {
            Parameters = new JObject();
        }

        /// <summary>
        /// Create a command execution request with specified command and parameters.
        /// </summary>
        public CommandExecutionRequest(string command, JObject parameters = null)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Parameters = parameters ?? new JObject();
        }

        /// <summary>
        /// Get a parameter value as the specified type.
        /// Returns the default value if the parameter is not found or cannot be converted.
        /// </summary>
        public T GetParameter<T>(string name, T defaultValue = default(T))
        {
            if (Parameters == null || !Parameters.ContainsKey(name))
                return defaultValue;

            try
            {
                return Parameters[name].ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if a parameter exists in the request.
        /// </summary>
        public bool HasParameter(string name)
        {
            return Parameters != null && Parameters.ContainsKey(name);
        }

        /// <summary>
        /// Validate the request structure.
        /// </summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Command))
                return "Command name is required";

            if (Timeout.HasValue && Timeout.Value <= 0)
                return "Timeout must be a positive number";

            return null; // No validation errors
        }
    }
}