# Build, compilation & test commands

These commands drive the build/compile/test dev loop, plus build configuration: triggering Player builds and reading the resulting BuildReport, switching the active build target, enumerating targets, reading/writing EditorUserBuildSettings, and listing, creating and editing Build Profiles. Several commands are asynchronous: trigger the operation, then poll the matching status command (`build status`, `build.target switch_status`, `script recompile_status`, `test status`) until it reports completion. The client must tolerate connection errors during a domain reload (recompile, target switch) or a long blocking build.

### `build run`
Trigger an async Player build and report the full BuildReport. Returns immediately (queued); poll `build status` until status is 'completed'. DetailedBuildReport is included by default unless 'options' is supplied. Use dry-run to validate without building.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | no | `–` | BuildTarget name (e.g. StandaloneWindows64). Defaults to the active target. Must be installed. |
| `output-path` | no | `–` | Output path (absolute, or relative to the project root). Defaults to the last/auto path. |
| `profile-name` | no | `–` | Build Profile to activate and build with (Unity 6 only). Must be on the active build target; without scenes, its scene list is built. |
| `options` | no | `–` | BuildOptions names. Omit to get just DetailedBuildReport; supplying any disables that default. |
| `scenes` | no | `–` | Scene asset paths to build (e.g. Assets/Scenes/Main.unity). Defaults to the profile's scene list, or EditorBuildSettings. |
| `confirm` | no | `false` | Acknowledge and run the build; without it the call is refused. Use `dry-run` to validate only. |
| `dry-run` | no | `false` | Validate target/output-path/profile-name/scenes without building; reports the target and scenes the build would use. |

Supported `options` values (case-insensitive): `Development`, `AllowDebugging`, `ConnectWithProfiler`, `EnableHeadlessMode`, `SymlinkSources`, `BuildAdditionalStreamedScenes`, `CleanBuildCache`, `DetailedBuildReport`. Any unrecognized value is a validation error.

**Returns:** `object`: `{ "status": "queued", "buildId" }`, or `{ "status": "dry_run", "valid": true, "target", "scenes" }` for a dry run that passed validation, with the target and scenes the build would use. The finished report is read via `build status`.
**Notes:** `MainThreadRequired = false`. Async — validates + queues, returns `queued` immediately, then an editor tick runs the (blocking) build; poll `build status` until status is `completed`. Only one build at a time: another build queued or running exits `Failed`. Mutating: gated by the `confirm`/`dry-run` convention — pass `confirm=true` for a real build, or `dry-run=true` to validate target/outputPath/scenes without building (nothing is queued on a dry run or on validation failure). An invalid configuration (dry run or not) and a missing `confirm` exit `InvalidUsage`, with every validation error, each prefixed by its field (`target`, `options`, `scenes`, `output-path`), on stderr. With `profile-name`, the profile is resolved when the command runs, not when the build starts: without `scenes`, the build uses the profile's scene list (its own list when `override-global-scenes` is on, otherwise the global one), even if the profile is not active yet. A missing profile, `profile-name` on an editor without Build Profiles, a `target` that differs from the profile's platform, and a profile on another build target or standalone subtarget are validation errors: activating such a profile would switch the target inside the build. For another build target, run `build.target switch` first; for another standalone subtarget (Player or Server), switch it from the Build Profiles window, since `build.target switch` takes no subtarget. On a standalone target, a profile that is not active is also refused when the editor cannot read whether it is a Player or a Server profile; activate it from the Build Profiles window first. If the profile cannot be activated when the build starts, the build fails instead of using the current settings.

### `build status`
Status of the current/most recent build: idle | queued | building | completed, with the full BuildReport (files, packedAssets, buildSteps, errors, warnings) once completed. Retained until the next build.

No parameters.

**Returns:** `object` (the status file, so the command succeeds even when the build it reports failed). `idle` when no build has run; `queued`/`building` (the latter with live `elapsedMs`) while in progress; a full `BuildReportResult` once `completed`.

`BuildReportResult` fields (returned when `status` is `completed`):

