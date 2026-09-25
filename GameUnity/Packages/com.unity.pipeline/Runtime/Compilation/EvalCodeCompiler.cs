using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Models;
using UnityEngine;

namespace Unity.Pipeline.Compilation
{
    /// <summary>
    /// C# code evaluation service using shared Roslyn compilation infrastructure.
    /// Wraps user code in Execute() method, compiles, and executes with result serialization.
    /// Supports both Editor and Runtime compilation for desktop development builds.
    /// </summary>
    public static class EvalCodeCompiler
    {
#if UNITY_EDITOR || (UNITY_STANDALONE && DEBUG)

        // Fixed lines in the generated source before user code begins
        private const int BaseCodeLineOffset = 11;

        /// <summary>
        /// Compile and execute code on Unity's main thread using Roslyn.
        /// Eval is a MainThreadRequired command, so the server marshals the caller to the main
        /// thread before invoking this; compilation + execution run synchronously here.
        /// </summary>
        public static EvalResponse CompileAndExecuteOnMainThread(string code, int timeoutMs, System.Diagnostics.Stopwatch stopwatch)
        {
            if (stopwatch == null)
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
            }

            try
            {
                // Compile on current thread
                var compilationResult = CompileCodeToAssembly(code, timeoutMs, stopwatch);

                if (!compilationResult.Success)
                {
                    stopwatch.Stop();
                    return EvalResponse.EvalFailure(
                        "Compilation Failed",
                        "Code compilation failed",
                        stopwatch.ElapsedMilliseconds,
                        compilationResult.Diagnostics);
                }

                // Execute on current thread
                var executionResult = ExecuteCompiledAssembly(compilationResult.Assembly);

                stopwatch.Stop();

                if (executionResult.Success)
                {
                    return EvalResponse.EvalSuccess(
                        executionResult.ReturnValue,
                        executionResult.Output,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    return EvalResponse.EvalFailure(
                        executionResult.Error,
                        executionResult.ErrorDetails,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return EvalResponse.EvalFailure(
                    "Execution Failed",
                    ex.ToString(),
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Compile C# code to assembly using shared RoslynCompilationService.
        /// Thread-safe and can run on background thread.
        /// </summary>
        private static CompilationResult CompileCodeToAssembly(string userCode, int timeoutMs, System.Diagnostics.Stopwatch stopwatch)
        {
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);

            // Generate wrapper code
            var sourceCode = BuildSourceCode(id, userCode, Application.isEditor);

            // Use shared compilation service
            var request = new CompilationRequest
            {
                SourceCode = sourceCode,
                AssemblyName = $"PipelineEval_{id}",
                LineNumberOffset = BaseCodeLineOffset // Adjust diagnostics for wrapper code
            };

            var result = RoslynCompilationService.Compile(request);

            if (result.Success)
            {
                // Add execution context for eval-specific needs
                return new CompilationResult
                {
                    Success = true,
                    Assembly = result.Assembly,
                    ExecutionContext = new ExecutionContext
                    {
                        AssemblyId = id,
                        TypeName = $"PipelineEvaluation.PipelineEval_{id}",
                        MethodName = "Execute"
                    },
                    Diagnostics = result.Diagnostics
                };
            }
            else
            {
                return new CompilationResult
                {
                    Success = false,
                    Diagnostics = result.Diagnostics
                };
            }
        }

        /// <summary>
        /// Generate C# source code wrapper for user code with conditional editor support.
        /// </summary>
        private static string BuildSourceCode(string id, string userCode, bool isEditorContext)
        {
            var editorUsing = isEditorContext ? "using UnityEditor;" : "";

            return $@"using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
{editorUsing}

namespace PipelineEvaluation
{{
    public static class PipelineEval_{id}
    {{
        public static object Execute()
        {{
            {userCode}
            return null;
        }}
    }}
}}";
        }


        /// <summary>
        /// Execute compiled assembly and return result.
        /// Must run on Unity's main thread for safe Unity API access.
        /// </summary>
        private static ExecutionResult ExecuteCompiledAssembly(Assembly assembly)
        {
            try
            {
                var executionContext = GetExecutionContextFromAssembly(assembly);

                var type = assembly.GetType(executionContext.TypeName);
                var method = type?.GetMethod(executionContext.MethodName, BindingFlags.Public | BindingFlags.Static);

                if (type == null)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = "Assembly Load Error",
                        ErrorDetails = $"Could not find type {executionContext.TypeName} in compiled assembly"
                    };
                }

                if (method == null)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = "Method Not Found",
                        ErrorDetails = $"Could not find method {executionContext.MethodName} in compiled type"
                    };
                }

                var result = method.Invoke(null, null);

                return new ExecutionResult
                {
                    Success = true,
                    ReturnValue = SerializeResult(result)
                };
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Error = "Runtime Error",
                    ErrorDetails = tie.InnerException.Message,
                    StackTrace = tie.InnerException.StackTrace
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Error = "Execution Error",
                    ErrorDetails = ex.Message,
                    StackTrace = ex.StackTrace
                };
            }
        }

        /// <summary>
        /// Get execution context from compiled assembly.
        /// </summary>
        private static ExecutionContext GetExecutionContextFromAssembly(Assembly assembly)
        {
            // Find PipelineEvaluation types
            var evalTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "PipelineEvaluation" && t.Name.StartsWith("PipelineEval_"))
                .ToList();

            if (evalTypes.Any())
            {
                var evalType = evalTypes.First();
                return new ExecutionContext
                {
                    TypeName = evalType.FullName,
                    MethodName = "Execute"
                };
            }

            // Fallback
            return new ExecutionContext
            {
                TypeName = "PipelineEvaluation.PipelineEval_Unknown",
                MethodName = "Execute"
            };
        }

        /// <summary>
        /// Serialize result for JSON response.
        /// </summary>
        private static object SerializeResult(object value)
        {
            if (value == null) return null;

            // Handle primitive types directly
            if (value is string || value is bool || value is int || value is long || value is float || value is double)
                return value;

            // Handle Unity objects
            if (value is UnityEngine.Object unityObj)
            {
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject(UnityEngine.JsonUtility.ToJson(unityObj));
                }
                catch
                {
                    return value.ToString();
                }
            }

            // Handle other objects via JSON serialization
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject(
                    Newtonsoft.Json.JsonConvert.SerializeObject(value));
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Result from compilation process.
        /// </summary>
        private class CompilationResult
        {
            public bool Success { get; set; }
            public Assembly Assembly { get; set; }
            public ExecutionContext ExecutionContext { get; set; }
            public List<DiagnosticInfo> Diagnostics { get; set; } = new List<DiagnosticInfo>();
        }

        /// <summary>
        /// Result from code execution.
        /// </summary>
        private class ExecutionResult
        {
            public bool Success { get; set; }
            public object ReturnValue { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
            public string ErrorDetails { get; set; }
            public string StackTrace { get; set; }
        }

        /// <summary>
        /// Execution context for compiled assembly.
        /// </summary>
        private class ExecutionContext
        {
            public string AssemblyId { get; set; }
            public string TypeName { get; set; }
            public string MethodName { get; set; }
        }

#else
        /// <summary>
        /// Runtime compilation not supported on this platform.
        /// Desktop development builds only (Windows/Mac/Linux).
        /// </summary>
        public static EvalResponse CompileAndExecuteOnMainThread(string code, int timeoutMs, System.Diagnostics.Stopwatch stopwatch)
        {
            return EvalResponse.EvalFailure(
                "Platform Not Supported",
                "Runtime code compilation only supported on Desktop development builds");
        }
#endif
    }
}