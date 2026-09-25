using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Get/set a representative slice of 3D <see cref="Physics"/> settings (CLI-202): gravity (by
    /// component), default solver iterations, and the bounce threshold.
    /// </summary>
    public static class PhysicsSettingsCommands
    {
        const string Group = "physics";

        [CliCommand("get_physics_settings", "Read Physics settings (gravity, solver iterations, bounce threshold).", MainThreadRequired = true)]
        public static ProjectSettingsResponse Get() => ProjectSettingsCommand.Get(Group, Read);

        [CliCommand("set_physics_settings", "Change Physics settings. Requires confirm=true; use dry_run to preview. Not undoable via Ctrl+Z.", MainThreadRequired = true)]
        public static ProjectSettingsResponse Set(
            [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] PhysicsSettingsInput settings = null,
            [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
            [CliArg("dry_run", "Preview the change without applying it.")] bool dryRun = false)
        {
            if (settings == null)
                return ProjectSettingsResponse.Fail(Group, "No 'settings' object provided.");

            var changes = new List<string>();

            var gravity = Physics.gravity;
            var newGravity = gravity;
            if (settings.GravityX.HasValue) newGravity.x = settings.GravityX.Value;
            if (settings.GravityY.HasValue) newGravity.y = settings.GravityY.Value;
            if (settings.GravityZ.HasValue) newGravity.z = settings.GravityZ.Value;
            if (newGravity != gravity)
                changes.Add($"gravity {gravity} -> {newGravity}");

            if (settings.DefaultSolverIterations.HasValue && settings.DefaultSolverIterations.Value != Physics.defaultSolverIterations)
                changes.Add($"defaultSolverIterations {Physics.defaultSolverIterations} -> {settings.DefaultSolverIterations.Value}");
            if (settings.BounceThreshold.HasValue && !Mathf.Approximately(settings.BounceThreshold.Value, Physics.bounceThreshold))
                changes.Add($"bounceThreshold {Physics.bounceThreshold} -> {settings.BounceThreshold.Value}");

            if (changes.Count == 0)
                return NoChanges();

            var planText = "Set physics settings: " + string.Join("; ", changes);

            void Apply()
            {
                Physics.gravity = newGravity;
                if (settings.DefaultSolverIterations.HasValue) Physics.defaultSolverIterations = settings.DefaultSolverIterations.Value;
                if (settings.BounceThreshold.HasValue) Physics.bounceThreshold = settings.BounceThreshold.Value;
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
            var g = Physics.gravity;
            return new Dictionary<string, object>
            {
                ["gravityX"] = g.x,
                ["gravityY"] = g.y,
                ["gravityZ"] = g.z,
                ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                ["bounceThreshold"] = Physics.bounceThreshold
            };
        }
    }

    /// <summary>Physics settings to change. Null/omitted fields are left unchanged.</summary>
    public class PhysicsSettingsInput : IStructuredCommandInput
    {
        [CliArg("gravityX", "Gravity X component.")]
        public float? GravityX { get; set; }

        [CliArg("gravityY", "Gravity Y component (e.g. -9.81).")]
        public float? GravityY { get; set; }

        [CliArg("gravityZ", "Gravity Z component.")]
        public float? GravityZ { get; set; }

        [CliArg("defaultSolverIterations", "Default solver iteration count.")]
        public int? DefaultSolverIterations { get; set; }

        [CliArg("bounceThreshold", "Bounce threshold velocity.")]
        public float? BounceThreshold { get; set; }
    }
}
