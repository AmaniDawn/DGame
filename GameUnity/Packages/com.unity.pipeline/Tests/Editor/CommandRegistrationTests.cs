using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for command registration system using [CliCommand] and [CliArg] attributes.
    /// These test the attribute-based command definition that CLI tools will discover.
    /// </summary>
    public class CommandRegistrationTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure TypeCache discovery is available for tests
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }
        [Test]
        public void CliCommandAttribute_AppliedToMethod_CanBeRetrievedViaReflection()
        {
            // Arrange - Get the test command method (private static, resolved via reflection)
            var methodInfo = typeof(DummyCommands).GetMethod("TestLogCommand",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "Test command method should exist");

            // Act - Try to get the CliCommand attribute
            var attribute = methodInfo.GetCustomAttribute<CliCommandAttribute>();

            // Assert - Attribute should be present and contain expected data
            Assert.IsNotNull(attribute, "Method should have CliCommand attribute");
            Assert.AreEqual("log_editor", attribute.Name, "Command name should match attribute");
            Assert.AreEqual("Log a message to Unity Editor console", attribute.Description,
                "Command description should match attribute");
        }

        [Test]
        public void CliArgAttribute_AppliedToParameter_CanBeRetrievedViaReflection()
        {
            // Arrange - Get the test command method and its parameters (private static)
            var methodInfo = typeof(DummyCommands).GetMethod("TestLogCommand",
                BindingFlags.NonPublic | BindingFlags.Static);
            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(1, parameters.Length, "Test command should have one parameter");

            // Act - Try to get the CliArg attribute from the parameter
            var parameter = parameters[0];
            var attribute = parameter.GetCustomAttribute<CliArgAttribute>();

            // Assert - Parameter attribute should be present and contain expected data
            Assert.IsNotNull(attribute, "Parameter should have CliArg attribute");
            Assert.AreEqual("message", attribute.Name, "Parameter name should match attribute");
            Assert.AreEqual("The message to log", attribute.Description,
                "Parameter description should match attribute");
            Assert.IsTrue(attribute.Required, "Parameter should be marked as required");
        }

        [Test]
        public void CommandRegistry_DiscoverCommands_FindsRegisteredCommands()
        {
            // Arrange & Act - Discover all commands via CommandRegistry
            var commands = CommandRegistry.DiscoverCommands();

            // Assert - Should find our test command
            Assert.IsNotNull(commands, "Command discovery should return a collection");
            Assert.Greater(commands.Count(), 0, "Should discover at least one command");

            // Find our specific test command
            var testCommand = commands.FirstOrDefault(cmd => cmd.Name == "log_editor");
            Assert.IsNotNull(testCommand, "Should discover the log_editor test command");
            Assert.AreEqual("Log a message to Unity Editor console", testCommand.Description);
            Assert.IsTrue(testCommand.MainThreadRequired, "Test command should require main thread");

            // Verify parameter information
            Assert.AreEqual(1, testCommand.Parameters.Count, "Command should have one parameter");
            var messageParam = testCommand.Parameters.First();
            Assert.AreEqual("message", messageParam.Name);
            Assert.AreEqual("The message to log", messageParam.Description);
            Assert.IsTrue(messageParam.Required);
            Assert.AreEqual(typeof(string), messageParam.ParameterType);
        }

        [Test]
        public void CommandRegistry_DiscoverCommands_PopulatesRuntimeOnlyFlag()
        {
            // Arrange & Act
            var commands = CommandRegistry.DiscoverCommands().ToList();

            // Assert - runtime commands are tagged RuntimeOnly, editor commands are not
            var editorStatus = commands.First(c => c.Name == "editor_status");
            var runtimeStatus = commands.First(c => c.Name == "runtime_status");

            Assert.IsTrue(runtimeStatus.RuntimeOnly, "eval should be marked RuntimeOnly");
            Assert.IsFalse(editorStatus.RuntimeOnly, "editor_status should not be RuntimeOnly");
        }

        [Test]
        public void JsonSchemaGeneration_FromCommandInfo_CreatesValidSchema()
        {
            // Arrange - Get our test command
            var commands = CommandRegistry.DiscoverCommands();
            var testCommand = commands.First(cmd => cmd.Name == "log_editor");

            // Act - Generate JSON schema for the command
            var schema = JsonSchemaGenerator.GenerateCommandSchema(testCommand);

            // Assert - Schema should be valid JSON Schema format
            Assert.IsNotNull(schema, "Schema should not be null");

            // Parse as JSON to verify structure
            var schemaJson = JObject.Parse(schema);

            // Verify root schema properties
            Assert.AreEqual("object", schemaJson["type"]?.ToString());
            Assert.IsNotNull(schemaJson["properties"], "Schema should have properties section");
            Assert.IsNotNull(schemaJson["required"], "Schema should have required array");

            // Verify command metadata
            Assert.AreEqual("log_editor", schemaJson["title"]?.ToString());
            Assert.AreEqual("Log a message to Unity Editor console", schemaJson["description"]?.ToString());

            // Verify parameter schema
            var properties = schemaJson["properties"] as JObject;
            Assert.IsNotNull(properties["message"], "Should have message parameter");

            var messageProperty = properties["message"] as JObject;
            Assert.AreEqual("string", messageProperty["type"]?.ToString());
            Assert.AreEqual("The message to log", messageProperty["description"]?.ToString());

            // Verify required array
            var required = schemaJson["required"] as JArray;
            Assert.Contains("message", required.ToObject<string[]>());
        }

        [Test]
        public void JsonSchemaGeneration_MultipleParameterTypes_MapsTypesCorrectly()
        {
            // Arrange - Get test command with multiple parameter types
            var commands = CommandRegistry.DiscoverCommands();
            var testCommand = commands.First(cmd => cmd.Name == "test_types");

            // Act - Generate schema
            var schema = JsonSchemaGenerator.GenerateCommandSchema(testCommand);
            var schemaJson = JObject.Parse(schema);

            // Assert - Verify different parameter types are mapped correctly
            var properties = schemaJson["properties"] as JObject;

            // Integer parameter
            var countProperty = properties["count"] as JObject;
            Assert.AreEqual("integer", countProperty["type"]?.ToString());
            Assert.AreEqual(1, countProperty["default"]?.ToObject<int>());

            // Boolean parameter
            var enabledProperty = properties["enabled"] as JObject;
            Assert.AreEqual("boolean", enabledProperty["type"]?.ToString());
            Assert.AreEqual(false, enabledProperty["default"]?.ToObject<bool>());

            // Float parameter
            var factorProperty = properties["factor"] as JObject;
            Assert.AreEqual("number", factorProperty["type"]?.ToString());
            Assert.AreEqual(1.0f, factorProperty["default"]?.ToObject<float>());

            // Verify no required parameters (all have defaults)
            var required = schemaJson["required"] as JArray;
            Assert.AreEqual(0, required.Count, "All parameters should be optional with default values");
        }

        [Test]
        public void JsonSchemaGeneration_StructuredObjectParameter_EmitsNestedObjectSchema()
        {
            // Arrange - command whose single parameter is an IStructuredCommandInput DTO
            var commands = CommandRegistry.DiscoverCommands();
            var testCommand = commands.First(cmd => cmd.Name == "test_structured");

            // Act
            var schemaJson = JObject.Parse(JsonSchemaGenerator.GenerateCommandSchema(testCommand));

            // Assert - the payload param is a real nested object schema, not a "string" fallback
            var payload = schemaJson["properties"]?["payload"] as JObject;
            Assert.IsNotNull(payload, "payload parameter should be present");
            Assert.AreEqual("object", payload["type"]?.ToString(), "structured param should map to object");
            Assert.AreEqual(false, payload["additionalProperties"]?.ToObject<bool>());

            var props = payload["properties"] as JObject;
            Assert.IsNotNull(props, "nested object should expose its own properties");

            // Scalar member with description from [CliArg]
            Assert.AreEqual("string", props["name"]?["type"]?.ToString());
            Assert.AreEqual("Display name", props["name"]?["description"]?.ToString());
            Assert.AreEqual("integer", props["count"]?["type"]?.ToString());

            // Array member emits items schema
            Assert.AreEqual("array", props["tags"]?["type"]?.ToString());
            Assert.AreEqual("string", props["tags"]?["items"]?["type"]?.ToString());

            // Nested structured member recurses
            var nested = props["nested"] as JObject;
            Assert.IsNotNull(nested, "nested structured member should be present");
            Assert.AreEqual("object", nested["type"]?.ToString());
            Assert.AreEqual("boolean", nested["properties"]?["enabled"]?["type"]?.ToString());

            // Required propagates from [CliArg(Required = true)] on a member
            var required = payload["required"]?.ToObject<string[]>();
            Assert.IsNotNull(required, "structured object should have a required array");
            CollectionAssert.Contains(required, "name");
        }

        [Test]
        public void EditorStatusCommand_IsDiscovered_ByCommandRegistry()
        {
            // Arrange & Act - Discover all commands
            var commands = CommandRegistry.DiscoverCommands();

            // Assert - Should find the editor_status command
            var editorStatusCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_status");
            Assert.IsNotNull(editorStatusCommand, "Should discover the editor_status command");
            Assert.AreEqual("Get detailed Unity Editor status and state information", editorStatusCommand.Description);
            Assert.IsTrue(editorStatusCommand.MainThreadRequired, "editor_status should require main thread");

            // Verify it has no parameters (editor status doesn't need input)
            Assert.AreEqual(0, editorStatusCommand.Parameters.Count, "editor_status should have no parameters");
        }

        [Test]
        public void EditorStatusCommand_DirectExecution_WorksCorrectly()
        {
            // Arrange & Act - Execute editor_status command directly (bypass all HTTP and threading)
            try
            {
                var result = Unity.Pipeline.Editor.Commands.EditorStatusCommand.GetEditorStatus();

                // Assert
                Assert.IsNotNull(result, "editor_status command should return a result");
                Assert.IsInstanceOf<Unity.Pipeline.Models.StatusResponse>(result, "Should return StatusResponse type");

                var status = result as Unity.Pipeline.Models.StatusResponse;
                Assert.IsNotNull(status.Status, "Status should have Status field");
                Assert.IsNotNull(status.UnityVersion, "Status should have UnityVersion field");
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Test commands for verifying command registration attributes.
    /// </summary>
    public static class DummyCommands
    {
        // Private on purpose: verifies the registry can discover and execute
        // non-public static methods (see CommandRegistry.CreateCommandInfo).
        [CliCommand("log_editor", "Log a message to Unity Editor console")]
        private static void TestLogCommand([CliArg("message", "The message to log", Required = true)] string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [CliCommand("test_types", "Test command with various parameter types")]
        public static void TestTypesCommand(
            [CliArg("count", "Number of items")] int count = 1,
            [CliArg("enabled", "Whether feature is enabled")] bool enabled = false,
            [CliArg("factor", "Multiplier factor")] float factor = 1.0f)
        {
            // Test command with various types
        }

        [CliCommand("test_structured", "Test command with a structured object parameter")]
        public static void TestStructuredCommand(
            [CliArg("payload", "Structured payload")] SampleStructuredInput payload = null)
        {
            // Test command with a nested-object parameter
        }
    }

    /// <summary>Structured-input DTO used to verify nested object schema generation.</summary>
    public class SampleStructuredInput : IStructuredCommandInput
    {
        [CliArg("name", "Display name", Required = true)]
        public string Name { get; set; }

        [CliArg("count", "How many")]
        public int Count { get; set; }

        [CliArg("tags", "Associated tags")]
        public string[] Tags { get; set; }

        [CliArg("nested", "Nested child object")]
        public SampleNestedInput Nested { get; set; }
    }

    public class SampleNestedInput : IStructuredCommandInput
    {
        [CliArg("enabled", "Toggle")]
        public bool Enabled { get; set; }
    }
}