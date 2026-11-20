using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using static Unity.Pipeline.Editor.Commands.CommandFailure;

namespace Unity.Pipeline.Editor.Commands.GameObjects
{
    /// <summary>
    /// Component authoring commands (CLI-192): add/remove a component on a GameObject and get/set its
    /// serialized properties.
    ///
    /// WHY property get/set goes exclusively through <see cref="SerializedObject"/> /
    /// <see cref="SerializedProperty"/>: it is the only mutation path that dirties the object, records
    /// prefab overrides, and registers a single Undo step (via <see cref="Undo.RecordObject"/> +
    /// <see cref="SerializedObject.ApplyModifiedProperties"/>) the way the Inspector does. Direct
    /// reflection on backing fields would silently desync the Editor. Component types are resolved by
    /// name across all loaded assemblies (<see cref="TypeResolver"/>) so agents can use short or
    /// fully-qualified names.
    /// </summary>
    static class ComponentCommands
    {
        const string Group = "component";

        /// <summary>
        /// Add a component (resolved by type name) to a GameObject. Registered with
        /// <see cref="Undo.AddComponent{T}(GameObject)"/> via the non-generic overload so the addition
        /// is reversible. Writes the new component's identity so its properties can be set next.
        /// </summary>
        [CliGroupCommand(Group, "Add a component (by type name) to a GameObject.", Tags = new[] { "gameobjects/components" }, Effect = CommandEffect.Additive)]
        internal static ExitCode Add(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("type", "Component type name (e.g. 'Rigidbody' or 'UnityEngine.Camera').", Required = true)] string type)
        {
            var code = GameObjectCommands.TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            var componentType = TypeResolver.ResolveComponentType(type);
            if (componentType == null)
                return Fail(cmd, ExitCode.NotFound, $"Could not resolve component type '{type}'.");

            using (new AuthoringUndoScope("Add Component"))
            {
                var component = Undo.AddComponent(go, componentType);
                if (component == null)
                    return Fail(cmd, ExitCode.Failed,
                        $"Failed to add component '{componentType.Name}' to '{go.name}' (it may be disallowed on this GameObject).");

                GameObjectCommands.MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(component));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Remove a component from a GameObject. The component is addressed directly by handle (an
        /// <see cref="ObjectRef"/> resolving to a Component), or by GameObject handle plus a type name.
        /// Uses <see cref="Undo.DestroyObjectImmediate"/> so the removal is reversible.
        /// </summary>
        [CliGroupCommand(Group,
            "Remove a component from a GameObject. Provide either a component handle (target) or a GameObject handle (target) plus a type name.",
            Tags = new[] { "gameobjects/components" },
            Effect = CommandEffect.Destructive)]
        internal static ExitCode Remove(CommandContext cmd,
            [CliArg("target", "Handle of the component to remove, OR of the GameObject when 'type' is given.", Required = true)] ObjectRef target,
            [CliArg("type", "Component type name to remove from the target GameObject (omit when 'target' already points at a component).")] string type = null)
        {
            var code = TryResolveComponent(target, type, out var component, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            var go = component.gameObject;
            var described = ObjectResolver.Describe(component);

            using (new AuthoringUndoScope("Remove Component"))
            {
                Undo.DestroyObjectImmediate(component);
                GameObjectCommands.MarkDirty(go);
            }

            cmd.Result.Write(described);
            return ExitCode.Success;
        }

        /// <summary>
        /// Read the serialized properties of a component as a JSON map. Iterates the visible serialized
        /// surface (skipping the script reference) and converts each property with
        /// <see cref="SerializedPropertyConverter.Read"/>.
        /// </summary>
        [CliGroupCommand(Group,
            "Get a component's serialized properties as a JSON map. Address the component by handle, or by GameObject handle + type.",
            Tags = new[] { "gameobjects/components" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetProperties(CommandContext cmd,
            [CliArg("target", "Handle of the component, OR of the GameObject when 'type' is given.", Required = true)] ObjectRef target,
            [CliArg("type", "Component type name on the target GameObject (omit when 'target' is a component handle).")] string type = null)
        {
            var code = TryResolveComponent(target, type, out var component, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            cmd.Result.Write(ReadProperties(cmd, component));
            return ExitCode.Success;
        }

        /// <summary>
        /// Set one or more serialized properties on a component. Each entry in <paramref name="properties"/>
        /// maps a property path (e.g. "m_Mass" or "myField") to a JSON value. The whole batch is one
        /// Undo step: <see cref="Undo.RecordObject"/> snapshots the component, every property is written
        /// through <see cref="SerializedPropertyConverter.Write"/>, and
        /// <see cref="SerializedObject.ApplyModifiedProperties"/> commits + dirties as one operation.
        /// An unknown property name fails the whole batch with a clear error (no partial apply).
        /// </summary>
        [CliGroupCommand(Group,
            "Set serialized properties on a component (one Undo step). 'properties' maps property name -> value; object references accept an ObjectRef handle.",
            Tags = new[] { "gameobjects/components" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetProperties(CommandContext cmd,
            [CliArg("target", "Handle of the component, OR of the GameObject when 'type' is given.", Required = true)] ObjectRef target,
            [CliArg("properties", "Map of serialized property name to value. Vectors/colors are arrays; object refs are handle objects.", Required = true)] JObject properties,
            [CliArg("type", "Component type name on the target GameObject (omit when 'target' is a component handle).")] string type = null)
        {
            if (properties == null || properties.Count == 0)
                return Fail(cmd, ExitCode.InvalidUsage, "'properties' must contain at least one property to set.");

            var code = TryResolveComponent(target, type, out var component, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Set Component Properties"))
            {
                Undo.RecordObject(component, "Set Component Properties");
                var so = new SerializedObject(component);

                foreach (var pair in properties)
                {
                    var property = so.FindProperty(pair.Key);
                    // Nothing is applied until ApplyModifiedProperties, so returning here writes nothing.
                    if (property == null)
                        return Fail(cmd, ExitCode.NotFound,
                            $"Component '{component.GetType().Name}' has no serialized property '{pair.Key}'.");

                    SerializedPropertyConverter.Write(property, pair.Value);
                }

                // Applies the changes, registers Undo, and dirties the object/scene.
                so.ApplyModifiedProperties();
                GameObjectCommands.MarkDirty(component.gameObject);
            }

            // Re-read so the caller sees the committed values.
            cmd.Result.Write(ReadProperties(cmd, component));
            return ExitCode.Success;
        }

        #region Helpers

        static ComponentPropertiesResult ReadProperties(CommandContext cmd, Component component)
        {
            var so = new SerializedObject(component);

            var properties = new Dictionary<string, JToken>();
            var iterator = so.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false; // only iterate top-level visible properties
                if (iterator.name == "m_Script")
                    continue;

                try
                {
                    properties[iterator.name] = SerializedPropertyConverter.Read(iterator.Copy());
                }
                catch (Exception ex)
                {
                    // Degrade per property rather than failing the whole read.
                    properties[iterator.name] = new JValue($"<error:{ex.Message}>");
                    cmd.Log.Warning($"Could not read property '{iterator.name}': {ex.Message}");
                }
            }

            return new ComponentPropertiesResult
            {
                Component = ObjectResolver.Describe(component),
                Properties = properties
            };
        }

        /// <summary>
        /// Resolve a component either directly (the handle points at a Component) or by GameObject
        /// handle + type name. Throws an <see cref="ArgumentException"/> with a descriptive message
        /// when neither resolves.
        /// </summary>
        internal static Component ResolveComponent(ObjectRef handle, string type)
        {
            if (TryResolveComponent(handle, type, out var component, out var error) != ExitCode.Success)
                throw new ArgumentException(error);
            return component;
        }

        /// <summary>
        /// Resolve a component either directly (the handle points at a Component) or by GameObject
        /// handle + type name. Returns <see cref="ExitCode.NotFound"/> when the handle, the type or the
        /// component doesn't exist and <see cref="ExitCode.InvalidUsage"/> when the arguments don't
        /// fit together, with <paramref name="error"/> saying why.
        /// </summary>
        internal static ExitCode TryResolveComponent(ObjectRef handle, string type, out Component component, out string error)
        {
            component = null;
            error = null;
            if (handle == null || handle.IsEmpty)
            {
                error = "'target' is required.";
                return ExitCode.InvalidUsage;
            }

            if (!ObjectResolver.TryResolve(handle, out var obj, out var resolveError))
            {
                error = $"Could not resolve 'target': {resolveError}";
                return ExitCode.NotFound;
            }

            // The handle already points at a specific Component: honour it directly so we never
            // mutate/remove the wrong instance when the GameObject has several components of the same
            // type. A 'type' here is only a constraint to validate against, NOT a reason to re-fetch
            // GetComponent(type) (which returns the first match and would ignore the handle).
            if (obj is Component directComponent)
            {
                if (!string.IsNullOrEmpty(type))
                {
                    var requestedType = TypeResolver.ResolveComponentType(type);
                    if (requestedType == null)
                    {
                        error = $"Could not resolve component type '{type}'.";
                        return ExitCode.NotFound;
                    }

                    if (!requestedType.IsInstanceOfType(directComponent))
                    {
                        error = $"'target' resolves to a {directComponent.GetType().Name}, which is not a '{requestedType.Name}'.";
                        return ExitCode.InvalidUsage;
                    }
                }

                component = directComponent;
                return ExitCode.Success;
            }

            var go = obj as GameObject;
            if (go == null)
            {
                error = $"'target' did not resolve to a GameObject or Component (got {obj.GetType().Name}).";
                return ExitCode.InvalidUsage;
            }

            if (string.IsNullOrEmpty(type))
            {
                error = "'type' is required when 'target' is a GameObject.";
                return ExitCode.InvalidUsage;
            }

            var componentType = TypeResolver.ResolveComponentType(type);
            if (componentType == null)
            {
                error = $"Could not resolve component type '{type}'.";
                return ExitCode.NotFound;
            }

            component = go.GetComponent(componentType);
            if (component == null)
            {
                error = $"GameObject '{go.name}' has no component of type '{componentType.Name}'.";
                return ExitCode.NotFound;
            }

            return ExitCode.Success;
        }


        #endregion
    }
}
