# Creating authoring commands

*Authoring commands* are the subset of commands that **create or mutate project content** — assets,
scenes, GameObjects, components — on behalf of an agent. They build on the base command API in
[Creating commands](creating-commands.md); this page covers the four concerns that are specific to
content authoring:

- **The authoring root** — the sandbox that bare paths resolve against and writes are confined to,
  alongside the project's embedded and local packages.
- **`ObjectRef`** — how a command *receives* a reference to an existing object.
- **`AuthoringResult`** — how a command *returns* the identity of an object it created or touched.
- **Undo/redo** — grouping a command's scene mutations into a single, revertible step.

Together these let one command's output feed the next command's input, so an agent can chain calls
(`gameobject create` → `component add` → `component set_properties` → `prefab create`) without ever
handling a raw Unity object itself. If you want to support a content type the package doesn't cover
yet (materials, audio, lighting, terrain, …), this is the pattern to follow.

Read [Creating commands](creating-commands.md) first — this page assumes you know how `[CliGroupCommand]`,
`[CliArg]`, `MainThreadRequired`, and the response envelope work.

## Authoring vs. management commands

Not every state-changing command is an *authoring* command. This page — and the [checklist](#checklist-for-a-new-authoring-command)
at the end — applies to commands that **create or mutate project content** (assets, scenes,
GameObjects, components) and hand the agent back an object identity.

**Management commands** change *configuration* or *drive tooling* rather than author content:
project settings (`Editor/Commands/ProjectSettings/`), builds (`Build/`), and packages
(`PackageManager/`). They are a **different category** and deliberately do
**not** follow the full authoring contract. They share only the cross-cutting safety conventions
(the `confirm`/`dry-run` gate and, where relevant, `ObjectResolver`/`ProjectPaths`) — see
[Safety & mutations](safety-and-mutations.md).

| Concern | Authoring command | Management command |
|---|---|---|
| Object input | `ObjectRef` → `ObjectResolver.TryResolve` | same, when it references an object (e.g. a scene in the build list) |
| Path input | `ProjectPaths.Resolve` (confined to the authoring root and embedded/local packages) | as needed; may be unconfined by design (e.g. a build output path) |
| Undo | scene/object mutations wrapped in `AuthoringUndoScope` + registered `Undo` APIs | **none** — settings / AssetDatabase / UPM writes don't participate in Undo, so no scope; note `Not undoable via Ctrl+Z.` in the description instead |
| Destructive gate | `confirm`/`dry-run` where destructive | `confirm`/`dry-run` where destructive (same convention) |
| Result | `cmd.Result.Write(AuthoringResult)` via `ObjectResolver.Describe`; one `WriteRecord` per object for several | a domain model (e.g. `ProjectSettingsResponse`, `PackageMutationResponse`) or a plain result object, with no success or message fields |
| Failure | `cmd.Log.Error` and a non-zero `ExitCode` (`InvalidUsage`, `NotFound`, `Failed`); a shared helper may throw | the same |

If you're adding a config/build/package command, follow the management column and the shared safety
conventions; the checklist below is for content-authoring commands.

## The building blocks

| Type | Namespace | Role |
|------|-----------|------|
| `ProjectPaths` | `Unity.Pipeline.Editor.Authoring` | Resolve/confine agent-supplied paths to the authoring root and embedded/local packages. |
| `ObjectRef` | `Unity.Pipeline.Models` | Input handle to an existing object (asset or scene object). |
| `AuthoringResult` | `Unity.Pipeline.Models` | Output identity of an object your command created or acted on. |
| `ObjectResolver` | `Unity.Pipeline.Editor.Authoring` | `TryResolve(ObjectRef …)` (handle → object) and `Describe(object)` (object → `AuthoringResult`). |
| `AuthoringUndoScope` | `Unity.Pipeline.Editor.Authoring` | Collapses all `Undo`-registered mutations in its lifetime into one editor undo step. |

Authoring commands are Editor-only. Put them under `Editor/Commands/<Area>/` in the
`Unity.Pipeline.Editor.Commands.<Area>` namespace (see `Editor/Commands/Authoring/AuthoringConfigCommands.cs`).

## The authoring root (`authoring set_root`)

Agents pass **bare, relative paths** (`"Materials/Stone"`), not full project paths. `ProjectPaths`
resolves those against a configurable **authoring root** and *confines* every write to it, or to an
embedded or local package. The root defaults to `Assets` (full project access); an agent can narrow it
to a sub-folder to sandbox itself:

```
authoring get_root                      → { "root": "Assets" }
authoring set_root --root Assets/AgentWork
```

These are just thin commands over `ProjectPaths.AuthoringRoot` (`Editor/Commands/Authoring/AuthoringConfigCommands.cs`):

```csharp
[CliGroupCommand(Group, "Set the base folder (under Assets/) that bare authoring paths resolve against and are confined to. Use 'Assets' for full project access.")]
internal static ExitCode SetRoot(
    CommandContext cmd,
    [CliArg("root", "Project-relative folder under Assets/, e.g. Assets/AgentWork. Use 'Assets' to allow the whole project.", Required = true)] string root)
{
    // Throws ArgumentException for invalid roots (outside Assets/ or containing ".."); the
    // pipeline logs its message as an error and exits with InvalidUsage.
    ProjectPaths.AuthoringRoot = root;
    cmd.Result.Write(new { root = ProjectPaths.AuthoringRoot });
    return ExitCode.Success;
}
```

**Every path parameter your command accepts must go through `ProjectPaths.Resolve`.** This is the
sandbox boundary — it rejects `..` traversal and anything that escapes the root or a writable package, and it lets callers
omit the `Assets/` prefix. Do not build asset paths by string concatenation.

```csharp
var normalized = ProjectPaths.Resolve(path, out var error);
if (normalized == null)
{
    cmd.Log.Error(error);   // e.g. "Path '../secrets' must not contain '..'."
    return ExitCode.InvalidUsage;
}
// normalized is now a project-relative path under the authoring root or an embedded/local package.
```

Resolution rules (`Editor/Authoring/ProjectPaths.cs`):

- Bare paths (`"Materials/Stone"`) are taken relative to the root → `Assets/AgentWork/Materials/Stone`.
- Explicit `Assets/…` paths are used as-is (but still confined to the root).
- `Packages/<name>/…` paths are used as-is when `<name>` is an embedded or local package, whatever the
  root. Registry, Git and built-in packages are read-only and refused:
  `Packages/com.example.tools/Materials/Stone.mat` is accepted if `com.example.tools` is embedded in
  `Packages/` or referenced with `file:`.
- Absolute paths must live under the project root and are converted to project-relative.
- `..` anywhere, or a result outside the root and every writable package, returns `null` + an `error` string.

## Receiving objects: `ObjectRef`

When a command needs to act on an object that *already* exists, take an `ObjectRef` parameter. The
agent supplies **one** of several forms; `ObjectResolver.TryResolve` tries them in order — `globalId`,
`path`, `guid` (+ optional `fileId`), `instanceId`, `hierarchyPath` — and hands you the live object:

```csharp
public static Renderer ResolveRenderer(ObjectRef target)
{
    if (!ObjectResolver.TryResolve(target, out var obj, out var error))
        throw new ArgumentException(error);

    var go = obj as GameObject ?? (obj as Component)?.gameObject;
    var renderer = go != null ? go.GetComponent<Renderer>() : null;
    if (renderer == null)
        throw new ArgumentException($"Object '{target}' has no Renderer.");
    return renderer;
}
```

Resolve **outside** any undo scope / before mutating, so a bad handle fails before your command
changes anything (see `gameobject create`, which resolves its `parent` before entering the scope).

When the handle is a plain string (the usual agent input), a value with a file extension and no
leading `/` — e.g. `"Materials/Floor.mat"` — is taken as an asset path and normalized under the
authoring root, so the `Assets/` prefix is optional (mirroring path-taking commands). A leading `/`
or an extension-less value stays a `hierarchyPath`; a dotted scene name like `"Cube.001"` still
resolves because the `path` branch falls back to a hierarchy lookup.

A `path` of the form `<asset>:<child path>` addresses a GameObject inside a prefab or model asset,
e.g. `"Assets/UI/Screen.prefab:Canvas/Header"`. The child path is relative to the asset's root and
may start with the root's name; naming only the root returns the root. The object is the asset's
own, so a change to it edits the prefab, not a scene instance.

## Returning objects: `AuthoringResult`

Any command that creates or modifies an object should write its identity so the agent can reference
it in a follow-up call. Don't build this by hand — call `ObjectResolver.Describe(obj)`, which fills in
the right fields for the object kind:

- **Assets** get `assetPath`, `guid`, `fileId`, `type` (+ `globalId`).
- **Scene / loaded objects** get `instanceId`, `hierarchyPath`, `type` (+ `globalId`).

```csharp
var result = ObjectResolver.Describe(asset) ?? new AuthoringResult { Type = nameof(Material) };
result.AssetPath = assetPath;   // ensure the path is set even if Describe returned a fresh result
cmd.Result.Write(result);
return ExitCode.Success;
```

`AuthoringResult` is **identity only** — no success flag, no message. How the command ended is its
exit code. To report a failure, log it with `cmd.Log.Error` and return `ExitCode.InvalidUsage` for bad
input, `ExitCode.NotFound` for a handle or path that doesn't resolve, or `ExitCode.Failed` for an
operation that failed (see [Creating commands → Exit codes](creating-commands.md#exit-codes)). A
command that creates several objects writes one `AuthoringResult` record per object.

## Undo/redo

Undo is Unity's native `UnityEditor.Undo`, grouped per command by `AuthoringUndoScope` so a single
call reverts as a single Ctrl+Z step. Register each mutation with the matching `Undo` API **inside**
the scope:

```csharp
using (new AuthoringUndoScope("Set Material"))
{
    Undo.RecordObject(renderer, "Set Material");   // record BEFORE mutating
    renderer.sharedMaterial = material;
    EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
}
```

Which `Undo` call to use:

| Mutation | Call |
|----------|------|
| New scene object | `Undo.RegisterCreatedObjectUndo(obj, name)` |
| Change fields on an existing object | `Undo.RecordObject(obj, name)` / `RegisterCompleteObjectUndo` (before the change) |
| Add a component | `Undo.AddComponent(go, type)` |
| Reparent | `Undo.SetTransformParent(child, parent, name)` |
| Serialized properties | `SerializedObject` + `so.ApplyModifiedProperties()` (registers undo itself) |

> **Important caveat.** `AssetDatabase` operations are **not** part of Unity's undo system. Creating a
> folder or asset, importing, `AssetDatabase.CreateAsset`, `SaveAsPrefabAsset`, and file writes are
> **not undone** by Ctrl+Z. `AuthoringUndoScope` only covers scene/object mutations. If your command
> writes to disk, say so in its description and don't rely on undo to clean up — validate up front and
> fail before writing.

## Worked example — a new content type

Say the package has no material (rendering) commands and you want to add them. Two commands cover the
whole pattern: one that **creates an asset** (path handling + `AuthoringResult` out, no undo because
it's an `AssetDatabase` op) and one that **mutates a scene object** (`ObjectRef` in + undo).

```csharp
using System.IO;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.Rendering
{
    /// <summary>Authoring commands for materials (rendering).</summary>
    static class MaterialCommands
    {
        const string Group = "rendering";

        [CliGroupCommand(Group,
            "Create a Material asset (default shader 'Standard') under the authoring root. " +
            "NOTE: asset creation is not undoable via Ctrl+Z.")]
        internal static ExitCode CreateMaterial(
            CommandContext cmd,
            [CliArg("path", "Asset path relative to the authoring root; the Assets/ prefix and the .mat extension are optional. e.g. Materials/Stone", Required = true)] string path,
            [CliArg("shader", "Shader name to assign. Defaults to 'Standard'.")] string shader = "Standard")
        {
            // 1. Resolve + confine the path (the sandbox boundary).
            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
            {
                cmd.Log.Error(error);
                return ExitCode.InvalidUsage;
            }
            if (!normalized.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase))
                normalized += ".mat";

            var found = Shader.Find(shader);
            if (found == null)
            {
                cmd.Log.Error($"Shader '{shader}' was not found.");
                return ExitCode.InvalidUsage;
            }

            // 2. Do the work (AssetDatabase op — no undo scope; it wouldn't be undoable anyway).
            EnsureParentFolder(normalized);
            var material = new Material(found);
            AssetDatabase.CreateAsset(material, normalized);
            AssetDatabase.SaveAssets();

            // 3. Write the created object's identity so the agent can reference it next.
            var result = ObjectResolver.Describe(material) ?? new AuthoringResult { Type = nameof(Material) };
            result.AssetPath = normalized;
            cmd.Result.Write(result);
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Assign a material asset to a Renderer on a scene GameObject.")]
        internal static ExitCode SetMaterial(
            CommandContext cmd,
            [CliArg("target", "Handle of the GameObject (or Renderer) to modify.", Required = true)] ObjectRef target,
            [CliArg("material", "Handle of the material asset to assign (path/guid/globalId).", Required = true)] ObjectRef material)
        {
            // Resolve both handles up front, before mutating, so a bad handle changes nothing.
            var renderer = ResolveRenderer(target);
            if (!ObjectResolver.TryResolve(material, out var matObj, out var matError))
            {
                cmd.Log.Error(matError);
                return ExitCode.NotFound;
            }
            if (matObj is not Material mat)
            {
                cmd.Log.Error($"'{material}' is not a Material.");
                return ExitCode.InvalidUsage;
            }

            using (new AuthoringUndoScope("Set Material"))
            {
                Undo.RecordObject(renderer, "Set Material");   // record BEFORE the change
                renderer.sharedMaterial = mat;
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            }

            cmd.Result.Write(ObjectResolver.Describe(renderer));
            return ExitCode.Success;
        }

        // A shared helper may throw: the pipeline logs the message as an error, and an
        // ArgumentException exits with InvalidUsage.
        private static Renderer ResolveRenderer(ObjectRef target)
        {
            if (!ObjectResolver.TryResolve(target, out var obj, out var error))
                throw new ArgumentException(error);

            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            var renderer = go != null ? go.GetComponent<Renderer>() : null;
            if (renderer == null)
                throw new ArgumentException($"Object '{target}' has no Renderer.");
            return renderer;
        }

        // Mirrors the shared helper used by the built-in asset commands.
        private static void EnsureParentFolder(string assetPath)
        {
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || AssetDatabase.IsValidFolder(parent))
                return;
            // Create intermediate folders (see `asset create_folder` / CreateFolderRecursive).
            Directory.CreateDirectory(ProjectPaths.ProjectRoot + "/" + parent);
            AssetDatabase.Refresh();
        }
    }
}
```

An agent chains them by feeding the first result into the second:

```
rendering create_material --path Materials/Stone --shader Standard
    → { "assetPath": "Assets/AgentWork/Materials/Stone.mat", "guid": "…", "type": "Material" }

rendering set_material --target '{"hierarchyPath":"/Ground"}' --material '{"path":"Assets/AgentWork/Materials/Stone.mat"}'
    → { "instanceId": …, "hierarchyPath": "/Ground", "type": "MeshRenderer" }
```

## Checklist for a new authoring command

For *content-authoring* commands. Config/build/package commands follow the lighter
[management-command conventions](#authoring-vs-management-commands) instead.

- [ ] Editor-only, under `Editor/Commands/<Area>/`, `internal static`, takes a `CommandContext` and returns `ExitCode`, tagged `[CliGroupCommand]` with the class's `const string Group`; the method name, verb first, gives the command name.
- [ ] Every path parameter resolved through `ProjectPaths.Resolve` (never concatenated).
- [ ] Existing-object inputs taken as `ObjectRef`, resolved via `ObjectResolver.TryResolve`; resolve **before** mutating.
- [ ] Scene/object mutations wrapped in an `AuthoringUndoScope` and registered with the matching `Undo` API.
- [ ] `AssetDatabase` writes noted as non-undoable in the description; validate before writing.
- [ ] Writes `AuthoringResult` (via `ObjectResolver.Describe`) with `cmd.Result.Write`, or one record per object with `cmd.Result.WriteRecord`.
- [ ] Reports failures with `cmd.Log.Error` and a non-zero `ExitCode`; never writes a success flag or message into the result.

## Build & verify

New commands register automatically after the next recompile (see
[Creating commands → Discovery](creating-commands.md#discovery)). Drive the live editor to verify:

```
command script recompile
command script recompile_status          # poll until done
command rendering create_material --path Materials/Stone
```

## See also

- [Creating commands](creating-commands.md) — the base command API this page builds on.
- [Asset & file commands](commands/assets-and-files.md) — reference for the built-in asset commands.
- [GameObject & component commands](commands/gameobjects-and-components.md) — the scene-mutation commands.
