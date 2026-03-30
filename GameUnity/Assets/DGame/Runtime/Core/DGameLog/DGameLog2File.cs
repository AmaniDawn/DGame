using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

namespace DGame
{
    public class DGameLog2File : MonoBehaviour
    {
        private struct LogData
        {
            public string logMsg;
            public LogType type;
            public string trace;

            public LogData(string logMsg, LogType type, string trace)
            {
                this.logMsg = logMsg;
                this.type = type;
                this.trace = trace;
            }
        }

        private StreamWriter m_logWriter;
        private readonly ConcurrentQueue<LogData> m_logQueue = new ConcurrentQueue<LogData>();
        private bool m_threadRuning = false;
        private readonly ManualResetEvent m_manualResetEvent = new ManualResetEvent(false);
        private string m_nowTime => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private string m_logFileSavePath;

        private void OnEnable()
        {
            m_logFileSavePath = Path.Combine(Application.persistentDataPath, "Log");
            if (!Directory.Exists(m_logFileSavePath))
            {
                Directory.CreateDirectory(m_logFileSavePath);
            }
            string filePath = Path.Combine(m_logFileSavePath, "game_log.txt");
            m_logWriter = new StreamWriter(filePath, true);
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
            m_threadRuning = true;
            Thread fileThread = new Thread(FileLogThread);
            fileThread.Start();
        }

        private void FileLogThread()
        {
            while (m_threadRuning)
            {
                m_manualResetEvent.WaitOne();//让线程进入等待，并进行阻塞
                if (m_logWriter==null)
                {
                    break;
                }
                while (m_logQueue.Count > 0 && m_logQueue.TryDequeue(out var data))
                {
                    switch (data.type)
                    {
                        case LogType.Error:
                            m_logWriter.Write("[Error] ► ");
                            m_logWriter.WriteLine(data.logMsg);
                            m_logWriter.WriteLine(data.trace);
                            break;

                        case LogType.Assert:
                            m_logWriter.Write("[Assert] ► ");
                            m_logWriter.WriteLine(data.logMsg);
                            m_logWriter.WriteLine(data.trace);
                            break;

                        case LogType.Warning:
                            m_logWriter.Write("[Warning] ► ");
                            m_logWriter.WriteLine(data.logMsg);
                            m_logWriter.WriteLine(data.trace);
                            break;

                        case LogType.Log:
                            m_logWriter.Write("[Log] ► ");
                            m_logWriter.WriteLine(data.logMsg);
                            m_logWriter.WriteLine(data.trace);
                            break;

                        case LogType.Exception:
                            m_logWriter.Write("[Exception] ► ");
                            m_logWriter.WriteLine(data.logMsg);
                            m_logWriter.WriteLine(data.trace);
                            break;

                        default:
                            m_logWriter.Write("[Log] ► [NULL]");
                            break;
                    }
                    m_logWriter.Write("\r\n");
                }
                //保存当前文件内容，使其生效
                m_logWriter.Flush();
                m_manualResetEvent.Reset();
                Thread.Sleep(1);
            }
        }

        private void OnApplicationQuit()
        {
            // Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            // m_threadRuning = false;
            // m_manualResetEvent.Reset();
            // m_logWriter.Close();
            // m_logWriter = null;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            m_threadRuning = false;
            m_manualResetEvent.Reset();
            m_logWriter.Close();
            m_logWriter = null;
        }

        private void OnLogMessageReceivedThreaded(string condition, string stacktrace, LogType type)
        {
            m_logQueue.Enqueue(new LogData( $"[{m_nowTime}] {condition}", type, stacktrace));
            m_manualResetEvent.Set();
            //mManualRestEvent.WaitOne();//让线程进入等待，并进行阻塞
            //mManualRestEvent.Set();//设置一个信号，表示线程是需要工作的
            //mManualRestEvent.Reset();//重置信号，表示没有人指定需要工作
        }
    }
}