using NUnit.Framework;
using Unity.Pipeline.Models;
using Unity.Pipeline.Runtime.Commands;
using Unity.Pipeline.Tests;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the eval command (CodeEvalCommand), exercised directly and via PipelineClient.
    /// Compiler-level behavior (EvalCodeCompiler) is covered by EvalCodeCompilerTests.
    /// </summary>
    public class CodeEvalCommandTests
    {
        #region Direct

        [Test]
        public void EvaluateCode_SimpleArithmetic_ReturnsResult()
        {
            var r = CodeEvalCommand.EvaluateCode("return 2 + 2;");
            Assert.IsTrue(r.Success, r.Error);
            Assert.AreEqual(4, r.Result);
        }

        [Test]
        public void EvaluateCode_StringExpression_ReturnsString()
        {
            var r = CodeEvalCommand.EvaluateCode("return \"Hello World\";");
            Assert.IsTrue(r.Success, r.Error);
            Assert.AreEqual("Hello World", r.Result);
        }

        [Test]
        public void EvaluateCode_UnityApi_ReturnsVersion()
        {
            var r = CodeEvalCommand.EvaluateCode("return Application.unityVersion;");
            Assert.IsTrue(r.Success, r.Error);
            Assert.IsInstanceOf<string>(r.Result);
        }

        [Test]
        public void EvaluateCode_DebugLog_ReturnsExplicitValue()
        {
            var r = CodeEvalCommand.EvaluateCode("Debug.Log(\"x\"); return \"logged\";");
            Assert.IsTrue(r.Success, r.Error);
            Assert.AreEqual("logged", r.Result);
        }

        [Test]
        public void EvaluateCode_EmptyCode_BadRequest()
        {
            var r = CodeEvalCommand.EvaluateCode("");
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        [Test]
        public void EvaluateCode_NullCode_BadRequest()
        {
            var r = CodeEvalCommand.EvaluateCode(null);
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        [Test]
        public void EvaluateCode_SyntaxError_CompilationFailed()
        {
            var r = CodeEvalCommand.EvaluateCode("return 2 +;");
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Compilation Failed", r.Error);
            Assert.Greater(r.Diagnostics.Count, 0);
        }

        [Test]
        public void EvaluateCode_RuntimeException_Fails()
        {
            var r = CodeEvalCommand.EvaluateCode("throw new System.Exception(\"boom\");");
            Assert.IsFalse(r.Success);
            Assert.IsNotNull(r.ErrorDetails);
        }

        [Test]
        public void EvaluateCode_ZeroTimeout_BadRequest()
        {
            var r = CodeEvalCommand.EvaluateCode("return 1;", timeout: 0);
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        [Test]
        public void EvaluateCode_ExcessiveTimeout_BadRequest()
        {
            var r = CodeEvalCommand.EvaluateCode("return 1;", timeout: 40000);
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        [Test]
        public void EvaluateCode_RecordsExecutionTime()
        {
            var r = CodeEvalCommand.EvaluateCode("return 42;", timeout: 5000);
            Assert.IsTrue(r.Success, r.Error);
            Assert.Greater(r.ExecutionTimeMs, 0);
        }

        #endregion

        #region EvalFile

        [Test]
        public void EvaluateFile_FromFile_ReturnsResult()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eval_test_" + System.Guid.NewGuid().ToString("N") + ".cs");
            System.IO.File.WriteAllText(path, "return 2 + 2;");
            try
            {
                var r = CodeEvalCommand.EvaluateFile(path);
                Assert.IsTrue(r.Success, r.Error);
                Assert.AreEqual(4, r.Result);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Test]
        public void EvaluateFile_FileNotFound_BadRequest()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eval_missing_" + System.Guid.NewGuid().ToString("N") + ".cs");
            var r = CodeEvalCommand.EvaluateFile(path);
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        [Test]
        public void EvaluateFile_NonCsFile_BadRequest()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eval_test_" + System.Guid.NewGuid().ToString("N") + ".txt");
            System.IO.File.WriteAllText(path, "return 2 + 2;");
            try
            {
                var r = CodeEvalCommand.EvaluateFile(path);
                Assert.IsFalse(r.Success);
                Assert.AreEqual("Bad Request", r.Error);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Test]
        public void EvaluateFile_EmptyFile_BadRequest()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eval_test_" + System.Guid.NewGuid().ToString("N") + ".cs");
            System.IO.File.WriteAllText(path, "   ");
            try
            {
                var r = CodeEvalCommand.EvaluateFile(path);
                Assert.IsFalse(r.Success);
                Assert.AreEqual("Bad Request", r.Error);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Test]
        public void EvaluateFile_NullFile_BadRequest()
        {
            var r = CodeEvalCommand.EvaluateFile(null);
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Bad Request", r.Error);
        }

        #endregion

        #region ViaClient

        [Test]
        public void Eval_ViaClient_ReturnsResult()
        {
            using (var server = new PipelineTestServer())
            {
                // The test client authenticates with the server's bearer token.
                var response = server.Execute("eval", new { code = "return Application.platform.ToString();", timeout = 5000 });
                Assert.IsTrue(response.IsSuccess, response.Error);

                var r = response.GetTypedResponse<EvalResponse>();
                Assert.IsNotNull(r, "Should deserialize an EvalResponse");
                Assert.IsTrue(r.Success, r.Error);
                Assert.IsInstanceOf<string>(r.Result);
            }
        }

        [Test]
        public void Eval_ViaClient_SyntaxError_CompilationFailed()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("eval", new { code = "return 2 +;", timeout = 5000 });

                var r = response.GetTypedResponse<EvalResponse>();
                Assert.IsNotNull(r, "Should deserialize an EvalResponse");
                Assert.IsFalse(r.Success);
                Assert.AreEqual("Compilation Failed", r.Error);
            }
        }

        [Test]
        public void EvalFile_ViaClient_ReturnsResult()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eval_test_" + System.Guid.NewGuid().ToString("N") + ".cs");
            System.IO.File.WriteAllText(path, "return 6 * 7;");
            try
            {
                using (var server = new PipelineTestServer())
                {
                    var response = server.Execute("eval_file", new { file = path, timeout = 5000 });
                    Assert.IsTrue(response.IsSuccess, response.Error);

                    var r = response.GetTypedResponse<EvalResponse>();
                    Assert.IsNotNull(r, "Should deserialize an EvalResponse");
                    Assert.IsTrue(r.Success, r.Error);
                    Assert.AreEqual(42, r.Result);
                }
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        #endregion
    }
}
