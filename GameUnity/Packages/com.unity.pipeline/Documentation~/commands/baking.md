# Baking commands

Bake and clear precomputed scene data: lightmaps, NavMesh, and occlusion culling. Each `bake_*` runs asynchronously (returns immediately) — poll the matching `*_bake_status` until `completed`. The `clear_*` commands are destructive and require `confirm=true`. The `get_*_settings` / `set_*_settings` pairs read and tune the bake parameters.

## Lighting

### `bake_lighting`
Trigger an async lightmap bake of the open scene(s) via Lightmapping.BakeAsync(). Returns immediately; poll `lighting_bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Recommended (true): a bake overwrites existing lightmap data. Accepted for parity; not required. |
| `dry_run` | no | `false` | If true, validate there is an open bakeable scene and return the current lighting settings without baking. |

**Returns:** `object`

### `lighting_bake_status`
Get the status of the last lighting bake: idle | baking | completed.

**Returns:** `string`

### `cancel_lighting_bake`
Cancel an in-progress lighting bake (Lightmapping.Cancel()).

**Returns:** `object`

### `clear_baked_lighting`
Clear baked lightmap data for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `include_disk_cache` | no | `false` | If true, also clear the GI disk cache (Lightmapping.ClearDiskCache()). |
| `dry_run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `object`

### `get_lighting_settings`
Read the active LightingSettings (lightmapper, bounces, resolution, directional mode, AO, etc.).

**Returns:** `LightingSettingsResult`

### `set_lighting_settings`
Apply a subset of lighting settings to the active LightingSettings. Returns `{ applied[], unknown[] }`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | yes | `–` | JSON object with a subset of lighting fields to set (same names/enums as get_lighting_settings). |
| `dry_run` | no | `false` | If true, validate the keys and report applied/unknown without changing anything. |

**Returns:** `object`

## NavMesh

### `bake_navmesh`
Trigger an async legacy NavMesh bake of the open scene(s) via UnityEditor.AI.NavMeshBuilder. Returns immediately; poll `navmesh_bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Accepted for parity (a bake overwrites the existing NavMesh); not required. |
| `dry_run` | no | `false` | If true, validate there is an open scene and return current NavMesh settings without baking. |

**Returns:** `object`

### `navmesh_bake_status`
Get the status of the last NavMesh bake: idle | baking | completed.

**Returns:** `string`

### `cancel_navmesh_bake`
Cancel an in-progress NavMesh bake (NavMeshBuilder.Cancel()).

**Returns:** `object`

### `clear_navmesh`
Clear the baked NavMesh for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `dry_run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `object`

### `get_navmesh_settings`
Read the default agent's legacy NavMesh bake settings (agentRadius/Height/Slope/Climb, minRegionArea, voxelSize).

**Returns:** `NavMeshSettingsResult`

### `set_navmesh_settings`
Apply a subset of legacy NavMesh bake settings to the default agent. Returns `{ applied[], unknown[] }`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | yes | `–` | JSON object with a subset of NavMesh fields to set (same names as get_navmesh_settings). |
| `dry_run` | no | `false` | If true, validate the keys and report applied/unknown without changing anything. |

**Returns:** `object`

### `bake_navmesh_surfaces`
Bake NavMeshSurface components (AI Navigation package). v1 stub: returns package_not_found when the package is absent.

**Returns:** `object`

## Occlusion culling

### `bake_occlusion_culling`
Trigger an async occlusion-culling bake of the open scene(s) via StaticOcclusionCulling.GenerateInBackground(). Returns immediately; poll `occlusion_bake_status` until completed.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `smallest_occluder` | no | `–` | Smallest object that will occlude others (meters). Defaults to Unity's current value. |
| `smallest_hole` | no | `–` | Smallest gap geometry can have that the view can see through (meters). Defaults to Unity's current value. |
| `backface_threshold` | no | `–` | Backface threshold (1-100); lower trims more backfaces. Defaults to Unity's current value. |
| `confirm` | no | `false` | Accepted for parity (a bake overwrites existing occlusion data); not required. |
| `dry_run` | no | `false` | If true, validate there is an open scene and report the parameters that would be used without baking. |

**Returns:** `object`

### `occlusion_bake_status`
Get the status of the last occlusion bake: idle | baking | completed.

**Returns:** `string`

### `cancel_occlusion_bake`
Cancel an in-progress occlusion bake (StaticOcclusionCulling.Cancel()).

**Returns:** `object`

### `clear_occlusion_culling`
Clear baked occlusion-culling data for the open scene(s). Destructive: requires confirm=true.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `confirm` | no | `false` | Must be true to actually clear (destructive, not undoable via Unity's Undo). |
| `dry_run` | no | `false` | If true, report what would be cleared without clearing. |

**Returns:** `object`

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
