using System;
using UnityEngine;

namespace DGame
{
    public sealed partial class DebuggerDriver
    {
        public sealed class LogNode : MemoryObject
        {
            private DateTime m_logTime = default(DateTime);
            private int m_logFrameCount;
            private LogType m_logType = LogType.Error;
            private string m_logMessage;
            private string m_stackTrace;

            public DateTime LogTime => m_logTime;
            public int LogFrameCount => m_logFrameCount;
            public LogType LogType => m_logType;
            public string LogMessage => m_logMessage;
            public string StackTrace => m_stackTrace;

            public static LogNode Create(LogType logType, string logMessage, string stackTrace)
            {
                LogNode logNode = MemoryObject.Spawn<LogNode>();
                logNode.m_logTime = DateTime.Now;
                logNode.m_logFrameCount = Time.frameCount;
                logNode.m_logType = logType;
                logNode.m_logMessage = logMessage;
                logNode.m_stackTrace = stackTrace;
                return logNode;
            }

            public override void OnRelease()
            {
                m_logTime = default(DateTime);
                m_logFrameCount = 0;
                m_logType = LogType.Error;
                m_logMessage = null;
                m_stackTrace = null;
            }
        }

        public sealed class PrintLogNode : MemoryObject
        {
            private DateTime m_logTime;
            private int m_logFrameCount;
            private LogType m_logType;
            private string m_logMessage;
            private string m_stackTrace;

            public DateTime LogTime => m_logTime;
            public int LogFrameCount => m_logFrameCount;
            public LogType LogType => m_logType;
            public string LogMessage => m_logMessage;
            public string StackTrace => m_stackTrace;

            public static PrintLogNode Create(LogType logType, string logMessage, string stackTrace)
            {
                var node = MemoryObject.Spawn<PrintLogNode>();
                node.m_logTime = DateTime.Now;
                node.m_logFrameCount = Time.frameCount;
                node.m_logType = logType;
                node.m_logMessage = logMessage;
                node.m_stackTrace = stackTrace;
                return node;
            }

            public override void OnRelease()
            {
                m_logTime = default;
                m_logFrameCount = 0;
                m_logType = LogType.Error;
                m_logMessage = string.Empty;
                m_stackTrace = string.Empty;
            }
        }
    }
}