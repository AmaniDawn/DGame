using System;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Editor
{
    public class EditorPipelineServer : BasePipelineServer
    {
        private InstanceDescriptor m_InstanceDescriptor;

        /// <summary>When true, every command transaction is written to the project transaction log.</summary>
        public bool LogRequestsResponses { get; set; }

        public override DateTime StartedAt => m_InstanceDescriptor == null ? new DateTime() : m_InstanceDescriptor.StartedAt;

        protected override void ServerStarted()
        {
            var isAutomated = false;
            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-automated")
                {
                    isAutomated = true;
                    break;
                }
            }
            // Only warn in an interactive Editor: batchmode (CI, upm-pvp, UTR) can't show modal
            // popups, so the warning is pure noise there.
            if (!isAutomated && !Application.isBatchMode)
            {
                Debug.LogWarning("Editor is not in automated mode. Modal Pop up might break continuous command workflow. Start the editor with -automated");
            }
        }

        protected override void OnTransactionProcessed(string requestJson, string responseJson)
        {
            if (!LogRequestsResponses)
                return;

            PipelineTransactionLog.Append(requestJson, responseJson);
        }

        // Runtime-only commands (eval, reload_file_override, …) are part of the Player surface; don't
        // advertise them when a client is connected to the Editor.
        protected override bool IncludeRuntimeOnlyCommands => false;
        protected override void CreateInstanceDescriptor()
        {
            // Create and write instance descriptor for CLI discovery
            m_InstanceDescriptor = InstanceDescriptor.CreateCurrent(Port);
            InstanceDescriptor.WriteToProjectRoot(m_InstanceDescriptor);
        }

        protected override void DeleteInstanceDescriptor()
        {
            // Clean up instance descriptor file
            if (m_InstanceDescriptor != null)
            {
                InstanceDescriptor.RemoveFromProjectRoot(m_InstanceDescriptor.ProjectPath);
            }
            m_InstanceDescriptor = null;
        }

        protected override void UpdateHeartBeat()
        {
            // Update heartbeat in instance descriptor
            if (m_InstanceDescriptor != null)
            {
                m_InstanceDescriptor.LastHeartbeat = DateTime.UtcNow;
                try
                {
                    InstanceDescriptor.WriteToProjectRoot(m_InstanceDescriptor);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to update instance descriptor: {ex.Message}");
                }
            }
        }

        protected override object GetServerStatus()
        {
            UpdateHeartBeat();
            return new
            {
                status = m_InstanceDescriptor == null ? "error" : "ready",
                lastHeartbeat = m_InstanceDescriptor?.LastHeartbeat
            };
        }

        protected override string GetToken()
        {
            return m_InstanceDescriptor.EvalToken;
        }
    }
}
