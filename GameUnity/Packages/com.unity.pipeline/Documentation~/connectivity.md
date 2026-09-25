# Connectivity

How a client reaches a running Unity instance: the loopback-only HTTP servers, their port ranges, the port descriptor file used for discovery, and the bearer-token authentication every request must carry.

The rest of this page covers the implementation. If you just want to connect, start with the two sections below.

## Connecting to a running Editor

Connections go through the `unity` CLI. Run `unity command` with no command name to connect to a Unity instance and list its available commands:

```bash
# Auto-discover a Unity instance from the current directory
unity command

# Connect to a specific project
unity command --project-path /path/to/your/unity/project
```

Once connected, run any command by name, e.g. `unity command editor_status`.

## Connecting to a running Player (game)

To target a running development Player instead of the Editor, use `--runtime` (by process name) or `--runtime-path` (by the location of the runtime port file). These options go **after** `command` and **before** the command name:

```bash
# By Player process/executable name
unity command --runtime MyGame.exe runtime_status

# By the folder/bundle where the runtime port file lives
unity command --runtime-path "C:\Builds\MyGame" runtime_status             # Windows: next to the .exe
unity command --runtime-path "/Users/me/Builds/MyGame.app" runtime_status  # macOS: the .app bundle
```

> The Runtime server only runs in a **development Player build** with the runtime manager enabled — see [Runtime connection & setup](runtime-setup.md).

## Finding the port in the Unity logs

