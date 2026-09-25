# Runtime connection & setup

How to run the Pipeline server inside a built Player so a client can drive it the same way it drives the Editor. The runtime server only exists in **development builds**, so setup is mostly about building correctly.

## The `RuntimePipelineManager` component

The runtime server is owned by a `RuntimePipelineManager` MonoBehaviour. Add it to a scene via the component menu:

> **Add Component → Pipeline → Runtime Pipeline Manager**

It is `[DisallowMultipleComponent]` and survives scene loads (`DontDestroyOnLoad`); only one instance should exist. Its `Update` pumps the server's dispatcher each frame (a Player has no `EditorApplication.update` to do this) and drives the listener watchdog.

### Inspector fields

| Field | Default | Meaning |
|-------|---------|---------|
| `enableInBuilds` | `false` | Master switch. The server only starts when this is `true`. **Off by default for safety.** |
| `autoStart` | `true` | Start the server automatically in `Start()` (requires `enableInBuilds`). |
| `port` | `0` | Listen port. `0` = auto-assign from `7900`–`7949`. |
| `requestTimeoutMs` | `30000` | Per-request timeout. |
| `enableAuditLogging` | `true` | Log remote requests for auditing. |
| `maxWorkItemsPerFrame` | `10` | Dispatcher work items processed per frame. |

If `autoStart` is off you can start/stop manually with `StartServer()` / `StopServer()`.

## Development-build gating

The runtime server, code evaluation, and hot reload are all gated behind a compile-time guard:

```csharp
#if UNITY_EDITOR || (UNITY_STANDALONE && DEBUG)
```

`DEBUG` is defined for **standalone Development Builds** only. In a non-development (release) build the compilation/eval/hot-reload code is compiled out entirely, so it cannot run — even if `enableInBuilds` is `true`. To use the runtime server in a Player you therefore need **both**:

1. A **Development Build** (defines `DEBUG`), built for a **standalone** target (Windows/macOS/Linux).
2. `enableInBuilds = true` on the `RuntimePipelineManager`.

## Step-by-step: enable in a dev build

1. Add a **Runtime Pipeline Manager** component to a GameObject in your startup scene (component menu *Pipeline → Runtime Pipeline Manager*).
2. Tick **`enableInBuilds`** on that component. Leave `autoStart` on and `port` at `0` (auto).
3. Open **File → Build Settings** and enable **Development Build** for a **Standalone** platform.
4. Build and run the Player.
5. On start, the manager creates a `RuntimePipelineServer`, which binds the first free port in `7900`–`7949` and writes the runtime descriptor `.unity-pipeline-runtime-port` (with `port` and `evalToken`).
6. Connect a client using that port and token.

## Security

The runtime server applies the same protections as the editor server (see [Connectivity](connectivity.md)):

- It binds the **IPv4 loopback** (`http://127.0.0.1:<port>/`, plus the `localhost` hostname), so it is reachable only from the same machine and never exposed on a routable interface. Clients should connect to `127.0.0.1` explicitly — see [Loopback-only binding](connectivity.md#loopback-only-binding) for why `localhost` can resolve to the IPv6 loopback (`::1`), which Unity's Mono `HttpListener` cannot serve.
- Every request must present `Authorization: Bearer <evalToken>`; the token is generated at startup and published in the runtime descriptor.

Because the server exposes code evaluation and hot reload, only enable it in development/QA builds — never in a production build without additional safeguards.

## See also

- [Connectivity](connectivity.md) — ports, descriptor file, and auth in detail.
- [Hot reload](hot-reload.md) — applying live code changes to a running Player.
