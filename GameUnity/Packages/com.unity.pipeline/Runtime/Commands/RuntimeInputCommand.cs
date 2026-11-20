using System;
using Unity.Pipeline.Commands;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && PIPELINE_HAS_INPUT_SYSTEM_PACKAGE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// <para>
    /// Input-simulation commands that drive a running Player by injecting synthetic input — the enabler
    /// for automated play-testing (CLI-208). Keyboard and pointer events are injected through the Input
    /// System (<c>InputSystem.QueueEvent</c> with a state event captured from the live device), which also
    /// drives uGUI / UI Toolkit through the Input System UI module — so a simulated pointer click lands on
    /// on-screen UI the same way a real one does.
    /// </para>
    ///
    /// <para><b>Input stack:</b> device injection requires the Input System package to be present
    /// (<c>PIPELINE_HAS_INPUT_SYSTEM_PACKAGE</c>) and active (<c>ENABLE_INPUT_SYSTEM</c>). Legacy <c>UnityEngine.Input</c> is poll-only and exposes no injection
    /// API, so it is intentionally not supported; when the Input System is absent these commands fail with an
    /// "unavailable" error rather than silently no-op'ing.</para>
    ///
    /// <para><b>Safety:</b> these are write/destructive commands. The package has no safety-policy gate yet
    /// (CAT-2509 is not implemented), so they follow the existing unguarded write-command model (cf.
    /// <c>set_target_framerate</c>). TODO(CAT-2509): route input commands through the safety policy when it
    /// lands.</para>
    /// </summary>
    static class RuntimeInputCommand
    {
        const string Group = "runtime";

        [CliGroupCommand(Group, "Simulate a keyboard key event (Input System). Drives the running app.",
            MainThreadRequired = true,
            Tags = new[] { "runtime/input" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode SimulateKey(CommandContext cmd,
            [CliArg("key", "Input System Key name, e.g. Space, W, Enter, LeftArrow", Required = true)] string key,
            [CliArg("action", "down | up | press (down+up). Default: press")] string action = "press")
        {
#if ENABLE_INPUT_SYSTEM && PIPELINE_HAS_INPUT_SYSTEM_PACKAGE
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                cmd.Log.Error("No keyboard device is present.");
                return ExitCode.Failed;
            }

            if (!Enum.TryParse<Key>(key, ignoreCase: true, out var parsedKey) || parsedKey == Key.None)
            {
                cmd.Log.Error($"Unknown key '{key}'. Use an Input System Key name (e.g. Space, W, Enter).");
                return ExitCode.InvalidUsage;
            }

            var control = keyboard[parsedKey];
            var act = Normalize(action);
            switch (act)
            {
                case "down":
                    QueueButton(control, true);
                    break;
                case "up":
                    QueueButton(control, false);
                    break;
                case "press":
                    QueueButton(control, true);
                    QueueButton(control, false);
                    break;
                default:
                    cmd.Log.Error($"Unknown action '{action}'. Use down | up | press.");
                    return ExitCode.InvalidUsage;
            }

            // Flush queued events so the effect is observable synchronously to the caller.
            InputSystem.Update();
            cmd.Result.Write(new InputSimulationResponse { Command = CommandNames.RuntimeSimulateKey, Detail = $"key={parsedKey} action={act}" });
            return ExitCode.Success;
#else
            return Unavailable(cmd);
#endif
        }

        [CliGroupCommand(Group, "Simulate a mouse/pointer event at screen coordinates (Input System).",
            MainThreadRequired = true,
            Tags = new[] { "runtime/input" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode SimulatePointer(CommandContext cmd,
            [CliArg("x", "Screen X in pixels (origin bottom-left)", Required = true)] float x,
            [CliArg("y", "Screen Y in pixels (origin bottom-left)", Required = true)] float y,
            [CliArg("action", "move | down | up | click (down+up). Default: click")] string action = "click",
            [CliArg("button", "left | right | middle. Default: left")] string button = "left")
        {
#if ENABLE_INPUT_SYSTEM && PIPELINE_HAS_INPUT_SYSTEM_PACKAGE
            var mouse = Mouse.current;
            if (mouse == null)
            {
                cmd.Log.Error("No mouse/pointer device is present.");
                return ExitCode.Failed;
            }

            var position = new Vector2(x, y);
            var act = Normalize(action);

            ButtonControl btn;
            switch (Normalize(button))
            {
                case "left": btn = mouse.leftButton; break;
                case "right": btn = mouse.rightButton; break;
                case "middle": btn = mouse.middleButton; break;
                default:
                    cmd.Log.Error($"Unknown button '{button}'. Use left | right | middle.");
                    return ExitCode.InvalidUsage;
            }

            switch (act)
            {
                case "move":
                    QueuePointer(mouse, position, null, false);
                    break;
                case "down":
                    QueuePointer(mouse, position, btn, true);
                    break;
                case "up":
                    QueuePointer(mouse, position, btn, false);
                    break;
                case "click":
                    QueuePointer(mouse, position, btn, true);
                    QueuePointer(mouse, position, btn, false);
                    break;
                default:
                    cmd.Log.Error($"Unknown action '{action}'. Use move | down | up | click.");
                    return ExitCode.InvalidUsage;
            }

            InputSystem.Update();
            cmd.Result.Write(new InputSimulationResponse { Command = CommandNames.RuntimeSimulatePointer, Detail = $"pos=({x},{y}) action={act} button={Normalize(button)}" });
            return ExitCode.Success;
#else
            return Unavailable(cmd);
#endif
        }

        /// <summary>Fails a simulation the Input System can't take, saying how to enable it.</summary>
        internal static ExitCode Unavailable(CommandContext cmd)
        {
            cmd.Log.Error(InputSimulationResponse.UnavailableReason);
            return ExitCode.Failed;
        }

#if ENABLE_INPUT_SYSTEM && PIPELINE_HAS_INPUT_SYSTEM_PACKAGE
        // Capture the device's current state into a state event, override a single control, then queue it.
        // Capturing current state (rather than sending a zeroed full state) preserves any other controls
        // that are already held — so chained down/up calls compose correctly.
        private static void QueueButton(InputControl<float> control, bool pressed)
        {
            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        private static void QueuePointer(Mouse mouse, Vector2 position, ButtonControl button, bool pressed)
        {
            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(position, eventPtr);
                if (button != null)
                    button.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
#endif

        private static string Normalize(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();
    }

    /// <summary>
    /// What an input-simulation command writes once it injected the event: the command and a short
    /// detail of what it injected. A command that couldn't inject it fails instead.
    /// </summary>
    [Serializable]
    class InputSimulationResponse
    {
        public string Command { get; set; }
        public string Detail { get; set; }

        // Two causes, two different remedies, so they get two different messages.
        // PIPELINE_HAS_INPUT_SYSTEM_PACKAGE comes from the versionDefines entry on
        // Unity.Pipeline.asmdef and means com.unity.inputsystem is installed. ENABLE_INPUT_SYSTEM is
        // Unity's own define, set only when Active Input Handling actually includes the new system.
        // The package installed but excluded by the handler is a settings fix, not an install.
        internal static readonly string UnavailableReason =
#if PIPELINE_HAS_INPUT_SYSTEM_PACKAGE
            "Input simulation requires Active Input Handling to include the Input System. The " +
            "com.unity.inputsystem package is installed but inactive: set Project Settings > Player > " +
            "Active Input Handling to 'Input System Package' or 'Both'. Legacy input injection is " +
            "not supported.";
#else
            "Input simulation requires the com.unity.inputsystem package, which is not installed in " +
            "this project. Install it, then set Project Settings > Player > Active Input Handling to " +
            "'Input System Package' or 'Both'. Legacy input injection is not supported.";
#endif
    }
}
