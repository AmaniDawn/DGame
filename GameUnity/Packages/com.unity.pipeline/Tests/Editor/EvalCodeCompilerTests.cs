using NUnit.Framework;
using Unity.Pipeline.Compilation;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for EvalCodeCompiler's synchronous main-thread compilation/execution.
    /// eval is a MainThreadRequired command, so the server marshals to the main thread before
    /// invoking; the compiler runs synchronously on the caller (no background/dispatcher path).
    /// </summary>
    public class EvalCodeCompilerTests
    {
        [Test]
        public void CompileAndExecuteOnMainThread_ReturnsValue()
        {
            var result = EvalCodeCompiler.CompileAndExecuteOnMainThread("return 42;", 5000, null);

            Assert.IsTrue(result.Success, $"Should succeed. Error: {result.Error}");
            Assert.AreEqual(42, result.Result, "Should return the evaluated value");
        }

        [Test]
        public void CompileAndExecuteOnMainThread_AccessesUnityApi()
        {
            var result = EvalCodeCompiler.CompileAndExecuteOnMainThread("return Application.unityVersion;", 5000, null);

            Assert.IsTrue(result.Success, $"Unity API access should work. Error: {result.Error}");
            Assert.IsInstanceOf<string>(result.Result, "Unity version should be a string");
            Assert.IsNotEmpty((string)result.Result, "Unity version should not be empty");
        }

        [Test]
        public void CompileAndExecuteOnMainThread_CompilationError_ReturnsDiagnostics()
        {
            var result = EvalCodeCompiler.CompileAndExecuteOnMainThread("return 2 +;", 5000, null);

            Assert.IsFalse(result.Success, "Invalid syntax should fail");
            Assert.AreEqual("Compilation Failed", result.Error, "Should report a compilation error");
            Assert.IsNotNull(result.Diagnostics, "Should have diagnostics");
            Assert.Greater(result.Diagnostics.Count, 0, "Should include compilation diagnostics");
        }
    }
}
