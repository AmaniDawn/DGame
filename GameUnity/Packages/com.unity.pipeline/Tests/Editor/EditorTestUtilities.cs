using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Utility methods for Unity Editor testing scenarios.
    /// Provides async/await support for Editor state changes and transitions.
    /// </summary>
    public static class EditorTestUtilities
    {
        public static async Task WaitFor(Func<bool> condition, int timeoutMs = 1000)
        {
            var tcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(timeoutMs);
            timeoutCts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(false); // Indicate timeout, but don't throw
                }
            });

            while (!condition())
            {
                await Task.Delay(10); // Small delay to avoid busy waiting
            }
        }

        [MenuItem("PipelineTests/Invoke Modal!")]
        public static void InvokeModal()
        {
            EditorUtility.DisplayDialog("Hello", "world", "okay", "nope");
        }
    }
}