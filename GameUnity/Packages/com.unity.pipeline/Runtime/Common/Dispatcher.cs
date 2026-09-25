using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Pipeline.Threading
{
    /// <summary>
    /// Dispatches work to Unity's main thread from background threads (required for accessing Unity
    /// APIs from HTTP request handlers).
    ///
    /// Each pipeline server owns its own instance (no global singleton): it is initialized on Start
    /// and pumped from the main thread — auto-pumped via EditorApplication.update in the editor, and
    /// by RuntimePipelineManager.Update in a player.
    /// </summary>
    public class Dispatcher
    {
        private readonly ConcurrentQueue<WorkItem> m_WorkQueue = new ConcurrentQueue<WorkItem>();
        private volatile bool m_IsInitialized;
        private int m_MainThreadId = -1;

        public bool IsInitialized => m_IsInitialized;

        /// <summary>
        /// Initialize the dispatcher. Must be called from Unity's main thread.
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized)
                return;

            m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
            m_IsInitialized = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += ProcessWorkQueue;
#endif
        }

        /// <summary>
        /// Shutdown the dispatcher and cancel any pending work.
        /// </summary>
        public void Shutdown()
        {
            if (!m_IsInitialized)
                return;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= ProcessWorkQueue;
#endif

            while (m_WorkQueue.TryDequeue(out var item))
            {
                try
                {
                    item.SetException(new OperationCanceledException("Dispatcher is shutting down"));
                }
                catch { }
            }

            m_IsInitialized = false;
        }

        /// <summary>
        /// Execute a function on the main thread and return the result (synchronous wait).
        /// </summary>
        public T Invoke<T>(Func<T> function, int timeoutMs = 60000)
        {
            if (!m_IsInitialized)
                throw new InvalidOperationException("Dispatcher must be initialized first");

            if (IsMainThread())
                return function();

            var workItem = new WorkItem<T>(function);
            m_WorkQueue.Enqueue(workItem);

            var startTime = DateTime.UtcNow;
            var task = workItem.TaskCompletionSource.Task;

            while (!task.IsCompleted)
            {
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                    throw new TimeoutException($"Main thread operation timed out after {timeoutMs}ms");

                Thread.Sleep(1);
            }

            if (task.IsFaulted)
                throw task.Exception?.GetBaseException() ?? new Exception("Unknown error");

            if (task.IsCanceled)
                throw new OperationCanceledException("Main thread operation was cancelled");

            return task.Result;
        }

        /// <summary>
        /// Execute an action on the main thread.
        /// </summary>
        public void Invoke(Action action, int timeoutMs = 60000)
        {
            Invoke<object>(() =>
            {
                action();
                return null;
            }, timeoutMs);
        }

        /// <summary>
        /// Execute a function on the main thread and return the result (async version).
        /// </summary>
        public async Task<T> InvokeAsync<T>(Func<T> function, int timeoutMs = 60000)
        {
            return await Task.Run(() => Invoke(function, timeoutMs));
        }

        /// <summary>
        /// Execute an action on the main thread (async version).
        /// </summary>
        public async Task InvokeAsync(Action action, int timeoutMs = 60000)
        {
            await Task.Run(() => Invoke(action, timeoutMs));
        }

        /// <summary>
        /// Check if we're currently on Unity's main thread.
        /// </summary>
        public bool IsMainThread()
        {
            return m_MainThreadId != -1 && Thread.CurrentThread.ManagedThreadId == m_MainThreadId;
        }

        /// <summary>
        /// Process queued work items. Called from EditorApplication.update or MonoBehaviour.Update.
        /// </summary>
        /// <param name="maxItemsPerFrame">Max items to process per call, to limit frame-rate impact.</param>
        public void ProcessWorkQueue(int maxItemsPerFrame)
        {
            int processedCount = 0;

            while (processedCount < maxItemsPerFrame && m_WorkQueue.TryDequeue(out var workItem))
            {
                try
                {
                    workItem.Execute();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Dispatcher work item failed: {ex.Message}");
                    workItem.SetException(ex);
                }

                processedCount++;
            }
        }

        public void ProcessWorkQueue()
        {
            ProcessWorkQueue(10);
        }

        private abstract class WorkItem
        {
            public abstract void Execute();
            public abstract void SetException(Exception exception);
        }

        private class WorkItem<T> : WorkItem
        {
            private readonly Func<T> m_Function;
            public TaskCompletionSource<T> TaskCompletionSource { get; }

            public WorkItem(Func<T> function)
            {
                m_Function = function ?? throw new ArgumentNullException(nameof(function));
                TaskCompletionSource = new TaskCompletionSource<T>();
            }

            public override void Execute()
            {
                try
                {
                    var result = m_Function();
                    TaskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    TaskCompletionSource.SetException(ex);
                }
            }

            public override void SetException(Exception exception)
            {
                TaskCompletionSource.SetException(exception);
            }
        }
    }
}
