using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Get/set <see cref="QualitySettings"/> (CLI-202): the active quality level plus a couple of
    /// representative per-level toggles (vSync, anti-aliasing). Level changes apply expensive changes
    /// so the switch takes full effect.
    /// </summary>
    public static class QualitySettingsCommands
    {
        const string Group = "quality";

        [CliCommand("get_quality_settings", "Read QualitySettings (current level, level names, vSync, anti-aliasing).", MainThreadRequired = true)]
        public static ProjectSettingsResponse Get() => ProjectSettingsCommand.Get(Group, Read);

        [CliCommand("set_quality_settings", "Change QualitySettings. Requires confirm=true; use dry_run to preview. Not undoable via Ctrl+Z.", MainThreadRequired = true)]
        public static ProjectSettingsResponse Set(
            [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] QualitySettingsInput settings = null,
            [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
            [CliArg("dry_run", "Preview the change without applying it.")] bool dryRun = false)
        {
            if (settings == null)
                return ProjectSettingsResponse.Fail(Group, "No 'settings' object provided.");

            var names = QualitySettings.names;
            var changes = new List<string>();

            if (settings.Level.HasValue)
            {
                if (settings.Level.Value < 0 || settings.Level.Value >= names.Length)
                    return ProjectSettingsResponse.Fail(Group, $"level {settings.Level.Value} out of range (0..{names.Length - 1}).");
                if (settings.Level.Value != QualitySettings.GetQualityLevel())
                    changes.Add($"level {QualitySettings.GetQualityLevel()} -> {settings.Level.Value}");
            }

            if (settings.VSyncCount.HasValue && settings.VSyncCount.Value != QualitySettings.vSyncCount)
                changes.Add($"vSyncCount {QualitySettings.vSyncCount} -> {settings.VSyncCount.Value}");
            if (settings.AntiAliasing.HasValue && settings.AntiAliasing.Value != QualitySettings.antiAliasing)
                changes.Add($"antiAliasing {QualitySettings.antiAliasing} -> {settings.AntiAliasing.Value}");

            if (changes.Count == 0)
                return NoChanges();

            var planText = "Set quality settings: " + string.Join("; ", changes);

            void Apply()
            {
                if (settings.Level.HasValue) QualitySettings.SetQualityLevel(settings.Level.Value, true);
                if (settings.VSyncCount.HasValue) QualitySettings.vSyncCount = settings.VSyncCount.Value;
                if (settings.AntiAliasing.HasValue) QualitySettings.antiAliasing = settings.AntiAliasing.Value;
                AssetDatabase.SaveAssets();
            }

            return ProjectSettingsCommand.Apply(Group, confirm, dryRun, settings, () => planText, Apply, Read);
        }

        static ProjectSettingsResponse NoChanges()
        {
            var response = ProjectSettingsCommand.Get(Group, Read);
            response.Message = "No changes specified; nothing to apply.";
            return response;
        }

        static Dictionary<string, object> Read()
        {
            var names = QualitySettings.names;
            var level = QualitySettings.GetQualityLevel();
            return new Dictionary<string, object>
            {
                ["level"] = level,
                ["levelName"] = (level >= 0 && level < names.Length) ? names[level] : null,
                ["levelNames"] = names,
                ["vSyncCount"] = QualitySettings.vSyncCount,
                ["antiAliasing"] = QualitySettings.antiAliasing
            };
        }
    }

    /// <summary>Quality settings to change. Null/omitted fields are left unchanged.</summary>
    public class QualitySettingsInput : IStructuredCommandInput
    {
        [CliArg("level", "Quality level index (see levelNames from get_quality_settings).")]
        public int? Level { get; set; }

        [CliArg("vSyncCount", "VSync count (0 = off, 1, 2).")]
        public int? VSyncCount { get; set; }

        [CliArg("antiAliasing", "MSAA sample count (0, 2, 4, 8).")]
        public int? AntiAliasing { get; set; }
    }
}
