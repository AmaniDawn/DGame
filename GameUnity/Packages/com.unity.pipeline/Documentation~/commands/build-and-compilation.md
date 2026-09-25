# Build, compilation & test commands

These commands drive the build/compile/test dev loop, plus build configuration: triggering Player builds and reading the resulting BuildReport, switching the active build target, enumerating targets, reading/writing EditorUserBuildSettings, and listing Build Profiles. Several commands are asynchronous: trigger the operation, then poll a matching `*_status` command until it reports completion. The client must tolerate connection errors during a domain reload (recompile, target switch) or a long blocking build.

### `build`
Trigger an async Player build and report the full BuildReport. Returns immediately (queued); poll build_status until status is 'completed'. DetailedBuildReport is included by default unless 'options' is supplied. Use dry_run to validate without building.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | no | `–` | BuildTarget name (e.g. StandaloneWindows64). Defaults to the active target. Must be installed. |
| `outputPath` | no | `–` | Output path (absolute, or relative to the project root). Defaults to the last/auto path. |
| `profileName` | no | `–` | Build Profile name to activate before building (Unity 6 only; ignored otherwise). |
| `options` | no | `–` | BuildOptions names. Omit to get just DetailedBuildReport; supplying any disables that default. |
| `scenes` | no | `–` | Scene asset paths to build (e.g. Assets/Scenes/Main.unity). Defaults to EditorBuildSettings. |
| `confirm` | no | `false` | Acknowledge and run the build; without it the call is refused. Use `dry_run` to validate only. |
| `dry_run` | no | `false` | Validate target/outputPath/scenes without building. |

Supported `options` values (case-insensitive): `Development`, `AllowDebugging`, `ConnectWithProfiler`, `EnableHeadlessMode`, `SymlinkSources`, `BuildAdditionalStreamedScenes`, `CleanBuildCache`, `DetailedBuildReport`. Any unrecognized value is a validation error.

**Returns:** `object` (transient payloads: `queued` / `busy` / `dry_run` / `error`; the finished report is read via `build_status`).
**Notes:** `MainThreadRequired = false`. Async — validates + queues, returns `queued` immediately, then an editor tick runs the (blocking) build; poll `build_status` until status is `completed`. Only one build at a time (returns `busy` otherwise). Mutating: gated by the `confirm`/`dry_run` convention — pass `confirm=true` for a real build, or `dry_run=true` to validate target/outputPath/scenes without building (nothing is queued on a dry run or on validation failure).

### `build_status`
Status of the current/most recent build: idle | queued | building | completed, with the full BuildReport (files, packedAssets, buildSteps, errors, warnings) once completed. Retained until the next build.

No parameters.

**Returns:** `string` (JSON). `idle` when no build has run; `queued`/`building` (the latter with live `elapsedMs`) while in progress; a full `BuildReportResult` once `completed`.

`BuildReportResult` fields (returned when `status` is `completed`):

| Field | Description |
|-------|-------------|
| `status` | Always `completed` here. |
| `buildId` | Id returned by the triggering `build` call. |
| `result` | `Succeeded` \| `Failed` \| `Cancelled` \| `Unknown`. |
| `platform` | Build target the report is for. |
| `outputPath` | Final output location. |
| `totalSizeBytes` | Total build size in bytes. |
| `buildTimeMs` | Total build duration in milliseconds. |
| `buildStartedAt` / `buildEndedAt` | ISO-8601 timestamps (omitted when unset). |
| `totalWarnings` / `totalErrors` | Aggregate counts from the report summary. |
| `files` | Output files (path, role, sizeBytes). Present only on success. |
| `packedAssets` | Packed bundles with per-asset breakdown. Present only on success and requires DetailedBuildReport. |
| `buildSteps` | Per-step name, durationMs, depth, and messages. |
| `errors` / `warnings` | Build issues, each with `message` and best-effort `file`. |

**Notes:** `MainThreadRequired = false`. Reads a Temp status file off-thread, so polling keeps working while a build holds the main thread. The file also survives the domain reload a build can incur, so the last report is retained until the next build.

### `switch_build_target`
Switch the active build target (destructive, long-running: triggers a full reimport + domain reload). Requires confirm=true. Returns immediately; poll switch_build_target_status.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | yes | `–` | BuildTarget name to switch to (must be installed; see list_build_targets). |
| `confirm` | no | `false` | Apply the switch. Without it the call is refused. |

**Returns:** `object` (`switching` / `busy` / `completed` / `error`). Returns `completed` immediately if already on the requested target.
**Notes:** `MainThreadRequired = false`. Async — validates + queues, returns `switching` immediately, then an editor tick performs the (blocking) switch; poll `switch_build_target_status` until `completed`. Only one switch at a time (returns `busy` otherwise). Mutating and confirm-gated: without `confirm=true` the call is refused. The status file survives the domain reload the switch causes, and is reconciled against the active target on the next load.

### `switch_build_target_status`
Status of the last target switch: idle | switching | completed (with success + activeBuildTarget).

No parameters.

