using System;
using Unity.Pipeline.Commands;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
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
    /// <para><b>Input stack:</b> device injection requires the Input System package to be present and active
    /// (<c>ENABLE_INPUT_SYSTEM</c>). Legacy <c>UnityEngine.Input</c> is poll-only and exposes no injection
    /// API, so it is intentionally not supported; when the Input System is absent these commands report an
    /// "unavailable" result rather than silently no-op'ing.</para>
    ///
    /// <para><b>Safety:</b> these are write/destructive commands. The package has no safety-policy gate yet
    /// (CAT-2509 is not implemented), so they follow the existing unguarded write-command model (cf.
    /// <c>set_target_framerate</c>). TODO(CAT-2509): route input commands through the safety policy when it
    /// lands.</para>
    /// </summary>
    public static class RuntimeInputCommand
    {
        [CliCommand("simulate_key", "Simulate a keyboard key event (Input System). Drives the running app.",
            MainThreadRequired = true, RuntimeOnly = true)]
        public static InputSimulationResponse SimulateKey(
            [CliArg("key", "Input System Key name, e.g. Space, W, Enter, LeftArrow", Required = true)] string key,
            [CliArg("action", "down | up | press (down+up). Default: press")] string action = "press")
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return InputSimulationResponse.Fail("simulate_key", "No keyboard device is present.");

            if (!Enum.TryParse<Key>(key, ignoreCase: true, out var parsedKey) || parsedKey == Key.None)
                return InputSimulationResponse.Fail("simulate_key", $"Unknown key '{key}'. Use an Input System Key name (e.g. Space, W, Enter).");

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
                    return InputSimulationResponse.Fail("simulate_key", $"Unknown action '{action}'. Use down | up | press.");
            }

            // Flush queued events so the effect is observable synchronously to the caller.
            InputSystem.Update();
            return InputSimulationResponse.Ok("simulate_key", $"key={parsedKey} action={act}");
#else
            return InputSimulationResponse.Unavailable("simulate_key");
#endif
        }

        [CliCommand("simulate_pointer", "Simulate a mouse/pointer event at screen coordinates (Input System).",
            MainThreadRequired = true, RuntimeOnly = true)]
        public static InputSimulationResponse SimulatePointer(
            [CliArg("x", "Screen X in pixels (origin bottom-left)", Required = true)] float x,
            [CliArg("y", "Screen Y in pixels (origin bottom-left)", Required = true)] float y,
            [CliArg("action", "move | down | up | click (down+up). Default: click")] string action = "click",
            [CliArg("button", "left | right | middle. Default: left")] string button = "left")
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
                return InputSimulationResponse.Fail("simulate_pointer", "No mouse/pointer device is present.");

            var position = new Vector2(x, y);
            var act = Normalize(action);

            ButtonControl btn;
            switch (Normalize(button))
            {
                case "left": btn = mouse.leftButton; break;
                case "right": btn = mouse.rightButton; break;
                case "middle": btn = mouse.middleButton; break;
                default:
                    return InputSimulationResponse.Fail("simulate_pointer", $"Unknown button '{button}'. Use left | right | middle.");
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
                    return InputSimulationResponse.Fail("simulate_pointer", $"Unknown action '{action}'. Use move | down | up | click.");
            }

            InputSystem.Update();
            return InputSimulationResponse.Ok("simulate_pointer", $"pos=({x},{y}) action={act} button={Normalize(button)}");
#else
            return InputSimulationResponse.Unavailable("simulate_pointer");
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
    /// Structured result for an input-simulation command: whether the event was injected, and a short
    /// human-readable detail (or an error / unavailable reason).
    /// </summary>
    [Serializable]
    public class InputSimulationResponse
    {
        public bool Success { get; set; }
        public string Command { get; set; }
        public string Detail { get; set; }
        public string Error { get; set; }

        public static InputSimulationResponse Ok(string command, string detail) =>
            new InputSimulationResponse { Success = true, Command = command, Detail = detail };

        public static InputSimulationResponse Fail(string command, string error) =>
            new InputSimulationResponse { Success = false, Command = command, Error = error };

        public static InputSimulationResponse Unavailable(string command) =>
            new InputSimulationResponse
            {
                Success = false,
                Command = command,
                Error = "Input simulation requires the Input System package to be present and active " +
                        "(ENABLE_INPUT_SYSTEM). Legacy input injection is not supported."
            };
    }
}
