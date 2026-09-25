# Project settings commands

Read and change a representative slice of Unity's project-wide settings (Audio, Graphics, Input, Physics, Player, Quality, Tags & Layers, Time). `get_*` commands read a group's current values into a `ProjectSettingsResponse`; `set_*` commands mutate them through the shared `confirm`/`dry_run` convention, so `confirm=true` is required to apply and `dry_run=true` previews the change without applying it. Every command in this group is `MainThreadRequired = true`. On a `set_*`, omitted fields are left unchanged, and the group is re-read after the write so the response reflects the resulting state. Settings writes are not undoable via Ctrl+Z.

### `get_audio_settings`
Read project Audio settings (volume, rolloff scale, doppler factor).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_audio_settings`
Change project Audio settings. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `volume` | no | `–` | Global audio volume (0..1). |
| `rolloffScale` | no | `–` | Global rolloff scale. |
| `dopplerFactor` | no | `–` | Global doppler factor. |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

### `get_graphics_settings`
Read GraphicsSettings (default render pipeline).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_graphics_settings`
Set the default render pipeline asset. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `renderPipelineAsset` | no | `–` | Reference (path / guid / globalId) to a RenderPipelineAsset to set as the default. Pass an empty reference (`{}`) to select the built-in pipeline; omit to leave unchanged. |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

### `get_input_settings`
Read the legacy Input Manager axes (names and count).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_input_settings`
Tune a legacy Input Manager axis (sensitivity/gravity/dead) by name. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Axis change. 'axis' selects the axis by name; omitted numeric fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `axis` | yes | – | Name of the axis to modify (e.g. 'Horizontal'). |
| `sensitivity` | no | `–` | New sensitivity. |
| `gravity` | no | `–` | New gravity. |
| `dead` | no | `–` | New dead-zone size. |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

### `get_physics_settings`
Read Physics settings (gravity, solver iterations, bounce threshold).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_physics_settings`
Change Physics settings. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `gravityX` | no | `–` | Gravity X component. |
| `gravityY` | no | `–` | Gravity Y component (e.g. -9.81). |
| `gravityZ` | no | `–` | Gravity Z component. |
| `defaultSolverIterations` | no | `–` | Default solver iteration count. |
| `bounceThreshold` | no | `–` | Bounce threshold velocity. |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

### `get_player_settings`
Read PlayerSettings (company/product/version, scripting backend, API level).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_player_settings`
Change PlayerSettings. Requires confirm=true; use dry_run to preview. Scripting backend / API level changes trigger a domain reload.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `companyName` | no | `–` | Company name. |
| `productName` | no | `–` | Product name. |
| `bundleVersion` | no | `–` | Bundle/application version string. |
| `scriptingBackend` | no | `–` | Scripting backend (e.g. Mono2x, IL2CPP). Triggers a domain reload. |
| `apiCompatibilityLevel` | no | `–` | API compatibility level (e.g. NET_Standard_2_0, NET_Unity_4_8). Triggers a domain reload. |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`. Changing `scriptingBackend` or `apiCompatibilityLevel` triggers a domain reload (`requiresDomainReload`); scripting-backend / API-level changes are read and written against the active build target.

### `get_quality_settings`
Read QualitySettings (current level, level names, vSync, anti-aliasing).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_quality_settings`
Change QualitySettings. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `level` | no | `–` | Quality level index (see levelNames from get_quality_settings). |
| `vSyncCount` | no | `–` | VSync count (0 = off, 1, 2). |
| `antiAliasing` | no | `–` | MSAA sample count (0, 2, 4, 8). |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

### `get_tags_layers`
Read the project's tags and (named) layers.

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_tags_layers`
Add/remove tags and assign user layer names (index 8-31). Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Tag/layer changes to make (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `addTags` | no | `–` | Tag names to add. |
| `removeTags` | no | `–` | Tag names to remove. |
| `setLayers` | no | `–` | User layer assignments (index 8-31); array of layer-assignment objects (fields below). |

`setLayers[]` (layer-assignment) fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `index` | yes | – | Layer index (8-31 for user layers). |
| `name` | yes | – | Layer name (empty string clears the slot). |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`. User layers are indices 8-31 (0-7 are reserved).

### `get_time_settings`
Read Time settings (fixedDeltaTime, maximumDeltaTime, timeScale).

No parameters.

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`.

### `set_time_settings`
Change Time settings. Requires confirm=true; use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged (fields below). |
| `confirm` | no | `false` | Apply the change. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields:

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `fixedDeltaTime` | no | `–` | Fixed timestep in seconds (e.g. 0.02). |
| `maximumDeltaTime` | no | `–` | Maximum allowed timestep in seconds. |
| `timeScale` | no | `–` | Time scale (1 = real-time). |

**Returns:** `ProjectSettingsResponse`
**Notes:** `MainThreadRequired = true`. `confirm=true` required to apply; supports `dry_run`.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
