using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>
    /// Forces a script recompile that works even when the editor is unfocused or minimized.
    ///
    /// Why this is non-trivial:
    ///  - Unity only runs script compilation while the editor is the active OS application. Pass
    ///    focus=true to bring it to the foreground (editor_focus) before AssetDatabase.Refresh();
    ///    it is off by default because the server keeps the editor ticking while unfocused, so
    ///    compilation still proceeds without stealing the user's foreground window.
    ///  - A successful compile triggers a domain reload, which destroys the managed AppDomain
    ///    (HTTP server, in-flight requests, statics). The triggering request therefore cannot stay
    ///    open and return when done.
    ///
    /// Pattern (mirrors the test runner): completion is reported via a status file that survives the
    /// domain reload. Call "recompile" to trigger, then poll "recompile_status" until status is
    /// "completed" or "up_to_date". The client must tolerate connection errors during the reload.
    /// </summary>
    [InitializeOnLoad]
    public static class RecompileCommand
    {
        const string StatusFile = "Temp/pipeline_recompile_status.json";

        static readonly List<string> s_Errors = new List<string>();

        // Focus action, indirected so tests can observe whether focus was performed without
        // actually stealing the OS foreground window.
        internal static Action s_FocusAction = () => FocusEditorCommand.FocusEditor();

        static RecompileCommand()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        [CliCommand("recompile", "Force a script recompile (works while unfocused/minimized). Poll recompile_status for completion.", MainThreadRequired = true)]
        public static object Recompile(
            [CliArg("focus", "If true, bring the Editor to the foreground before compiling. Off by default.")] bool focus = false)
        {
            // Unity compiles only while it is the active application. Optionally bring it to the
            // foreground first; off by default (the server keeps the editor ticking while unfocused,
            // so compilation still proceeds).
            if (focus)
                s_FocusAction();

            WriteStatus("triggered", false, null);

            // Trigger asset import + compilation.
            AssetDatabase.Refresh();

            if (EditorApplication.isCompiling)
            {
                // compilationStarted has written "compiling"; a domain reload will follow on success.
                return new { status = "compiling", message = "Recompilation started. Poll recompile_status until completed." };
            }

            // Nothing needed recompilation.
            WriteStatus("up_to_date", false, null);
            return new { status = "up_to_date", message = "No scripts needed recompilation." };
        }

        [CliCommand("recompile_status", "Get the status of the last recompile: idle | triggered | compiling | completed | up_to_date.", MainThreadRequired = false)]
        public static string RecompileStatus()
        {
            if (File.Exists(StatusFile))
                return File.ReadAllText(StatusFile);
            return "{\"status\":\"idle\"}";
        }

        static void OnCompilationStarted(object _)
        {
            s_Errors.Clear();
            WriteStatus("compiling", false, null);
        }

        static void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] messages)
        {
            if (messages == null) return;
            foreach (var m in messages)
                if (m.type == CompilerMessageType.Error)
                    s_Errors.Add(m.message);
        }

        static void OnCompilationFinished(object _)
        {
            // Written before the domain reload, so it persists for pollers after the reload.
            WriteStatus("completed", s_Errors.Count > 0, s_Errors.ToArray());
        }

        static void WriteStatus(string status, bool failed, string[] errors)
        {
            try
            {
                var payload = new { status, failed, errors = errors ?? Array.Empty<string>() };
                File.WriteAllText(StatusFile, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Recompile] Failed to write status file: {ex.Message}");
            }
        }
    }
}
