# Scene commands

Commands for creating, opening, saving and inspecting scenes, controlling the active scene, snapshotting a scene's hierarchy, and managing the Build Settings scene list. Mutating scene operations are blocked while the editor is entering or in play mode (they exit `Failed` with an error on stderr before touching any state); read-only commands are safe in play mode. Every command writes its result to stdout and its errors to stderr; a path outside the authoring root exits `InvalidUsage`.

### `scene create`
Create a new scene and save it to the given path under the authoring root.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | yes | – | Scene path relative to the authoring root (default Assets/); the Assets/ prefix and the .unity extension are optional. e.g. Scenes/Level1 |
| `additive` | no | `false` | Open the new scene additively alongside currently open scenes instead of replacing them. |
| `template` | no | `empty` | Initial contents: 'empty' (default) for a blank scene, or 'default' to seed a Main Camera + Directional Light matching Unity's built-in 3D template. |

**Returns:** `AuthoringResult` of the saved scene asset (`assetPath`, `guid`, `type`, …).
**Fails:** `InvalidUsage` for an unknown `template`; `Failed` in play mode or when the scene can't be saved.
**Notes:** Blocked during Play mode.

### `scene open`
Open an existing scene from the given path.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | yes | – | Scene path relative to the authoring root (default Assets/); the Assets/ prefix and the .unity extension are optional. |
| `additive` | no | `false` | Open additively alongside currently open scenes instead of replacing them. |

**Returns:** `AuthoringResult` of the opened scene asset.
**Fails:** `NotFound` when no scene asset is at `path`; `Failed` in play mode or when it can't be opened.
**Notes:** Blocked during Play mode.

### `scene save`
Save an open scene. Saves the active scene when no path is given.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | no | `–` | Path of the open scene to save (authoring-root relative; Assets/ prefix and .unity optional). Omit to save the active scene. |

**Returns:** `AuthoringResult` of the saved scene asset.
**Fails:** `NotFound` when the scene isn't open (or there's no active scene); `Failed` in play mode, for a never-saved (untitled) scene, or when the save fails.
**Notes:** Blocked during Play mode.

### `editor save_all`
Save all open scenes that have unsaved changes.

No parameters.

**Returns:** One record per scene it saved: `{ path }`. Nothing when no open scene had unsaved changes. Logs `Saved N scene(s).` at info level.
**Fails:** `Failed` in play mode, when a dirty scene has never been saved (nothing is saved), or when a save fails.
**Notes:** Blocked during Play mode.

### `scene list_open`
List all currently open scenes with their load/active/dirty state.

No parameters.

**Returns:** One record per open scene: `{ name, path, isLoaded, isDirty, isActive, rootCount }`.

### `scene set_active`
Set which open scene is the active scene (new objects are created in the active scene).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | yes | – | Path of an already-open scene to make active (authoring-root relative; Assets/ prefix and .unity optional). |

**Returns:** `AuthoringResult` of the now-active scene asset.
**Fails:** `NotFound` when the scene isn't open; `Failed` in play mode or when it can't be made active.
**Notes:** Blocked during Play mode.

### `scene get_hierarchy`
Return the GameObject tree of an open scene (or the active scene). Each node carries instanceId + hierarchyPath usable by GameObject commands.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | no | `–` | Path of the open scene to snapshot (authoring-root relative; Assets/ prefix and .unity optional). Omit for the active scene. |

**Returns:** `SceneHierarchy` — `{ sceneName, scenePath, isDirty, isActive, roots: [node] }`, each node `{ name, instanceId, hierarchyPath, activeSelf, components, children }`.
**Fails:** `NotFound` when the scene isn't open (or there's no active scene).

### `scene add_to_build`
Add a scene to the Build Settings scene list (idempotent). Optionally enable it.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | yes | – | Scene path to add (authoring-root relative; Assets/ prefix and .unity optional). |
| `enabled` | no | `true` | Whether the scene is enabled in the build list. |

**Returns:** `{ path, enabled, buildIndex, count }` — `count` is the size of the build scene list.
**Fails:** `NotFound` when no scene asset is at `path`; `Failed` in play mode.
**Notes:** Blocked during Play mode.

### `scene remove_from_build`
Remove a scene from the Build Settings scene list (idempotent).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | yes | – | Scene path to remove (authoring-root relative; Assets/ prefix and .unity optional). |

**Returns:** `{ path, removed, count }` — `removed` is false when the scene wasn't in the list.
**Fails:** `Failed` in play mode.
**Notes:** Blocked during Play mode.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