| Field | Description |
|-------|-------------|
| `status` | Always `completed` here. |
| `buildId` | Id returned by the triggering `build run` call. |
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

### `build.target switch`
Switch the active build target (destructive, long-running: triggers a full reimport + domain reload). Requires confirm=true. Returns immediately; poll `build.target switch_status`.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | yes | `–` | BuildTarget name to switch to (must be installed; see `build.target list`). |
| `confirm` | no | `false` | Apply the switch. Without it the call is refused. |

**Returns:** `object`: `{ "status": "switching", "fromTarget", "toTarget" }`, or `{ "status": "completed", "activeBuildTarget" }` immediately if already on the requested target.
**Notes:** `MainThreadRequired = false`. Async — validates + queues, returns `switching` immediately, then an editor tick performs the (blocking) switch; poll `build.target switch_status` until `completed`. A missing or unknown `target` and a missing `confirm` exit `InvalidUsage`; a target whose build support isn't installed, or a switch already queued or in progress, exits `Failed`. Errors are on stderr. The status file survives the domain reload the switch causes, and is reconciled against the active target on the next load.

### `build.target switch_status`
Status of the last target switch: idle | switching | completed (with success + activeBuildTarget).

No parameters.

**Returns:** `object` (the status file, so the command succeeds even when the switch it reports failed). `idle`, `switching`, or `completed` (with `success` and `activeBuildTarget`, or `errors` on failure).
**Notes:** `MainThreadRequired = false`. Reads a Temp status file off-thread, so it keeps answering while the switch holds the main thread.

### `build.target list`
List the known BuildTarget values with their group and whether build support is installed.

No parameters.

**Returns:** one record per target (`BuildTargetInfo`: `name`, `displayName`, `targetGroup`, `isInstalled`), sorted by group then name. Obsolete and sentinel targets are excluded.
**Notes:** `MainThreadRequired = true`.

### `build get_settings`
Read the current build configuration from EditorUserBuildSettings / EditorBuildSettings.

No parameters.

**Returns:** `object` (`BuildSettingsResult`).

`BuildSettingsResult` fields:

| Field | Description |
|-------|-------------|
| `active-build-target` | Active BuildTarget name. |
| `active-build-target-group` | Active BuildTargetGroup name. |
| `development-build` | Whether a Development Player is configured. |
| `allow-debugging` | Whether script debugging is allowed. |
| `connect-with-profiler` | Whether the Profiler auto-connects. |
| `build-scripts-only` | Whether only scripts are built (skip data). |
| `symlink-sources` | Whether runtime/plugin sources are symlinked. |
| `il2cpp-code-generation` | `OptimizeSpeed` \| `OptimizeSize` for the active target. |
| `scenes` | Build Settings scene list (each: `path`, `guid`, `enabled`). |

**Notes:** `MainThreadRequired = true`.

### `build set_settings`
Set mutable EditorUserBuildSettings fields. Does NOT manage scenes (use `scene add_to_build` / `scene remove_from_build`) or switch target (use `build.target switch`). Use dry-run to preview.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `settings` | no | `–` | Fields to change; omitted fields are left unchanged. |
| `confirm` | no | `false` | Apply the changes. Without it the call is refused. |
| `dry-run` | no | `false` | Preview the change without applying it. |

`settings` fields (a structured input DTO; every field is optional — only supplied fields change):

| Field | Description |
|-------|-------------|
| `development-build` | Build a Development Player (enables the debugger/profiler). |
| `allow-debugging` | Allow script debugging (only effective with development-build=true). |
| `connect-with-profiler` | Auto-connect the Profiler (only effective with development-build=true). |
| `build-scripts-only` | Build only the scripts (skip data) for faster iteration. |
| `symlink-sources` | Symlink runtime/plugin sources instead of copying (where supported). |
| `il2cpp-code-generation` | IL2CPP code generation for the active target: OptimizeSpeed \| OptimizeSize. |

