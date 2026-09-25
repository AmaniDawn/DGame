# com.unity.pipeline

Unity package for **remote-controlling a running Unity Editor (or dev Player) over HTTP**.
A client — CLI, CI, or an agent — connects and executes registered commands: recompile,
run tests, eval C#, hot-reload files, play-mode control, status/heartbeat.

## Layout (standard UPM package)

| Folder | Assembly | Notes |
|--------|----------|-------|
| `Runtime/` | `Unity.Pipeline` | Ships in player builds. HTTP server base, command registry, models, runtime commands, hot reload, player server. |
| `Editor/` | `Unity.Pipeline.Editor` | Editor-only. Live server + its owner, settings asset, editor commands, test runner. |
| `CodeGen/` | `Unity.Pipeline.CodeGen` | ILPostProcessor (Mono.Cecil) for in-place hot reload. Root-level by Unity convention (cf. Burst/Entities) — leave as-is. |
| `Tests/Editor/` | `Unity.Pipeline.Tests.Editor` | **EditMode** suite (the main one). |
| `Tests/Runtime/` | `Unity.Pipeline.Tests.Runtime` | **PlayMode** suite. |
| `Samples/` | — | Hot-reload sample scenes. |

## Architecture entry points

- **`Runtime/Common/BasePipelineServer.cs`** — shared HTTP listener + routing (`/api/exec`,
  `/api/status`, `/api/commands`, …). Subclassed by `Editor/EditorPipelineServer.cs` and
  `Runtime/PlayerSupport/RuntimePipelineServer.cs`.
- **`Editor/EditorPipelineStartup.cs`** (`PipelineServerStartup`) — `[InitializeOnLoad]` static
  owner of the live editor server; survives domain reloads; auto-enables autotick.
- **`Editor/EditorPipelineManager.cs`** — inspectable settings asset (`Pipeline/Settings...` menu).
- Commands are discovered via `[CliCommand]` attributes. Editor command groups live under
  `Editor/Commands/` (Assets, Scenes, GameObjects, Prefabs, Scripts, Build, PackageManager,
  ProjectSettings, …); runtime commands under `Runtime/`.
- **`Editor/Authoring/`** — helpers shared by state-changing commands: `AuthoringUndoScope`
  (collapses a command's `Undo`-registered mutations into one step) and `ProjectPaths`/`ObjectResolver`
  (authoring-root path resolution + object handles).
  Destructive/overwriting commands gate on `confirm`/`dry_run` inline (see `delete_asset`). Structured
  multi-field command args implement `IStructuredCommandInput` (`Runtime/Common/`). See
  `Documentation~/safety-and-mutations.md`, `Documentation~/authoring-commands.md`, and
  `Documentation~/creating-commands.md`.

## Driving & verifying (agents)

Use the **`unity-pipeline` skill**, which drives the live editor via the `unity` CLI.
Canonical verb is `command`; the auth token is the `evalToken` field inside the port file
`<liveProject>/Library/Pipeline/.unity-pipeline-port`, sent as `Authorization: Bearer <token>`.

Edit→verify loop: make a logical change (may span several files) → `command recompile` →
poll `command recompile_status` → `command run_tests --filter <TestClass>`. The server keeps the
editor ticking while unfocused, so compiles proceed even when focus is elsewhere.

## Conventions

- Private fields (including static): `m_PascalCase`. Consts: `PascalCase`.
- Don't `git commit`/`push` without an explicit request.
