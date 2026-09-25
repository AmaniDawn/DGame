using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using Newtonsoft.Json.Linq;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for HTTP API endpoints that CLI tools will consume.
    /// These test the complete server API surface for remote command execution.
    /// </summary>
    public class ApiEndpointsTests
    {
        private EditorPipelineServer m_Server;
        private Unity.Pipeline.Tests.Runtime.PipelineClient m_PipelineClient;

        [SetUp]
        public void SetUp()
        {
            // Setup command discovery for tests
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            // Start an ISOLATED test server (ports 7850-7899, writes no descriptor) for endpoint
            // testing, so we never bind the live server's port (7800) or clobber its descriptor.
            m_Server = new TestEditorPipelineServer();
            m_Server.Start();

            m_PipelineClient = new Unity.Pipeline.Tests.Runtime.PipelineClient(m_Server);
        }

        [TearDown]
        public void TearDown()
        {
            m_PipelineClient?.Dispose();
            m_Server?.Stop();
        }

        [Test]
        public async Task ApiCommands_GetEndpoint_ReturnsCommandList()
        {
            // Act - Call /api/commands endpoint using unified Pipeline client
            var httpResponse = await m_PipelineClient.GetHttpAsync("/api/commands");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();

            // Assert - Response structure
            Assert.IsTrue(httpResponse.IsSuccessStatusCode,
                $"Commands endpoint should return success, got: {httpResponse.StatusCode}");
            Assert.AreEqual("application/json", httpResponse.Content.Headers.ContentType.MediaType,
                "Commands endpoint should return JSON content type");

            // Assert - JSON parsing
            var responseJson = JObject.Parse(jsonContent);
            Assert.IsNotNull(responseJson, "Should be able to parse commands JSON");

            // Verify response structure
            Assert.IsNotNull(responseJson["commands"], "Response should have commands array");
            Assert.IsNotNull(responseJson["count"], "Response should have count field");
            Assert.IsNotNull(responseJson["server"], "Response should have server info");

            // Verify commands array contains discovered commands
            var commands = responseJson["commands"] as JArray;
            Assert.Greater(commands.Count, 0, "Should have at least one discovered command");

            // Verify a specific test command is included
            var testCommand = commands.Cast<JObject>()
                .FirstOrDefault(cmd => cmd["name"]?.ToString() == "log_editor");
            Assert.IsNotNull(testCommand, "Should include log_editor test command");
            Assert.AreEqual("Log a message to Unity Editor console", testCommand["description"]?.ToString());
        }

        [Test]
        public async Task ApiCommands_OnEditorServer_ExcludesRuntimeOnlyCommands()
        {
            // Act - List commands from the Editor server
            var httpResponse = await m_PipelineClient.GetHttpAsync("/api/commands");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(jsonContent);
            var commandNames = (responseJson["commands"] as JArray)
                .Cast<JObject>()
                .Select(cmd => cmd["name"]?.ToString())
                .ToList();

            // Assert - editor commands are listed, runtime-only commands are hidden
            Assert.Contains("editor_status", commandNames, "Editor command should be listed");
            CollectionAssert.DoesNotContain(commandNames, "runtime_status", "Runtime-only eval should be hidden on the Editor server");
            CollectionAssert.DoesNotContain(commandNames, "set_target_framerate", "Runtime-only reload_file_override should be hidden on the Editor server");
        }

        [Test]
        public async Task ApiCommands_CommandStructure_ContainsRequiredFields()
        {
            // Act - Call /api/commands endpoint using unified Pipeline client
            var httpResponse = await m_PipelineClient.GetHttpAsync("/api/commands");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(jsonContent);

            // Assert - Command structure validation
            var commands = responseJson["commands"] as JArray;
            var firstCommand = commands[0] as JObject;

            // Required command fields
            Assert.IsNotNull(firstCommand["name"], "Command should have name field");
            Assert.IsNotNull(firstCommand["description"], "Command should have description field");
            Assert.IsNotNull(firstCommand["parameters"], "Command should have parameters array");
            Assert.IsNotNull(firstCommand["schema"], "Command should have JSON schema");
            Assert.IsNotNull(firstCommand["mainThreadRequired"], "Command should have mainThreadRequired field");

            // Verify schema is valid JSON
            var schema = firstCommand["schema"]?.ToString();
            var schemaJson = JObject.Parse(schema);
            Assert.AreEqual(firstCommand["name"]?.ToString(), schemaJson["title"]?.ToString(),
                "Schema title should match command name");
        }

        [Test]
        public async Task ApiStatus_GetBasicStatus_ReturnsServerInfo()
        {
            // Act - Call basic /api/status endpoint using unified Pipeline client
            var httpResponse = await m_PipelineClient.GetHttpAsync("/api/status");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();

            // Assert - Response structure
            Assert.IsTrue(httpResponse.IsSuccessStatusCode,
                $"Basic status endpoint should return success, got: {httpResponse.StatusCode}");
            Assert.AreEqual("application/json", httpResponse.Content.Headers.ContentType.MediaType,
                "Basic status endpoint should return JSON content type");

            // Assert - JSON structure (basic server info only, no Editor APIs)
            var statusJson = JObject.Parse(jsonContent);
            Assert.IsNotNull(statusJson["status"], "Should have status field");

            // Verify basic values
            Assert.AreEqual("ready", statusJson["status"]?.ToString());
        }

        [Test]
        public async Task ApiEditorStatus_GetDetailedStatus_ReturnsEditorInfo()
        {
            // Act - Call rich /api/editor_status endpoint using unified Pipeline client
            var httpResponse = await m_PipelineClient.GetHttpAsync("/api/editor_status");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();

            // Assert - Response structure
            Assert.IsTrue(httpResponse.IsSuccessStatusCode,
                $"Editor status endpoint should return success, got: {httpResponse.StatusCode}. Response: {jsonContent}");
            Assert.AreEqual("application/json", httpResponse.Content.Headers.ContentType.MediaType,
                "Editor status endpoint should return JSON content type");

            // Assert - Rich Editor status structure
            var statusJson = JObject.Parse(jsonContent);
            Assert.IsNotNull(statusJson["status"], "Should have overall status");
            Assert.IsNotNull(statusJson["compiling"], "Should have compiling state");
            Assert.IsNotNull(statusJson["domainReloadInProgress"], "Should have domain reload state");
            Assert.IsNotNull(statusJson["playMode"], "Should have play mode state");
            Assert.IsNotNull(statusJson["unityVersion"], "Should have Unity version");

            // Verify Editor-specific data is present
            Assert.Contains(statusJson["status"]?.ToString(), new[] { "ready", "compiling", "playing", "reloading" });
            Assert.Contains(statusJson["playMode"]?.ToString(), new[] { "stopped", "playing", "paused" });
            Assert.IsInstanceOf<bool>(statusJson["compiling"]?.ToObject<bool>());
        }

        [Test]
        public async Task ApiExec_PostCommand_ExecutesSuccessfully()
        {
            // Arrange
            var commandRequest = new CommandExecutionRequest("log_editor");
            commandRequest.Parameters["message"] = "Test message from CLI";

            // Act - Execute command via /api/exec endpoint using unified Pipeline client
            var response = await m_PipelineClient.PostJsonAsync("/api/exec", commandRequest);
            var responseContent = response.RawResponse;

            // Assert - Response structure
            Assert.IsTrue(response.IsSuccess,
                $"Exec endpoint should return success, got: {response.StatusCode}. Response: {responseContent}");

            // Assert - JSON parsing and structure
            Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
            var responseJson = response.JsonResponse;
            Assert.IsNotNull(responseJson["success"], "Response should have success field");
            Assert.IsNotNull(responseJson["command"], "Response should have command field");
            Assert.IsNotNull(responseJson["executedAt"], "Response should have executedAt timestamp");

            // Assert - Successful execution
            Assert.IsTrue(responseJson["success"].ToObject<bool>(), "Command should execute successfully");
            Assert.AreEqual("log_editor", responseJson["command"]?.ToString());
        }

        [Test]
        public async Task ApiExec_InvalidCommand_ReturnsError()
        {
            // Arrange
            var invalidRequest = new CommandExecutionRequest("nonexistent_command");

            // Act - Execute invalid command via /api/exec endpoint using unified Pipeline client
            var response = await m_PipelineClient.PostJsonAsync("/api/exec", invalidRequest);
            var responseContent = response.RawResponse;

            LogAssert.Expect(new Regex("^ExecuteCommandByName: No command named"));

            // Assert - Should return error
            Assert.IsFalse(response.IsSuccess, "Should return error for invalid command");

            Assert.IsTrue(response.HasValidJson, "Error response should have valid JSON");
            var responseJson = response.JsonResponse;
            Assert.IsNotNull(responseJson["error"], "Error response should have error field");
            Assert.IsNotNull(responseJson["message"], "Error response should have message field");
        }

        [Test]
        public async Task ApiExec_MissingRequiredParameter_ReturnsValidationError()
        {
            // Arrange - Try to execute log_editor without required message parameter
            var invalidRequest = new CommandExecutionRequest("log_editor");
            // Intentionally not setting the 'message' parameter to test validation

            // Act - Execute command with missing parameter via /api/exec endpoint using unified Pipeline client
            var response = await m_PipelineClient.PostJsonAsync("/api/exec", invalidRequest);
            var responseContent = response.RawResponse;

            LogAssert.Expect("ExecuteCommandByName: Parameter validation failed: Required parameter 'message' is missing or empty");

            // Assert - Should return validation error
            Assert.IsFalse(response.IsSuccess, "Should return error for missing required parameter");

            Assert.IsTrue(response.HasValidJson, "Error response should have valid JSON");
            var responseJson = response.JsonResponse;
            Assert.IsNotNull(responseJson["error"], "Should have error field");
            Assert.That(responseJson["errorDetails"]?.ToString(),
                Contains.Substring("message").IgnoreCase,
                "Error should mention missing message parameter");
        }

        [Test]
        public async Task ApiExec_OversizedBody_ReturnsPayloadTooLarge()
        {
            // Arrange - a request whose body exceeds the 1 MiB cap (Content-Length will advertise it).
            var oversized = new CommandExecutionRequest("log_editor");
            oversized.Parameters["message"] = new string('a', (1 * 1024 * 1024) + 1024);

            // Act
            var response = await m_PipelineClient.PostJsonAsync("/api/exec", oversized);

            // Assert - rejected with 413 before the command ever runs.
            Assert.AreEqual(413, response.StatusCode,
                $"Oversized body should be rejected with 413. Response: {response.RawResponse}");
            Assert.IsTrue(response.HasValidJson, "413 response should have valid JSON");
            Assert.That(response.JsonResponse["error"]?.ToString(),
                Contains.Substring("Payload Too Large"),
                "413 response should identify the payload-too-large error");
        }
    }
}