using UnityEditor;
using Unity.Pipeline.Console;

namespace Unity.Pipeline.Editor.Console
{
    /// <summary>
    /// Editor-only bootstrap for <see cref="ConsoleLogCapture"/>. The capture itself (the shared
    /// buffer and the log-callback subscription) lives in the runtime assembly so it also works in
    /// player builds; this type adds the two things that only make sense in the Editor:
    ///
    ///  - <c>[InitializeOnLoad]</c> starts capture on every editor load and after every domain reload.
    ///  - Persistence to a Temp file across domain reloads, so entries and the cursor survive a
    ///    <c>recompile</c>. Players have no domain reloads, so the runtime path skips persistence.
    ///
    /// The static constructor restores the persisted buffer first, then starts capture, so logs are
    /// never appended ahead of a restore.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorConsoleCaptureBootstrap
    {
        // Lives under Temp/ like the recompile status file: cleared by Unity on project-level cleanup,
        // survives domain reloads, and never committed.
        internal const string PersistencePath = "Temp/pipeline_console_log.json";

        static EditorConsoleCaptureBootstrap()
        {
            // Restore before capture starts so restored entries precede any newly captured ones.
            ConsoleLogCapture.Buffer.Load(PersistencePath);
            ConsoleLogCapture.EnsureCapturing();

            AssemblyReloadEvents.beforeAssemblyReload -= Persist;
            AssemblyReloadEvents.beforeAssemblyReload += Persist;

            EditorApplication.quitting -= Persist;
            EditorApplication.quitting += Persist;
        }

        static void Persist()
        {
            ConsoleLogCapture.Buffer.Save(PersistencePath);
        }
    }
}
