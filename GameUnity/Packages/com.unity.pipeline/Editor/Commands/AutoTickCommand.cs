using System;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Opt-in mode that keeps the Unity Editor ticking at full rate even when its window is
    /// unfocused. Unity throttles EditorApplication.update when the editor is in the background,
    /// which can stall long-running work (HTTP commands, async test runs) driven from the CLI.
    ///
    /// When enabled, this forces EditorApplication.SignalTick() on every update, which schedules
    /// the next tick immediately and keeps the loop spinning. It is off by default and changes
    /// nothing in normal package behavior. Cross-platform: no native/OS calls.
    ///
    /// Note: state is static and resets on domain reload, so auto-tick turns itself off after a
    /// recompile; re-enable it if needed. Forcing full-rate ticks uses CPU like a focused editor.
    /// </summary>
    public static class AutoTickCommand
    {
        private static bool s_Enabled;
        private static Action s_SignalTick;
        private static EditorApplication.CallbackFunction s_Pump;
        private static readonly System.Diagnostics.Stopwatch s_Stopwatch = new System.Diagnostics.Stopwatch();
        private static long s_IntervalMs;

        [CliCommand("set_autotick", "Keep the editor ticking while unfocused by forcing EditorApplication.SignalTick at a throttled rate", MainThreadRequired = true)]
        public static string SetAutoTick(
            [CliArg("enable", "Enable (true) or disable (false) auto-tick mode")] bool enable = true,
            [CliArg("interval_ms", "Minimum milliseconds between forced ticks. 0 = every update (max rate, pegs a CPU core). Default 16 (~60Hz).")] int intervalMs = 16)
        {
            if (enable && s_Enabled)
            {
                s_IntervalMs = Math.Max(0, intervalMs);
                return $"Auto-tick already enabled (interval updated to {s_IntervalMs}ms)";
            }
            if (!enable && !s_Enabled)
                return "Auto-tick already disabled";

            if (enable)
            {
                if (s_SignalTick == null)
                {
                    var method = typeof(EditorApplication).GetMethod(
                        "SignalTick",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    if (method == null)
                        return "Error: EditorApplication.SignalTick not found (internal API may have changed)";

                    try
                    {
                        s_SignalTick = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    }
                    catch (Exception ex)
                    {
                        return $"Error: could not bind EditorApplication.SignalTick: {ex.Message}";
                    }
                }

                s_IntervalMs = Math.Max(0, intervalMs);
                s_Stopwatch.Restart();
                s_Pump = () =>
                {
                    if (s_IntervalMs <= 0 || s_Stopwatch.ElapsedMilliseconds >= s_IntervalMs)
                    {
                        s_Stopwatch.Restart();
                        s_SignalTick();
                    }
                };
                EditorApplication.update += s_Pump;
                s_Enabled = true;
                return $"Auto-tick enabled (interval {s_IntervalMs}ms)";
            }

            if (s_Pump != null)
            {
                EditorApplication.update -= s_Pump;
                s_Pump = null;
            }
            s_Stopwatch.Stop();
            s_Enabled = false;
            return "Auto-tick disabled";
        }
    }
}
