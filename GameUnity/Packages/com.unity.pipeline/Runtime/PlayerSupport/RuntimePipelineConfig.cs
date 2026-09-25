using System;
using UnityEngine;

namespace Unity.Pipeline.Config
{
    /// <summary>
    /// Configuration for Pipeline server functionality in Unity Player builds.
    /// Create this asset and place in Resources folder to enable runtime Pipeline support.
    /// </summary>
    [CreateAssetMenu(fileName = "RuntimePipelineConfig", menuName = "Pipeline/Runtime Configuration", order = 1)]
    public class RuntimePipelineConfig : ScriptableObject
    {
        [Header("Server Configuration")]
        [Tooltip("Enable Pipeline HTTP server in Player builds. SECURITY WARNING: Only enable in development/QA builds, never production without proper security measures.")]
        public bool enableInBuilds = false;

        [Tooltip("HTTP port for Pipeline server. Use 0 for auto-assignment from range 7900-7999.")]
        [Range(0, 65535)]
        public int port = 0;

        [Header("Advanced")]
        [Tooltip("Request timeout in milliseconds. Higher values allow longer-running commands.")]
        [Range(1000, 60000)]
        public int requestTimeoutMs = 30000;

        [Tooltip("Enable detailed logging of all remote requests for security auditing.")]
        public bool enableAuditLogging = true;

        /// <summary>
        /// Validate the configuration for correctness.
        /// Called by build processor to ensure safe deployment.
        /// </summary>
        public ValidationResult Validate()
        {
            if (!enableInBuilds)
                return ValidationResult.Success("Runtime Pipeline disabled");

            // Port validation
            if (port != 0 && (port < 7900 || port > 7999))
            {
                return ValidationResult.Warning(
                    $"Port {port} is outside recommended runtime range 7900-7999. May conflict with Editor instances.");
            }

            return ValidationResult.Success("Configuration is valid");
        }
    }

    /// <summary>
    /// Result from configuration validation.
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Level { get; set; } // "success", "warning", "error"
        public string Message { get; set; }

        public static ValidationResult Success(string message = "Valid") =>
            new ValidationResult { IsValid = true, Level = "success", Message = message };

        public static ValidationResult Warning(string message) =>
            new ValidationResult { IsValid = true, Level = "warning", Message = message };

        public static ValidationResult Error(string message) =>
            new ValidationResult { IsValid = false, Level = "error", Message = message };
    }
}
