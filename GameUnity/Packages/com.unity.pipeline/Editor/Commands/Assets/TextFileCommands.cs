using System;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;

namespace Unity.Pipeline.Editor.Commands.Assets
{
    /// <summary>
    /// Text-file authoring commands (CLI-191): read and write UTF-8 text files inside the authoring
    /// sandbox. Useful for editing data assets that are plain text (JSON/CSV/.txt/.cs/.shader, etc.).
    ///
    /// Every path is funnelled through <see cref="ProjectPaths.Resolve"/>, so reads/writes are confined
    /// to the authoring root and "../"/out-of-project paths are rejected. Writing imports the file back
    /// into the AssetDatabase so Unity picks up the change.
    ///
    /// NOTE: writing is potentially destructive (it can overwrite an existing file), so overwriting an
    /// existing file requires an explicit <c>confirm</c> argument; <c>dry-run</c> is supported.
    /// Filesystem writes are not part of Unity's Undo system.
    /// </summary>
    static class TextFileCommands
    {
        const string Group = "asset";

        [CliGroupCommand(Group, "Read a UTF-8 text file under the authoring root and return its contents.", MainThreadRequired = true, Tags = new[] { "assets/text" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode ReadText(CommandContext cmd,
            [CliArg("path", "Text file path relative to the authoring root. The Assets/ prefix is optional.", Required = true)] string path,
            [CliArg("max-bytes", "Reject files larger than this many bytes (default 1048576 = 1 MiB) to avoid huge payloads.")] int maxBytes = 1024 * 1024)
        {
            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
            {
                cmd.Log.Error(error);
                return ExitCode.InvalidUsage;
            }

            var absolute = ToAbsolute(normalized);
            if (!File.Exists(absolute))
            {
                cmd.Log.Error($"No file at '{normalized}'.");
                return ExitCode.NotFound;
            }

            var length = new FileInfo(absolute).Length;
            if (length > maxBytes)
            {
                cmd.Log.Error($"File '{normalized}' is {length} bytes, which exceeds max-bytes={maxBytes}. Pass a larger max-bytes to read it.");
                return ExitCode.InvalidUsage;
            }

            cmd.Result.Write(new ReadTextFileResult
            {
                AssetPath = normalized,
                Bytes = (int)length,
                Contents = File.ReadAllText(absolute)
            });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Write UTF-8 text to a file under the authoring root, then import it. Overwriting an existing file requires confirm=true.", Tags = new[] { "assets/text" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode WriteText(CommandContext cmd,
            [CliArg("path", "Text file path relative to the authoring root, including extension. The Assets/ prefix is optional.", Required = true)] string path,
            [CliArg("contents", "The full text content to write (replaces the file).", Required = true)] string contents,
            [CliArg("confirm", "Required (true) only when overwriting an existing file at the path.")] bool confirm = false,
            [CliArg("dry-run", "If true, validate inputs and report what would be written without writing anything.")] bool dryRun = false)
        {
            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
            {
                cmd.Log.Error(error);
                return ExitCode.InvalidUsage;
            }

            if (contents == null)
            {
                cmd.Log.Error("contents is required (use an empty string to write an empty file).");
                return ExitCode.InvalidUsage;
            }

            var absolute = ToAbsolute(normalized);
            var exists = File.Exists(absolute);
            if (exists && !confirm)
            {
                cmd.Log.Error($"A file already exists at '{normalized}'. Pass confirm=true to overwrite it.");
                return ExitCode.InvalidUsage;
            }

            if (dryRun)
            {
                cmd.Result.Write(new AuthoringResult { AssetPath = normalized, Type = "TextAsset" });
                return ExitCode.Success;
            }

            EnsureParentFolder(normalized);
            File.WriteAllText(absolute, contents);
            AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceUpdate);

            var loaded = AssetDatabase.LoadMainAssetAtPath(normalized);
            var result = ObjectResolver.Describe(loaded) ?? new AuthoringResult { Type = "TextAsset" };
            result.AssetPath = normalized;
            cmd.Result.Write(result);
            return ExitCode.Success;
        }

        private static void EnsureParentFolder(string assetPath)
        {
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || AssetDatabase.IsValidFolder(parent))
                return;

            CreateFolderRecursive(parent);
        }

        private static void CreateFolderRecursive(string assetsPath)
        {
            if (AssetDatabase.IsValidFolder(assetsPath))
                return;

            var parent = Path.GetDirectoryName(assetsPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid folder path '{assetsPath}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToAbsolute(string projectRelative)
        {
            return $"{ProjectPaths.ProjectRoot.Replace('\\', '/')}/{projectRelative}";
        }
    }

    /// <summary>Result of <c>asset read_text</c>: the file contents plus its identity and size.</summary>
    [Serializable]
    class ReadTextFileResult
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        [JsonProperty("bytes")]
        public int Bytes { get; set; }

        [JsonProperty("contents")]
        public string Contents { get; set; }
    }
}
