# Baking commands

Bake and clear precomputed scene data: lightmaps, NavMesh, and occlusion culling. The three bake families have a group each: `lighting`, `navmesh` and `occlusion`. Each `bake` command runs asynchronously (returns immediately) — poll the matching `bake_status` in the same group until `completed`. The `clear_bake` commands are destructive and require `confirm=true`. The `get_settings` / `set_settings` pairs in `lighting` and `navmesh` read and tune the bake parameters.

Each command writes one JSON object to stdout (`navmesh bake_surfaces`, a stub, writes none) and its errors to stderr. A `bake` with no open, saved scene exits `NotFound` (nothing is started); a `bake` while one of its kind is already running exits `Failed`; a `clear_bake` without `confirm=true` exits `InvalidUsage`. A `bake` returns as soon as the bake starts, so stopping the command doesn't stop the bake: use the group's `cancel_bake`.

## Lighting

### `lighting bake`
Trigger an async lightmap bake of the open scene(s) via Lightmapping.BakeAsync(). Returns immediately; poll `lighting bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Recommended (true): a bake overwrites existing lightmap data. Accepted for parity; not required. |
| `dry-run` | no | `false` | If true, validate there is an open bakeable scene and return the current lighting settings without baking. |

**Returns:** `{ "status": "baking", "bakeId": "…" }`; with `dry-run`, `{ "status": "dry_run", "wouldBake": true, "settings": { … } }` (the `lighting get_settings` object).
**Fails:** `NotFound` with no open, saved scene; `Failed` when a lighting bake is already running or `Lightmapping.BakeAsync()` refuses to start.

### `lighting bake_status`
Get the status of the last lighting bake: idle | baking | completed.

**Returns:** the bake's status payload (`status`, `bakeId`, `result`, `isBaking`, `bakeTimeMs`, `stats` once completed), or `{"status":"idle"}` before the first bake.

### `lighting cancel_bake`
Cancel an in-progress lighting bake (Lightmapping.Cancel()).

**Returns:** `{ "cancelled": <bool> }` — whether a bake was running.

### `lighting clear_bake`
Clear baked lightmap data for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `include-disk-cache` | no | `false` | If true, also clear the GI disk cache (Lightmapping.ClearDiskCache()). |
| `dry-run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `{ "cleared": true, "includeDiskCache": <bool> }`; with `dry-run`, `{ "status": "dry_run", "wouldClear": true, "includeDiskCache": <bool> }`.
**Fails:** `InvalidUsage` without `confirm=true`.

### `lighting get_settings`
Read the active LightingSettings (lightmapper, bounces, resolution, directional mode, AO, etc.).

**Returns:** `LightingSettingsResult` (`available`, `lightmapper`, `bakedGI`, `realtimeGI`, sample counts, `bounces`, `lightmapResolution`, `lightmapPadding`, `maxLightmapSize`, `lightmapCompression`, `directionalMode`, `ao`, `aoMaxDistance`, `filteringMode`). `available` is false when the active scene exposes no LightingSettings.

### `lighting set_settings`
Apply a subset of lighting settings to the active LightingSettings. Returns `{ applied[], unknown[] }`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | yes | `–` | JSON object with a subset of lighting fields to set (same names/enums as `lighting get_settings`). |
| `dry-run` | no | `false` | If true, validate the keys and report applied/unknown without changing anything. |

**Returns:** `{ "applied": [...], "unknown": [...], "settingsSource": "asset"|"scene-default"|"created"|"none", "dryRun": <bool> }`. Unknown keys are ignored with a warning on stderr.
**Fails:** `InvalidUsage` for a value that isn't valid for its key (e.g. an unknown enum name).

## NavMesh

