using System;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Pipeline.Config;
using System.IO;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Instance descriptor for runtime Unity Player builds with Pipeline server.
    /// Different from Editor InstanceDescriptor to reflect runtime context and security.
    /// </summary>
    [Serializable]
    public class RuntimeInstanceDescriptor
    {
        private const string RuntimeDescriptorFileName = ".unity-pipeline-runtime-port";

        /// <summary>
        /// Process ID of the Unity Player application
        /// </summary>
        [JsonProperty("pid")]
        public int Pid { get; set; }

        /// <summary>
        /// HTTP port the pipeline server is listening on (7900-7999 range)
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary>
        /// Unity runtime platform (Windows, macOS, Linux, etc.)
        /// </summary>
        [JsonProperty("platform")]
        public string Platform { get; set; }

        /// <summary>
        /// Unity Player version string
        /// </summary>
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }

        /// <summary>
        /// Unique build identifier
        /// </summary>
        [JsonProperty("buildGuid")]
        public string BuildGuid { get; set; }

        /// <summary>
        /// When the runtime instance was started
        /// </summary>
        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Last heartbeat timestamp
        /// </summary>
        [JsonProperty("lastHeartbeat")]
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Working directory where the application is running
        /// </summary>
        [JsonProperty("workingDirectory")]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Security token used to authorize requests to this runtime server.
        /// </summary>
        [JsonProperty("evalToken")]
        public string EvalToken { get; set; }

        /// <summary>
        /// Create runtime instance descriptor for current Player application.
        /// </summary>
        public static RuntimeInstanceDescriptor CreateCurrent(int port, RuntimePipelineConfig config)
        {
            try
            {
                var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                var unityVersion = Application.unityVersion;
                var platform = Application.platform.ToString();
                var buildGuid = Application.buildGUID;
                var workingDir = Directory.GetCurrentDirectory();

                var token = SecurityTokenManager.GetOrCreateToken();

                return new RuntimeInstanceDescriptor
                {
                    Pid = pid,
                    Port = port,
                    Platform = platform,
                    UnityVersion = unityVersion,
                    BuildGuid = buildGuid,
                    StartedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    WorkingDirectory = workingDir,
                    EvalToken = token
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Pipeline: CreateCurrent failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Write runtime instance descriptor to working directory.
        /// </summary>
        public static void WriteToWorkingDirectory(RuntimeInstanceDescriptor descriptor)
        {
            try
            {
                var filePath = GetDescriptorFilePath();
                var isNewFile = !File.Exists(filePath);
                var json = JsonConvert.SerializeObject(descriptor, Formatting.Indented);

                File.WriteAllText(filePath, json);

                // The descriptor carries the auth token; keep it readable only by the current user.
                // Applied once on creation (heartbeat rewrites preserve the existing permissions).
                if (isNewFile)
                    FilePermissions.RestrictToCurrentUser(filePath);

                System.Console.WriteLine($"Pipeline: Runtime descriptor written to {filePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Pipeline: Failed to write runtime descriptor: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Read runtime instance descriptor from working directory.
        /// </summary>
        public static RuntimeInstanceDescriptor ReadFromWorkingDirectory()
        {
            var filePath = GetDescriptorFilePath();

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<RuntimeInstanceDescriptor>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Pipeline: Failed to read runtime descriptor: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Remove runtime instance descriptor from working directory.
        /// </summary>
        public static void RemoveFromWorkingDirectory()
        {
            var filePath = GetDescriptorFilePath();

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Pipeline: Failed to remove runtime descriptor: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Update heartbeat timestamp in existing descriptor file.
        /// </summary>
        public static void UpdateHeartbeat()
        {
            try
            {
                var descriptor = ReadFromWorkingDirectory();
                if (descriptor != null)
                {
                    descriptor.LastHeartbeat = DateTime.UtcNow;
                    WriteToWorkingDirectory(descriptor);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Pipeline: Failed to update runtime heartbeat: {ex.Message}");
            }
        }

        private static string GetDescriptorFilePath()
        {
            var workingDir = new FileInfo($"{Application.dataPath}/..").FullName;
            return Path.Combine(workingDir, RuntimeDescriptorFileName);
        }
    }
}