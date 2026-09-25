# Creating commands

A *command* is a `static` method that the Pipeline server can invoke over HTTP. The method's accessibility does not matter — `public`, `internal`, and `private` static methods can all be registered. This page covers the command-authoring API: how to declare a command, describe its parameters, return a result, and have it discovered automatically.

## The handler + response pattern

Authoring a command has two halves:

1. **The handler** — your `static` method, tagged `[CliCommand]`. It does the work and returns a value.
2. **The response** — the server wraps whatever your handler returns in a [`CommandExecutionResponse`](#the-commandexecutionresponse-envelope) and serializes it to JSON for the client.

You never build the HTTP response yourself. Return a `string`, a number, an anonymous object, a typed model, or `null`; the server takes care of the envelope, timing, and error reporting.

## Declaring a command

Tag a `static` method with `[CliCommand]` (the examples below use `public`, but any accessibility works):

```csharp
[CliCommand("name", "description", MainThreadRequired = true, RuntimeOnly = false)]
public static <ReturnType> Handler(...);
```

| Argument | Meaning |
|----------|---------|
| `name` | Unique command name used by the client (`unity command <name>`). |
| `description` | Human-readable text shown in help and the `/api/commands` listing. |
| `MainThreadRequired` | Whether the handler must run on Unity's main thread. **Default `true`.** |
| `RuntimeOnly` | Whether the command is hidden from an Editor server's command listing. **Default `false`.** |

The method **must be `static`**, but its accessibility does not matter: `public`, `internal`, and `private` static methods can all be registered. `CommandRegistry` invokes handlers through reflection, so a `private` handler runs exactly like a `public` one. Only a non-static (instance) method fails to register — `CommandRegistry` skips it and logs a warning.

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

## Worked example 1 — returning a string

A command can return a plain `string`. The server places it in the response `result` field.

```csharp
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

public static class PlayModeCommands
{
    [CliCommand("editor_play", "Enter Unity Editor play mode")]
    public static string EnterPlayMode()
    {
        if (EditorApplication.isPlaying)
            return "Already in play mode";

        EditorApplication.isPlaying = true;
        return "Entered play mode";
    }
}
```

Calling `editor_play` yields a `CommandExecutionResponse` whose `result` is `"Entered play mode"`.

## Worked example 2 — returning a response model

For richer results, return a model. Returning a type that extends `CommandExecutionResponse` (like `EvalResponse`) lets you populate response fields directly; you can also return any plain serializable model (like `AuthoringResult`) and let the server wrap it.

```csharp
using Unity.Pipeline.Commands;
using Unity.Pipeline.Models;
using Unity.Pipeline.Compilation;

public static class CodeEvalCommand
{
    [CliCommand("eval", "Evaluate C# code dynamically using Roslyn compiler", MainThreadRequired = true)]
    public static EvalResponse EvaluateCode(
        [CliArg("code", "C# code to evaluate", Required = true)] string code,
        [CliArg("timeout", "Timeout in milliseconds")] int timeout = 5000)
    {
        if (string.IsNullOrWhiteSpace(code))
            return EvalResponse.EvalFailure("Bad Request", "Code parameter is required and cannot be empty");

        var result = EvalCodeCompiler.CompileAndExecuteOnMainThread(code, timeout, null);
        return result ?? EvalResponse.EvalFailure("Unknown Error", "Compilation returned null result");
    }
}
```

`EvalResponse` adds `output` and `diagnostics` on top of the standard envelope. Another common shape is a domain model such as `AuthoringResult` — the canonical identity (asset path, GUID, instance id, hierarchy path) of an object a command created, returned so the client can reference it in a follow-up call.

## Structured (multi-field) parameters

When a command needs a structured argument with several fields, don't spread them across many `[CliArg]` parameters — declare a small DTO that implements `IStructuredCommandInput` and take it as a single parameter. The type is advertised to clients as a nested JSON **object** schema in `GET /api/commands` (instead of collapsing to `string`), and the value is deserialized automatically via Newtonsoft — no extra wiring.

```csharp
[CliCommand("set_time_settings", "Change Time settings. Requires confirm=true; use dry_run to preview.")]
public static ProjectSettingsResponse Set(
    [CliArg("settings", "Fields to change; omitted fields are left unchanged.")] TimeSettingsInput settings = null,
    [CliArg("confirm", "Apply the change. Without it the call is refused.")] bool confirm = false,
    [CliArg("dry_run", "Preview the change without applying it.")] bool dryRun = false)
{ /* ... */ }

public class TimeSettingsInput : IStructuredCommandInput
{
    [CliArg("fixedDeltaTime", "Fixed timestep in seconds (e.g. 0.02).")]
    public float? FixedDeltaTime { get; set; }

    [CliArg("timeScale", "Time scale (1 = real-time).")]
    public float? TimeScale { get; set; }
}
```

`JsonSchemaGenerator` reflects over the type's public, writable fields and properties to emit `{ "type": "object", "properties": { ... } }`, recursing into nested `IStructuredCommandInput` members and arrays/lists of them. Member metadata mirrors command parameters:

- `[CliArg(name, description, Required = ...)]` controls the property name, description, and whether it appears in the schema's `required` array.
- Without a `[CliArg]`, the member (or its Newtonsoft `[JsonProperty]`) name is used and it is optional. Use nullable types (`float?`) for "omitted = leave unchanged" semantics.
- `[JsonIgnore]` members are omitted from the schema.

`[CliArg]` is valid on parameters, fields, and properties, so the same attribute annotates DTO members. Most commands that take an `IStructuredCommandInput` are mutations — pair the DTO with `confirm`/`dry_run` and follow the [safety conventions](safety-and-mutations.md) (inline gate + `AuthoringUndoScope`).

## The `CommandExecutionResponse` envelope

Whatever your handler returns, the client receives a `CommandExecutionResponse`:

| Field | Type | Meaning |
|-------|------|---------|
| `success` | `bool` | Whether the command ran without throwing. |
| `command` | `string` | The command name. |
| `result` | `object` | Your handler's return value (a string, model, anonymous object, or `null`). |
| `executionTimeMs` | `long?` | How long the command took. |
| `error` | `string` | Error summary when `success` is `false`. |

If your handler throws, the server catches it and returns a failure envelope with `success = false` and the exception message in `error` — you do not need to catch-and-wrap yourself unless you want a tailored message.

## `MainThreadRequired`

- **Default: `true`.** Most Unity APIs (scene, GameObject, asset, play-mode access) must run on the main thread. The server marshals these handlers onto the main thread via its dispatcher.
- **Set `false` only** for handlers that are thread-safe and read-only / pollable (e.g. a status or buffer-read command). These run on a background thread so they never block the main thread or deadlock against a busy editor.

When in doubt, leave it `true`.

## `RuntimeOnly`

- **Default: `false`** — the command is advertised in an Editor server's `/api/commands` listing.
- **Set `true`** to hide a command from the Editor command listing. It remains **executable**; it is simply not advertised when a client is connected to an Editor (Runtime/Player servers still list it). Use this for commands that only make sense against a running Player.

## Discovery

Commands are discovered by `CommandRegistry`, which scans for `[CliCommand]`-tagged methods through a pluggable `ICommandDiscovery`:

- In the **Editor**, `TypeCacheCommandDiscovery` provides fast `TypeCache`-based discovery.
- In a **Player**, the registry falls back to reflection over loaded assemblies.

Results are cached until the next domain reload. **A newly added command becomes available after the next recompile** — no registration call is needed; just declare it and recompile.

## Minimal custom command template

```csharp
using Unity.Pipeline.Commands;

public static class MyCommands
{
    [CliCommand("my_command", "What this command does")]
    public static object MyCommand(
        [CliArg("text", "Some input", Required = true)] string text,
        [CliArg("count", "How many times")] int count = 1)
    {
        // Do work on the main thread (MainThreadRequired defaults to true).
        return new { echoed = text, count };   // anonymous object → response.result
    }
}
```

Recompile, then invoke it from your client (`unity command my_command --text hello --count 3`).

## See also

- [Command reference](commands/runtime.md)
- [Connectivity](connectivity.md) — how clients reach the server and authenticate.