**Returns:** `object` (`SetBuildSettingsResult`: `dryRun`, `applied` map of fields that changed, `skipped` map of supplied fields that already matched). A count of the changed and unchanged fields is logged at the `info` level.
**Notes:** `MainThreadRequired = true`. Mutating: refused unless `confirm=true` (or `dry-run=true` to preview). Fields already at the requested value are reported under `skipped` rather than re-applied. No `settings` object, a missing `confirm` and an invalid `il2cpp-code-generation` exit `InvalidUsage` with the reason on stderr, and change nothing.

### `build list_profiles`
List Build Profile assets in the project (Unity 6 only). Fails on earlier versions.

No parameters.

**Returns:** one record per profile (`BuildProfileInfo`: `name`, `guid`, `platform`, `isActive`). Exits `Failed` on editors older than Unity 6.
**Notes:** `MainThreadRequired = true`.

### `build list_platforms`
List the platforms a Build Profile can target, with their platform id (Unity 6 only). Use the `displayName` or `id` with `build create_profile`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `show-all` | `bool` | Include platforms whose build support module is not installed. Defaults to `false`. |

**Returns:** one record per platform (`BuildPlatformInfo`: `displayName`, `id`, `buildTarget`, `subtarget`, `isInstalled`, `isDerived`). Exits `Failed`, with the reason on stderr, on editors older than Unity 6.
**Notes:** `MainThreadRequired = true`. By default only platforms with build support installed are listed, since those are the only ones `build create_profile` will accept; pass `show-all=true` to see the rest. A Build Profile platform is not the same thing as a `BuildTarget` — Meta Quest, Android XR and Android all report `buildTarget: "Android"`, with the first two flagged `isDerived`. Use `id` to select one unambiguously.

### `build create_profile`
Create a Build Profile asset for a platform (Unity 6 only). Call `build list_platforms` first to get a platform id.

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | `string` | **Required.** Profile name (e.g. `"Meta Quest"`). A Build Profile is named after its file, so this is also the file name and the name `build run --profile-name` matches. |
| `platform` | `string` | **Required.** Platform id, display name (`"Meta Quest"`) or `BuildTarget` name (`"Android"`). |
| `subtarget` | `string` | Standalone only: `Player` \| `Server`. Disambiguates desktop platforms; ignored for platforms that have no Player/Server split. |
| `folder` | `string` | Folder to create the profile in. Defaults to `Assets/Settings/Build Profiles`. The file is always `<folder>/<name>.asset`. |
| `activate` | `bool` | Make the new profile the active Build Profile after creating it. Defaults to `false`. Only allowed when the profile's platform uses the editor's active build target: activating another target would switch it, so switch first with `build.target switch`. |
| `confirm` | `bool` | Required (true) only when overwriting an existing profile of the same name in the folder. |
| `dry-run` | `bool` | Validate the name, platform and folder and report what would be created, without writing anything. |

