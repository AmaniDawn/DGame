using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Models;
using Unity.Pipeline.Tests.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for Editor play mode control commands.
    /// These test the Unity Editor automation capabilities that CLI tools will use.
    /// </summary>
    /// <remarks>
    /// Excluded from the default (dogfood) run: these tests set EditorApplication.isPlaying, which
    /// flips the live editor that agents drive over HTTP into play mode. <see cref="PlayModeReloadGuard"/>
    /// disables domain reload for the duration of the run, so entering play mode no longer reloads the
    /// domain (the previous "unexpected assembly reload" abort) or tears down the server — but because
    /// they still mutate the editor's play state, run them deliberately from the Test Runner window
    /// rather than in the default suite.
    /// </remarks>
    [Explicit("Drives EditorApplication.isPlaying (flips the live editor into play mode). Run deliberately from the Test Runner window; PlayModeReloadGuard disables the domain reload that previously aborted the run.")]
    [Category("ServerLifecycle")]
    public class PlayModeCommandsTests
    {
        // Whether the live editor pipeline server was advertising before this test, so TearDown can
        // restore it (entering/exiting play mode tears it down; with "Reload Domain disabled" the
        // editor's [InitializeOnLoad] does not re-run to restart it).
        private bool m_GlobalServerWasRunning;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Setup command discovery
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

            // Remember whether a live server was running so TearDown can restore it (and so we never
            // *start* one that wasn't there, e.g. in CI).
            m_GlobalServerWasRunning =
                InstanceDescriptor.ReadFromProjectRoot(System.IO.Path.GetDirectoryName(Application.dataPath)) != null;

            // Ensure we're in edit mode before starting tests
            if (EditorApplication.isPlaying)
            {
                yield return SetPlayMode(false);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Ensure we exit play mode after tests
            if (EditorApplication.isPlaying)
            {
                yield return SetPlayMode(false);
            }

            // Restore the live editor server that play mode tore down, so the autonomous loop / agent
            // can keep driving it after these tests run. Only if it was running before (don't start
            // one that wasn't there).
            if (m_GlobalServerWasRunning)
            {
                PipelineServerStartup.RestartServer();
            }
        }

        static IEnumerator SetPlayMode(bool enterPlayMode)
        {
            EditorApplication.isPlaying = enterPlayMode;
            yield return null;
        }

        [Test]
        public void EditorPlayCommand_IsDiscovered_ByCommandRegistry()
        {
            // Arrange & Act - Discover all commands
            var commands = CommandRegistry.DiscoverCommands();

            // Assert - Should find the editor_play command
            var editorPlayCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_play");
            Assert.IsNotNull(editorPlayCommand, "Should discover the editor_play command");
            Assert.AreEqual("Enter Unity Editor play mode", editorPlayCommand.Description);
            Assert.IsTrue(editorPlayCommand.MainThreadRequired, "editor_play should require main thread");

            // Verify it has no parameters (play mode doesn't need input)
            Assert.AreEqual(0, editorPlayCommand.Parameters.Count, "editor_play should have no parameters");
        }

        [UnityTest]
        public IEnumerator EditorPlayCommand_DirectExecution_EntersPlayMode()
        {
            // Arrange - Ensure we're in edit mode
            Assert.IsFalse(EditorApplication.isPlaying, "Should start in edit mode");

            // Act - Execute editor_play command directly (bypass HTTP)
            var commands = CommandRegistry.DiscoverCommands();
            var editorPlayCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_play");
            Assert.IsNotNull(editorPlayCommand, "editor_play command should be available");

            // Execute the command method directly
            var result = editorPlayCommand.Method.Invoke(null, new object[0]);

            yield return null;

            // Assert - Should have entered play mode
            Assert.IsTrue(EditorApplication.isPlaying, "Editor should be in play mode after editor_play command");
        }

        [Test]
        public async Task EditorPlayCommand_ViaApiExec_EntersPlayMode()
        {
            // Arrange - Start server and ensure edit mode
            var server = new EditorPipelineServer();
            server.Start();

            Assert.IsFalse(EditorApplication.isPlaying, "Should start in edit mode");

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Act - Execute editor_play command via HTTP API
                    var response = await client.ExecuteCommandAsync("editor_play", null);

                    // Assert - Command should execute successfully
                    Assert.IsTrue(response.IsSuccess,
                        $"editor_play command should execute successfully. Response: {response.RawResponse}");

                    Assert.IsTrue(response.JsonResponse["success"].ToObject<bool>(), "Command should report success");
                    Assert.AreEqual("editor_play", response.JsonResponse["command"]?.ToString());

                    // Assert - Editor should be in play mode
                    Assert.IsTrue(EditorApplication.isPlaying, "Editor should be in play mode after HTTP execution");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public async Task EditorStatusCommand_ReflectsPlayModeState_Accurately()
        {
            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Step 1: Verify editor_status shows "stopped" in edit mode
                    var editModeResponse = await client.GetAsync("/api/editor_status");
                    Assert.AreEqual("stopped", editModeResponse.JsonResponse["playMode"]?.ToString(),
                        "editor_status should show 'stopped' in edit mode");

                    // Step 2: Enter play mode and wait for actual state change
                    EditorApplication.isPlaying = true;

                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);

                    var playModeResponse = await client.GetAsync("/api/editor_status");
                    Assert.AreEqual("playing", playModeResponse.JsonResponse["playMode"]?.ToString(),
                        "editor_status should show 'playing' in play mode");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public void EditorStopCommand_IsDiscovered_ByCommandRegistry()
        {
            // Arrange & Act - Discover all commands
            var commands = CommandRegistry.DiscoverCommands();

            // Assert - Should find the editor_stop command
            var editorStopCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_stop");
            Assert.IsNotNull(editorStopCommand, "Should discover the editor_stop command");
            Assert.AreEqual("Exit Unity Editor play mode", editorStopCommand.Description);
            Assert.IsTrue(editorStopCommand.MainThreadRequired, "editor_stop should require main thread");

            // Verify it has no parameters
            Assert.AreEqual(0, editorStopCommand.Parameters.Count, "editor_stop should have no parameters");
        }

        [UnityTest]
        public IEnumerator EditorStopCommand_FromPlayMode_ExitsToEditMode()
        {
            // Arrange - Enter play mode first
            yield return SetPlayMode(true);
            Assert.IsTrue(EditorApplication.isPlaying, "Should be in play mode");

            // Act - Execute editor_stop command directly
            var commands = CommandRegistry.DiscoverCommands();
            var editorStopCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_stop");
            Assert.IsNotNull(editorStopCommand, "editor_stop command should be available");

            var result = editorStopCommand.Method.Invoke(null, new object[0]);
            yield return null;

            // Assert - Should have exited play mode
            Assert.IsFalse(EditorApplication.isPlaying, "Editor should be in edit mode after editor_stop command");
            Assert.IsInstanceOf<string>(result, "Command should return a string result");

            string resultMessage = result as string;
            Assert.That(resultMessage, Contains.Substring("Exited play mode"),
                "Command should return descriptive success message");
        }

        [Test]
        public void EditorStopCommand_AlreadyInEditMode_HandlesGracefully()
        {
            Assert.IsFalse(EditorApplication.isPlaying, "Should be in edit mode");

            // Act - Execute editor_stop command when already stopped
            var commands = CommandRegistry.DiscoverCommands();
            var editorStopCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_stop");

            var result = editorStopCommand.Method.Invoke(null, new object[0]);

            // Assert - Should remain in edit mode and handle gracefully
            Assert.IsFalse(EditorApplication.isPlaying, "Editor should remain in edit mode");
            Assert.IsInstanceOf<string>(result, "Command should return a string result");

            string resultMessage = result as string;
            Assert.That(resultMessage, Contains.Substring("Already in edit mode").IgnoreCase,
                "Command should indicate it was already in the desired state");
        }

        [Test]
        public async Task PlayStopCycle_ViaHttpApi_WorksEndToEnd()
        {
            // Arrange - Start server and ensure edit mode
            var server = new EditorPipelineServer();
            server.Start();

            Assert.IsFalse(EditorApplication.isPlaying, "Should start in edit mode");

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Step 1: Enter play mode via HTTP
                    var playResponse = await client.ExecuteCommandAsync("editor_play", null);
                    Assert.IsTrue(playResponse.IsSuccess, "editor_play via HTTP should succeed");

                    // Wait for Editor to actually enter play mode
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);
                    Assert.IsTrue(EditorApplication.isPlaying, "Should be in play mode after HTTP editor_play");

                    // Step 2: Exit play mode via HTTP
                    var stopResponse = await client.ExecuteCommandAsync("editor_stop", null);
                    Assert.IsTrue(stopResponse.IsSuccess, "editor_stop via HTTP should succeed");

                    // Wait for Editor to actually exit play mode
                    await EditorTestUtilities.WaitFor(() => !EditorApplication.isPlaying);
                    Assert.IsFalse(EditorApplication.isPlaying, "Should be in edit mode after HTTP editor_stop");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public void EditorPauseCommand_IsDiscovered_ByCommandRegistry()
        {
            // Arrange & Act - Discover all commands
            var commands = CommandRegistry.DiscoverCommands();

            // Assert - Should find the editor_pause command
            var editorPauseCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_pause");
            Assert.IsNotNull(editorPauseCommand, "Should discover the editor_pause command");
            Assert.AreEqual("Pause Unity Editor play mode", editorPauseCommand.Description);
            Assert.IsTrue(editorPauseCommand.MainThreadRequired, "editor_pause should require main thread");

            // Verify it has no parameters
            Assert.AreEqual(0, editorPauseCommand.Parameters.Count, "editor_pause should have no parameters");
        }

        [UnityTest]
        public IEnumerator EditorPauseCommand_InPlayMode_TogglesPauseState()
        {
            // Arrange - Enter play mode first
            EditorApplication.isPlaying = true;
            yield return null; // Wait for play mode to activate

            Assert.IsTrue(EditorApplication.isPlaying, "Should be in play mode");
            Assert.IsFalse(EditorApplication.isPaused, "Should not be paused initially");

            // Act - Execute editor_pause command to pause
            var commands = CommandRegistry.DiscoverCommands();
            var editorPauseCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_pause");
            Assert.IsNotNull(editorPauseCommand, "editor_pause command should be available");

            var pauseResult = editorPauseCommand.Method.Invoke(null, new object[0]);
            yield return null; // Wait for pause state to change

            // Assert - Should be paused
            Assert.IsTrue(EditorApplication.isPaused, "Editor should be paused after first editor_pause");
            Assert.IsInstanceOf<string>(pauseResult, "Command should return string result");

            string pauseMessage = pauseResult as string;
            Assert.That(pauseMessage, Contains.Substring("paused"), "Should indicate paused state");

            // Act - Execute editor_pause again to unpause
            var unpauseResult = editorPauseCommand.Method.Invoke(null, new object[0]);
            yield return null; // Wait for pause state to change

            // Assert - Should be unpaused
            Assert.IsFalse(EditorApplication.isPaused, "Editor should be unpaused after second editor_pause");
            string unpauseMessage = unpauseResult as string;
            Assert.That(unpauseMessage, Contains.Substring("unpaused"), "Should indicate unpaused state");
        }

        [Test]
        public void EditorPauseCommand_InEditMode_HandlesGracefully()
        {
            // Arrange - Ensure we're in edit mode (not playing)
            EditorApplication.isPlaying = false;
            Assert.IsFalse(EditorApplication.isPlaying, "Should be in edit mode");

            // Act - Try to pause when not in play mode
            var commands = CommandRegistry.DiscoverCommands();
            var editorPauseCommand = commands.FirstOrDefault(cmd => cmd.Name == "editor_pause");

            var result = editorPauseCommand.Method.Invoke(null, new object[0]);

            // Assert - Should handle gracefully and provide clear message
            Assert.IsFalse(EditorApplication.isPlaying, "Should remain in edit mode");
            Assert.IsInstanceOf<string>(result, "Command should return string result");

            string resultMessage = result as string;
            Assert.That(resultMessage, Contains.Substring("Cannot pause").IgnoreCase,
                "Command should explain why pause failed");
            Assert.That(resultMessage, Contains.Substring("not in play mode").IgnoreCase,
                "Command should mention play mode requirement");
        }

        [Test]
        public async Task CompletePlayModeWorkflow_AllCommands_WorkEndToEnd()
        {
            Assert.IsFalse(EditorApplication.isPlaying, "Should start in edit mode");

            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Step 1: Enter play mode
                    var playResponse = await client.ExecuteCommandAsync("editor_play", null);
                    Assert.IsTrue(playResponse.IsSuccess, "editor_play should succeed");

                    // Wait for Editor to actually enter play mode
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);
                    Assert.IsTrue(EditorApplication.isPlaying, "Should be in play mode");
                    Assert.IsFalse(EditorApplication.isPaused, "Should not be paused");

                    // Step 2: Pause play mode
                    var pauseResponse = await client.ExecuteCommandAsync("editor_pause", null);
                    Assert.IsTrue(pauseResponse.IsSuccess, "editor_pause should succeed");

                    // Wait for Editor to actually enter pause state
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying && EditorApplication.isPaused);
                    Assert.IsTrue(EditorApplication.isPlaying, "Should still be in play mode");
                    Assert.IsTrue(EditorApplication.isPaused, "Should be paused");

                    // Step 3: Verify status endpoint reflects pause state
                    var statusResponse = await client.GetAsync("/api/editor_status");
                    Assert.AreEqual("paused", statusResponse.JsonResponse["playMode"]?.ToString(),
                        "editor_status should show 'paused' when paused");

                    // Step 4: Unpause (same pause command toggles)
                    var unpauseResponse = await client.ExecuteCommandAsync("editor_pause", null);
                    Assert.IsTrue(unpauseResponse.IsSuccess, "editor_pause (unpause) should succeed");

                    // Wait for Editor to actually exit pause state
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying && !EditorApplication.isPaused);

                    Assert.IsTrue(EditorApplication.isPlaying, "Should still be in play mode");
                    Assert.IsFalse(EditorApplication.isPaused, "Should be unpaused");

                    // Step 5: Stop play mode
                    var stopResponse = await client.ExecuteCommandAsync("editor_stop", null);
                    Assert.IsTrue(stopResponse.IsSuccess, "editor_stop should succeed");

                    // Wait for Editor to actually exit play mode
                    await EditorTestUtilities.WaitFor(() => !EditorApplication.isPlaying);

                    Assert.IsFalse(EditorApplication.isPlaying, "Should be back in edit mode");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public async Task EditorStatus_AllPlayModeStates_ReflectedAccurately()
        {
            // This test verifies that the editor_status command accurately reports all play mode states
            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    var editStatus = (await client.GetAsync("/api/editor_status")).JsonResponse;

                    Assert.AreEqual("stopped", editStatus["playMode"]?.ToString(),
                        "editor_status should show 'stopped' in edit mode");
                    Assert.AreEqual("ready", editStatus["status"]?.ToString(),
                        "Overall status should be 'ready' in edit mode");

                    // Test State 2: Play Mode (playing)
                    EditorApplication.isPlaying = true;
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);

                    var playStatus = (await client.GetAsync("/api/editor_status")).JsonResponse;

                    Assert.AreEqual("playing", playStatus["playMode"]?.ToString(),
                        "editor_status should show 'playing' in play mode");
                    Assert.AreEqual("playing", playStatus["status"]?.ToString(),
                        "Overall status should be 'playing' when in play mode");

                    // Test State 3: Paused Play Mode (paused)
                    EditorApplication.isPaused = true;
                    await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);

                    var pauseStatus = (await client.GetAsync("/api/editor_status")).JsonResponse;

                    Assert.AreEqual("paused", pauseStatus["playMode"]?.ToString(),
                        "editor_status should show 'paused' when paused");
                    Assert.AreEqual("playing", pauseStatus["status"]?.ToString(),
                        "Overall status should still be 'playing' when paused");
                }
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public async Task AutonomousPlayModeAutomation_FullWorkflow_DemonstratesValue()
        {
            // This test demonstrates the complete value proposition:
            // CLI tools can fully automate Unity Editor play mode for testing/automation workflows

            var server = new EditorPipelineServer();
            server.Start();

            try
            {
                using (var client = new PipelineClient(server))
                {
                    // Step 1: Automation discovers available commands
                    var commandsJson = (await client.GetAsync("/api/commands")).JsonResponse;
                    var commands = commandsJson["commands"] as JArray;

                    var playModeCommands = commands.Cast<JObject>()
                        .Where(cmd => cmd["name"]?.ToString().StartsWith("editor_") == true)
                        .Select(cmd => cmd["name"]?.ToString())
                        .ToList();

                    Assert.Contains("editor_play", playModeCommands, "Should find editor_play command");
                    Assert.Contains("editor_stop", playModeCommands, "Should find editor_stop command");
                    Assert.Contains("editor_pause", playModeCommands, "Should find editor_pause command");

                    // Step 2: Check initial Editor state
                    await client.GetAsync("/api/editor_status");

                    // Step 3: Automation workflow - Play mode testing cycle
                    var testSequence = new[]
                    {
                        new { command = "editor_play", expectedPlayMode = "playing", description = "Enter play mode for testing" },
                        new { command = "editor_pause", expectedPlayMode = "paused", description = "Pause to inspect state" },
                        new { command = "editor_pause", expectedPlayMode = "playing", description = "Resume testing" },
                        new { command = "editor_stop", expectedPlayMode = "stopped", description = "Return to edit mode" }
                    };

                    foreach (var step in testSequence)
                    {
                        // Execute command
                        var commandResponse = await client.ExecuteCommandAsync(step.command, null);
                        Assert.IsTrue(commandResponse.IsSuccess,
                            $"Command {step.command} should execute successfully");

                        // Wait for appropriate state change based on expected result
                        if (step.expectedPlayMode == "stopped")
                        {
                            await EditorTestUtilities.WaitFor(() => !EditorApplication.isPlaying);
                        }
                        else if (step.expectedPlayMode == "playing" && step.command == "editor_play")
                        {
                            await EditorTestUtilities.WaitFor(() => EditorApplication.isPlaying);
                        }
                        else if (step.expectedPlayMode == "paused")
                        {
                            await EditorTestUtilities.WaitFor(() => EditorApplication.isPaused);
                        }
                        else if (step.expectedPlayMode == "playing" && step.command == "editor_pause")
                        {
                            // This is the unpause case
                            await EditorTestUtilities.WaitFor(() => !EditorApplication.isPaused);
                        }

                        // Verify state change
                        var verifyStatus = (await client.GetAsync("/api/editor_status")).JsonResponse;

                        Assert.AreEqual(step.expectedPlayMode, verifyStatus["playMode"]?.ToString(),
                            $"After {step.command}, playMode should be '{step.expectedPlayMode}'");
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