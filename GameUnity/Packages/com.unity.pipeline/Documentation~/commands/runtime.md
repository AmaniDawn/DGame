# Runtime commands

Commands served by the Player / dev-build server. Many require a Development build running `RuntimePipelineManager` — see [Runtime setup](../runtime-setup.md). Commands marked `RuntimeOnly` are only registered in a player build.

### `runtime_status`
Get comprehensive runtime application status.

No parameters.

**Returns:** `RuntimeStatusResponse`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `quit`
Gracefully quit the Unity application.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `exitCode` | no | `0` | Exit code for the application |

**Returns:** `string`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `set_target_framerate`
Set the target frame rate for the application.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `frameRate` | yes | `–` | Target frame rate (-1 for platform default, 0 for unlimited) |

**Returns:** `string`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `set_timescale`
Set the time scale for the application.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `scale` | yes | `–` | Time scale multiplier (0.0 to pause, 1.0 for normal speed) |

**Returns:** `string`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `simulate_key`
Simulate a keyboard key event (Input System). Drives the running app.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `key` | yes | `–` | Input System Key name, e.g. Space, W, Enter, LeftArrow. |
| `action` | no | `press` | down \| up \| press (down+up). |

**Returns:** `InputSimulationResponse`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`. Requires the Input System package (`ENABLE_INPUT_SYSTEM`); reports unavailable otherwise.

### `simulate_pointer`
Simulate a mouse/pointer event at screen coordinates (Input System).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `x` | yes | `–` | Screen X in pixels (origin bottom-left). |
| `y` | yes | `–` | Screen Y in pixels (origin bottom-left). |
| `action` | no | `click` | move \| down \| up \| click (down+up). |
| `button` | no | `left` | left \| right \| middle. |

**Returns:** `InputSimulationResponse`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`. Requires the Input System package (`ENABLE_INPUT_SYSTEM`); reports unavailable otherwise.

### `log`
Write a message to Unity console.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `message` | yes | `–` | Message to log to console |
| `level` | no | `info` | Log level: info, warning, error |

**Returns:** `string`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `console`
Get captured Unity console output (Editor or Player; supports tail, level filtering, and follow via a cursor).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `tail` | no | `100` | Maximum number of most-recent entries to return |
| `level` | no | `log` | Minimum severity to include: log | warn | error |
| `since` | no | `-1` | Cursor: only return entries newer than this seq. Use the 'cursor' from a previous response to follow. |

**Returns:** `ConsoleLogResponse`
**Notes:** `MainThreadRequired = false`. Available in both the Editor and player builds.

### `eval`
Evaluate C# code dynamically using Roslyn compiler.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `code` | yes | `–` | C# code to evaluate |
| `timeout` | no | `5000` | Timeout in milliseconds |

**Returns:** `EvalResponse`
**Notes:** `MainThreadRequired = true`. Available on both editor and runtime.

### `eval_file`
Evaluate C# code read from a `.cs` file on disk. A convenience alternative to `eval` for code
too long to pass inline — edit the file, then evaluate it. The resolved source is run through the
same evaluation path as `eval`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `file` | yes | `–` | Path to a `.cs` file to evaluate |
| `timeout` | no | `5000` | Timeout in milliseconds |

The `file` is read on the Unity side (relative paths resolve against the Unity process working
directory) and must end in `.cs`. A missing file, a non-`.cs` extension, or an empty file returns
a `Bad Request`.

**Returns:** `EvalResponse`
**Notes:** `MainThreadRequired = true`. Available on both editor and runtime.

### `reload_file`
Compile and apply in-place [HotReload] edits from a source file.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `filename` | yes | `–` | Source file containing [HotReload] methods (e.g. Assets/Scripts/Player.cs) |
| `timeout` | no | `30000` | Compilation timeout in milliseconds |
| `assemblyDir` | no | `–` | Directory to save compiled assemblies to disk (optional, default is in-memory only) |
| `pdb` | no | `false` | Emit debug symbols (portable PDB) mapped to the original source so breakpoints bind in your editor. Compiles unoptimized. |

**Returns:** `HotReloadResponse`
**Notes:** `MainThreadRequired = true`. See [Hot reload](../hot-reload.md).

### `reload_file_override`
Compile and apply hot reload file changes immediately.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `filename` | yes | `–` | Hot reload source file to compile (e.g. PlayerTweaks.cs) |
| `timeout` | no | `30000` | Compilation timeout in milliseconds |
| `assemblyDir` | no | `–` | Directory to save compiled assemblies to disk (optional, default is in-memory only) |

**Returns:** `HotReloadResponse`
**Notes:** `MainThreadRequired = true`. See [Hot reload](../hot-reload.md).

### `hotreload_status`
Show current hot reload registry status and statistics.

No parameters.

**Returns:** `HotReloadResponse`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

### `cleanup_hotreload`
Remove old hot reload DLL versions and clear registry.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `assemblyDir` | yes | `–` | Directory containing assemblies to cleanup |
| `force_domain_reload` | no | `true` | Force Unity domain reload after cleanup |

**Returns:** `HotReloadResponse`
**Notes:** `MainThreadRequired = true`, `RuntimeOnly`.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
