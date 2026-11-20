using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Unity.Pipeline.Editor.Commands.CommandFailure;

namespace Unity.Pipeline.Editor.Commands.GameObjects
{
    /// <summary>
    /// GameObject authoring commands (CLI-192): creating GameObjects, querying the scene hierarchy,
    /// and mutating the core GameObject/Transform surface (parenting, transform, active state, tag,
    /// layer, name, deletion).
    ///
    /// WHY these go through the same conventions as the rest of the authoring foundation:
    /// every mutation is wrapped in an <see cref="AuthoringUndoScope"/> and registered with Unity's
    /// Undo system so an agent's multi-step action reverts as a single collapsible step. Each
    /// mutated object is marked dirty (and its scene flagged dirty) so the change survives a Save and
    /// is visible to the rest of the Editor. Results come back as the canonical
    /// <see cref="AuthoringResult"/> envelope (via <see cref="ObjectResolver.Describe"/>) so the
    /// agent can address the same object in a follow-up call.
    /// </summary>
    static class GameObjectCommands
    {
        const string Group = "gameobject";

        /// <summary>
        /// Create an empty GameObject or a built-in primitive in the active scene.
        ///
        /// Creation goes through <see cref="GameObject.CreatePrimitive"/> for primitives (so the
        /// matching mesh + collider are attached exactly as the Editor would) and a plain
        /// <c>new GameObject</c> for the empty case. The object is registered with
        /// <see cref="Undo.RegisterCreatedObjectUndo"/> so it can be reverted, and an optional parent
        /// is resolved by handle so a whole hierarchy can be built in one sequence of calls.
        /// </summary>
        [CliGroupCommand(Group,
            "Create an empty GameObject or a built-in primitive (cube/sphere/capsule/cylinder/plane/quad) in the active scene.",
            Tags = new[] { "gameobjects" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode Create(CommandContext cmd,
            [CliArg("name", "Name for the new GameObject. Defaults to 'GameObject' (or the primitive name).")] string name = null,
            [CliArg("primitive", "Optional primitive type: cube, sphere, capsule, cylinder, plane, quad. Omit for an empty GameObject.")] string primitive = null,
            [CliArg("parent", "Optional parent handle (globalId/path/guid/instanceId/hierarchyPath). The new object becomes a child of it.")] ObjectRef parent = null)
        {
            // Validate and resolve before the scope, so a bad argument creates nothing.
            if (!TryParsePrimitive(primitive, out var primitiveType, out var error))
                return Fail(cmd, ExitCode.InvalidUsage, error);
            var code = TryResolveParent(parent, out var parentTransform, out error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Create GameObject"))
            {
                var go = CreateOne(primitiveType, name, parentTransform);
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Create N GameObjects (empty or a primitive) in one server round-trip (CLI-223).
        ///
        /// WHY a separate plural command rather than overloading <see cref="Create"/>: the
        /// single command's contract — one call writes one <see cref="AuthoringResult"/> — is relied
        /// on by existing callers and tests, so it is left untouched. This plural command is the batch
        /// surface and writes one <see cref="AuthoringResult"/> record per created object, in creation
        /// order. When <paramref name="count"/> &gt; 1 and no explicit name array is supplied, each
        /// object is suffixed Name1..NameN so they remain individually addressable.
        ///
        /// Per-index <paramref name="positions"/>/<paramref name="rotations"/>/<paramref name="scales"/>
        /// are arrays of [x,y,z] vectors. When a vectors array is supplied its length MUST equal
        /// <paramref name="count"/> (a mismatch is a validation error). Omitted channels leave the
        /// object at its creation default (origin / identity / unit scale). The whole batch is wrapped
        /// in ONE <see cref="AuthoringUndoScope"/> so it reverts as a single Undo step.
        /// </summary>
        [CliGroupCommand(Group,
            "Batch-create N empty GameObjects or primitives in one call. Optional positions/rotations/scales are arrays of [x,y,z] (length must equal count). Returns the created identities.",
            Tags = new[] { "gameobjects" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode CreateMany(CommandContext cmd,
            [CliArg("name", "Base name. With count>1 and no explicit names, objects are suffixed Name1..NameN.")] string name = null,
            [CliArg("primitive", "Optional primitive type: cube, sphere, capsule, cylinder, plane, quad. Omit for empty GameObjects.")] string primitive = null,
            [CliArg("parent", "Optional parent handle. Every created object becomes a child of it.")] ObjectRef parent = null,
            [CliArg("count", "How many GameObjects to create. Default 1.")] int count = 1,
            [CliArg("positions", "Local positions, one [x,y,z] per object. Length must equal count when supplied.")] float[][] positions = null,
            [CliArg("rotations", "Local Euler rotations (degrees), one [x,y,z] per object. Length must equal count when supplied.")] float[][] rotations = null,
            [CliArg("scales", "Local scales, one [x,y,z] per object. Length must equal count when supplied.")] float[][] scales = null)
        {
            // Validate and resolve before the scope, so a bad argument creates nothing.
            if (count < 1)
                return Fail(cmd, ExitCode.InvalidUsage, $"'count' must be at least 1, got {count}.");

            var error = ValidateVectorArray(positions, count, "positions")
                ?? ValidateVectorArray(rotations, count, "rotations")
                ?? ValidateVectorArray(scales, count, "scales");
            if (error != null)
                return Fail(cmd, ExitCode.InvalidUsage, error);

            if (!TryParsePrimitive(primitive, out var primitiveType, out error))
                return Fail(cmd, ExitCode.InvalidUsage, error);
            var code = TryResolveParent(parent, out var parentTransform, out error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Create GameObjects"))
            {
                for (int i = 0; i < count; i++)
                {
                    // count==1 keeps the bare name (no "1" suffix) so a single-item batch reads
                    // naturally; count>1 suffixes 1..N for individual addressability.
                    var itemName = count > 1 && !string.IsNullOrEmpty(name) ? name + (i + 1) : name;
                    var go = CreateOne(primitiveType, itemName, parentTransform);

                    if (positions != null)
                        go.transform.localPosition = ToVector3(positions[i]);
                    if (rotations != null)
                        go.transform.localEulerAngles = ToVector3(rotations[i]);
                    if (scales != null)
                        go.transform.localScale = ToVector3(scales[i]);

                    MarkDirty(go);
                    cmd.Result.WriteRecord(ObjectResolver.Describe(go));
                }
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Find GameObjects in the loaded scenes by name, tag, component type, and/or hierarchy path.
        /// All supplied filters are combined (AND). Writes the canonical identity of each match as a
        /// record so an agent can act on the results without a second lookup.
        ///
        /// This is intentionally a structured query rather than a single-object resolve: agents need
        /// to discover what exists before they can reference it, and the records carry the same
        /// <see cref="AuthoringResult"/> identities used everywhere else.
        /// </summary>
        [CliGroupCommand(Group,
            "Find GameObjects in loaded scenes by name, tag, component type, and/or hierarchy path (filters are combined). Returns structured identities.",
            Tags = new[] { "gameobjects" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Find(CommandContext cmd,
            [CliArg("name", "Exact name to match.")] string name = null,
            [CliArg("tag", "Tag to match (e.g. 'Player').")] string tag = null,
            [CliArg("type", "Component type name to match (e.g. 'Rigidbody', 'UnityEngine.Camera').")] string type = null,
            [CliArg("hierarchy-path", "Exact hierarchy path to match (e.g. '/Root/Child').")] string hierarchyPath = null,
            [CliArg("include-inactive", "Include inactive GameObjects. Default true.")] bool includeInactive = true)
        {
            Type componentType = null;
            if (!string.IsNullOrEmpty(type))
            {
                componentType = TypeResolver.ResolveComponentType(type);
                if (componentType == null)
                    return Fail(cmd, ExitCode.NotFound, $"Could not resolve component type '{type}'.");
            }

            // Resolve the hierarchy filter once so it is comparable to each object's computed path.
            var hierarchyFilter = string.IsNullOrEmpty(hierarchyPath) ? null : NormalizeHierarchyPath(hierarchyPath);

            // Validate the tag up front: comparing against an undefined tag (CompareTag / .tag) would
            // log a Unity error per object. An unknown tag simply yields no matches.
            if (!string.IsNullOrEmpty(tag) && !InternalEditorTagExists(tag))
            {
                cmd.Log.Debug($"Tag '{tag}' is not defined in the project, so nothing matches it.");
                return ExitCode.Success;
            }

            foreach (var go in EnumerateSceneGameObjects(includeInactive))
            {
                cmd.CancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(name) && go.name != name)
                    continue;

                if (!string.IsNullOrEmpty(tag) && go.tag != tag)
                    continue;

                if (componentType != null && go.GetComponent(componentType) == null)
                    continue;

                if (hierarchyFilter != null && GetHierarchyPath(go) != hierarchyFilter)
                    continue;

                var described = ObjectResolver.Describe(go);
                if (described != null)
                    cmd.Result.WriteRecord(described);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Set the local transform (position, rotation in Euler degrees, scale) of a GameObject.
        /// Any omitted channel is left unchanged. Goes through <see cref="Undo.RegisterCompleteObjectUndo"/>
        /// on the Transform so the change is reversible and recorded as a prefab override where relevant.
        /// </summary>
        [CliGroupCommand(Group,
            "Set a GameObject's local position/rotation(euler)/scale. Omitted channels are left unchanged.",
            Tags = new[] { "gameobjects" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetTransform(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject to modify.", Required = true)] ObjectRef target,
            [CliArg("position", "Local position as [x,y,z].")] float[] position = null,
            [CliArg("rotation", "Local rotation as Euler angles [x,y,z] in degrees.")] float[] rotation = null,
            [CliArg("scale", "Local scale as [x,y,z].")] float[] scale = null)
        {
            // Validate every channel before changing any, so a bad one leaves the transform as it was.
            var error = ValidateVector(position, "position") ?? ValidateVector(rotation, "rotation") ?? ValidateVector(scale, "scale");
            if (error != null)
                return Fail(cmd, ExitCode.InvalidUsage, error);

            var code = TryResolveGameObject(target, "target", out var go, out error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Set Transform"))
            {
                var transform = go.transform;
                Undo.RegisterCompleteObjectUndo(transform, "Set Transform");

                if (position != null)
                    transform.localPosition = ToVector3(position);
                if (rotation != null)
                    transform.localEulerAngles = ToVector3(rotation);
                if (scale != null)
                    transform.localScale = ToVector3(scale);

                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Reparent a GameObject (or clear its parent so it becomes a scene root). Uses
        /// <see cref="Undo.SetTransformParent"/> so the reparent — including the sibling-index change —
        /// is a single undoable operation. World position is preserved by default to match the
        /// Editor's drag-to-reparent behavior.
        /// </summary>
        [CliGroupCommand(Group,
            "Reparent a GameObject under a new parent, or detach it to scene root when no parent is given.",
            Tags = new[] { "gameobjects" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode SetParent(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject to reparent.", Required = true)] ObjectRef target,
            [CliArg("parent", "Handle of the new parent. Omit (or empty) to move the object to the scene root.")] ObjectRef parent = null,
            [CliArg("world-position-stays", "Keep the object's world position when reparenting. Default true.")] bool worldPositionStays = true)
        {
            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);
            code = TryResolveParent(parent, out var parentTransform, out error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Set Parent"))
            {
                Undo.SetTransformParent(go.transform, parentTransform, worldPositionStays, "Set Parent");
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>Set a GameObject's active self-state.</summary>
        [CliGroupCommand(Group, "Set a GameObject's active self-state (activeSelf).", Tags = new[] { "gameobjects" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetActive(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("active", "Desired active state.", Required = true)] bool active)
        {
            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Set Active"))
            {
                Undo.RegisterCompleteObjectUndo(go, "Set Active");
                go.SetActive(active);
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Set a GameObject's tag. The tag must already exist in the project's Tag Manager; an unknown
        /// tag fails with a clear error rather than silently creating one (tag creation is a project
        /// settings mutation outside this command's scope).
        /// </summary>
        [CliGroupCommand(Group, "Set a GameObject's tag (the tag must already exist in the project).", Tags = new[] { "gameobjects" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetTag(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("tag", "Tag to assign (must exist in the Tag Manager).", Required = true)] string tag)
        {
            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);
            if (!InternalEditorTagExists(tag))
                return Fail(cmd, ExitCode.NotFound, $"Tag '{tag}' does not exist in the project. Add it via the Tag Manager first.");

            using (new AuthoringUndoScope("Set Tag"))
            {
                Undo.RegisterCompleteObjectUndo(go, "Set Tag");
                go.tag = tag;
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Set a GameObject's layer by name or numeric index. A name is resolved via
        /// <see cref="LayerMask.NameToLayer"/>; an out-of-range index or unknown name is rejected.
        /// </summary>
        [CliGroupCommand(Group, "Set a GameObject's layer by name or numeric index (0-31).", Tags = new[] { "gameobjects" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetLayer(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("layer", "Layer name (e.g. 'UI') or numeric index 0-31.", Required = true)] string layer)
        {
            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);
            code = TryResolveLayer(layer, out var layerIndex, out error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Set Layer"))
            {
                Undo.RegisterCompleteObjectUndo(go, "Set Layer");
                go.layer = layerIndex;
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>Rename a GameObject.</summary>
        [CliGroupCommand(Group, "Rename a GameObject.", Tags = new[] { "gameobjects" }, Effect = CommandEffect.Additive)]
        internal static ExitCode Rename(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("name", "New name.", Required = true)] string name)
        {
            if (string.IsNullOrEmpty(name))
                return Fail(cmd, ExitCode.InvalidUsage, "New name must not be empty.");

            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            using (new AuthoringUndoScope("Rename GameObject"))
            {
                Undo.RegisterCompleteObjectUndo(go, "Rename GameObject");
                go.name = name;
                MarkDirty(go);
                cmd.Result.Write(ObjectResolver.Describe(go));
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// Delete a GameObject from the scene. Uses <see cref="Undo.DestroyObjectImmediate"/> so the
        /// deletion is reversible. The result describes the object's identity captured *before*
        /// destruction (the live object is gone afterwards).
        /// </summary>
        [CliGroupCommand(Group, "Delete a GameObject from the scene (reversible via Undo).", Tags = new[] { "gameobjects" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode Delete(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject to delete.", Required = true)] ObjectRef target)
        {
            var code = TryResolveGameObject(target, "target", out var go, out var error);
            if (code != ExitCode.Success)
                return Fail(cmd, code, error);

            // Capture identity before destruction; the live object is invalid afterwards.
            var described = ObjectResolver.Describe(go);
            var scene = go.scene;

            using (new AuthoringUndoScope("Delete GameObject"))
            {
                Undo.DestroyObjectImmediate(go);
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            cmd.Result.Write(described);
            return ExitCode.Success;
        }

        #region Helpers

        /// <summary>
        /// Resolve an <see cref="ObjectRef"/> to a GameObject. Accepts either a GameObject handle or a
        /// Component handle (in which case its owning GameObject is used), matching how agents tend to
        /// hold references. Returns <see cref="ExitCode.NotFound"/> when the handle matches nothing and
        /// <see cref="ExitCode.InvalidUsage"/> when it is empty or names something that isn't a
        /// GameObject, with <paramref name="error"/> saying why.
        /// </summary>
        internal static ExitCode TryResolveGameObject(ObjectRef handle, string argName, out GameObject go, out string error)
        {
            go = null;
            if (handle == null || handle.IsEmpty)
            {
                error = $"'{argName}' is required.";
                return ExitCode.InvalidUsage;
            }

            if (!ObjectResolver.TryResolve(handle, out var obj, out var resolveError))
            {
                error = $"Could not resolve '{argName}': {resolveError}";
                return ExitCode.NotFound;
            }

            go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go == null)
            {
                error = $"'{argName}' did not resolve to a GameObject (got {obj.GetType().Name}).";
                return ExitCode.InvalidUsage;
            }

            error = null;
            return ExitCode.Success;
        }

        /// <summary>Mark a GameObject and its scene dirty so changes persist and the Editor refreshes.</summary>
        internal static void MarkDirty(GameObject go)
        {
            EditorUtility.SetDirty(go);
            if (go.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(go.scene);
        }


        /// <summary>
        /// Create a single GameObject (empty, or a primitive when <paramref name="primitiveType"/> is
        /// set), apply an optional name, register it for Undo, and parent it under
        /// <paramref name="parentTransform"/> when given. Shared by the single <see cref="Create"/> and
        /// the batch <see cref="CreateMany"/> so both build objects identically. MUST be called inside
        /// an <see cref="AuthoringUndoScope"/>.
        /// </summary>
        private static GameObject CreateOne(PrimitiveType? primitiveType, string name, Transform parentTransform)
        {
            var go = primitiveType.HasValue ? GameObject.CreatePrimitive(primitiveType.Value) : new GameObject();

            if (!string.IsNullOrEmpty(name))
                go.name = name;

            Undo.RegisterCreatedObjectUndo(go, "Create GameObject");

            if (parentTransform != null)
                Undo.SetTransformParent(go.transform, parentTransform, "Create GameObject");

            return go;
        }

        /// <summary>
        /// Resolve an optional parent handle to its Transform; null (and success) when no parent is
        /// supplied. Resolved before any creation so an invalid handle fails fast (and a batch creates
        /// nothing).
        /// </summary>
        private static ExitCode TryResolveParent(ObjectRef parent, out Transform parentTransform, out string error)
        {
            parentTransform = null;
            error = null;
            if (parent == null || parent.IsEmpty)
                return ExitCode.Success;

            var code = TryResolveGameObject(parent, "parent", out var go, out error);
            if (code == ExitCode.Success)
                parentTransform = go.transform;
            return code;
        }

        /// <summary>
        /// Validate a per-item vectors array (positions/rotations/scales) up front, before any object is
        /// created. When supplied it must have exactly one entry per object, and every entry must be a
        /// non-null [x,y,z] triple (a JSON array can carry a null element even when its length matches
        /// count). Returns an error naming the offending index, or null when valid, so a batch applies
        /// fully or creates nothing.
        /// </summary>
        private static string ValidateVectorArray(float[][] vectors, int count, string argName)
        {
            if (vectors == null)
                return null;

            if (vectors.Length != count)
                return $"'{argName}' has {vectors.Length} entries but count is {count}; they must match.";

            for (int i = 0; i < vectors.Length; i++)
            {
                var error = ValidateVector(vectors[i], $"{argName}[{i}]", required: true);
                if (error != null)
                    return error;
            }

            return null;
        }

        /// <summary>Returns why <paramref name="values"/> isn't an [x,y,z] triple, or null when it is (or is omitted and not required).</summary>
        private static string ValidateVector(float[] values, string argName, bool required = false)
        {
            if (values == null)
                return required ? $"'{argName}' must be an [x,y,z] array but was null." : null;
            if (values.Length != 3)
                return $"'{argName}' must have exactly 3 components [x,y,z], got {values.Length}.";
            return null;
        }

        /// <summary>Parses an optional primitive name; null (and success) when none is given.</summary>
        private static bool TryParsePrimitive(string value, out PrimitiveType? primitiveType, out string error)
        {
            error = null;
            primitiveType = null;
            if (string.IsNullOrEmpty(value))
                return true;

            switch (value.Trim().ToLowerInvariant())
            {
                case "cube": primitiveType = PrimitiveType.Cube; return true;
                case "sphere": primitiveType = PrimitiveType.Sphere; return true;
                case "capsule": primitiveType = PrimitiveType.Capsule; return true;
                case "cylinder": primitiveType = PrimitiveType.Cylinder; return true;
                case "plane": primitiveType = PrimitiveType.Plane; return true;
                case "quad": primitiveType = PrimitiveType.Quad; return true;
                default:
                    error = $"Unknown primitive '{value}'. Expected one of: cube, sphere, capsule, cylinder, plane, quad.";
                    return false;
            }
        }

        /// <summary>Converts a vector already checked by <see cref="ValidateVector"/>.</summary>
        private static Vector3 ToVector3(float[] values) => new Vector3(values[0], values[1], values[2]);

        private static ExitCode TryResolveLayer(string layer, out int index, out string error)
        {
            error = null;
            if (int.TryParse(layer, out index))
            {
                if (index >= 0 && index <= 31)
                    return ExitCode.Success;
                error = $"Layer index {index} is out of range (0-31).";
                return ExitCode.InvalidUsage;
            }

            index = LayerMask.NameToLayer(layer);
            if (index >= 0)
                return ExitCode.Success;
            error = $"Layer '{layer}' does not exist in the project.";
            return ExitCode.NotFound;
        }

        private static bool InternalEditorTagExists(string tag)
        {
            return UnityEditorInternal.InternalEditorUtility.tags.Contains(tag);
        }

        private static IEnumerable<GameObject> EnumerateSceneGameObjects(bool includeInactive)
        {
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var go in EnumerateRecursive(root, includeInactive))
                        yield return go;
                }
            }
        }

        private static IEnumerable<GameObject> EnumerateRecursive(GameObject go, bool includeInactive)
        {
            if (!includeInactive && !go.activeInHierarchy)
                yield break;

            yield return go;

            var transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                foreach (var child in EnumerateRecursive(transform.GetChild(i).gameObject, includeInactive))
                    yield return child;
            }
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var sb = new System.Text.StringBuilder(go.name);
            var parent = go.transform.parent;
            while (parent != null)
            {
                sb.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return "/" + sb;
        }

        private static string NormalizeHierarchyPath(string path)
        {
            var trimmed = "/" + path.Trim().Trim('/');
            return trimmed;
        }

        #endregion
    }
}
