# Runtime commands

Commands served by the Player / dev-build server. Many require a Development build with the runtime Pipeline enabled — see [Runtime setup](../runtime-setup.md). Commands tagged `runtime` target a running Player: they are registered and listed everywhere, but they can only do something useful against a Player, and an Editor batch refuses them.

### `runtime status`
Get comprehensive runtime application status.

No parameters.

**Returns:** `RuntimeStatusResponse`, one result.
**Notes:** `MainThreadRequired = true`, tagged `runtime`.

### `runtime quit`
Gracefully quit the Unity application. In the Editor, behaves like editor quit.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `exit-code` | no | `0` | Exit code for the application |

**Returns:** `{ exitCode }`. What it scheduled is logged at `info`.
**Notes:** `MainThreadRequired = true`, tagged `runtime`. In the Editor it quits the Editor like [`editor quit`](editor-lifecycle-and-observability.md#editor-quit) with no flags, so unsaved changes make it fail.

### `runtime set_target_frame_rate`
Set the target frame rate for the application.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `frame-rate` | yes | `–` | Target frame rate (-1 for platform default, 0 for unlimited) |

**Returns:** `{ frameRate, previousFrameRate }`.
**Notes:** `MainThreadRequired = true`, tagged `runtime`.

### `runtime set_time_scale`
Set the time scale for the application.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `scale` | yes | `–` | Time scale multiplier (0.0 to pause, 1.0 for normal speed) |

**Returns:** `{ scale, previousScale }`.
**Errors:** a negative `scale` exits `InvalidUsage` and changes nothing.
**Notes:** `MainThreadRequired = true`, tagged `runtime`.

### `runtime simulate_key`
Simulate a keyboard key event (Input System). Drives the running app.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `key` | yes | `–` | Input System Key name, e.g. Space, W, Enter, LeftArrow. |
| `action` | no | `press` | down \| up \| press (down+up). |

**Returns:** `{ Command, Detail }`, the detail naming the key and action injected.
**Errors:** on stderr, with a non-zero exit code: `InvalidUsage` for an unknown key or action; `Failed` when no keyboard is present, or when the Input System is unavailable (the error says how to enable it).
**Notes:** `MainThreadRequired = true`, tagged `runtime`. Requires the Input System package (`ENABLE_INPUT_SYSTEM`).

### `runtime simulate_pointer`
Simulate a mouse/pointer event at screen coordinates (Input System).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `x` | yes | `–` | Screen X in pixels (origin bottom-left). |
| `y` | yes | `–` | Screen Y in pixels (origin bottom-left). |
| `action` | no | `click` | move \| down \| up \| click (down+up). |
| `button` | no | `left` | left \| right \| middle. |

**Returns:** `{ Command, Detail }`, the detail naming the position, action and button injected.
**Errors:** on stderr, with a non-zero exit code: `InvalidUsage` for an unknown action or button; `Failed` when no mouse is present, or when the Input System is unavailable (the error says how to enable it).
**Notes:** `MainThreadRequired = true`, tagged `runtime`. Requires the Input System package (`ENABLE_INPUT_SYSTEM`).

### `console write`
Write a message to Unity console.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `message` | yes | `–` | Message to log to console |
| `level` | no | `info` | Log level: info, warning, error |

**Returns:** `{ level, message }`: the level it logged at and the message. An unknown `level` logs at `info`, with a warning on stderr.
**Errors:** an empty `message` exits `InvalidUsage`.
**Notes:** `MainThreadRequired = true`, tagged `runtime`.

### `console get`
Get captured Unity console output (Editor or Player; supports tail, level filtering, and follow via a cursor).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `tail` | no | `100` | Maximum number of most-recent entries to return |
| `level` | no | `log` | Minimum severity to include: log | warn | error |
| `since` | no | `-1` | Cursor: only return entries newer than this seq. Use the 'cursor' from a previous response to follow. |
| `since-session` | no | `–` | Session the `since` cursor came from: pass the `session` from a previous response. A cursor from another session is refused and the tail returned with `reset=true`. |

**Returns:** `ConsoleLogResponse`, one result: its entries travel with the `cursor` and `session` that follow them.
**Notes:** `MainThreadRequired = false`. Available in both the Editor and player builds.

A cursor is only meaningful paired with the `session` that issued it. Sequence numbers restart from
zero when the Editor restarts (the buffer is persisted under `Temp/`, which Unity deletes at every
launch), so a cursor from an earlier session names different entries. Always echo `session` back as
`since-session`; when the pair cannot be honored the response sets `reset=true`, returns the tail
instead of nothing, and gives you a fresh `cursor`/`session` to adopt.

Beyond the entries, the response carries:

- `counts` — errors/warnings/logs the buffer currently retains, before the level filter and `tail`.
- `groundTruth` — what the Editor itself reports: `compilationFailed`, `compiling`, the Editor
  console's own `consoleErrors`/`consoleWarnings`/`consoleLogs`, whether the buffer has been
  `seeded` from the console's store, and `ageMs` for how stale the sample is. Null in a player.

`groundTruth.consoleErrors` exceeding `counts.error` means the Editor console is showing entries the
buffer does not hold. That is expected for output produced before the package was first imported;
otherwise it is the signal that something was missed.

### `console status`
Console ground truth and buffer counters without pulling entries: compile-failure flag, Editor
console counts, and the buffer's retained counts and cursor.

No parameters.

**Returns:** `ConsoleLogResponse` with an empty `entries` array.
**Notes:** `MainThreadRequired = false`. Cheap enough to poll in a loop while a compile runs; use
`console get` when you want the entries themselves.

### `console clear`
Clear the captured log buffer and the Unity Editor console.

No parameters.

**Returns:** `{ cleared: true }`
**Notes:** Available in both the Editor and player builds; in a player only the captured buffer is
cleared, since there is no Editor console. Editor compile errors are logged as sticky entries, which
Unity's own clear skips, so they remain in the Console window until the next compile completes.
Clearing keeps the cursor, so a client following `console get` sees `dropped=true` on its next poll
rather than silently skipping the window that was cleared.

### `script eval`
Runs in a desktop development Player as well as the Editor. See [`script eval`](scripts.md#script-eval) in Script commands.

### `script.methodreload reload`
Compile and apply [MethodReload] edits from a source file.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `filename` | yes | `–` | Source file containing [MethodReload] methods, under Assets/ or a loaded package (e.g. Assets/Scripts/Player.cs) |
| `timeout` | no | `60` | Compilation timeout in seconds |
| `assembly-dir` | no | `–` | Directory to save compiled assemblies to disk (optional, default is in-memory only) |
| `pdb` | no | `false` | Emit debug symbols (portable PDB) mapped to the original source so breakpoints bind in your editor. Compiles unoptimized. |

**Returns:** `{ AssemblyName, Items }`: the assembly it loaded and the ids of the methods it registered. When every method already matches the compiled baseline it reloads nothing: `AssemblyName` is empty and `info` says so. Methods it compiled but skipped are warnings on stderr.
**Errors:** every failure exits `Failed`, with the reason on stderr: an empty or unknown file, a file outside `Assets/` or a loaded package, a compile error, or an edit that compiled but bound no method (each skip reason follows as an error).
**Notes:** `MainThreadRequired = true`. See [Reloading code](../method-reload.md).

### `script.methodreload reload_editor`
Compile [MethodReload] edits and run them through the IlInterpreter VM in this process instead of `Assembly.Load` (IL2CPP-safe; only a minimal host API + the target type are available).

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `filename` | yes | `–` | Source file containing [MethodReload] methods, under Assets/ or a loaded package (e.g. Assets/Scripts/Player.cs) |
| `timeout` | no | `60` | Compilation timeout in seconds |
| `assembly-dir` | no | `–` | Directory to also save the compiled IL to disk (optional, default is in-memory only) |
| `pdb` | no | `false` | Emit a portable PDB mapped to the original source. Runtime errors already report source lines on this backend, so this only matters if you also want the .pdb on disk. |

**Returns:** `{ AssemblyName, Items }`, with the same warnings and failures as `script.methodreload reload`.
**Notes:** `MainThreadRequired = true`. The public-members-only constraint of `script.methodreload reload` does not apply. See [Editor interpreter](../method-reload.md#editor-interpreter).

### `script.methodreload reload_player`
Compile a file's (or folder's) [MethodReload] methods in the Editor and push the IL to connected development players over PlayerConnection (IL2CPP-safe). Folders are walked recursively; files that do not mention `MethodReload` are skipped.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `filename` | yes | `–` | Source `.cs` file or a folder containing [MethodReload] methods (e.g. Assets/pong.cs or Assets/Scripts) |
| `player` | no | `-1` | Target connected player id; `-1` broadcasts to all connected players |

**Returns:** for a file, `{ file, pushed, diagnostics? }`; for a folder, one record per reloadable file, `{ file, pushed, error?, diagnostics? }`. The push summary is logged at the `info` level. A file that fails to compile or push fails (`Failed`, exit 6) with the error and its diagnostics on stderr; a folder with any failed file fails after writing every record. A path that doesn't exist, or a folder with no reloadable file, exits `NotFound` (13).
**Notes:** `MainThreadRequired = true`. Served by the **Editor** server (it needs Roslyn and PlayerConnection), not the player; the player only needs the runtime Pipeline driver, not its HTTP server. See [Player interpreter](../method-reload.md#player-interpreter).

### `script.methodreload status`
Show current method reload registry status and statistics.

No parameters.

**Returns:** `{ ReloadableMethodCount, ActiveOverrideCount, LoadedTypeCount, ActiveOverrideIds }`.
**Notes:** `MainThreadRequired = true`, tagged `scripts/methodreload`. Reachable on an Editor server too — it only reads registry statics.

### `script.methodreload generate_link_xml`
Generate the method-reload `link.xml` at `ProjectSettings/Packages/com.unity.pipeline/MethodReload.link.xml`. Development builds pass it to the linker while **Enable in Development & Profiled Builds** is on, so method reloaded onto an IL2CPP device can call any member of a type the game already uses. Same as **Generate link.xml** in Project Settings → Pipeline → Runtime → Method Reload.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `delete` | no | `false` | Remove the generated file instead of (re)generating it. |

**Returns:** `object` with `path` and `stats` (types kept per source: referenced engine/package types, the game's own types, the interpreter's host surface, `[MethodReload]` targets; file size). With `delete=true`: `deleted` (whether a file existed) and `path`.
**Notes:** `MainThreadRequired = true`. A one-line summary of the stats is logged at the `info` level. Reads the compiled player assemblies under `Library/ScriptAssemblies`, so run it after a successful compile. The file is a snapshot: run again after larger code changes. See [Method reload on IL2CPP devices and managed stripping](../runtime-setup.md#method-reload-on-il2cpp-devices-and-managed-stripping).

### `script.methodreload cleanup`
Remove old method reload DLL versions and clear registry.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `assembly-dir` | yes | `–` | Directory containing assemblies to cleanup |
| `force-domain-reload` | no | `true` | Force Unity domain reload after cleanup |

**Returns:** `{ DeletedFiles, DomainReloadRequested }`. A failed cleanup exits `Failed`, with the reason on stderr.
**Notes:** `MainThreadRequired = true`, tagged `scripts/methodreload`. Reachable on an Editor server too, but excluded from `job batch`: it requests a domain reload, which would tear the batch down mid-run.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
