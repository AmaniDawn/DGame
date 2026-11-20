# Safety & mutations

Mutating commands — asset writes, project-settings changes, package/build changes —
share two conventions so command authors and clients know what to expect: a `confirm`/`dry-run` gate
on destructive or overwriting operations, and Undo grouping via `AuthoringUndoScope`. Commands that
resolve caller-supplied asset paths confine them with `ProjectPaths` (see
[Creating authoring commands](authoring-commands.md)). The helpers live in `Editor/Authoring/`.

### The confirm / dry-run convention

Destructive or overwriting commands expose two boolean `[CliArg]`s — `confirm` and `dry-run` — and
gate their own mutation on them. There is no central dispatcher; each command applies the same simple
rules inline:

- **`dry-run == true`** → validate and describe the intended change, mutate nothing, write a preview.
  This wins even if `confirm` is also true.
- **`confirm == false`** (and not a dry run) → refuse without mutating: log why with `cmd.Log.Error`
  and return `ExitCode.InvalidUsage`.
- **otherwise** (`confirm == true`, not a dry run) → perform the mutation. Group *scene/object*
  mutations in an `AuthoringUndoScope` (see below). Asset/settings/package writes are **not** part of
  Unity's Undo, so they run directly — say so in the command description ("Not undoable via Ctrl+Z.").

```csharp
[CliGroupCommand(Group, "Clear baked lightmap data for the open scene(s). Destructive: requires confirm=true (use dry-run to preview). Not undoable via Ctrl+Z.")]
internal static ExitCode ClearBake(
    CommandContext cmd,
    [CliArg("confirm", "Apply the clear. Without it the call is refused.")] bool confirm = false,
    [CliArg("dry-run", "Preview what would be cleared without clearing it.")] bool dryRun = false)
{
    // …resolve the open scene(s)…
    if (dryRun)
    {
        cmd.Result.Write(new { status = "dry_run", wouldClear = true });
        return ExitCode.Success;
    }
    if (!confirm)
    {
        cmd.Log.Error("Refusing to clear baked lighting. Pass confirm=true to apply, or dry-run=true to preview.");
        return ExitCode.InvalidUsage;
    }

    // Clearing baked GI is not part of Unity's Undo — no AuthoringUndoScope.
    Lightmapping.Clear();

    cmd.Result.Write(new { cleared = true });
    return ExitCode.Success;
}
```

Purely additive, non-destructive commands (e.g. `asset create_folder`, `animation.animator add_parameter`)
do not take `confirm`/`dry-run`; they just perform the mutation directly.

### Undo grouping

Scene/object mutations are grouped with `AuthoringUndoScope` so a multi-step command reverts as a
single Ctrl+Z step. It increments Unity's current Undo group on construction and calls
`Undo.CollapseUndoOperations` on dispose, collapsing everything the body registers — via
`Undo.RecordObject`, `Undo.RegisterCreatedObjectUndo`, and similar — into one named step. The command
opts into Undo by calling those APIs inside the scope. See
[Creating authoring commands](authoring-commands.md) for the full pattern and the per-mutation `Undo`
API table.

> **Caveat.** Some operations are not part of Unity's Undo system. `AssetDatabase` operations, UPM
> package changes, and settings backed by native assets may not be undoable; for those the collapse is
> simply a no-op and Ctrl+Z will not revert the change. Don't rely on Undo to clean up such writes —
> validate up front and fail before writing.

### Path confinement

Commands that accept a caller-supplied asset path resolve it through `ProjectPaths.Resolve`, which
normalizes the path, rejects `..` traversal, and confines it to the configurable authoring root
(default `Assets`) or to an embedded or local package under `Packages/`, so agent-supplied paths cannot
escape the project. Registry, Git and built-in packages are read-only and refused. It returns a
project-relative path, or `null` with an `error` string when the path escapes both.

```csharp
var normalized = ProjectPaths.Resolve(path, out var error);
if (normalized == null)
{
    cmd.Log.Error(error);
    return ExitCode.InvalidUsage;
}
// normalized is project-relative, under the authoring root or an embedded/local package.
```

See [Creating authoring commands](authoring-commands.md) for the full path-handling rules and the
`authoring set_root` / `authoring get_root` commands that adjust the root.

Parameters that name an arbitrary file rather than a project asset are not confined, and accept any
path the user can access:

| Command | Parameter |
|---|---|
| `asset import` | `source` (the destination `path` is confined) |
| `capture editor_element`, `capture runtime_element` | `output` |
| `diagnostics run_audit` | `output` |
| `build run` | `outputPath` |
| `script eval`, `script run` | `file` |
| `script.methodreload reload_player` | `filename` |

`script.methodreload reload` only accepts files under `Assets/` or a loaded package.

### For command authors

To make a mutating command safe:

1. If the operation is destructive or overwrites existing content, add `confirm` and `dry-run` boolean
   `[CliArg]`s and gate on them inline (preview on `dry-run`, refuse on `!confirm`).
2. Group scene/object mutations in `using (new AuthoringUndoScope("<operation>"))` and register Undo
   operations inside it where the mutation is undoable.
3. Resolve any caller-supplied asset path through `ProjectPaths.Resolve` before touching the filesystem
   — ideally during validation, so a bad path fails before anything is applied.
4. Declare `Effect = CommandEffect.Destructive` on a command that takes `confirm`, so a client knows to
   ask before it runs (see [Declaring the effect](creating-commands.md#declaring-the-effect)).

See [Creating commands](creating-commands.md) and [Creating authoring commands](authoring-commands.md).