**Returns:** `object` (`CreateBuildProfileResult`: `dryRun`, `assetPath`, `platform`, `profile` (omitted on a dry run), `activated`). Exits `Failed` on editors older than Unity 6; a refused argument exits `InvalidUsage`. What was created (or would be, on a dry run) is logged at the `info` level.
**Notes:** `MainThreadRequired = true`. `platform` is resolved as id, then exact display name, then `BuildTarget` name — the last of which only matches a non-derived platform, so `"Android"` never selects Meta Quest. Refuses a platform whose build support module is not installed, since the resulting profile would be unusable. A `name` the command cannot use is refused rather than rewritten: one containing `/` or `\` (on every host, since either would put the profile in a subfolder), one containing a character this host cannot put in a file name, and a location Unity does not import (a name starting with a dot, or a folder inside the project starting with a dot or ending in `~`). Whether a name also works on the team's other operating systems — Windows reserved names such as `CON`, or characters only Windows refuses — is the caller's choice, as it is in the Build Profiles window. A name already used by a profile in another folder is refused, because `build --profile-name` matches by name. With `confirm=true`, an existing profile is overwritten in place: its settings are replaced but it keeps its GUID, so references to it — including the active-profile setting — stay valid. Overwriting the active profile with a different platform is refused, since the in-place copy would skip the build-target switch; a non-profile asset at the path is never overwritten, nor is a file there that Unity cannot load (corrupt, or not imported yet) — it is refused rather than created around under another name. On a Unity 6 editor whose internal Build Profile API has changed, `build list_platforms` and `build create_profile` exit `Failed` naming the missing members, rather than an empty or wrong platform list. `activate=true` for a profile on another build target (or another standalone subtarget) is refused, because activating it would perform a full reimport and domain reload inside the create; switch with `build.target switch` first (confirm-gated, polled with `build.target switch_status`), then create with `activate=true`. A derived platform on the active target, such as Meta Quest on an Android project, activates without a switch. If activation itself fails after the profile is written, the command still succeeds and reports `activated: false`, with the reason as a warning on stderr. Missing parent folders are created. To build with the new profile without activating it, pass `build --profile-name`.

### `build set_profile`
Edit an existing Build Profile's scene list and scripting defines (Unity 6 only). Every setting is optional; only the ones supplied change. The platform cannot be changed here: recreate the profile with `build create_profile` and `confirm=true`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | `string` | **Required.** Name of the profile to edit (see `build list_profiles`). |
| `folder` | `string` | Folder holding the profile. Only needed when several profiles share the name. |
| `override-global-scenes` | `bool?` | Build with the profile's own scene list instead of the global Build Settings list. Omit to leave unchanged. |
| `scenes` | `string[]` | Scene asset paths in build order, replacing the profile's scene list; all are enabled. An empty list clears it. Omit to leave unchanged. |
| `scripting-defines` | `string[]` | Scripting define symbols replacing the profile's own. An empty list clears them. Omit to leave unchanged. |
| `confirm` | `bool` | Accepted for parity; not required, since the edit is undoable. |
| `dry-run` | `bool` | Validate the changes and report what would change, without writing anything. |

From the CLI, pass lists as JSON: `unity command build set_profile --name "Meta Quest" --scripting-defines '["QUEST","DEBUG_OVERLAY"]' --override-global-scenes true`.

**Returns:** `object` (`EditBuildProfileResult`: `dryRun`, `assetPath`, `profile`, `applied` map of settings that changed, `skipped` map of supplied settings that already matched). Exits `Failed` on editors older than Unity 6; a refused argument exits `InvalidUsage`.
**Notes:** `MainThreadRequired = true`. Every supplied value is validated before anything is written, so one bad value leaves the profile untouched: scene paths must be scene assets anywhere in the project, `Packages/` included, like `build --scenes` — a scene in the list is only referenced, so the authoring root does not confine it (bare paths still resolve against the root, and the `.unity` extension is optional) — and a rejected path names its reason, and defines must be valid C# preprocessor symbols. A name shared by several profiles is refused until `folder` picks one. The profile itself is what the command writes, so it must lie under the authoring root, however it was found. The edit is saved to the asset and reverts as a single Undo step; undo and redo are saved too — including an undo of a `batch` that edited several profiles in one step — so the file on disk always matches what the editor shows. In a transactional `batch` the command is refused, since its disk write is outside Undo. A warning on stderr points out a scene list that builds will ignore because `override-global-scenes` is off, and defines changed on the active profile, which alter script compilation. Platform settings and the player-settings override are not editable: Unity keeps them internal.

### `script recompile`
Force a script recompile (works while unfocused/minimized). Poll `script recompile_status` for completion.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `focus` | no | `false` | Bring the Editor to the foreground before compiling. |

**Returns:** `{ status }` — `compiling` (a compile started), `up_to_date` (nothing needed recompiling) or `failed`. What happened is logged at the `info` level.
**Notes:** `MainThreadRequired = true`. Async — poll `script recompile_status` until status is `completed` or `up_to_date`. A successful compile triggers a domain reload, so the triggering request cannot stay open. When nothing needed recompiling but scripts still have compile errors, it writes `{ "status": "failed" }` and exits `Failed` (6), pointing to `console get` on stderr.

### `script recompile_status`
Get the status of the last recompile: idle | triggered | compiling | completed | up_to_date.

No parameters.

**Returns:** `{ status, failed, errors, compilationFailed }`. It exits `Success` whatever the status: read `failed` and `errors` for the outcome of the compile.
**Notes:** `MainThreadRequired = false`.

### `test list`
List all available tests (EditMode and/or PlayMode) without running them.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `mode` | no | `all` | Test mode: all, editor, playmode (default: all) |

**Returns:** one record per test: `{ FullName, Mode, Assembly, Categories, Explicit }`. How many were found is logged at the `info` level. An unknown `mode` exits `InvalidUsage` (2).
**Notes:** `MainThreadRequired = true`.

### `test run`
Execute Unity tests with filtering options.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `mode` | no | `editor` | Test mode: `editor`, `playmode` or `all`. `playmode` and `all` need `async-tests`. |
| `filter` | no | `–` | Test name filter pattern (case-insensitive partial match) |
| `filter-type` | no | `TestName` | One of `TestName`, `Assembly`, `Category` (case-insensitive). Any other value is rejected. |
| `include-explicit` | no | `false` | Include tests marked with [Explicit] attribute |
| `async-tests` | no | `false` | Start the run and return at once with no results; poll `test status` for them |
| `timeout` | no | `600` | Test execution timeout in seconds |

```bash
unity command test run --mode editor --filter-type category --filter Smoke
```

A run succeeds only when at least one test ran and none failed. Skipped and inconclusive tests don't fail it.

| Exit | Outcome |
|------|---------|
| 0 | All tests that ran passed |
| 2 | Invalid arguments, including a synchronous `mode playmode` or `mode all`, or no tests matched the filter |
| 6 | Tests failed (the message lists them), or the run errored |
| 14 | The run timed out (6 over HTTP) |

**Returns:** one record per test that ran: `{ fullName, status, duration, message, stackTrace }`, written on failure too. The summary (passed, failed, skipped, duration) is logged at the `info` level; why a run failed, including the failed tests' names, is logged as an error. An async run writes no records and exits 0 once the run has started: it logs that it started, and `test status` has the results.
**Notes:** `MainThreadRequired = true`. Synchronous by default: it returns once the run ends, with every result. With `async-tests=true` it returns as soon as the run starts — poll `test status` for results. Entering play mode reloads the domain, which drops the request, so `mode playmode` and `mode all` run only with `async-tests=true`. An async `mode all` run is one run: the EditMode tests, then the PlayMode tests, even when EditMode tests failed. `test status` reports it running until both are done, then reports their combined summary and results. A test that recompiles scripts or reloads the domain drops a synchronous run's request, so its results are lost and `test status` reports the run as interrupted; run such tests with `async-tests=true`.

### `test status`
Get status of running async test execution.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `detail` | no | `full` | full (default) for every test's result; failures for only the failed tests' results; summary for the counts and no per-test results. |

**Returns:** the run's status payload, or `{"status":"no_tests"}` when no run is in progress. It is written whatever the exit code; why a finished run failed is logged as an error.
**Notes:** `MainThreadRequired = false`. Immediate: it answers at once while another command runs. A poll for a finished run fails with the `test run` exit codes when tests failed or none matched; an errored run exits 6, and a cancelled run exits 130. `detail` trims only the finished run's `results` list, whether the run passed or failed; `summary` is kept at every level. Pass `--detail failures` to read the failed tests' messages and stack traces without every other result, or `--detail summary` to keep polling a large suite cheap. With the default `full`, a failed poll of a large suite returns every result. Any other value is refused.

### `test cancel`
Cancel running test execution.

No parameters.

**Returns:** `{ status }` — `cancelled`, or `no_tests` when no run was in progress (which still succeeds).
**Notes:** `MainThreadRequired = true`. Stops the run in the Test Framework, including one a domain reload interrupted; the remaining tests don't run. Starting a new `test run`, or a synchronous run timing out, stops the previous run the same way.

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
