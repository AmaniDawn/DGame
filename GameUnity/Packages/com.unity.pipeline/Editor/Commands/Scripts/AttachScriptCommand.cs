using System;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Unity.Pipeline.Editor.Commands.CommandFailure;

namespace Unity.Pipeline.Editor.Commands.Scripts
{
    /// <summary>
    /// Authoring command that adds a MonoBehaviour component to a GameObject, addressing the script
    /// either by its (compiled) type name OR by its source asset path (CLI-195, CLI-224).
    ///
    /// COMPILE-AWARE: the type must already be compiled. A script created via create_script does not
    /// have a compiled <see cref="Type"/> until a domain reload runs, so attaching it before that
    /// fails with a CLEAR, RECOVERABLE error (no crash, no silent no-op) telling the agent to
    /// recompile and retry. The full flow is:
    ///   create_script -> recompile -> poll recompile_status (completed/up_to_date) -> attach_script.
    ///
    /// Addressing by asset path resolves the backing class through
    /// <see cref="MonoScript.GetClass"/>, which correctly handles a class whose name differs from
    /// the filename. A path whose MonoScript has no class yet (not compiled, or no single matching
    /// top-level type) surfaces the same recoverable "not yet compiled / ambiguous" error.
    ///
    /// The component add is wrapped in an <see cref="AuthoringUndoScope"/> and registered with the
    /// Undo system so it reverts as one step and the owning object/prefab is marked dirty.
    /// </summary>
    static class AttachScriptCommand
    {
        const string Group = "script";

        [CliGroupCommand(Group,
            "Add a MonoBehaviour to a GameObject by its (compiled) type name OR by its script asset path. " +
            "Provide exactly one of 'type' or 'script'. " +
            "If the type isn't compiled yet, returns a recoverable error: " + CommandNames.ScriptRecompile + ", poll " + CommandNames.ScriptRecompileStatus + ", then retry.",
            Tags = new[] { "scripts" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode Attach(CommandContext cmd,
            [CliArg("target", "Reference to the GameObject to add the component to (globalId/path/guid/instanceId/hierarchyPath).", Required = true)] ObjectRef target,
            [CliArg("type", "Component type name to add, e.g. PlayerController or Game.Player.PlayerController. Must already be compiled. Mutually exclusive with 'script'.")] string type = null,
            [CliArg("script", "Script asset path, e.g. 'Assets/Pool/Scripts/CueShooter.cs'. The backing class is resolved via MonoScript.GetClass(), so the class name may differ from the filename. Mutually exclusive with 'type'.")] string script = null)
        {
            if (target == null || target.IsEmpty)
                return Fail(cmd, ExitCode.InvalidUsage, CommandNames.ScriptAttach + " 'target' is required.");

            // Exactly one addressing form must be supplied. Both is ambiguous; neither is incomplete.
            var hasType = !string.IsNullOrWhiteSpace(type);
            var hasScript = !string.IsNullOrWhiteSpace(script);
            if (hasType && hasScript)
                return Fail(cmd, ExitCode.InvalidUsage, "Provide either 'type' (class name) or 'script' (asset path), not both.");
            if (!hasType && !hasScript)
                return Fail(cmd, ExitCode.InvalidUsage, "Provide either 'type' (class name) or 'script' (asset path).");

            if (!ObjectResolver.TryResolve(target, out var obj, out var resolveError))
                return Fail(cmd, ExitCode.NotFound, $"Could not resolve target: {resolveError}");

            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go == null)
                return Fail(cmd, ExitCode.InvalidUsage,
                    $"Target '{target}' resolved to a {obj.GetType().Name}, which is not a GameObject. " +
                    CommandNames.ScriptAttach + " needs a GameObject.");

            // Resolve the component Type from whichever addressing form was given. A type that exists
            // but isn't compiled yet (or is the wrong kind) fails with the recovery instructions; a
            // script path that points at no asset is NotFound.
            Type componentType;
            string typeError;
            var code = hasScript
                ? TryResolveTypeFromScriptPath(script, out componentType, out typeError)
                : TryResolveTypeFromName(type, out componentType, out typeError);
            if (code != ExitCode.Success)
                return Fail(cmd, code, typeError);

            using (new AuthoringUndoScope($"Attach {componentType.Name}"))
            {
                // Undo.AddComponent registers the add for undo and returns the new component.
                var component = Undo.AddComponent(go, componentType);
                if (component == null)
                    return Fail(cmd, ExitCode.Failed,
                        $"Failed to add component '{componentType.FullName}' to '{go.name}'. " +
                        "It may conflict with [DisallowMultipleComponent] or a required-component rule.");

                EditorUtility.SetDirty(go);

                cmd.Result.Write(ObjectResolver.Describe(component) ?? new AuthoringResult { Type = componentType.Name });
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Resolve a component type by its compiled type name. A missing type is the expected,
        /// recoverable "created but not yet compiled" case, so it fails rather than being NotFound.
        /// </summary>
        private static ExitCode TryResolveTypeFromName(string type, out Type componentType, out string error)
        {
            return ScriptTypeResolver.TryResolveComponentType(type, out componentType, out error)
                ? ExitCode.Success
                : ExitCode.Failed;
        }

        /// <summary>
        /// Resolve a component type from a script asset path. Loads the <see cref="MonoScript"/> at the
        /// path and resolves its backing class via <see cref="MonoScript.GetClass"/> — which correctly
        /// handles a class whose name differs from the filename. Validates the class is a concrete
        /// MonoBehaviour. A null class is the recoverable "not yet compiled / ambiguous" case and
        /// mirrors the type-not-found message style. A path that resolves to no MonoScript asset is
        /// <see cref="ExitCode.NotFound"/>, not a recompile-recoverable failure.
        /// </summary>
        private static ExitCode TryResolveTypeFromScriptPath(string path, out Type componentType, out string error)
        {
            componentType = null;
            error = null;
            var trimmed = path.Trim();

            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(trimmed);
            if (mono == null)
            {
                error = $"No MonoScript at '{trimmed}'. Check the asset path (it should be a project-relative " +
                    "path to a .cs file, e.g. 'Assets/Scripts/PlayerController.cs').";
                return ExitCode.NotFound;
            }

            var cls = mono.GetClass();
            if (cls == null)
            {
                error = $"Script at '{trimmed}' has no resolvable class. " +
                    $"If you just created or edited this script it is not compiled yet: run '{CommandNames.ScriptRecompile}', poll " +
                    $"'{CommandNames.ScriptRecompileStatus}' until it reports completed/up_to_date, then retry {CommandNames.ScriptAttach}. " +
                    "If it should already exist, ensure the file has a single top-level type matching the " +
                    "script and that it compiled without errors.";
                return ExitCode.Failed;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(cls))
            {
                error = $"Type '{cls.FullName}' (from '{trimmed}') does not derive from MonoBehaviour and cannot be added as a component.";
                return ExitCode.Failed;
            }

            if (cls.IsAbstract)
            {
                error = $"Type '{cls.FullName}' (from '{trimmed}') is abstract and cannot be instantiated as a component.";
                return ExitCode.Failed;
            }

            componentType = cls;
            return ExitCode.Success;
        }

    }
}
