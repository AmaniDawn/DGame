# Editor lifecycle & observability commands

Commands to control Editor play mode, focus, and menus, and to observe the Editor's state, console, and performance.

### `editor_play`
Enter Unity Editor play mode.

No parameters.

**Returns:** `string`

### `editor_stop`
Exit Unity Editor play mode.

No parameters.

**Returns:** `string`

### `editor_pause`
Pause Unity Editor play mode.

No parameters.

**Returns:** `string`

### `editor_status`
Get detailed Unity Editor status and state information.

No parameters.

**Returns:** `StatusResponse`

### `editor_focus`
Bring the Unity Editor window to the foreground.

No parameters.

**Returns:** `string`
**Notes:** `MainThreadRequired = true`.

### `menu`
Execute an Editor menu item by path, or list available items when no path is given.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | no | `–` | Menu item path to execute, e.g. "Assets/Reimport All". Omit to list available menu items. |

**Returns:** `MenuResponse`
**Notes:** `MainThreadRequired = true`.

### `screenshot`
Capture the Scene or Game view as a PNG and return its file path.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `view` | no | `game` | Which view to capture: 'game' (default) or 'scene' |
| `output` | no | `–` | Output PNG path (absolute, or relative to the project root). Defaults to a timestamped file under <project>/Temp/pipeline-screenshots/. |
| `width` | no | `0` | Output width in pixels. 0 (default) uses the view camera's current width. |
| `height` | no | `0` | Output height in pixels. 0 (default) uses the view camera's current height. |

**Returns:** `ScreenshotResponse`
**Notes:** `MainThreadRequired = true`.

### `set_autotick`
Keep the editor ticking while unfocused by forcing EditorApplication.SignalTick at a throttled rate.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `enable` | no | `true` | Enable (true) or disable (false) auto-tick mode |
| `interval_ms` | no | `16` | Minimum milliseconds between forced ticks. 0 = every update (max rate, pegs a CPU core). Default 16 (~60Hz). |

**Returns:** `string`
**Notes:** `MainThreadRequired = true`. State is static and resets on domain reload (turns itself off after a recompile).

### `get_console_logs`
Read recently captured Editor console logs (structured).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `severity` | no | `all` | Filter: all | log | warning | error. 'all' = every entry; 'log' = Log only; 'warning' = Warning only; 'error' = Error/Exception/Assert only. |
| `limit` | no | `100` | Max entries to return (most-recent first), capped at 1000. |

**Returns:** `object`

### `clear_console`
Clear the captured log buffer and the Unity Editor console.

No parameters.

**Returns:** `object`

### `get_performance_stats`
Read render, memory, and frame-timing stats (structured, read-only).

No parameters.

**Returns:** `PerformanceStats`

### `get_authoring_root`
Get the base folder (under Assets/) that bare authoring paths resolve against.

No parameters.

**Returns:** `object`

### `set_authoring_root`
Set the base folder (under Assets/) that bare authoring paths resolve against and are confined to. Use 'Assets' for full project access.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `root` | yes | `–` | Project-relative folder under Assets/, e.g. Assets/AgentWork. Use 'Assets' to allow the whole project. |

**Returns:** `object`

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
