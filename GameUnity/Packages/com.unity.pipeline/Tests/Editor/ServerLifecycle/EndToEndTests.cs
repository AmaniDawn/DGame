using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.IO;
using Unity.Pipeline.Tests.Editor;
using Unity.Pipeline.Tests.Runtime;

namespace Unity.Pipeline.Tests.Editor.ServerLifecyle
{
    /// <summary>
    /// End-to-end tests demonstrating complete Pipeline workflow.
    /// These test the full roundtrip: discovery → connection → command execution
    /// that simulates real CLI usage patterns.
    ///
    /// Marked [Explicit]: starts its own EditorPipelineServer (and manages the shared descriptor),
    /// which conflicts with the live editor server. Excluded from the normal/dogfood run.
    /// TODO: to rejoin the dogfood run, refactor to drive an isolated TestEditorPipelineServer
    /// (ports 7850+, WritesDescriptor=false) instead of starting a real server.
    /// </summary>
    [Explicit("Starts its own server; conflicts with the live editor server. Run deliberately.")]
    [Category("ServerLifecycle")]
    public class EndToEndTests
    {
        [Test]
        public async Task CompleteWorkflow_DiscoverConnectExecute_WorksEndToEnd()
        {
            // Setup
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            // Step 1: Start Pipeline Server (simulates Editor startup)
            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);

                // Step 2: CLI Discovery - Find running instances
                var instances = MockCliDiscovery.DiscoverInstances(new[] { projectPath });
                var runningInstances = instances.Where(i => i.IsRunning).ToList();

                Assert.Greater(runningInstances.Count, 0, "Should discover at least one running instance");

                var targetInstance = runningInstances.First();

                // Give server a moment to fully start
                await Task.Delay(100);

                // Step 3: CLI Connection Validation
                var isReachable = await MockCliDiscovery.ValidateConnection(targetInstance.Descriptor);
                Assert.IsTrue(isReachable, "Discovered instance should be reachable");

                // Step 4: CLI Command Discovery - Get available commands
                using (var client = new PipelineClient(server))
                {
                    var commandsResponse = await client.GetAsync("/api/commands");
                    Assert.IsTrue(commandsResponse.IsSuccess, "Should be able to get commands list");

                    var commands = commandsResponse.JsonResponse["commands"] as JArray;
                    Assert.Greater(commands.Count, 0, "Should have available commands");

                    // Step 5: CLI Command Execution - Execute log_editor command
                    var execResponse = await client.ExecuteCommandAsync(
                        "log_editor", new { message = "Hello from end-to-end test!" });

                    Assert.IsTrue(execResponse.IsSuccess,
                        $"Command execution should succeed. Response: {execResponse.RawResponse}");

                    var execData = execResponse.JsonResponse;
                    Assert.IsTrue(execData["success"].ToObject<bool>(), "Command should execute successfully");
                    Assert.AreEqual("log_editor", execData["command"]?.ToString());
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public async Task RealWorldScenario_MultipleCommands_ExecutesSequentially()
        {
            // Setup
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Simulate CLI batch execution of multiple commands
                    var commands = new CommandExecutionRequest[]
                    {
                        new CommandExecutionRequest("log_editor") { Parameters = { ["message"] = "Starting batch execution" } },
                        new CommandExecutionRequest("test_types") { Parameters = { ["count"] = 5, ["enabled"] = true, ["factor"] = 2.5f } },
                        new CommandExecutionRequest("log_editor") { Parameters = { ["message"] = "Batch execution completed" } }
                    };

                    foreach (var cmd in commands)
                    {
                        var response = await client.ExecuteCommandAsync(cmd.Command, cmd.Parameters);

                        Assert.IsTrue(response.IsSuccess,
                            $"Command '{cmd.Command}' should execute successfully. Response: {response.RawResponse}");

                        Assert.IsTrue(response.JsonResponse["success"].ToObject<bool>(),
                            $"Command '{cmd.Command}' should succeed");
                    }
                }
            }
            finally
            {
                server.Stop();
            }
        }
    }
}