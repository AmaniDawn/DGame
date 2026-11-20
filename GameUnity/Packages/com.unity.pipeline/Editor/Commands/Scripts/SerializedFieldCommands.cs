using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.GameObjects;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Unity.Pipeline.Editor.Commands.CommandFailure;

namespace Unity.Pipeline.Editor.Commands.Scripts
{
    /// <summary>
    /// Read and write serialized ([SerializeField] / public) fields on a component or asset through
    /// Unity's <see cref="SerializedObject"/> model (CLI-195).
    ///
    /// Why SerializedObject and not plain reflection: it honours Unity's serialization rules (private
    /// [SerializeField] fields, property drawers, prefab overrides), marks the object dirty correctly,
    /// and integrates with Undo. Writes are wrapped in an <see cref="AuthoringUndoScope"/> so an
    /// agent's change reverts as one step.
    ///
    /// Field addressing uses Unity's native SerializedProperty path syntax, so array elements are
    /// reachable directly: "myArray.Array.data[2]" sets the third element; "myArray.Array.size" sets
    /// the array length. Nested fields use dotted paths ("settings.speed").
    /// </summary>
    static class SerializedFieldCommands
    {
        const string Group = "component";

        [CliGroupCommand(Group,
            "Set a serialized field on a component/asset. Supports primitives, enums, Vector/Color/Rect/Bounds, " +
            "object references (value = an ObjectRef: asset by guid/fileId/path or scene object by instanceId/hierarchyPath), " +
            "[Serializable] structs/classes (value = an object of their fields), whole arrays/lists, " +
            "and array elements via 'name.Array.data[i]' (or 'name.Array.size' to resize).",
            Tags = new[] { "scripts", "gameobjects/components" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetField(CommandContext cmd,
            [CliArg("target", "Reference to the component or asset to modify (globalId/path/guid/instanceId/hierarchyPath). May be a GameObject when 'component' is given.", Required = true)] ObjectRef target,
            [CliArg("field", "SerializedProperty path, e.g. 'speed', 'settings.speed', or 'waypoints.Array.data[0]'.", Required = true)] string field,
            [CliArg("value", "JSON value to assign. Vectors, colors and rects as {\"x\":1,\"y\":2}, [1,2] or \"1,2\". A struct/class as an object of the fields to change. Enums by name ([Flags]: \"A|B\") or underlying value. For object references pass an ObjectRef object (or null to clear).", Required = true)] JToken value,
            [CliArg("component", "Component type name on the target GameObject (e.g. 'Rigidbody'). Use when 'target' is a GameObject; omit when 'target' is already a component handle.")] string component = null)
        {
            if (target == null || target.IsEmpty)
                return Fail(cmd, ExitCode.InvalidUsage, CommandNames.ComponentSetField + " 'target' is required.");
            if (string.IsNullOrWhiteSpace(field))
                return Fail(cmd, ExitCode.InvalidUsage, CommandNames.ComponentSetField + " 'field' is required.");

            var code = TryResolveSerializable(target, component, out var obj, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            // Find the field before the Undo scope so a wrong path changes nothing.
            var so = new SerializedObject(obj);
            var prop = so.FindProperty(field);
            if (prop == null)
                return Fail(cmd, ExitCode.NotFound,
                    $"Field '{field}' was not found on '{obj.GetType().Name}'. " +
                    "Use a SerializedProperty path (e.g. 'speed', 'settings.speed', 'items.Array.data[0]').");

            using (new AuthoringUndoScope($"Set {field}"))
            {
                // RegisterCompleteObjectUndo captures the pre-change state for revert; the
                // SerializedObject then records the change for prefab/asset dirtying.
                Undo.RegisterCompleteObjectUndo(obj, $"Set {field}");

                SerializedPropertyConverter.SetValue(prop, value);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }

            cmd.Result.Write(ObjectResolver.Describe(obj) ?? new AuthoringResult { Type = obj.GetType().Name });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Read serialized fields of a component/asset. Returns each top-level field's name, type and value " +
            "(object references are returned as re-usable handles). Pass 'field' to read a single SerializedProperty path. " +
            "Pass detail='value' to return just the value(s) — a token-efficient read for scalars.",
            Tags = new[] { "scripts", "gameobjects/components" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetFields(CommandContext cmd,
            [CliArg("target", "Reference to the component or asset to read (globalId/path/guid/instanceId/hierarchyPath). May be a GameObject when 'component' is given.", Required = true)] ObjectRef target,
            [CliArg("field", "Optional single SerializedProperty path to read (e.g. 'speed' or 'items.Array.data[0]'). Omit to read all top-level fields.")] string field = null,
            [CliArg("component", "Component type name on the target GameObject (e.g. 'Rigidbody'). Use when 'target' is a GameObject; omit when 'target' is already a component handle.")] string component = null,
            [CliArg("detail", "full (default) for a descriptor per field {name,path,propertyType,isArray,arrayLength,value}; value for just the value(s): the raw value for a single 'field', or a {name: value} map for all fields.")] string detail = null)
        {
            if (target == null || target.IsEmpty)
                return Fail(cmd, ExitCode.InvalidUsage, CommandNames.ComponentGetFields + " 'target' is required.");

            // Value projection (AUTHAPI-21): drop the per-field descriptor and return only the value(s)
            // so a scalar read costs bytes, not a descriptor. Anything other than 'value' is the default
            // descriptor form, so existing callers are unaffected.
            var valueOnly = string.Equals(detail, "value", StringComparison.OrdinalIgnoreCase);

            var code = TryResolveSerializable(target, component, out var obj, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            var so = new SerializedObject(obj);
            cmd.Log.Debug($"Reading fields of a {obj.GetType().Name}.");

            if (!string.IsNullOrWhiteSpace(field))
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                    return Fail(cmd, ExitCode.NotFound, $"Field '{field}' was not found on '{obj.GetType().Name}'.");

                if (valueOnly)
                    cmd.Result.Write(SerializedPropertyConverter.GetValue(prop));
                else
                    cmd.Result.Write(DescribeProperty(prop));
                return ExitCode.Success;
            }

            // The value form is one {name: value} map; the descriptor form is one record per field.
            var values = valueOnly ? new Dictionary<string, object>() : null;
            var iterator = so.GetIterator();
            // enterChildren=true on the first MoveNext to step into the top level; then false to stay
            // at the top level and skip nested children (callers drill in via an explicit 'field').
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // m_Script is Unity's hidden back-reference to the MonoScript; not a user field.
                if (iterator.propertyPath == "m_Script")
                    continue;

                if (valueOnly)
                {
                    // Degrade per-field rather than failing the whole map read (mirrors
                    // component get_properties' per-property guard in ComponentCommands).
                    try { values[iterator.name] = SerializedPropertyConverter.GetValue(iterator); }
                    catch (Exception ex)
                    {
                        values[iterator.name] = $"<error:{ex.Message}>";
                        cmd.Log.Warning($"Could not read field '{iterator.name}': {ex.Message}");
                    }
                }
                else
                {
                    cmd.Result.WriteRecord(DescribeProperty(iterator));
                }
            }

            if (valueOnly)
                cmd.Result.Write(values);
            return ExitCode.Success;
        }

        /// <summary>
        /// Resolve a handle to an object that a SerializedObject can wrap: a Component or an asset.
        ///
        /// Two addressing forms (CLI-225):
        ///   * The handle already points at a Component (or asset) → use it directly. An optional
        ///     <paramref name="component"/> type name is validated against it as a guard.
        ///   * The handle points at a GameObject AND <paramref name="component"/> is given → look up
        ///     the matching component(s) on that GameObject. Exactly one match is used; MULTIPLE
        ///     same-type components are an error that lists each instanceId so the agent can re-address
        ///     by instanceId; zero matches is a clear error.
        ///
        /// A GameObject with no <paramref name="component"/> is rejected (a GameObject's "fields" are
        /// its components), with a hint to pass --component or a specific component handle.
        ///
        /// This mirrors <see cref="ComponentCommands.TryResolveComponent"/>'s GO-path+type pattern, but
        /// deliberately errors on MULTIPLE matches (that helper returns the first) so a set/get never
        /// silently targets the wrong instance. Returns <see cref="ExitCode.NotFound"/> when the handle,
        /// type or component doesn't exist and <see cref="ExitCode.InvalidUsage"/> when the arguments
        /// don't fit together, with <paramref name="error"/> saying why.
        /// </summary>
        private static ExitCode TryResolveSerializable(ObjectRef target, string component, out Object result, out string error)
        {
            result = null;
            error = null;
            if (!ObjectResolver.TryResolve(target, out var obj, out var resolveError))
            {
                error = $"Could not resolve target: {resolveError}";
                return ExitCode.NotFound;
            }

            // The handle already points at a specific Component: honour it directly. A 'component' here
            // is only a constraint to validate against, never a reason to re-fetch (which could pick a
            // different same-type instance and ignore the handle).
            if (obj is Component directComponent)
            {
                if (!string.IsNullOrWhiteSpace(component))
                {
                    var requestedType = TypeResolver.ResolveComponentType(component);
                    if (requestedType == null)
                    {
                        error = $"Could not resolve component type '{component}'.";
                        return ExitCode.NotFound;
                    }

                    if (!requestedType.IsInstanceOfType(directComponent))
                    {
                        error = $"Target resolves to a {directComponent.GetType().Name}, which is not a '{requestedType.Name}'.";
                        return ExitCode.InvalidUsage;
                    }
                }

                result = directComponent;
                return ExitCode.Success;
            }

            if (obj is GameObject go)
            {
                if (string.IsNullOrWhiteSpace(component))
                {
                    error = $"Target '{target}' is a GameObject. Pass --component <TypeName> to pick a component on it, " +
                        "or target a specific component directly (use its instanceId/globalId), " +
                        "to read or set serialized fields.";
                    return ExitCode.InvalidUsage;
                }

                var componentType = TypeResolver.ResolveComponentType(component);
                if (componentType == null)
                {
                    error = $"Could not resolve component type '{component}'.";
                    return ExitCode.NotFound;
                }

                var matches = go.GetComponents(componentType).Where(c => c != null).ToArray();
                if (matches.Length == 0)
                {
                    error = $"GameObject '{go.name}' has no component of type '{componentType.Name}'.";
                    return ExitCode.NotFound;
                }

                if (matches.Length > 1)
                {
                    var sb = new StringBuilder();
                    sb.Append($"GameObject '{go.name}' has {matches.Length} components of type '{componentType.Name}'. ")
                      .Append("Re-target a specific one by its instanceId: ");
                    for (int i = 0; i < matches.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append($"instanceId {PipelineUtils.GetObjectId(matches[i])}");
                    }
                    sb.Append('.');
                    error = sb.ToString();
                    return ExitCode.InvalidUsage;
                }

                result = matches[0];
                return ExitCode.Success;
            }

            // Not a Component, not a GameObject — an asset (e.g. ScriptableObject) handled directly.
            // A 'component' filter is meaningless for an asset; reject it rather than silently ignore it
            // (a supplied component here signals a misrouted or misspelled request).
            if (!string.IsNullOrWhiteSpace(component))
            {
                error = $"Target '{target}' resolved to a {obj.GetType().Name} (an asset), which has no components. " +
                    "Omit --component when targeting an asset directly.";
                return ExitCode.InvalidUsage;
            }

            result = obj;
            return ExitCode.Success;
        }


        private static object DescribeProperty(SerializedProperty prop)
        {
            return new
            {
                name = prop.name,
                path = prop.propertyPath,
                propertyType = prop.propertyType.ToString(),
                isArray = prop.isArray && prop.propertyType != SerializedPropertyType.String,
                arrayLength = (prop.isArray && prop.propertyType != SerializedPropertyType.String) ? prop.arraySize : (int?)null,
                value = SerializedPropertyConverter.GetValue(prop)
            };
        }
    }
}