**Returns:** `string` (JSON). `idle`, `switching`, or `completed` (with `success` and `activeBuildTarget`, or `errors` on failure).
**Notes:** `MainThreadRequired = false`. Reads a Temp status file off-thread, so it keeps answering while the switch holds the main thread.

### `list_build_targets`
List the known BuildTarget values with their group and whether build support is installed.

No parameters.

**Returns:** `object` (list of `BuildTargetInfo`: `name`, `displayName`, `targetGroup`, `isInstalled`), sorted by group then name. Obsolete and sentinel targets are excluded.
**Notes:** `MainThreadRequired = true`.

### `get_build_settings`
Read the current build configuration from EditorUserBuildSettings / EditorBuildSettings.

No parameters.

**Returns:** `object` (`BuildSettingsResult`).

`BuildSettingsResult` fields:

| Field | Description |
|-------|-------------|
| `activeBuildTarget` | Active BuildTarget name. |
| `activeBuildTargetGroup` | Active BuildTargetGroup name. |
| `developmentBuild` | Whether a Development Player is configured. |
| `allowDebugging` | Whether script debugging is allowed. |
| `connectWithProfiler` | Whether the Profiler auto-connects. |
| `buildScriptsOnly` | Whether only scripts are built (skip data). |
| `symlinkSources` | Whether runtime/plugin sources are symlinked. |
| `il2CppCodeGeneration` | `OptimizeSpeed` \| `OptimizeSize` for the active target. |
| `scenes` | Build Settings scene list (each: `path`, `guid`, `enabled`). |

**Notes:** `MainThreadRequired = true`.

### `set_build_settings`
Set mutable EditorUserBuildSettings fields. Does NOT manage scenes (use add_scene_to_build / remove_scene_from_build) or switch target (use switch_build_target). Use dry_run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged. |
| `confirm` | no | `false` | Apply the changes. Without it the call is refused. |
| `dry_run` | no | `false` | Preview the change without applying it. |

`settings` fields (a structured input DTO; every field is optional — only supplied fields change):

| Field | Description |
|-------|-------------|
| `developmentBuild` | Build a Development Player (enables the debugger/profiler). |
| `allowDebugging` | Allow script debugging (only effective with developmentBuild=true). |
| `connectWithProfiler` | Auto-connect the Profiler (only effective with developmentBuild=true). |
| `buildScriptsOnly` | Build only the scripts (skip data) for faster iteration. |
| `symlinkSources` | Symlink runtime/plugin sources instead of copying (where supported). |
| `il2CppCodeGeneration` | IL2CPP code generation for the active target: OptimizeSpeed \| OptimizeSize. |

**Returns:** `object` (`SetBuildSettingsResult`: `success`, `dryRun`, `applied` map of fields that changed, `skipped` map of supplied fields that already matched, `message`).
**Notes:** `MainThreadRequired = true`. Mutating: refused unless `confirm=true` (or `dry_run=true` to preview). Fields already at the requested value are reported under `skipped` rather than re-applied. Fails if no `settings` object is provided.

### `list_build_profiles`
List Build Profile assets in the project (Unity 6 only). Returns feature_unavailable on earlier versions.

No parameters.

**Returns:** `object` (list of `BuildProfileInfo`: `name`, `guid`, `platform`, `isActive`), or `{ error, code = "feature_unavailable" }` on editors older than Unity 6.
**Notes:** `MainThreadRequired = true`.

### `recompile`
Force a script recompile (works while unfocused/minimized). Poll recompile_status for completion.

No parameters.

**Returns:** `object`
**Notes:** `MainThreadRequired = true`. Async — poll `recompile_status` until status is `completed` or `up_to_date`. A successful compile triggers a domain reload, so the triggering request cannot stay open.

### `recompile_status`
Get the status of the last recompile: idle | triggered | compiling | completed | up_to_date.

No parameters.

**Returns:** `string`
**Notes:** `MainThreadRequired = false`.

### `list_tests`
List all available tests (EditMode and/or PlayMode) without running them.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `mode` | no | `all` | Test mode: all, editor, playmode (default: all) |

**Returns:** `TestListResponse`
**Notes:** `MainThreadRequired = true`.

### `run_tests`
Execute Unity tests with filtering options.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `mode` | no | `all` | Test mode: all, editor, playmode (default: all) |
| `filter` | no | `–` | Test name filter pattern (case-insensitive partial match) |
| `filter_type` | no | `testName` | Filter type: testName, assembly, category (default: testName) |
| `include_explicit` | no | `false` | Include tests marked with [Explicit] attribute |
| `async_tests` | no | `false` | Run asynchronously - return immediately, poll /test-status for results |
| `timeout` | no | `300` | Test execution timeout in seconds (default: 300) |

**Returns:** `TestExecutionResponse`
**Notes:** `MainThreadRequired = true`. Synchronous by default; with `async_tests=true` it returns immediately — poll `test_status` for results.

### `test_status`
Get status of running async test execution.

No parameters.

**Returns:** `string`
**Notes:** `MainThreadRequired = false`.

### `cancel_tests`
Cancel running test execution.

No parameters.

**Returns:** `object`
**Notes:** `MainThreadRequired = true`.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