### `navmesh bake`
Trigger an async legacy NavMesh bake of the open scene(s) via UnityEditor.AI.NavMeshBuilder. Returns immediately; poll `navmesh bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Accepted for parity (a bake overwrites the existing NavMesh); not required. |
| `dry-run` | no | `false` | If true, validate there is an open scene and return current NavMesh settings without baking. |

**Returns:** `{ "status": "baking", "bakeId": "…" }`; with `dry-run`, `{ "status": "dry_run", "wouldBake": true, "settings": { … } }` (the `navmesh get_settings` object).
**Fails:** `NotFound` with no open, saved scene; `Failed` when a NavMesh bake is already running.

### `navmesh bake_status`
Get the status of the last NavMesh bake: idle | baking | completed.

**Returns:** the bake's status payload (`status`, `bakeId`, `result`, `bakeTimeMs`), or `{"status":"idle"}` before the first bake.

### `navmesh cancel_bake`
Cancel an in-progress NavMesh bake (NavMeshBuilder.Cancel()).

**Returns:** `{ "cancelled": <bool> }` — whether a bake was running.

### `navmesh clear_bake`
Clear the baked NavMesh for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `dry-run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `{ "cleared": true }`; with `dry-run`, `{ "status": "dry_run", "wouldClear": true }`.
**Fails:** `InvalidUsage` without `confirm=true`.

### `navmesh get_settings`
Read the default agent's legacy NavMesh bake settings (agentRadius/Height/Slope/Climb, minRegionArea, voxelSize).

**Returns:** `NavMeshSettingsResult` (`available`, `agentRadius`, `agentHeight`, `agentSlope`, `agentClimb`, `minRegionArea`, `voxelSize`, `manualVoxelSize`).

### `navmesh set_settings`
Apply a subset of legacy NavMesh bake settings to the default agent. Returns `{ applied[], unknown[] }`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | yes | `–` | JSON object with a subset of NavMesh fields to set (same names as `navmesh get_settings`). |
| `dry-run` | no | `false` | If true, validate the keys and report applied/unknown without changing anything. |

**Returns:** `{ "applied": [...], "unknown": [...], "dryRun": <bool> }`. Unknown keys are ignored with a warning on stderr.
**Fails:** `Failed` when the legacy NavMesh settings object can't be accessed.

### `navmesh bake_surfaces`
Bake NavMeshSurface components (AI Navigation package). v1 stub: fails, saying whether the package is absent or the bake is not implemented yet.

**Returns:** nothing.
**Fails:** always `Failed` (v1 stub); the error says whether the AI Navigation package is absent or the bake is not implemented yet.

## Occlusion culling

### `occlusion bake`
Trigger an async occlusion-culling bake of the open scene(s) via StaticOcclusionCulling.GenerateInBackground(). Returns immediately; poll `occlusion bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `smallest-occluder` | no | `–` | Smallest object that will occlude others (meters). Defaults to Unity's current value. |
| `smallest-hole` | no | `–` | Smallest gap geometry can have that the view can see through (meters). Defaults to Unity's current value. |
| `backface-threshold` | no | `–` | Backface threshold (1-100); lower trims more backfaces. Defaults to Unity's current value. |
| `confirm` | no | `false` | Accepted for parity (a bake overwrites existing occlusion data); not required. |
| `dry-run` | no | `false` | If true, validate there is an open scene and report the parameters that would be used without baking. |

**Returns:** `{ "status": "baking", "bakeId": "…" }`; with `dry-run`, `{ "status": "dry_run", "wouldBake": true, "parameters": { "smallestOccluder", "smallestHole", "backfaceThreshold" } }`.
**Fails:** `NotFound` with no open, saved scene; `Failed` when an occlusion bake is already running; `InvalidUsage` for a non-positive `smallest-occluder` / `smallest-hole` or a `backface-threshold` outside 1-100.

### `occlusion bake_status`
Get the status of the last occlusion bake: idle | baking | completed.

**Returns:** the bake's status payload (`status`, `bakeId`, `result`, `bakeTimeMs`, `stats.umbraDataSizeBytes` once completed), or `{"status":"idle"}` before the first bake.

### `occlusion cancel_bake`
Cancel an in-progress occlusion bake (StaticOcclusionCulling.Cancel()).

**Returns:** `{ "cancelled": <bool> }` — whether a bake was running.

### `occlusion clear_bake`
Clear baked occlusion-culling data for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `dry-run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `{ "cleared": true }`; with `dry-run`, `{ "status": "dry_run", "wouldClear": true }`.
**Fails:** `InvalidUsage` without `confirm=true`.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
