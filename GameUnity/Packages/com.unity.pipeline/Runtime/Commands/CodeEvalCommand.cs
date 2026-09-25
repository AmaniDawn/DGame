using System;
using Unity.Pipeline.Models;
using Unity.Pipeline.Compilation;
using UnityEngine;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Command for evaluating C# code dynamically using Roslyn compilation.
    /// Requires security token for authorization.
    /// </summary>
    public static class CodeEvalCommand
    {
        [CliCommand("eval", "Evaluate C# code dynamically using Roslyn compiler", MainThreadRequired = true)]
        public static EvalResponse EvaluateCode(
            [CliArg("code", "C# code to evaluate", Required = true)] string code,
            [CliArg("timeout", "Timeout in milliseconds")] int timeout = 5000)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    "Code parameter is required and cannot be empty");
            }

            return EvaluateSource(code, timeout);
        }

        [CliCommand("eval_file", "Evaluate C# code read from a .cs file on disk", MainThreadRequired = true)]
        public static EvalResponse EvaluateFile(
            [CliArg("file", "Path to a .cs file to evaluate", Required = true)] string file,
            [CliArg("timeout", "Timeout in milliseconds")] int timeout = 5000)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    "File parameter is required and cannot be empty");
            }

            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    "File must have a .cs extension");
            }

            if (!System.IO.File.Exists(file))
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    $"File not found: {file}");
            }

            string code;
            try
            {
                code = System.IO.File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    $"Failed to read file: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return EvalResponse.EvalFailure(
                    "Bad Request",
                    $"File is empty: {file}");
            }

            return EvaluateSource(code, timeout);
        }

        /// <summary>
        /// Shared evaluation path: compiles and executes the given C# source and returns the
        /// response. Both the <c>eval</c> and <c>eval_file</c> commands funnel their resolved
        /// source string into here.
        /// </summary>
        private static EvalResponse EvaluateSource(string code, int timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate timeout
                if (timeout <= 0 || timeout > 30000)
                {
                    stopwatch.Stop();
                    return EvalResponse.EvalFailure(
                        "Bad Request",
                        "Timeout must be between 1ms and 30000ms",
                        stopwatch.ElapsedMilliseconds);
                }

                // Execute compilation and evaluation. We're already on the main thread
                // (MainThreadRequired = true), so call the synchronous path directly.
                var result = EvalCodeCompiler.CompileAndExecuteOnMainThread(code, timeout, null);

                stopwatch.Stop();

                // Update execution time
                if (result != null)
                {
                    result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                }

                return result ?? EvalResponse.EvalFailure(
                    "Unknown Error",
                    "Compilation returned null result",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"Pipeline: Eval command failed: {ex.Message}");
                Debug.LogError($"Pipeline: Stack trace: {ex.StackTrace}");

                return EvalResponse.EvalFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}