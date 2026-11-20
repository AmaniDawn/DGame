using System;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditorInternal;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

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
    /// Unity runs EditorApplication.delayCall only while the Editor is active, so an unfocused or
    /// minimized Editor would hold every delayCall until it regains focus. While the Editor is not
    /// active, auto-tick runs them itself through Unity's own Internal_CallDelayFunctions, at the
    /// same 150 ms cadence Unity uses.
    ///
    /// Note: the update-loop subscription is static and does not survive a domain reload, but the
    /// enabled/interval choice is persisted to SessionState and restored by
    /// <see cref="RestoreFromSession"/> (called from <c>EditorPipelineStartup</c> on every server
    /// (re)start, including after a recompile). Forcing full-rate ticks uses CPU like a focused editor.
    /// </summary>
#if UNITY_6000_5_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    static class AutoTickCommand
    {
        const string Group = "editor";

        // Default interval when nothing else applies (fresh editor session, no prior SetAutoTick call).
        internal const int DefaultIntervalMs = 16;

        // SessionState keys: session-scoped editor state, survives domain reload, dies with the editor.
        private const string EnabledKey = "Unity.Pipeline.AutoTick.Enabled";
        private const string IntervalKey = "Unity.Pipeline.AutoTick.IntervalMs";

        private static bool s_Enabled;
        private static Action s_SignalTick;
        private static EditorApplication.CallbackFunction s_Pump;
        private static readonly System.Diagnostics.Stopwatch s_Stopwatch = new System.Diagnostics.Stopwatch();
        private static long s_IntervalMs;

        internal const int DelayCallFlushIntervalMs = 150;
        private static Action s_CallDelayFunctions;
        private static bool s_CallDelayFunctionsMissing;
        private static readonly System.Diagnostics.Stopwatch s_Clock = System.Diagnostics.Stopwatch.StartNew();
        private static Func<long> s_NowMs = RealNowMs;
        private static long s_LastDelayCallFlushMs;

        internal static bool IsEnabled => s_Enabled;
        internal static long CurrentIntervalMs => s_IntervalMs;

        [CliGroupCommand(Group, "Keep the editor ticking while unfocused by forcing EditorApplication.SignalTick at a throttled rate", MainThreadRequired = true, Tags = new[] { "editor" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetAutoTick(CommandContext cmd,
            [CliArg("enabled", "Turn auto-tick mode on (true) or off (false)")] bool enabled = true,
            [CliArg("interval-ms", "Minimum milliseconds between forced ticks. 0 = every update (max rate, pegs a CPU core). Default 16 (~60Hz).")] int intervalMs = DefaultIntervalMs,
            [CliArg("persist", "Persist this choice to SessionState so it survives a domain reload. Set false for a one-off/expensive setting (e.g. interval-ms=0) that should revert to the last persisted choice (or the default) after the next recompile instead of sticking for the rest of the session.")] bool persist = true)
        {
            if (!Set(enabled, intervalMs, persist, out var message))
            {
                cmd.Log.Error(message);
                return ExitCode.Failed;
            }

            cmd.Log.Info(message);
            cmd.Result.Write(new { enabled = s_Enabled, intervalMs = s_IntervalMs, persisted = persist });
            return ExitCode.Success;
        }

        /// <summary>
        /// Turns auto-tick on or off and, with <paramref name="persist"/>, stores the choice in
        /// SessionState. <paramref name="message"/> says what changed, or why it failed.
        /// </summary>
        internal static bool Set(bool enabled, int intervalMs, bool persist, out string message)
        {
            if (!ApplyAutoTick(enabled, intervalMs, out message))
                return false;

            if (!persist)
            {
                message += " (session-only, won't survive a domain reload)";
                return true;
            }

            SessionState.SetBool(EnabledKey, s_Enabled);
            SessionState.SetInt(IntervalKey, (int)s_IntervalMs);
            return true;
        }

        /// <summary>
        /// Restore auto-tick to whatever the user last explicitly set via <see cref="Set"/>
        /// this editor session (surviving the domain reload that just wiped the statics). If nothing
        /// was ever explicitly set this session, apply <paramref name="defaultEnabled"/> at
        /// <see cref="DefaultIntervalMs"/> instead — this is the only case where the watchdog's
        /// enabled setting decides auto-tick's state. Call on the main thread (touches SessionState).
        /// </summary>
        internal static void RestoreFromSession(bool defaultEnabled)
        {
            var storedInterval = SessionState.GetInt(IntervalKey, -1);
            if (storedInterval < 0)
            {
                ApplyAutoTick(defaultEnabled, DefaultIntervalMs, out _);
                return;
            }

            var storedEnabled = SessionState.GetBool(EnabledKey, defaultEnabled);
            ApplyAutoTick(storedEnabled, storedInterval, out _);
        }

        /// <summary>
        /// Test-only: drop the in-memory state (statics + update-loop subscription) but keep
        /// SessionState, to simulate a domain reload (statics cleared, SessionState survives).
        /// </summary>
        internal static void ResetForTests()
        {
            if (s_Pump != null)
            {
                EditorApplication.update -= s_Pump;
                s_Pump = null;
            }
            s_Stopwatch.Stop();
            s_Enabled = false;
            s_IntervalMs = 0;
        }

        /// <summary>
        /// One auto-tick step, run on every EditorApplication.update while auto-tick is enabled.
        /// Signals the next tick when the interval has elapsed, and runs pending delayCalls when
        /// <paramref name="applicationActive"/> is false, since Unity holds them until it is active again.
        /// </summary>
        internal static void Pump(bool applicationActive)
        {
            if (!s_Enabled)
                return;

            if (s_IntervalMs <= 0 || s_Stopwatch.ElapsedMilliseconds >= s_IntervalMs)
            {
                s_Stopwatch.Restart();
                s_SignalTick();
            }

            if (!applicationActive)
                FlushDelayCallsIfDue();
        }

        /// <summary>
        /// Test-only: make the next <see cref="Pump"/> flush delayCalls without waiting for
        /// <see cref="DelayCallFlushIntervalMs"/>.
        /// </summary>
        internal static void ExpireDelayCallFlushTimerForTests()
        {
            s_LastDelayCallFlushMs = s_NowMs() - DelayCallFlushIntervalMs;
        }

        /// <summary>
        /// Test-only: replace the clock that throttles delayCall flushes. Null restores the real clock.
        /// </summary>
        internal static void SetDelayCallClockForTests(Func<long> nowMs)
        {
            s_NowMs = nowMs ?? RealNowMs;
        }

        static long RealNowMs() => s_Clock.ElapsedMilliseconds;

        static void FlushDelayCallsIfDue()
        {
            var now = s_NowMs();
            if (s_CallDelayFunctions == null || now - s_LastDelayCallFlushMs < DelayCallFlushIntervalMs)
                return;

            s_LastDelayCallFlushMs = now;
            try
            {
                s_CallDelayFunctions();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        static bool IsEditorActive() =>
            IsEditorActive(InternalEditorUtility.isApplicationActive, EditorApplication.isPlaying, EditorApplication.isPaused);

        // Mirrors the condition under which Unity's own tick runs delayCall.
        internal static bool IsEditorActive(bool applicationActive, bool isPlaying, bool isPaused) =>
            applicationActive || (isPlaying && !isPaused);

        static void BindCallDelayFunctions()
        {
            if (s_CallDelayFunctions != null || s_CallDelayFunctionsMissing)
                return;

            var method = typeof(EditorApplication).GetMethod(
                "Internal_CallDelayFunctions",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (method != null)
            {
                try
                {
                    s_CallDelayFunctions = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            s_CallDelayFunctionsMissing = true;
            UnityEngine.Debug.LogWarning("Auto-tick can't run EditorApplication.delayCall while the Editor is unfocused: EditorApplication.Internal_CallDelayFunctions was not found. Delayed calls wait until the Editor regains focus.");
        }

        /// <summary>
        /// Test-only: erase the persisted SessionState copy (unlike <see cref="ResetForTests"/>, which
        /// deliberately leaves it intact). Use for test isolation, not to simulate a reload.
        /// </summary>
        internal static void EraseSessionForTests()
        {
            SessionState.EraseInt(IntervalKey);
            SessionState.EraseBool(EnabledKey);
        }

        /// <summary>
        /// Test-only: read the persisted SessionState copy without touching the in-memory statics.
        /// Returns false if nothing has ever been persisted this session.
        /// </summary>
        internal static bool TryGetPersistedSessionForTests(out bool enabled, out long intervalMs)
        {
            var storedInterval = SessionState.GetInt(IntervalKey, -1);
            if (storedInterval < 0)
            {
                enabled = false;
                intervalMs = 0;
                return false;
            }

            enabled = SessionState.GetBool(EnabledKey, false);
            intervalMs = storedInterval;
            return true;
        }

        /// <summary>
        /// Test-only: write the persisted SessionState copy directly, bypassing ApplyAutoTick.
        /// </summary>
        internal static void SetPersistedSessionForTests(bool enabled, long intervalMs)
        {
            SessionState.SetBool(EnabledKey, enabled);
            SessionState.SetInt(IntervalKey, (int)intervalMs);
        }

        private static bool ApplyAutoTick(bool enable, int intervalMs, out string message)
        {
            if (enable && s_Enabled)
            {
                s_IntervalMs = Math.Max(0, intervalMs);
                message = $"Auto-tick already enabled (interval updated to {s_IntervalMs}ms)";
                return true;
            }
            if (!enable && !s_Enabled)
            {
                message = "Auto-tick already disabled";
                return true;
            }

            if (enable)
            {
                if (s_SignalTick == null)
                {
                    var method = typeof(EditorApplication).GetMethod(
                        "SignalTick",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    if (method == null)
                    {
                        message = "EditorApplication.SignalTick not found (internal API may have changed)";
                        return false;
                    }

                    try
                    {
                        s_SignalTick = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    }
                    catch (Exception ex)
                    {
                        message = $"Could not bind EditorApplication.SignalTick: {ex.Message}";
                        return false;
                    }
                }

                BindCallDelayFunctions();

                s_IntervalMs = Math.Max(0, intervalMs);
                s_Stopwatch.Restart();
                s_Pump = () => Pump(IsEditorActive());
                EditorApplication.update += s_Pump;
                s_Enabled = true;
                message = $"Auto-tick enabled (interval {s_IntervalMs}ms)";
                return true;
            }

            if (s_Pump != null)
            {
                EditorApplication.update -= s_Pump;
                s_Pump = null;
            }
            s_Stopwatch.Stop();
            s_Enabled = false;
            message = "Auto-tick disabled";
            return true;
        }
    }
}
