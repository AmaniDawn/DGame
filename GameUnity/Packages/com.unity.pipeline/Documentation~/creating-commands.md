# Creating commands

A *command* is a `static` method that the Pipeline server can invoke over HTTP. This page covers the command-authoring API: how to declare a command, describe its parameters, write its result, and have it discovered automatically.

## The command method

A command is a static method marked with `[CliGroupCommand]`, which sets its name and the description shown in help. Each parameter marked with `[CliArg]` becomes a flag, such as `--path`, parsed into the parameter's type before the method runs. The method takes a `CommandContext`, which carries everything about its invocation, and returns its exit code:

```csharp
[CliGroupCommand(Group, "Print a greeting.", Effect = CommandEffect.ReadOnly)]
internal static ExitCode Greet(
    CommandContext cmd,
    [CliArg("name", "Who to greet")] string name = "world")
{
    cmd.Result.Write($"Hello {name}!");
    return ExitCode.Success;
}
```

- Return `ExitCode` when the command finishes within a frame. Return `Task<ExitCode>` when it runs across frames (see [Commands that run across frames](#commands-that-run-across-frames)).
- Taking a `CommandContext` is optional, but a command that writes a result or logs takes one. Put it first.
- Keep command methods `internal`. Any accessibility registers, but a command is called by the pipeline, not by other code.

`CommandContext` has:

| Member | What it is |
|--------|------------|
| `Result` | Writes the command's result in the format the user picked: `Write(obj)` for a single result, `WriteRecord(obj)` per item for a list. See [Writing output](#writing-output). |
| `Log` | `Error`, `Warning`, `Info`, `Debug`: messages to stderr, filtered by the user's `--log-level`. See [Logging](#logging). |
| `Std` | The command's `Out` and `Error` streams, for output that isn't a result. |
| `Arguments` | The arguments the command was called with, without the pipeline's own options. |
| `CancellationToken` | Cancelled when the user stops the command: Ctrl-C, SIGTERM or its timeout. See [Handling cancellation](#handling-cancellation). |

A method that returns anything else uses the [legacy signature](#the-legacy-signature), which is still supported.

## Declaring a command

Declare the command's group as a `const` in the class that holds the commands, and tag a `static` method with `[CliGroupCommand]`:

```csharp
const string Group = "<group>";

[CliGroupCommand(Group, "description", MainThreadRequired = true, Effect = CommandEffect.ReadOnly)]
internal static ExitCode <Verb>(CommandContext cmd, ...);
```

| Argument | Meaning |
|----------|---------|
| `group` | The group the command belongs to: one or more `.`-separated segments, e.g. `asset` or `animation.animator`. The client runs the command as `unity command <group> <name>`. |
| `description` | Human-readable text shown in help and the `/api/commands` listing. |
| `Name` | Optional. The command's name within its group. **Default: derived from the method name** (see [Groups and names](#groups-and-names)). Set it only when the derived name is wrong. |
| `MainThreadRequired` | Whether the handler must run on Unity's main thread. **Default `true`.** |
| `Tags` | Optional hierarchical tags for browsing commands across groups. Path-style: a `/` separates tag from subtag (e.g. `"assets"`, `"assets/import"`). **Default: empty.** |
| `Effect` | What running the command does (see [Declaring the effect](#declaring-the-effect)). **Default: `CommandEffect.Unspecified`.** |

Every discovered command also carries the **package** it originates from (derived from the declaring assembly, e.g. `Unity.Pipeline.Editor`). The command (its full name), group, tags and package appear in both the compact and full `/api/commands` listings — `detail=full` (the default) includes the parameters and generated JSON schema, while `detail=compact` returns just the lightweight index (`command`, `name`, `group`, `description`, `tags`, `package`). `detail=groups` and `detail=tags` return the group or tag index alone, which is how a client offers discovery without holding a catalog of its own. Groups and tags also drive the endpoint's `group` and `tag` filters and its `group_by=group` and `group_by=tag` trees (see [Connectivity](connectivity.md)).

The method **must be `static`**. Its accessibility doesn't affect registration: `public`, `internal`, and `private` static methods can all be registered, and the package keeps its own `internal`. `CommandRegistry` invokes handlers through reflection, so a `private` handler runs exactly like a `public` one. Only a non-static (instance) method fails to register — `CommandRegistry` skips it and logs a warning. To expose an instance method as a command, register it dynamically instead (see [Registering a command dynamically](#registering-a-command-dynamically)).

### Declaring the effect

`Effect` tells a client what a call does, so it can run a read without asking and ask before a change it cannot take back. An MCP client shows it as the tool's annotations. The `/api/commands` listing serves it as three booleans on the command's entry:

| `Effect` | `readOnly` | `destructive` | `idempotent` | Use for |
|----------|-----------|---------------|--------------|---------|
| `ReadOnly` | `true` | `false` | `true` | Commands that change nothing: `get`, `list`, `find`, `status`. |
| `Idempotent` | `false` | `false` | `true` | Changes that a repeat leaves as they are and that lose nothing: most `set` commands. |
| `Additive` | `false` | `false` | `false` | Changes that a repeat does not leave as they are, and that lose nothing: `create`, `add`, `run`, and a rename or reparent by a path the first call changed. |
| `Destructive` | `false` | `true` | `false` | Changes that can delete or overwrite work, and commands that run code the caller supplies. |
| `Unspecified` | — | — | — | Nothing is served; the client decides on its own. |

Pick the strongest effect any call can have. A command that takes `confirm`, even for an occasional overwrite, is `Destructive`.

### Which assembly to reference

`[CliGroupCommand]`, `[CliArg]`, `CommandContext` and `ExitCode` live in `Unity.Pipeline.Attributes`,
which carries no define constraints. An assembly definition declaring commands needs only that
reference — not `Unity.Pipeline` — so it compiles in every build, including a non-development Player
where the pipeline assemblies are absent and the commands are simply inert. Scripts that are not in
an assembly definition need no reference at all.

Referencing `Unity.Pipeline` is still required for the rest of the command API — response models,
`IStructuredCommandInput` and `CommandRegistry` — and that assembly only compiles for the editor and
for the builds selected in Project Settings → Pipeline → Runtime. A runtime command that uses them
therefore only compiles in those builds: put it in an editor-only assembly, or guard it with the
same defines.

### Shipping commands from another package

Your package can ship commands without depending on this one. Put them in an assembly definition that
compiles only when this package is installed: a `versionDefines` entry on `com.unity.pipeline` sets a
symbol that the assembly's `defineConstraints` require. Make it an editor assembly, or a runtime one if
the commands should also run in a development Player.

Set the expression to the earliest version the commands support, written exactly as published. A
prerelease sorts below its release, so `0.9.0` excludes `0.9.0-exp.1`.

Nothing limits the newest version, and this package's command API can still change, so a later release
can break the assembly. Your package's own projects and CI usually don't install this package, so the
assembly never compiles there. Keep the commands thin, put their logic in an assembly your tests
compile, and add a CI job that installs this package. Declare the group explicitly, with
`[CliGroupCommand]` or `[CliCommand(group, name, description)]`, rather than letting it come from
your namespace. For the group to use, see [Groups in the package](#groups-in-the-package).

### Declaring a command with `[CliCommand]`

`[CliCommand(group, name, description)]` declares a command with an explicit group and name, and takes the same `MainThreadRequired` and `Tags` as `[CliGroupCommand]`:

```csharp
[CliCommand("lighting", "bake", "Bake every lightmap")]
internal static ExitCode Bake(CommandContext cmd) { ... }
```

```bash
unity command lighting bake
```

`[CliCommand(name, description)]` takes the group and name as one dotted name: the text before the last `.` is the group, the rest is the name.

```csharp
[CliCommand("animation.skeleton.set_bone", "Set a bone")]
internal static ExitCode SetBone(CommandContext cmd) { ... }
```

```bash
unity command animation.skeleton set_bone
```

Without a `.`, the command is in the `global` group and runs by its name alone or as `global <name>`:

```csharp
[CliCommand("bake_lighting", "Bake every lightmap")]
internal static ExitCode Bake(CommandContext cmd) { ... }
```

```bash
unity command bake_lighting        # or: unity command global bake_lighting
```

`[CliCommand(description)]` infers both: the group is the declaring type's namespace and class, lowercased, and the name is derived from the method (see [Groups and names](#groups-and-names)). A nested class is joined to its outer class with a `.`.

```csharp
namespace My.Tools
{
    static class Lighting
    {
        [CliCommand("Bake every lightmap")]
        internal static ExitCode Bake(CommandContext cmd) { ... }
    }
}
```

```bash
unity command my.tools.lighting bake
```

A method cannot carry both `[CliCommand]` and `[CliGroupCommand]`.

### Groups and names

A command has a **group** and a **name**. On the command line the two are separate words (`unity command asset delete`, `unity command animation.animator add_state`); a group is one word, dots included. Replies, errors, help text and logs all name a command that way, as `group name`: the `command` field of a response is `"asset delete"`, and a batch operation writes `"command": "asset delete"`.

The **full name**, `group name` (`asset delete`), is the one name of a command: `CommandInfo.FullName`. The registry, the batch policy lists and the `CommandNames` constants all use it. When code names a command, in a lookup, a comparison or in text a user will read (a description, a hint such as "run X first", an error, a log line), use the `CommandNames` constant.

The name comes from the method name: it is split at each capital, joined with `_` and lowercased. An acronym counts as one word, and digits stay with the word before them.

| Method | Command name |
|--------|--------------|
| `CreateController` | `create_controller` |
| `GetUIElement` | `get_ui_element` |
| `Vector3Field` | `vector3_field` |

So name the method after the command, verb first, and reuse the package's verbs (`create`, `get`, `set`, `list`, `status`, …) where they fit. Renaming a method renames its command, which breaks every client that calls it. Use `Name =` when the derived name is wrong.

Declare the group as a `const string Group` beside the commands that use it. There is no central list of groups: any code may add commands to an existing group or start a new one.

The registry checks the same rules for every command, whether it is shipped, third-party or registered dynamically. An attribute-declared command that breaks one is not registered, and an error naming it is logged; a dynamic registration throws instead (see [Registering a command dynamically](#registering-a-command-dynamically)):

- The group is one or more non-empty `.`-separated segments, with no whitespace.
- The name is not empty and has no whitespace and no `.`.
- The group and name are unique together. When several methods declare the same group and name, none of them is registered and the error names each method. The one exception is a collision with a command of this package: when exactly one of the declarations is in one of the package's own assemblies, that one is kept and the others are refused, so a project cannot switch off a shipped command by declaring its group and name. The error names the kept method and each refused one.

Nothing else is enforced at registration. Lowercase snake_case command names, kebab-case parameter names (see [Describing parameters](#describing-parameters)), the verb vocabulary and splitting a busy group into subgroups (`animation.animator`) are the package's own conventions.

### Groups in the package

The package's own commands use these groups. A user command may use any of them or a new group of its own; it only has to pass the checks above.

| Group | Covers | Subgroups |
|-------|--------|-----------|
| `animation` | Animation authoring | `animator`, `clip`, `timeline` |
| `asset` | Asset CRUD, search, import settings, text files | — |
| `authoring` | Authoring-root configuration | — |
| `build` | Player builds, build settings and profiles | `target` |
| `capture` | Screenshots and view/element capture | — |
| `component` | Components on GameObjects, serialized fields | — |
| `console` | Console log reads, writes and counters | — |
| `diagnostics` | Project audit, performance stats, eval usage report | — |
| `editor` | Editor application control, menus, search, saving | `playmode` |
| `gameobject` | Scene GameObject manipulation and selection | — |
| `job` | Transactional batches and server-side waits | — |
| `lighting` | Lighting bakes and settings | — |
| `material` | Materials and shaders | — |
| `navmesh` | NavMesh bakes and settings | — |
| `occlusion` | Occlusion culling bakes | — |
| `package` | UPM package management | — |
| `pipeline` | The server's own state: command progress and the command list | — |
| `prefab` | Prefab workflows | — |
| `runtime` | Player-side application control and input simulation | — |
| `scene` | Scene lifecycle, hierarchy and build list | — |
| `script` | C# source workflows: create, compile, eval, run | `methodreload` |
| `settings` | Project settings | `audio`, `graphics`, `input`, `physics`, `player`, `quality`, `runtime_pipeline`, `tags_layers`, `time` |
| `test` | Test runner | — |

The package's own test fixtures use a `fixture` group. Commands declared by the test assemblies are hidden from `/api/commands` unless the request passes `include_tests=true`; they stay executable.

This package declares commands only in the groups listed above, so other groups are free for other packages. A package that ships its own commands uses its own group, or a subgroup of a root it shares with related packages. Under the collision rule in [Groups and names](#groups-and-names), a clash with this package would drop the other package's command.

### Tag taxonomy

Groups say where a command lives; tags are for browsing across groups and do not replace them. Tags are lowercase `/`-separated paths. Pick from the established top-level tags before inventing a new one, and add a subtag when a family is large enough to browse on its own:

| Top-level tag | Covers | Subtags in use |
|---------------|--------|----------------|
| `animation` | Animation authoring | `animator`, `clip`, `timeline` |
| `assets` | Asset CRUD and search | `import`, `text` |
| `authoring` | Authoring-root configuration | — |
| `batch` | Transactional multi-command execution (`job batch`) | — |
| `baking` | Bake pipelines | `lighting`, `navmesh`, `occlusion` |
| `build` | Player builds | `settings`, `targets` |
| `capture` | Screenshots and view/element capture | — |
| `editor` | Editor application control | `instances`, `playmode` |
| `gameobjects` | Scene GameObject manipulation | `components` |
| `materials` | Materials | `shaders` |
| `navigation` | Selection and search | — |
| `networking` | Multiplayer and network state, from networking packages | `netcode` |
| `observability` | Logs and diagnostics | `audit`, `console`, `eval_usage`, `performance` |
| `packages` | UPM package management | — |
| `prefabs` | Prefab workflows | — |
| `runtime` | Commands that target a running Player | `application`, `input` |
| `scenes` | Scene lifecycle and hierarchy | — |
| `scripts` | C# source workflows | `compile`, `eval`, `methodreload` |
| `settings` | Project settings | `audio`, `graphics`, `input`, `physics`, `player`, `quality`, `runtime_pipeline`, `tags_layers`, `time` |
| `tests` | Test runner | — |
| `ui` | UI element workflows | — |
| `wait` | Server-side condition waiting | — |

A command may carry multiple tags when it genuinely belongs to two families (e.g. `scene add_to_build` is tagged `scenes` and `build/settings`), while it has exactly one group.

### Describing parameters

Tag each parameter with `[CliArg]`:

```csharp
[CliArg("name", "description", Required = false, DefaultValue = null)]
```

| Property | Meaning |
|----------|---------|
| `name` | Parameter name as it appears in client arguments (`--name value`). |
| `description` | Human-readable description for help text. |
| `Required` | Whether the parameter must be supplied. Defaults to `false` — but if the parameter has no C# default value it is treated as required. |
| `DefaultValue` | Value used when the client omits the parameter (a C# default value takes precedence). |

`[CliArg]` is optional metadata. A parameter without it still works: its name defaults to the C# parameter name and `Required` defaults to "does this parameter lack a C# default value?".

The package's own commands name parameters in kebab-case, the same as the `unity` CLI's flags: `--output-path`, `--dry-run`, and keys such as `output-path` inside a structured input. They also share one word per concept, with no aliases:

| Concept | Name | Not |
|---------|------|-----|
| An on/off switch | `enabled` | `enable` |
| A file the command writes | `output-path` | `output`, `out`, `output-file`, `save-path` |
| A package's identity | `name` | `identifier`, `package`, `package-name` |
| Validate without changing anything | `dry-run` | `dryrun` |
| Acknowledge a destructive change | `confirm` | |

`ParameterNamesTests` enforces both rules for every command in the package's assemblies, including the keys of structured inputs. The registry does not check it, so commands declared in your own assemblies may use any naming convention.

#### Declaration order and `Required` are wire API

Clients may send an unparsed command line (`argv`/`commandLine` on `/api/exec`), and the server
binds positionals into **required parameters in declaration order, then optional parameters in
declaration order**. Three consequences for command authors:

- **Reordering parameters, or flipping one between required and optional, is a breaking change**
  for those clients even though the C# signature still compiles. Append new optional parameters at
  the end.
- **Enum parameters are validated by name** (case-insensitively), and an invalid value is rejected
  with the legal set echoed back — you get that for free, no attribute needed. A `[Flags]` enum
  also takes a comma-separated combination (`--channels Info,Warning`), and a numeric value is
  accepted, because validation defers to the same converter the executor uses.
- **Structured (`IStructuredCommandInput` / `JObject`) parameters are reachable from a command
  line only as a JSON-valued flag**, e.g. `--payload '{"name":"x"}'`. Single quotes are the
  practical form; see the tokenizer dialect in [Connectivity](connectivity.md).

A token the declared type cannot accept is now a hard `400` on the raw path, rather than the
parameter silently dropping out and the command running with a default.

#### Reserved parameter names

Don't name a parameter `format`, `json`, `fields`, `verbose` or `omit-nulls`. These are the
executor's own options (see [Output formats](connectivity.md#output-formats)): on a command line
the executor takes them out of the arguments, anywhere before `--`, before the command binds them,
so such a parameter never gets its value from one.

## Writing output

Results go to stdout and everything else goes to stderr. Write results with `cmd.Result`, and the pipeline formats them for the `--format` the user passed (see [Output formats](connectivity.md#output-formats)), so every command supports every format.

- **A list:** call `cmd.Result.WriteRecord(item)` once per item, as soon as it's ready, so output streams while the command runs. Each record is one table row, one tsv line or one JSON object per line. Don't wrap a list in an object that carries it with its count.
- **A single result**, such as a status, a summary, or the object a command created: call `cmd.Result.Write(obj)` once.
- **Output that isn't a result**, such as a file's raw contents: write it to `cmd.Std.Out`.

A command writes its result one way only: the pipeline throws if it calls `Result.Write` twice or calls both `Result.Write` and `Result.WriteRecord`. Don't mix `Result` with `Std.Out` either: `--format json` output would stop being valid.

An empty list writes nothing and exits with `0`. A command that should return one result but finds none fails with `ExitCode.NotFound`.

```csharp
[CliGroupCommand(Group, "List the scenes in the build.", Effect = CommandEffect.ReadOnly)]
internal static ExitCode ListBuildScenes(CommandContext cmd)
{
    foreach (var scene in EditorBuildSettings.scenes)
        cmd.Result.WriteRecord(new { path = scene.path, enabled = scene.enabled });
    return ExitCode.Success;
}
```

Any release can add fields to a result. Removing, renaming or changing the type of a field is a breaking change.

## Logging

`cmd.Log` writes to stderr, as much as the user's `--log-level` picks (`warning` by default), in the `--log-format` they picked (see [Connectivity](connectivity.md)). The log level never changes stdout.

| Level | Use for | Example |
|-------|---------|---------|
| `Error` | The command failed. Pair it with a non-zero exit code and, when possible, say what the user can do about it. | `cmd.Log.Error("Asset 'Assets/Player.prefab' not found.");` |
| `Warning` | Something unexpected happened, but the result is still valid. | `cmd.Log.Warning("Skipped 2 of 40 scenes because they are read-only.");` |
| `Info` | What the command did, at the level of milestones rather than individual items. | `cmd.Log.Info("Imported 38 assets.");` |
| `Debug` | Decisions and internal state that help diagnose a problem. | `cmd.Log.Debug("Resolved 'Player' to Assets/Prefabs/Player.prefab.");` |

## Exit codes

The command returns how it ended:

| `ExitCode` | When |
|------------|------|
| `Success` | The command did what it was asked. |
| `InvalidUsage` | The arguments are wrong in a way the parser can't tell, such as two flags that exclude each other. |
| `Failed` | The command failed. |
| `NotFound` | What the command was asked to work on doesn't exist, such as an asset path. |
| `TimedOut` | The command ran out of a time limit of its own, such as a batch's `timeout`. The request's own timeout is reported by the pipeline. |
| `Cancelled` | The user stopped the command, such as by cancelling a dialog it opened. |

Log why with `cmd.Log.Error` before returning anything but `Success`.

A command may also throw. The pipeline then logs the exception's message as an error and exits with `Failed`, or `InvalidUsage` for an `ArgumentException`. Return an exit code for the failures the command expects, and let exceptions report the ones it doesn't.

```csharp
[CliGroupCommand(Group, "Open a scene.", Effect = CommandEffect.Destructive)]
internal static ExitCode Open(CommandContext cmd, [CliArg("path", "Scene to open", Required = true)] string path)
{
    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
    {
        cmd.Log.Error($"No scene at '{path}'.");
        return ExitCode.NotFound;
    }

    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    cmd.Result.Write(new { path });
    return ExitCode.Success;
}
```

## Commands that run across frames

A command that returns `ExitCode` holds the main thread until it returns: the editor doesn't repaint or show progress, and the user can't cancel it. Make a command async when it handles many items or waits for anything:

```csharp
[CliGroupCommand(Group, "Reimport each asset under a folder.", Effect = CommandEffect.Idempotent)]
internal static async Task<ExitCode> Reimport(CommandContext cmd, [CliArg("folder", "Folder to reimport")] string folder)
{
    var imported = 0;
    foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        AssetDatabase.ImportAsset(path);
        cmd.Result.WriteRecord(path);
        imported++;
        await Task.Yield(); // the frame goes back to the editor
        cmd.CancellationToken.ThrowIfCancellationRequested();
    }
    cmd.Log.Info($"Reimported {imported} assets.");
    return ExitCode.Success;
}
```

An async command runs on the main thread, and each `await` gives the frame back to the editor. It should:

- Give the frame back with `await Task.Yield()`. Don't use `Awaitable.NextFrameAsync`, which never completes in edit mode, or `Task.Delay`, which counts wall-clock time rather than frames.
- Stay on the main thread. Don't move work off it with `Task.Run`, and don't add `ConfigureAwait(false)` to an `await`: most Unity APIs throw when called from another thread.
- Yield after a batch of items rather than after each one when items are cheap.

A `job batch` can't run an async command.

## Handling cancellation

A user can stop a command at any time: with Ctrl-C on a terminal, with SIGTERM from a script or CI job, or when its timeout runs out. The command finds out through `cmd.CancellationToken`. Cancellation is cooperative: a command that never checks the token runs to the end, and its result is thrown away.

- Check the token between units of work, such as each asset, scene or test, and pass it to every API that takes one. In an async command, check it after each `await`.
- Register the cancel method of a long Unity operation that has one: `using (cmd.CancellationToken.Register(Lightmapping.Cancel))`.
- Let the `OperationCanceledException` propagate. Don't catch it to return a partial result, and don't log it as an error.
- Leave the project in a consistent state: put cleanup in `finally` blocks and keep it short.

## The legacy signature

A command method that doesn't return `ExitCode` uses the legacy signature, which is still supported and isn't warned about. It returns its result, and the pipeline writes it: a list as records, anything else as a single result. A thrown exception, or a returned `CommandExecutionResponse` or `BaseResponse` that carries an error, fails the command.

```csharp
[CliGroupCommand(Group, "Print a greeting.")]
static string Greet([CliArg("name", "Who to greet")] string name = "world") => $"Hello {name}!";
```

A legacy command can't take a `CommandContext`, so it can't log at a level or stream records. Every command the package ships uses the `CommandContext` definition; `CommandSignatureTests` holds it to that.

## Commands that take a `CommandContext`

Instead of returning its result, a command can take a `CommandContext` and return its exit code.
The pipeline passes the context itself, so it is never a flag, and it may sit anywhere among the
parameters:

```csharp
[CliGroupCommand(Group, "Print a greeting.", Effect = CommandEffect.ReadOnly)]
internal static ExitCode Greet(
    CommandContext cmd,
    [CliArg("name", "Who to greet")] string name = "world")
{
    cmd.Log.Info($"Greeting {name}");
    cmd.Result.Write($"Hello {name}!");
    return ExitCode.Success;
}
```

| Member | What it is |
|--------|------------|
| `Result` | Writes the result in the format the user picked (see [Output formats](connectivity.md#output-formats)): `Write(obj)` once for a single result, or `WriteRecord(obj)` per item, written as each is ready. Not both. |
| `Log` | `Error`, `Warning`, `Info`, `Debug`: lines to stderr, as much as the user's [`--log-level`](connectivity.md#log-level) picks (`warning` by default). |
| `Std.In` | What the client sends, such as text or a file piped to the `unity` CLI. `await ReadLineAsync()` reads a line as UTF-8 text, null once the input ends. `await ReadAsync(buffer)` reads the bytes as the client sent them, 0 once the input ends. Both wait for the client without holding the Editor, and read from one position, so a command may mix them, such as a header line followed by binary. Cancelling a command ends its input, so after reading to the end, check the cancellation token. |
| `Std.Out`, `Std.Error` | Output that isn't a result, such as a file's contents: `Write(text)` and `WriteLine(text)` write UTF-8 text, `Write(bytes)` writes bytes as they are, in order with the text. A client that takes only text, such as one calling over HTTP, gets the bytes decoded as UTF-8. |
| `Arguments` | The arguments the command was called with, without the pipeline's own options. |
| `CancellationToken` | Cancelled when the user stops the command (Ctrl-C, SIGTERM) or it runs past its timeout. |

### Exit codes

Return the `ExitCode` that says how the command ended, and log why it failed with `cmd.Log.Error`:

| `ExitCode` | When |
|---|---|
| `Success` | It did what it was asked. |
| `InvalidUsage` | The arguments are wrong in a way the parser can't tell, such as two flags that exclude each other. |
| `Failed` | It failed. |
| `NotFound` | What it was asked to work on doesn't exist, such as an asset path. |
| `TimedOut` | It ran out of a time limit of its own, such as a batch's `timeout`. |
| `Cancelled` | The user stopped it, such as by cancelling a dialog it opened. |

The pipeline decides the rest itself: a timeout, a server that can't run commands now, and a modal
dialog that blocks the Editor. A command that fails without logging an error (a warning doesn't count) gets a one-line error
from the pipeline. Once a command ends, including when it runs past its timeout, what it still
writes is dropped. One that throws fails, its message logged as an error. One that stops on its
`CancellationToken` lets the `OperationCanceledException` propagate and ends cancelled.

### Commands that run across frames

Return `Task<ExitCode>` from an `async` method when the command waits: on the Editor, on a file, or
on its input:

```csharp
[CliGroupCommand(Group, "Count the lines piped in.", Effect = CommandEffect.ReadOnly)]
internal static async Task<ExitCode> CountLines(CommandContext cmd)
{
    var count = 0;
    while (await cmd.Std.In.ReadLineAsync() != null)
        count++;
    cmd.Result.Write(count);
    return ExitCode.Success;
}
```

The context works on any thread, so it can be used after any `await`; Unity APIs still need the
main thread. A command that returns a `Task` can't run inside
[`job batch`](commands/batch.md).

A method that takes a `CommandContext` and returns anything other than `ExitCode` or
`Task<ExitCode>` isn't registered: the Console says why.

## Structured (multi-field) parameters

When a command needs a structured argument with several fields, don't spread them across many `[CliArg]` parameters — declare a small DTO that implements `IStructuredCommandInput` and take it as a single parameter. The type is advertised to clients as a nested JSON **object** schema in `GET /api/commands` (instead of collapsing to `string`), and the value is deserialized automatically via Newtonsoft — no extra wiring.

```csharp
[CliGroupCommand(Group, "Change Time settings. Requires confirm=true; use dry-run to preview.")]
internal static ExitCode Set(
    CommandContext cmd,
    [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] TimeSettingsArgs settings = null,
    [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
    [CliArg("dry-run", "Preview the change without applying it.")] bool dryRun = false)
{ /* ... */ }

public class TimeSettingsArgs : IStructuredCommandInput
{
    [CliArg("fixed-delta-time", "Fixed timestep in seconds (e.g. 0.02).")]
    public float? FixedDeltaTime { get; set; }

    [CliArg("time-scale", "Time scale (1 = real-time).")]
    public float? TimeScale { get; set; }
}
```

`JsonSchemaGenerator` reflects over the type's public, writable fields and properties to emit `{ "type": "object", "properties": { ... } }`, recursing into nested `IStructuredCommandInput` members and arrays/lists of them. Member metadata mirrors command parameters:

- `[CliArg(name, description, Required = ...)]` controls the property name (the key clients send, which the value binds from), description, and whether it appears in the schema's `required` array.
- Without a `[CliArg]`, the member (or its Newtonsoft `[JsonProperty]`) name is used and it is optional. Use nullable types (`float?`) for "omitted = leave unchanged" semantics.
- `[JsonIgnore]` members are omitted from the schema.
- A key the client sends that names no member is refused, at any depth, with the closest member name as a suggestion.

`[CliArg]` is valid on parameters, fields, and properties, so the same attribute annotates DTO members. Most commands that take an `IStructuredCommandInput` are mutations — pair the DTO with `confirm`/`dry-run` and follow the [safety conventions](safety-and-mutations.md) (inline gate + `AuthoringUndoScope`).

## The `CommandExecutionResponse` envelope

Whatever your handler returns, the client receives a `CommandExecutionResponse`:

| Field | Type | Meaning | In lean reply? |
|-------|------|---------|----------------|
| `success` | `bool` | Whether the command ran without throwing. | always |
| `result` | `object` | The command's result: what it wrote with `cmd.Result` (its records as a list), or a legacy command's return value. | always on success, explicitly `null` when null; on failure, only when the command wrote one (e.g. [`test status`](commands/build-and-compilation.md#test-status) after a failed run) |
| `error` | `string` | Error summary when `success` is `false`. | when non-null |
| `errorDetails` | `string` | Extra error context. | when non-null |
| `warnings` | `string[]` | Corrective guidance the server wants the caller to see (e.g. an unrecognized option value). | when non-null (omitted when there is nothing to say, in every mode) |
| `command` | `string` | The command that ran, by its full name, `group name`. | verbose only; always in a reply to an `argv`/`commandLine` request |
| `executedAt` | `DateTime` | When the reply was produced. | verbose only |
| `executionTimeMs` | `long?` | How long the command took. | verbose only |

The envelope is **lean by default** (AUTHAPI-21): serialized compact (no indentation), with the
envelope's own null keys omitted and the always-on metadata (`command`, `executedAt`,
`executionTimeMs`) dropped (a reply to an `argv`/`commandLine` request keeps `command`). A minimal success is just `{"success":true,"result":...}`. Agents
consume the JSON directly; a client can pretty-print for a human if needed.

The boundaries of the lean contract, worth knowing:

- **Only the envelope's nulls are omitted; your `result` payload keeps explicit nulls.** The
  envelope's null keys are redundant (`success` already disambiguates: `error` is null on success),
  so they are dropped. Inside `result` the opposite holds — a scene object's `AuthoringResult`
  still carries `"assetPath": null` — because an absent payload key would be indistinguishable
  from a nonexistent or misspelled one.
- **A successful reply always carries `result`, even when its value is null — in every mode.** A
  null result on success is the command's actual value (e.g. a `detail="value"` read of an
  unassigned object-reference field), never a droppable envelope null. A *failure* omits the
  `result` key, unless the command returned a failure that keeps its payload: [`test status`](commands/build-and-compilation.md#test-status)
  fails a poll for a failed run and still carries the run's counts and results, so a client must
  not discard `result` on failure.
- **The metadata gating applies to the whole graph.** A command whose result is itself a response
  model (e.g. `script eval` returns an `EvalResponse` nested under `result`) has its nested
  `executedAt`/`executionTimeMs` stripped in lean mode too — and restored by verbose along with
  everything else.

Two independent request flags adjust the shape:

- `"verbose": true` — full fidelity back: every envelope field, explicit nulls, and nested response
  metadata, for debugging or correlation. Honored on request-validation failures too.
- `"omitNulls": true` — drop null keys from the whole reply **inside `result`** (the success
  `result` key itself always survives, see above). Opt in only when the result schema is already
  known and the nulls are pure bytes — the payoff case is bulk list reads (e.g. `material list_shaders`
  over hundreds of built-ins repeating `"assetPath": null` per item).

The `GET`-style endpoints (`/api/job`, `/api/progress`) have no envelope/payload split — every field
is payload — so they include null keys explicitly by default and accept `omit_nulls=true` as a query
parameter instead. An unrecognized value (e.g. `omit_nulls=1`) is not silently coerced: the reply
keeps its nulls and carries a `warnings` array naming the accepted values, so an agent can correct
itself instead of guessing.

If your command throws, the server catches it and returns a failure envelope with `success = false` and the exception message in `error`.

## `MainThreadRequired`

- **Default: `true`.** Most Unity APIs (scene, GameObject, asset, play-mode access) must run on the main thread. The server marshals these handlers onto the main thread via its dispatcher.
- **Set `false` only** for handlers that are thread-safe and read-only / pollable (e.g. a status or buffer-read command). These run on a background thread so they never block the main thread or deadlock against a busy editor.

When in doubt, leave it `true`.

## Timeouts

Every request has one timeout, in seconds (see [Timeouts](connectivity.md#timeouts)). A command
with no `timeout` parameter gets 60 s. To set a different default, or to see the timeout inside
the handler, declare a `timeout` parameter of type `double` (name the C# parameter
`timeoutInSeconds`); the server writes the time left into it. Convert it to a `TimeSpan` straight away and pass that on:

```csharp
[CliGroupCommand(Group, "Bake every lightmap")]
internal static ExitCode BakeAll(
    CommandContext cmd,
    [CliArg("timeout", "Timeout in seconds")] double timeoutInSeconds = 600)
{
    // What is left of the request's timeout; positive infinity for a detached job.
    var timeout = double.IsPositiveInfinity(timeoutInSeconds)
        ? System.Threading.Timeout.InfiniteTimeSpan
        : TimeSpan.FromSeconds(timeoutInSeconds);
    cmd.Result.Write(Lightmaps.BakeAll(timeout));
    return ExitCode.Success;
}
```

When the timeout runs out the server answers `504 TIMEOUT` whatever the command is doing, and
cancels `cmd.CancellationToken`. A command that wants to report its own timeout must stop a little
before it.

## The `runtime` tag

A command that targets a running Player carries the `runtime` tag. Declare it with `Tags = new[] { "runtime" }`, or a subtag such as `runtime/input`. `CommandInfo.RuntimeOnly` reads back off the tags, so it is true for anything in that subtree.

The tag does **not** hide a command. Every command is listed on every server, and `runtime` is how a client tells which ones target a running Player. It does, however, keep a command out of a `job batch` on an Editor server (see [Safety and mutations](safety-and-mutations.md)), since those commands cannot do anything useful there.

## Discovery

Commands are discovered by `CommandRegistry`, which scans for `[CliGroupCommand]`-tagged methods through a pluggable `ICommandDiscovery`:

- In the **Editor**, `TypeCacheCommandDiscovery` provides fast `TypeCache`-based discovery.
- In a **Player**, the registry falls back to reflection over loaded assemblies.

Results are cached until the next domain reload. **A newly added command becomes available after the next recompile** — no registration call is needed; just declare it and recompile.

## Registering a command dynamically

`[CliGroupCommand]` covers commands you know about at compile time. When the set of commands depends on runtime state — a tool that exposes one command per loaded document, a plugin host, a test that needs a throwaway command — register them directly instead:

```csharp
CommandRegistry.RegisterCommand(
    "mytool",
    "my_dynamic_command",
    "What this command does",
    new Func<CommandContext, string, int, ExitCode>(MyHandler),
    mainThreadRequired: true,
    tags: new[] { "mytool" },
    effect: CommandEffect.Additive);
```

The first two arguments are the group and the name, and the command is run as `mytool my_dynamic_command`. The same rules apply as for an attribute-declared command (see [Groups and names](#groups-and-names)): a group or name that breaks the format rules throws `ArgumentException`. To mark a command as targeting a Player, pass `tags: new[] { "runtime" }`.

The result is an ordinary command: it appears in `/api/commands`, carries the same metadata, and executes through `/api/exec` exactly like an attribute-declared one.

**Parameters still come from `[CliArg]`.** The registry reads the handler's own parameters, so a handler method annotates them exactly as an attribute-declared command does:

```csharp
private static ExitCode MyHandler(
    CommandContext cmd,
    [CliArg("text", "Some input", Required = true)] string text,
    [CliArg("count", "How many times")] int count = 1)
{
    cmd.Result.Write(new { echoed = text, count });
    return ExitCode.Success;
}
```

A lambda cannot carry `[CliArg]` on its parameters, so registering one yields default metadata (the C# parameter name, required only when there is no default value). Register a named method when you want full metadata.

**Instance methods are allowed.** Unlike the attribute path, the handler is a delegate, so it may be bound to an instance — the command then runs against that instance:

```csharp
var session = new MySession(documentId);
CommandRegistry.RegisterCommand("mytool", "read_document", "Read the open document",
    new Func<string>(session.Read), tags: new[] { "mytool" });
```

**Removing a command:**

```csharp
bool removed = CommandRegistry.UnregisterCommand("mytool", "my_dynamic_command");
```

`UnregisterCommand` takes the group and the name the command was registered with, and returns `false` when nothing was registered under them. Attribute-declared commands are part of the code and are never removable, so passing the group and name of one also returns `false`.

**Lifetime and collisions.** Registrations live until they are unregistered or the domain reloads — statics reset on reload, so re-register from wherever your code initializes (e.g. `[InitializeOnLoadMethod]`).

A registration made **while in Play Mode** is also dropped when Play Mode exits, so a script registering from `Awake` or `OnEnable` starts each Play session with a clean registry. This matters when Enter Play Mode has domain reload disabled: without it, the second entry would throw (the name would still be taken by the first entry) and a surviving registration would hold a delegate bound to the previous session's destroyed object. A registration made in **Edit Mode** survives a Play session — an `[InitializeOnLoadMethod]` runs once per domain, so nothing would restore it.

All registrations survive a discovery-mechanism swap. Registering a group and name that are already taken, by an attribute-declared command or an earlier dynamic one, throws `InvalidOperationException` rather than shadowing it; unregister first if you mean to replace it.

## Minimal custom command template

```csharp
using Unity.Pipeline.Commands;

static class MyCommands
{
    const string Group = "mytool";

    [CliGroupCommand(Group, "What this command does")]
    internal static ExitCode Echo(
        CommandContext cmd,
        [CliArg("text", "Some input", Required = true)] string text,
        [CliArg("count", "How many times")] int count = 1)
    {
        // Runs on the main thread (MainThreadRequired defaults to true).
        for (var i = 0; i < count; i++)
            cmd.Result.WriteRecord(text);
        return ExitCode.Success;
    }
}
```

Recompile, then invoke it from your client (`unity command mytool echo --text hello --count 3`).

## Adding a command to the package

When the command ships in `com.unity.pipeline` itself:

1. Reuse an existing group where one fits, or use a new one, and declare it as `const string Group` in the command class. Consider a subgroup when an object has several commands of its own; this is guidance, not a threshold.
2. Name the method verb first, reusing the package's verbs where they fit. The command name is derived from it; use `Name =` only when the derived name is wrong.
3. Add `Tags` for browsing across groups.
4. Set `Effect` (see [Declaring the effect](#declaring-the-effect)). Every command the package ships declares one.
5. Use the [`CommandContext` definition](#the-command-method): `internal`, takes a `CommandContext`, returns `ExitCode` or `Task<ExitCode>`, writes results with `cmd.Result` and diagnostics with `cmd.Log`.
6. Add a `CommandNames` constant if any code refers to the command by name.
7. Document it on the matching page under `Documentation~/commands/` and add its row to [the index](index.md).
8. Run `CommandNamesTests`, `CommandEffectTests` and `CommandSignatureTests`. To test the command itself, call it through `CommandRun` (see [Testing](testing.md)).

Renaming a method renames its command, so treat it as a breaking change: add a changelog entry to the pull request description and check whether the CLI calls the command by name.

## See also

- [Command reference](commands/runtime.md)
- [Connectivity](connectivity.md) — how clients reach the server and authenticate.
