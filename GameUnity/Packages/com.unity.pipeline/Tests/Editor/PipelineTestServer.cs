using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline.Security;
using Unity.Pipeline.Tests.Runtime;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Test fixture that owns an isolated <see cref="TestEditorPipelineServer"/> plus a
    /// <see cref="PipelineClient"/> pointed at it. ViaClient tests use this instead of the live
    /// editor server, so running the suite never disturbs the server agents drive over HTTP.
    ///
    /// Wrap in a `using` (or SetUp/TearDown) so the server is stopped after each test.
    /// </summary>
    public sealed class PipelineTestServer : IDisposable
    {
        private readonly TestEditorPipelineServer m_Server;
        private readonly PipelineClient m_Client;

        public int Port => m_Server.Port;
        public PipelineClient Client => m_Client;

        public PipelineTestServer()
        {
            m_Server = new TestEditorPipelineServer();
            m_Server.Start(); // auto-assigns a port in 7850-7899; writes no descriptor
            m_Client = new PipelineClient($"http://localhost:{m_Server.Port}", SecurityTokenManager.GetOrCreateToken());
        }

        /// <summary>
        /// Execute a command against this isolated server, pumping the server's own dispatcher
        /// while the HTTP call is in flight. This lets MainThreadRequired commands complete even
        /// when the editor update loop isn't ticking (e.g. during a synchronous run_tests that
        /// blocks the main thread), so it's safe to call from a plain [Test] without deadlocking.
        /// </summary>
        public PipelineResponse Execute(string command, object parameters = null, int timeoutMs = 30000)
        {
            // Run the whole client call on a threadpool thread (not the main thread) so none of its
            // async continuations capture Unity's SynchronizationContext. Otherwise they'd be queued
            // back to the main thread, which this method deliberately blocks below while pumping —
            // the task could never complete (sync-over-async deadlock). The server side still needs
            // the main thread, which the ProcessWorkQueue pump below provides.
            var task = Task.Run(() => m_Client.ExecuteCommandAsync(command, parameters));

            var start = DateTime.UtcNow;
            while (!task.IsCompleted)
            {
                m_Server.Dispatcher.ProcessWorkQueue();

                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    throw new TimeoutException($"Command '{command}' did not complete within {timeoutMs}ms");

                Thread.Sleep(1);
            }

            return task.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            m_Client?.Dispose();
            m_Server?.Stop();
        }
    }
}