The [port descriptor file](#port-descriptor-file) is the primary discovery channel, but the server also reports itself to the Unity log when it starts and stops — useful when you can't read the descriptor file directly. Look for `Pipeline`-prefixed lines.

- **Runtime (Player)** — logs its port **and** the descriptor file location on start, and a line on stop:

  ```
  Pipeline: Runtime server started successfully on port 7901
  Pipeline: Runtime descriptor written to /path/to/MyGame/.unity-pipeline-runtime-port
  Pipeline: Runtime server stopped
  ```

- **Editor** — logs its port when the server is (re)started from the **Pipeline ▸ Start Server** menu or re-opened by its watchdog (`Pipeline Server started on port 7801`). Its descriptor always lives at the fixed `Library/Pipeline/.unity-pipeline-port` path.

### Where the logs live

**Editor log** (`Editor.log`):

| OS | Path |
|----|------|
| Windows | `C:\Users\<user>\AppData\Local\Unity\Editor\Editor.log` |
| macOS | `~/Library/Logs/Unity/Editor.log` |

**Player log** (`Player.log`):

| OS | Path |
|----|------|
| Windows | `C:\Users\<user>\AppData\LocalLow\<company>\<product>\Player.log` |
| macOS | `~/Library/Logs/<company>/<product>/Player.log` |

(`<company>` and `<product>` are the project's **Company Name** and **Product Name** from Player Settings.)

## Loopback-only binding

Both the Editor and Runtime servers bind to the **IPv4 loopback** (`127.0.0.1`) — plus the `localhost` hostname for compatibility:

```csharp
if (Socket.OSSupportsIPv4)
    m_HttpListener.Prefixes.Add($"http://127.0.0.1:{m_Port}/");
m_HttpListener.Prefixes.Add($"http://localhost:{m_Port}/");
```

The server is never exposed on a routable interface — it is reachable only from the same machine.

Clients should connect to **`127.0.0.1`** explicitly rather than `localhost`. Unity's Mono `HttpListener` only reliably serves the IPv4 loopback: a request arriving over the IPv6 loopback (`::1`) is answered with `400` because Mono mis-parses the bracketed `[::1]` host. Since `localhost` resolves to `::1` or `127.0.0.1` non-deterministically (notably on Windows with Node's default DNS order), dialing `localhost` caused intermittent connection failures — dialing `127.0.0.1` avoids the IPv6 path entirely.

In addition, the server refuses any request that carries an `Origin` header (legitimate CLI/CI clients never send one), which blocks a browser page from reaching the local server and short-circuits CORS preflights.

## Port ranges

Each server type picks the first free port in its range (or you can pin one explicitly):

| Server | Production range | Test range |
|--------|------------------|------------|
| **Editor** | `7800`–`7849` | `7850`–`7899` |
| **Runtime** (Player) | `7900`–`7949` | `7950`–`7999` |

If no port in the range is free, startup throws (`No available ports in range …`).

## Port Descriptor File

When a server starts, it writes a small JSON **instance descriptor** so clients can discover it. This is the only discovery channel — there is no broadcast or registry.

| Server | Descriptor path |
|--------|-----------------|
| **Editor** | `<projectPath>/Library/Pipeline/.unity-pipeline-port` |
| **Runtime** | `<workingDirectory>/.unity-pipeline-runtime-port` |

The Editor descriptor lives under the git-ignored `Library/` folder. The file is created with permissions restricted to the current user (it carries the auth token). The server rewrites it on every heartbeat to refresh `lastHeartbeat`, and deletes it on shutdown.

The Runtime descriptor is written next to the Player, at `{Application.dataPath}/..`:

- **Windows / Linux** — beside the executable, e.g. `C:\Builds\MyGame\.unity-pipeline-runtime-port`.
- **macOS** — `Application.dataPath` is `<Game>.app/Contents`, so the descriptor lands at the **`.app` bundle root**: `MyGame.app/.unity-pipeline-runtime-port`. (The file is inside the `.app` bundle.) This is why `unity command --runtime-path` takes the `.app` bundle path on macOS — see [Runtime connection & setup](runtime-setup.md).

> Test servers do **not** write a descriptor (they override `WritesDescriptor => false`) — the test already knows its port, so it never clobbers the live server's file. See [Tests architecture](testing.md).

### Editor descriptor fields

```json
{
  "pid": 12345,
  "port": 7800,
  "projectPath": "/path/to/Project",
  "projectName": "Project",
  "unityVersion": "6000.x.y",
  "mode": "editor",
  "startedAt": "2026-06-25T10:00:00Z",
  "lastHeartbeat": "2026-06-25T10:05:00Z",
  "evalToken": "<base64 token>"
}
```

| Field | Meaning |
|-------|---------|
| `pid` | Process id of the Unity Editor. |
| `port` | Port the server is listening on. |
| `projectPath` | Absolute path to the Unity project. |
| `projectName` | Project folder name. |
| `unityVersion` | Editor version string. |
| `mode` | `"editor"` or `"batchmode"`. |
| `startedAt` | When the instance started (UTC). |
| `lastHeartbeat` | Last heartbeat (UTC), refreshed on status calls. |
| `evalToken` | Bearer token for authenticating requests. |

### Runtime descriptor fields

The runtime descriptor shares `pid`, `port`, `unityVersion`, `startedAt`, `lastHeartbeat`, and `evalToken`, and adds:

| Field | Meaning |
|-------|---------|
| `platform` | Unity runtime platform (e.g. `WindowsPlayer`). |
| `buildGuid` | Unique build identifier (`Application.buildGUID`). |
| `workingDirectory` | Directory the Player is running from. |

(The runtime descriptor carries `platform`/`buildGuid`/`workingDirectory` in place of the editor's `projectPath`/`projectName`/`mode`.)

## Authentication

Every request must authenticate with a bearer token:

```
Authorization: Bearer <evalToken>
```

- The server **generates the token at startup** (`SecurityTokenManager.GetOrCreateToken()` — 256 bits of CSPRNG output, base64-encoded, held in memory and regenerated after each domain reload).
- The token is published in the descriptor's `evalToken` field.
- The server validates the bearer token on **every** request (before routing) using a constant-time comparison. A missing or wrong token returns `401 Unauthorized`.

## Discovering and calling an instance

A client connects by:

1. Reading the descriptor file (editor: `Library/Pipeline/.unity-pipeline-port`).
2. Taking the `port` and `evalToken` from it.
3. Sending requests to `http://127.0.0.1:<port>/...` with `Authorization: Bearer <evalToken>`.

Endpoints exposed by the server include `/api/status`, `/api/editor_status`, `/api/commands` (lists available commands), `/api/exec` (POST — runs a command), and `/api/test-status`.

## See also

- [Runtime connection & setup](runtime-setup.md) — enabling the server in a Player build.
- [Creating commands](creating-commands.md) — authoring the commands clients call.
