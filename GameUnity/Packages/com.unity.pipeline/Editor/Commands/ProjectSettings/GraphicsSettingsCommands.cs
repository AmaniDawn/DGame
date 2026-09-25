using System.Collections.Generic;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Get/set the project's default render pipeline (CLI-202). The settable field is a handle to a
    /// <see cref="RenderPipelineAsset"/>; an empty reference selects the built-in pipeline (no SRP).
    /// </summary>
    public static class GraphicsSettingsCommands
    {
        const string Group = "graphics";

        [CliCommand("get_graphics_settings", "Read GraphicsSettings (default render pipeline).", MainThreadRequired = true)]
        public static ProjectSettingsResponse Get() => ProjectSettingsCommand.Get(Group, Read);

        [CliCommand("set_graphics_settings", "Set the default render pipeline asset. Requires confirm=true; use dry_run to preview. Not undoable via Ctrl+Z.", MainThreadRequired = true)]
        public static ProjectSettingsResponse Set(
            [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] GraphicsSettingsInput settings = null,
            [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
            [CliArg("dry_run", "Preview the change without applying it.")] bool dryRun = false)
        {
            if (settings == null)
                return ProjectSettingsResponse.Fail(Group, "No 'settings' object provided.");

            // omitted (null) = leave unchanged; an empty reference = built-in pipeline; otherwise a
            // handle to a RenderPipelineAsset resolved through the shared ObjectResolver.
            if (settings.RenderPipelineAsset == null)
                return NoChanges();

            RenderPipelineAsset asset = null;
            if (!settings.RenderPipelineAsset.IsEmpty)
            {
                if (!ObjectResolver.TryResolve(settings.RenderPipelineAsset, out var obj, out var resolveError))
                    return ProjectSettingsResponse.Fail(Group, resolveError);
                asset = obj as RenderPipelineAsset;
                if (asset == null)
                    return ProjectSettingsResponse.Fail(Group, $"'{settings.RenderPipelineAsset}' is not a RenderPipelineAsset.");
            }

            var current = GraphicsSettings.defaultRenderPipeline;
            if (current == asset)
                return NoChanges();

            var currentName = current != null ? current.name : "<built-in>";
            var newName = asset != null ? asset.name : "<built-in>";
            var planText = $"Set defaultRenderPipeline {currentName} -> {newName}";

            void Apply()
            {
                GraphicsSettings.defaultRenderPipeline = asset;
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
            var srp = GraphicsSettings.defaultRenderPipeline;
            return new Dictionary<string, object>
            {
                ["defaultRenderPipeline"] = srp != null ? srp.name : null,
                ["defaultRenderPipelinePath"] = srp != null ? AssetDatabase.GetAssetPath(srp) : null,
                ["usingScriptableRenderPipeline"] = srp != null
            };
        }
    }

    /// <summary>Graphics settings to change. Null/omitted fields are left unchanged.</summary>
    public class GraphicsSettingsInput : IStructuredCommandInput
    {
        [CliArg("renderPipelineAsset", "Reference (path / guid / globalId) to a RenderPipelineAsset to set as the default. Pass an empty reference ({}) to select the built-in pipeline; omit to leave unchanged.")]
        public ObjectRef RenderPipelineAsset { get; set; }
    }
}
