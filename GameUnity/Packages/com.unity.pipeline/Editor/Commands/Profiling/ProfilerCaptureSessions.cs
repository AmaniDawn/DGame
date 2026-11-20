using System;
using System.Collections.Generic;
using System.IO;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>
    /// The open capture sessions. <see cref="ProfilerDriver.LoadProfile"/> replaces the Editor's single Profiler
    /// history, so any number of sessions can be open but only one is resident (its capture is the one in the
    /// Profiler). Touching another session evicts the resident one and reloads the touched one.
    /// </summary>
#if UNITY_6000_5_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    static class ProfilerCaptureSessions
    {
        static readonly Dictionary<string, ProfilerCaptureSession> s_Sessions = new Dictionary<string, ProfilerCaptureSession>();
        static ProfilerCaptureSession s_Resident;

        static void OnBeforeAssemblyReload() => CloseAll();

        internal static IEnumerable<ProfilerCaptureSession> All => s_Sessions.Values;

        internal static bool IsResident(ProfilerCaptureSession session) => session != null && session == s_Resident;

        /// <summary>A busy error while a recording or memory snapshot owns the Profiler connection, else null.</summary>
        internal static CaptureError Busy()
        {
            var message = ProfilerTargetCommands.BusyMessage();
            return message == null ? null : ProfilerCaptureSession.Error(message);
        }

        /// <summary>True when loading would drop frames in the Profiler that no open session can reload.</summary>
        internal static bool WouldReplaceUnownedFrames(int historyFirst, int historyLast, ProfilerCaptureSession resident)
        {
            if (historyLast < 0)
                return false;

            return resident == null || resident.IsStale || !IsBackedByFile(resident) ||
                resident.First != historyFirst || resident.Last != historyLast;
        }

        static bool IsBackedByFile(ProfilerCaptureSession session)
        {
            return File.Exists(session.Path) && File.GetLastWriteTimeUtc(session.Path) == session.WriteTimeUtc;
        }

        static bool IsLoaded(ProfilerCaptureSession session)
        {
            return session == s_Resident &&
                !session.IsStale &&
                IsBackedByFile(session) &&
                ProfilerDriver.firstFrameIndex == session.First &&
                ProfilerDriver.lastFrameIndex == session.Last;
        }

        /// <summary>What loading <paramref name="path"/> would replace, for a dry run.</summary>
        static object DryRunPreview(string path)
        {
            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;
            var empty = last < 0;
            return new
            {
                status = "dry_run",
                capture = path,
                sizeBytes = new FileInfo(path).Length,
                wouldReplace = new
                {
                    empty,
                    firstFrame = empty ? (int?)null : first,
                    lastFrame = empty ? (int?)null : last,
                    frameCount = empty ? 0 : last - first + 1
                }
            };
        }

        internal static string ResolvePath(string capture)
        {
            if (Path.IsPathRooted(capture))
                return capture;
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, capture));
        }

        /// <summary>
        /// Opens a session on a capture, or returns the one already open on that file. A load replaces the
        /// Profiler history, so it needs <paramref name="confirm"/>; <paramref name="dryRun"/> previews it.
        /// When no session is returned, either <paramref name="error"/> says why, or the call was a dry run and
        /// <paramref name="dryRunPreview"/> holds what the load would replace.
        /// </summary>
        internal static ProfilerCaptureSession Open(string capture, bool confirm, bool dryRun, out CaptureError error, out bool loadedNow,
            out object dryRunPreview)
        {
            error = Busy();
            loadedNow = false;
            dryRunPreview = null;
            if (error != null)
                return null;

            var path = ResolvePath(capture);
            if (!File.Exists(path))
            {
                error = ProfilerCaptureSession.Error($"Capture file not found: '{path}'.", ExitCode.NotFound);
                return null;
            }

            var comparison = Application.platform == RuntimePlatform.LinuxEditor
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            ProfilerCaptureSession session = null;
            foreach (var existing in s_Sessions.Values)
            {
                if (string.Equals(existing.Path, path, comparison))
                {
                    session = existing;
                    break;
                }
            }

            if (session == null || !IsLoaded(session))
            {
                if (dryRun)
                {
                    dryRunPreview = DryRunPreview(path);
                    return null;
                }

                if (!confirm)
                {
                    error = ProfilerCaptureSession.Error("Refusing to load the capture: it replaces the Editor's current Profiler history, " +
                        "which is not recoverable. Pass confirm=true to load, or dry-run=true to preview.", ExitCode.InvalidUsage);
                    return null;
                }
            }

            var created = session == null;
            if (created)
            {
                session = new ProfilerCaptureSession(path);
                s_Sessions[session.Id] = session;
                SubscribeToReload();
            }

            if (!MakeResident(session, out error, out loadedNow, allowReplace: true))
            {
                if (created)
                {
                    s_Sessions.Remove(session.Id);
                    session.Dispose();
                    UnsubscribeWhenEmpty();
                }

                return null;
            }

            return session;
        }

        /// <summary>The session for an analysis command: busy while a recording owns the Profiler, else the open session or an error.</summary>
        internal static ProfilerCaptureSession Require(string id, out CaptureError error)
        {
            error = Busy();
            return error != null ? null : Get(id, out error);
        }

        /// <summary>The open session with this id, or null with an error result.</summary>
        internal static ProfilerCaptureSession Get(string id, out CaptureError error)
        {
            error = null;
            if (!string.IsNullOrEmpty(id) && s_Sessions.TryGetValue(id, out var session))
                return session;

            error = ProfilerCaptureSession.Error($"No open capture session '{id}'. Open one with " + CommandNames.DiagnosticsProfilerSession + ".",
                ExitCode.NotFound);
            return null;
        }

        internal static bool MakeResident(ProfilerCaptureSession session, out CaptureError error)
        {
            return MakeResident(session, out error, out _);
        }

        /// <summary>
        /// Loads the session's capture into the Profiler unless it already is there and unchanged. Without
        /// <paramref name="allowReplace"/>, it refuses to drop frames that no open session can reload.
        /// </summary>
        internal static bool MakeResident(ProfilerCaptureSession session, out CaptureError error, out bool loadedNow, bool allowReplace = false)
        {
            // The Profiler's frame views, and the disposal of them, belong to the main thread.
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
                throw new InvalidOperationException("Capture sessions use the Profiler API, which is only available on the main thread.");

            error = null;
            loadedNow = false;

            // Profiler events may arrive on any thread and only flag the session; the release happens here.
            if (session == s_Resident && session.IsStale)
            {
                session.Evict();
                s_Resident = null;
            }

            if (IsLoaded(session))
                return true;

            error = Busy();
            if (error != null)
                return false;

            if (!allowReplace &&
                WouldReplaceUnownedFrames(ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex, s_Resident))
            {
                error = ProfilerCaptureSession.Error($"The Profiler holds frames that are not from an open capture session, and reloading '{session.Path}' would replace them. " +
                    $"Run {CommandNames.DiagnosticsProfilerSession} --capture \"{session.Path}\" --confirm true to reload it.");
                return false;
            }

            s_Resident?.Evict();
            s_Resident = null;
            if (!session.Load(out error))
                return false;

            s_Resident = session;
            loadedNow = true;
            return true;
        }

        internal static bool Close(string id)
        {
            if (!s_Sessions.TryGetValue(id, out var session))
                return false;

            s_Sessions.Remove(id);
            if (session == s_Resident)
                s_Resident = null;
            session.Dispose();
            UnsubscribeWhenEmpty();
            return true;
        }

        internal static int CloseAll()
        {
            var count = s_Sessions.Count;
            foreach (var session in s_Sessions.Values)
                session.Dispose();
            s_Sessions.Clear();
            s_Resident = null;
            UnsubscribeWhenEmpty();
            return count;
        }

        /// <summary>Sessions end with the domain, so the reload hook is only held while one is open.</summary>
        static void SubscribeToReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void UnsubscribeWhenEmpty()
        {
            if (s_Sessions.Count == 0)
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }
    }
}
