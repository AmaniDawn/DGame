using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>The analysis cannot continue: its capture was closed or the Profiler's history changed under it.</summary>
    sealed class AnalysisInterruptedException : Exception
    {
        internal AnalysisInterruptedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Runs capture analysis in slices of <see cref="BudgetMs"/> on the main thread, with Editor ticks in between and
    /// sample walks on workers; fails the run if the Profiler's capture changes.
    /// </summary>
    /// <remarks>
    /// Create it before the command's first <c>await</c>: it captures <see cref="CommandExecutionContext.Current"/>, which is
    /// only set for the synchronous part of the invocation.
    /// </remarks>
    sealed class AnalysisSlicer
    {
        internal const double BudgetMs = 8;

        internal const string HistoryChangedMessage = "The Profiler history changed while this analysis ran: a capture was loaded or cleared, or frames were recorded. Nothing was returned; run the command again.";

        internal const string ClosedMessage = "The capture session was closed while this analysis ran. Open it again with " + CommandNames.DiagnosticsProfilerSession + ".";

        readonly ProfilerCaptureSession m_Session;
        readonly int m_Generation;
        readonly CommandExecutionContext m_Context;
        readonly Stopwatch m_Slice;

        internal AnalysisSlicer(ProfilerCaptureSession session)
        {
            m_Session = session;
            m_Generation = session.LoadGeneration;
            m_Context = CommandExecutionContext.Current;
            m_Slice = Stopwatch.StartNew();
        }

        /// <summary>Completes at once while the current slice has budget left; otherwise lets the Editor tick and resumes on the main thread.</summary>
        internal Task CheckpointAsync()
        {
            if (m_Slice.Elapsed.TotalMilliseconds < BudgetMs)
                return Task.CompletedTask;
            return YieldAsync();
        }

        /// <summary>Runs <paramref name="work"/> on a worker thread and resumes on the main thread. It must not touch the Profiler API.</summary>
        internal async Task<T> OffMainThreadAsync<T>(Func<T> work)
        {
            var result = await Task.Run(work);
            ThrowIfInterrupted();
            m_Slice.Restart();
            return result;
        }

        async Task YieldAsync()
        {
            await NextEditorUpdate();
            ThrowIfInterrupted();
            m_Slice.Restart();
        }

        static Task NextEditorUpdate()
        {
            var source = new TaskCompletionSource<bool>();
            EditorApplication.CallbackFunction handler = null;
            handler = () =>
            {
                EditorApplication.update -= handler;
                source.TrySetResult(true);
            };
            EditorApplication.update += handler;
            return source.Task;
        }

        void ThrowIfInterrupted()
        {
            if (m_Context != null && m_Context.IsCancellationRequested)
                throw new OperationCanceledException("Pipeline job cancellation was requested");
            if (m_Session.IsClosed)
                throw new AnalysisInterruptedException(ClosedMessage);
            if (m_Session.IsStale
                || !ProfilerCaptureSessions.IsResident(m_Session)
                || m_Session.LoadGeneration != m_Generation
                || ProfilerRecordCommands.IsActive)
                throw new AnalysisInterruptedException(HistoryChangedMessage);
        }
    }
}
