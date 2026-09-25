using System;
using Unity.Pipeline.Config;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline
{
    public class RuntimePipelineServer : BasePipelineServer
    {
        private RuntimePipelineConfig m_Config;
        private RuntimeInstanceDescriptor m_InstanceDescriptor;

        public override DateTime StartedAt => m_InstanceDescriptor == null ? new DateTime() : m_InstanceDescriptor.StartedAt;

        public RuntimePipelineServer(RuntimePipelineConfig config)
        {
            m_Config = config;
        }

        protected override (int basePort, int maxPort) GetPortRange()
        {
            return (7900, 7949); // Runtime production (test runtime servers use 7950-7999)
        }

        protected override void CreateInstanceDescriptor()
        {
            // Create and write instance descriptor for CLI discovery
            m_InstanceDescriptor = RuntimeInstanceDescriptor.CreateCurrent(Port, m_Config);
            RuntimeInstanceDescriptor.WriteToWorkingDirectory(m_InstanceDescriptor);
        }

        protected override void DeleteInstanceDescriptor()
        {
            // Clean up instance descriptor file
            if (m_InstanceDescriptor != null)
            {
                RuntimeInstanceDescriptor.RemoveFromWorkingDirectory();
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
                    RuntimeInstanceDescriptor.WriteToWorkingDirectory(m_InstanceDescriptor);
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
