using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Manages instance descriptor files for CLI discovery.
    /// Written to Library/Pipeline/.unity-pipeline-port (under the project's git-ignored Library
    /// folder) for CLI tools to find running Editor instances.
    /// </summary>
    [Serializable]
    public class InstanceDescriptor
    {
        private const string DescriptorFileName = ".unity-pipeline-port";

        /// <summary>
        /// Process ID of the Unity Editor
        /// </summary>
        [JsonProperty("pid")]
        public int Pid { get; set; }

        /// <summary>
        /// HTTP port the pipeline server is listening on
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary>
        /// Full path to the Unity project
        /// </summary>
        [JsonProperty("projectPath")]
        public string ProjectPath { get; set; }

        /// <summary>
        /// Name of the Unity project
        /// </summary>
        [JsonProperty("projectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Unity Editor version string
        /// </summary>
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }

        /// <summary>
        /// Editor mode: "editor", "batchmode"
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// When the Editor instance was started
        /// </summary>
        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Last heartbeat timestamp
        /// </summary>
        [JsonProperty("lastHeartbeat")]
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Security token for code evaluation commands
        /// </summary>
        [JsonProperty("evalToken")]
        public string EvalToken { get; set; }

        /// <summary>
        /// Create instance descriptor for current Editor session
        /// </summary>
        public static InstanceDescriptor CreateCurrent(int port)
        {
            try
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                var projectName = Path.GetFileName(projectPath);
                var pid = Process.GetCurrentProcess().Id;
                var unityVersion = Application.unityVersion;
                var isBatchMode = Application.isBatchMode;

                // Generate eval token at server startup for CLI auto-discovery
                var evalToken = SecurityTokenManager.GetOrCreateToken();

                return new InstanceDescriptor
                {
                    Pid = pid,
                    Port = port,
                    ProjectPath = projectPath,
                    ProjectName = projectName,
                    UnityVersion = unityVersion,
                    Mode = isBatchMode ? "batchmode" : "editor",
                    StartedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    EvalToken = evalToken
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"CreateCurrent failed: {ex.Message}");
                UnityEngine.Debug.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Write instance descriptor to project root
        /// </summary>
        public static void WriteToProjectRoot(InstanceDescriptor descriptor)
        {
            try
            {
                var filePath = GetDescriptorFilePath(descriptor.ProjectPath);
                var isNewFile = !File.Exists(filePath);
                // The descriptor lives under Library/Pipeline, which may not exist yet.
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                var json = JsonConvert.SerializeObject(descriptor, Formatting.Indented);
                File.WriteAllText(filePath, json);

                // The descriptor carries the auth token; keep it readable only by the current user.
                // Applied once on creation (heartbeat rewrites preserve the existing permissions).
                if (isNewFile)
                    FilePermissions.RestrictToCurrentUser(filePath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"WriteToProjectRoot failed: {ex.Message}");
                UnityEngine.Debug.LogError($"Descriptor: {descriptor?.ProjectPath}, PID: {descriptor?.Pid}");
                throw;
            }
        }

        /// <summary>
        /// Read instance descriptor from project root
        /// </summary>
        public static InstanceDescriptor ReadFromProjectRoot(string projectPath)
        {
            var filePath = GetDescriptorFilePath(projectPath);

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<InstanceDescriptor>(json);
            }
            catch
            {
                // Invalid or corrupted file
                return null;
            }
        }

        /// <summary>
        /// Remove instance descriptor from project root
        /// </summary>
        public static void RemoveFromProjectRoot(string projectPath)
        {
            var filePath = GetDescriptorFilePath(projectPath);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Update heartbeat timestamp in existing descriptor file
        /// </summary>
        public static void UpdateHeartbeat(string projectPath)
        {
            var descriptor = ReadFromProjectRoot(projectPath);
            if (descriptor != null)
            {
                descriptor.LastHeartbeat = DateTime.UtcNow;
                WriteToProjectRoot(descriptor);
            }
        }

        /// <summary>
        /// Absolute path to the editor descriptor file for a project: the git-ignored
        /// Library/Pipeline folder under the project root. Public so discovery code and tests
        /// derive the location from one place.
        /// </summary>
        public static string GetDescriptorFilePath(string projectPath)
        {
            return Path.Combine(projectPath, "Library", "Pipeline", DescriptorFileName);
        }
    }
}