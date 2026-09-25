using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Get/set the project Time settings (CLI-202): fixed timestep, maximum allowed timestep, and the
    /// time scale.
    /// </summary>
    public static class TimeSettingsCommands
    {
        const string Group = "time";

        [CliCommand("get_time_settings", "Read Time settings (fixedDeltaTime, maximumDeltaTime, timeScale).", MainThreadRequired = true)]
        public static ProjectSettingsResponse Get() => ProjectSettingsCommand.Get(Group, Read);

        [CliCommand("set_time_settings", "Change Time settings. Requires confirm=true; use dry_run to preview. Not undoable via Ctrl+Z.", MainThreadRequired = true)]
        public static ProjectSettingsResponse Set(
            [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] TimeSettingsInput settings = null,
            [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
            [CliArg("dry_run", "Preview the change without applying it.")] bool dryRun = false)
        {
            if (settings == null)
                return ProjectSettingsResponse.Fail(Group, "No 'settings' object provided.");

            var changes = new List<string>();

            if (settings.FixedDeltaTime.HasValue && !Mathf.Approximately(settings.FixedDeltaTime.Value, Time.fixedDeltaTime))
                changes.Add($"fixedDeltaTime {Time.fixedDeltaTime} -> {settings.FixedDeltaTime.Value}");
            if (settings.MaximumDeltaTime.HasValue && !Mathf.Approximately(settings.MaximumDeltaTime.Value, Time.maximumDeltaTime))
                changes.Add($"maximumDeltaTime {Time.maximumDeltaTime} -> {settings.MaximumDeltaTime.Value}");
            if (settings.TimeScale.HasValue && !Mathf.Approximately(settings.TimeScale.Value, Time.timeScale))
                changes.Add($"timeScale {Time.timeScale} -> {settings.TimeScale.Value}");

            if (changes.Count == 0)
                return NoChanges();

            var planText = "Set time settings: " + string.Join("; ", changes);

            void Apply()
            {
                if (settings.FixedDeltaTime.HasValue) Time.fixedDeltaTime = settings.FixedDeltaTime.Value;
                if (settings.MaximumDeltaTime.HasValue) Time.maximumDeltaTime = settings.MaximumDeltaTime.Value;
                if (settings.TimeScale.HasValue) Time.timeScale = settings.TimeScale.Value;
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

        static Dictionary<string, object> Read() => new Dictionary<string, object>
        {
            ["fixedDeltaTime"] = Time.fixedDeltaTime,
            ["maximumDeltaTime"] = Time.maximumDeltaTime,
            ["timeScale"] = Time.timeScale
        };
    }

    /// <summary>Time settings to change. Null/omitted fields are left unchanged.</summary>
    public class TimeSettingsInput : IStructuredCommandInput
    {
        [CliArg("fixedDeltaTime", "Fixed timestep in seconds (e.g. 0.02).")]
        public float? FixedDeltaTime { get; set; }

        [CliArg("maximumDeltaTime", "Maximum allowed timestep in seconds.")]
        public float? MaximumDeltaTime { get; set; }

        [CliArg("timeScale", "Time scale (1 = real-time).")]
        public float? TimeScale { get; set; }
    }
}
