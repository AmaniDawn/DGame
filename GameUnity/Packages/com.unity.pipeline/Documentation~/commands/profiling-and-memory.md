# Profiling & memory capture commands

Record a Profiler capture (`.data`) or take a memory snapshot (`.snap`) from a running Editor or a
connected development Player, then read a capture's CPU timings back without opening a window. Both
captures follow the `diagnostics run_audit` shape: the start command returns immediately, and you poll a
`*_status` command until it reports `completed`, then read the `path` it gives you.

Both capture from whatever the Editor's Profiler is connected to: the same connection the Profiler
window's target dropdown sets. Choose it with `diagnostics profiler_connect`, or leave it where it is.

```
unity command diagnostics profiler_targets
unity command diagnostics profiler_connect --target editor
unity command diagnostics profiler_record --frames 300
unity command diagnostics profiler_record_status
```

## Targets

### `diagnostics profiler_targets`
List what the Editor's Profiler can connect to: this Editor plus any connected development Players.
`current` marks what `diagnostics profiler_record` and `diagnostics memory_snapshot` capture from.

No parameters.

**Returns:** one record per target — `{ guid, name, isEditor, connectable, current, deepProfilingSupported }`.

**Notes:** `isEditor` means **this** Editor process, guid `-1`. A connection to another Editor is an
ordinary remote target. `deepProfilingSupported` is `false` for a Player built without Deep Profiling
Support. A Player attached through Autoconnect Profiler does not appear in Unity's own target list, so
the current connection is always listed even when Unity omits it.

### `diagnostics profiler_connect`
Point the Editor's Profiler connection at a target, as choosing it in the Profiler window does. The
connection stays there afterwards.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `target` | **yes** | `–` | `editor` for this Editor, or a guid or name from `diagnostics profiler_targets`. A name may be a unique part of one. |

**Returns:** `object` — `{ target, guid, switched }`. A switch logs at info level that the connection is settling.
Exits `NotFound` for an unknown target (the error lists the available ones), and `Failed` for a target that
is not connectable or that the Editor refused, with the reason on stderr.

**Notes:** Refused (exit `Failed`) while a recording or memory snapshot is in progress, because both read
from the connection for their whole run. A switched connection takes about 1.5 seconds to settle. A
recording or snapshot started in that window stays `queued` until it has, so you don't need to wait.

## Profiler recording

### `diagnostics profiler_record`
Record Unity Profiler frames from the connected target and save them to a `.data` file. Returns
immediately. Poll `diagnostics profiler_record_status` until status is `completed`, then read the path.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `frames` | no | `300` | Number of frames to record before stopping and saving. Use `0` to record until `diagnostics profiler_record_stop`. |
| `output-path` | no | `–` | Output `.data` path (absolute, or relative to the project root). Defaults to `<Profiler capture folder>/<productName>_<timestamp>_<captureId>.data`. Always written on the Editor machine, even when recording a Player. |
| `deep-profile` | no | `false` | Whether this is a deep-profiling recording. Must match the Profiler's Deep Profile setting; see [Deep profiling](#deep-profiling). |
| `timeout-seconds` | no | `300` | Stop and save what was recorded after this long. |
| `areas` | no | `–` | Comma-separated Profiler modules to turn on for this recording, as in the Profiler window: `CPU`, `GPU`, `Rendering`, `Memory`, `Audio`, `Video`, `Physics`, `Physics2D`, `NetworkMessages`, `NetworkOperations`, `UI`, `UIDetails`, `GlobalIllumination`, `VirtualTexturing`. `GPU` records per-sample GPU time; needs a graphics API that supports GPU recording. Modules not listed stay as they are; the listed ones go back to their previous setting when the recording ends. |
| `callstacks` | no | `–` | Comma-separated allocation call stacks to record, as in the Profiler window's Call Stacks menu: `GCAlloc`, `UnsafeUtilityMalloc`, `JobHandleComplete`, `NativeAlloc`, or `none`. Empty leaves the setting as it is; it goes back to its previous value when the recording ends. |

**Returns:** `object` — `{ status: "queued", captureId, path, target }`. Exits `InvalidUsage` for a bad
`frames`, `timeout-seconds`, `areas` or `callstacks`, and `Failed` while another recording or a snapshot runs
or when `deep-profile` disagrees with the Profiler's setting, with the reason on stderr.

**Notes:** One recording runs at a time, and not alongside a memory snapshot; a second call fails. Recording the **Editor** in Edit mode only advances frames as the Editor ticks, so keep
`editor set_auto_tick` enabled or the recording will time out with very few frames. While it records,
the Editor's Profiler records too, with its history cleared first. When the recording ends, Profiler
recording, the Editor's own profiling switch, and any module or call-stack setting it changed go back to
how they were, also when a domain reload ends the recording.

#### Frames recorded vs. frames saved

The Profiler keeps a bounded history — Preferences > Analysis > Profiler > Frame Count, 300 by default —
and the saved file holds what is in it. A recording that runs past that capacity, which includes any
`frames=0` recording left running, therefore loses its earliest frames.

The status reports both numbers: `framesRecorded` is what the recording observed and what `frames` is
measured against, `framesSaved` is what the file holds. When they differ the completed status says so in
`message`. Raise the Profiler frame count to keep more.

#### Deep profiling

Deep Profile is a single Profiler setting, and for the Editor it only takes effect through a script
reload, which would end the recording. This command therefore never changes it. `deep-profile` states
which kind of recording you expect, and the command fails at once if the setting disagrees, rather than
recording one kind and labelling it the other. Turn Deep Profile on or off in the Profiler window first.

A Player target also needs a development build made with Deep Profiling Support. The Editor otherwise
accepts deep profiling for a Player that ignores it, which would label an ordinary recording as deep.
The command checks `deepProfilingSupported` first and fails with a message naming the build option.

### `diagnostics profiler_record_stop`
Stop the recording early and save the frames recorded so far.

No parameters.

**Returns:** `ProfilerRecordStatus` (JSON) — the final, `completed` status, as `diagnostics profiler_record_status`
would report it, or `{ status: "idle" }` with a warning when nothing is queued or recording.

**Notes:** Saves before it returns. A recording stopped while still `queued`, or with no frames recorded,
exits `Failed` with the error on stderr rather than writing an empty file; `diagnostics profiler_record_status`
then reports it `failed`.

### `diagnostics profiler_record_status`
Get the status of the last Profiler recording: `idle` | `queued` | `recording` | `completed` | `failed` |
`interrupted`.

No parameters.

**Returns:** `ProfilerRecordStatus` (JSON) — `captureId`, `path`, `target`, `targetIsEditor`,
`framesRequested` and `deepProfile` throughout. `framesRecorded`, `framesSaved` and `elapsedSeconds`
while recording and once completed. `areas` (the Profiler modules on) and `callstacks` (the allocation call
stacks on) once recording has started. `sizeBytes` when `completed`, `error` when `failed`.

**Notes:** `MainThreadRequired = false`, so it answers while the main thread is busy. The status is
refreshed about twice a second while recording. `interrupted` means a domain reload ended the
recording — a script change, or entering Play mode with domain reload enabled — and nothing was saved.
The Profiler's recording and Editor profiling switches, and any module or call-stack setting the recording changed, are put back to what they were before the
recording started. To record Play mode, enter it
first, then start the recording.


## Capture analysis

These commands analyze a saved `.data` capture, such as one from `diagnostics profiler_record`. Open a session
with `diagnostics profiler_session`, pass the `sessionId` to every other command, and close it with
`diagnostics profiler_close`. A command that cannot answer writes no result and exits non-zero with the reason on
stderr: `NotFound` for an unknown session, a missing capture file, or a frame, thread, marker or sample that is not
there; `InvalidUsage` for an unknown `sort` or a load without `confirm`; `Failed` while a recording or snapshot
owns the Profiler, and for anything else. Times are milliseconds rounded to 3 decimals, sizes are bytes.

### Capture sessions

The Editor holds one Profiler history, and loading a capture replaces it. Any number of sessions can be open, but
only one is **resident**, meaning its capture is the one in the Profiler. Using another session reloads that
session's capture and releases the previous one's frame views; what was already computed is kept, so repeated
calls are cheap. Because of this, opening or using a session **replaces whatever the Profiler window shows**,
and every capture command fails while `diagnostics profiler_record` is running. A `sampleId` is a raw
sample index within one frame and thread, so it stays valid across reloads and can be passed between
`diagnostics profiler_frame`, `diagnostics profiler_markers` and `diagnostics profiler_sample`. Sessions end on
`diagnostics profiler_close` or a domain reload, and the cached
results are discarded if the capture file changes.

`diagnostics profiler_summary`, `diagnostics profiler_frame`, `diagnostics profiler_markers` and
`diagnostics profiler_counters` run in short slices with Editor ticks in between, so the Editor keeps responding while they read a capture. Loading a capture is the one
step that cannot be split, so opening a session, or the first command after another session was used, still blocks
the Editor once while the capture loads. If the Profiler's history changes during a run (a capture is loaded or
cleared, or frames are recorded), the command exits `Failed` with an error asking you to run it again, and
returns nothing partial. A long run on a large capture can pass the default timeout: pass `--timeout`, or `--detach`
and poll the job. These commands are not batchable.

Markers in results carry `description`, the Profiler's note on what the marker means and what to investigate.
Read it before choosing the next step. It is omitted when the Profiler has none for that marker.

Default frame range: the capture's second frame to its last, because the first frame is usually partial. An
info-level log line says so; pass `first-frame` to include it. Frames skipped for having no data, a GPU
ranking with no GPU timing, and a frame with no hot path after exclusions are reported as warnings on stderr. Statistics follow the Profile Analyzer: percentiles
take the lower index without interpolation, and a marker's mean, median, min and max are over the frames where
it appears.

### `diagnostics profiler_session`
Open a capture session on a saved `.data` capture, or list the open sessions.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `capture` | no | `–` | Path to a `.data` capture (absolute, or relative to the project root). Omit to list the open sessions. |
| `confirm` | no | `false` | Apply the load, replacing the Editor's current Profiler history. Without it the call is refused. Not needed when the capture is already loaded. |
| `dry-run` | no | `false` | Preview the capture and the Profiler history it would replace, without loading anything. |

Loading replaces the Editor's Profiler history, which is not recoverable: a capture open in the Profiler window or an
unsaved recording is discarded. The command refuses while a recording or a memory snapshot is running. If another
command needs to reload a capture while the Profiler holds frames that no open session owns, it returns an error asking
you to run `diagnostics profiler_session` with `confirm`.

**Returns:** `object` — with `dry-run`: `{ status: "dry_run", capture, sizeBytes, wouldReplace: { empty, firstFrame, lastFrame, frameCount } }`. With `capture`: `{ sessionId, capture, loaded, captureKind, frameRange: { first, last, count }, threads: [{ index, name, group }], markerDescriptions }`.
`loaded` is `true` when this call loaded the capture into the Profiler. `captureKind` is `editor` or `player`.
`markerDescriptions` is `false` on an Editor with no marker description table. Reopening the same file returns
its existing session. Without `capture`: one record per open session, `{ sessionId, capture, resident }`, and
nothing when none is open. Loading without `confirm` exits `InvalidUsage`; a missing file exits `NotFound`.

### `diagnostics profiler_close`
Close a capture session, releasing its cached frame views and Profiler event subscriptions.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId`, or `all`. |

**Returns:** `object` — `{ closed }`. The Profiler keeps whatever frames it holds. An unknown session exits `NotFound`.

### `diagnostics profiler_summary`
Summarize a capture session over a frame range: frame-time and GC statistics, frames over budget, hiccups (frames far above the median CPU time), the median frame, and the frames ranked by CPU, GC, GPU or thread active time.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId`. |
| `first-frame` | no | second frame | First frame to analyze. |
| `last-frame` | no | last frame | Last frame to analyze. |
| `target-frame-ms` | no | Profiler Target FPS | Frame budget in ms. `0` uses the Profiler's Target FPS (60 when unavailable). |
| `spikes` | no | `5` | How many frames to list, ranked by `sort`. |
| `sort` | no | `cpu` | `cpu`, `gc`, `gpu`, `main` or `render`; ranks the frames listed in `spikes`. |
| `hiccup-factor` | no | `2` | A frame is a hiccup when its CPU time is at least this many times the median. |

**Returns:** `object` — `{ sessionId, capture, loaded, captureKind, frameRange, analyzedRange: { first, last, count }, sort,
cpuFrameMs, mainActiveMs, renderActiveMs, gpuFrameMs, gcAllocBytes, budget: { targetFrameMs, framesOverBudget, percentOverBudget },
medianFrame, hiccupFrames, gcFrames, spikes }`. Each statistic is `{ min, p25, median, p75, p95, max, mean }`, or `null` when the capture
has no such series. A spike is `{ frame, cpuMs, mainActiveMs, renderActiveMs, gpuMs, gcAllocBytes, bottleneck, overBudget, hiccup }`,
where `bottleneck` is `gpu`, `cpu`, `none`, or `unknown`. `hiccupFrames` and `gcFrames` count over every analyzed frame.
Frames that do not allocate are not listed by `sort=gc`.

**Notes:** `gpuMs` is the frame's GPU time, recorded four frames later, so it is `null` for the last frames.

### `diagnostics profiler_frame`
Break down one frame: timings, bottleneck, the hot path through each thread's call tree, and the samples with the most
self time and the most direct GC allocation.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId`. |
| `frame` | no | slowest | Frame index. Defaults to the slowest frame by CPU frame time (by GPU time with `domain` `gpu`) in the default range. |
| `thread` | no | `Main Thread` | Thread name, or `all`. With `domain` `gpu` the default means every thread that has GPU samples; pass `all` or a thread name to be explicit. |
| `domain` | no | `cpu` | `cpu`, or `gpu` to rank and trace GPU time per sample instead of CPU time. |
| `limit` | no | `10` | How many entries to list in each of `hotPaths`, `topSelf` and `topGc`. |
| `exclude` | no | `–` | Comma-separated marker names (case-sensitive) to leave out of `hotPaths` and `topSelf`. `Idle`, `Semaphore.WaitForSignal` and `GC.Alloc` are always left out. |
| `target-frame-ms` | no | Profiler Target FPS | Frame budget in ms. |

**Returns:** `object` — `{ sessionId, domain, frame, frameSelectedBy, loaded, timing, threads, hotPaths, topSelf, topGc, excluded }`.
`frameSelectedBy` is `slowest` or `argument`. `timing` is `{ cpuMs, mainActiveMs, renderActiveMs, gpuMs, gcAllocBytes, bottleneck, targetFrameMs }`.
A `hotPaths` entry is `{ thread, threadMs, steps }`, ordered by the heaviest thread first. Each step is
`{ sampleId, name, instances, totalMs, selfMs, percentOfThread, percentOfParent, hotLeaf, description }`: the heaviest
child of the previous step, with same-named siblings merged (`instances` counts them) and `sampleId` pointing at the
longest instance. The path ends at the first step whose own time is at least that of its heaviest child (`hotLeaf`).
`topSelf` entries are `{ name, category, calls, selfMs, totalMs, gcAllocBytes, sampleId, thread, heaviestSelfMs, path, description }`
and `topGc` entries `{ name, category, calls, gcAllocBytes, sampleId, thread, heaviestGcBytes, path, description }`;
`sampleId` is the heaviest single instance in this frame.

With `domain` `gpu` the same shape describes GPU time: the default frame is the one with the most GPU time, the default
thread means every thread that has GPU samples, a step's `totalMs` and `selfMs` are GPU milliseconds, and the hierarchy
merges same-named samples, so a `sampleId` is the first raw sample of the merged item. `topSelf` entries are
`{ name, instances, selfMs, sampleId, thread, heaviestSelfMs, path, description }` summed by marker name, and `topGc` is empty.
Exits `Failed` when this Editor's Profiler API has no GPU hierarchy columns.

**Notes:** A `sampleId` is a raw sample index within one frame and thread, so pass it to
`diagnostics profiler_sample --sample-id` with the response's `frame` and the entry's `thread`.

### `diagnostics profiler_markers`
Aggregate CPU or GPU markers over a frame range, as the Profile Analyzer does.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId`. |
| `first-frame` | no | second frame | First frame to analyze. |
| `last-frame` | no | last frame | Last frame to analyze. |
| `thread` | no | `Main Thread` | Thread name, or `all` to rank leaf cost across every thread. With `domain` gpu the default means every thread that has GPU samples; pass `all` or a thread name to be explicit. |
| `sort` | no | `self` | `self`, `total`, `gc`, or `calls`; with `domain` gpu, `self` or `total`. Self, total and gc rank by cost per analyzed frame. |
| `filter` | no | `–` | Case-insensitive substring of the marker name. |
| `exclude` | no | `–` | Comma-separated marker names (case-sensitive) to leave out of the ranking and of `selfPercent`, e.g. `WaitForTargetFPS,Gfx.WaitForPresentOnGfxThread`. `Idle`, `Semaphore.WaitForSignal` and `GC.Alloc` are always left out. |
| `limit` | no | `25` | Maximum markers to return. |
| `domain` | no | `cpu` | `cpu`, or `gpu` to aggregate GPU time per marker instead of CPU time. Needs a capture recorded with the GPU module. |

**Returns:** one record per marker, ranked by `sort` —
`{ name, category, description, framesPresent, calls, selfPercent, selfMs, totalMs, gcAllocBytes, heaviest, heaviestGc }`.
The analyzed range, session and thread, and the `exclude` names that matched a marker, are logged at info level.
`selfMs` and `totalMs` are `{ sum, mean, median, min, max, perAnalyzedFrame }` (`totalMs` also has `maxFrame`);
`gcAllocBytes` is `{ sum, mean, max }`. `selfPercent` is the marker's share of the self time of all markers left
after exclusions, before `filter` and `limit`. `heaviest` is `{ frame, thread, sampleId, selfMs }`, the single
instance with the most self time; `heaviestGc` is `{ frame, thread, sampleId, gcAllocBytes }` for the instance that
allocated the most, present only when the marker allocates. Pass them to `diagnostics profiler_sample`.

**Notes:** Self time is a sample's duration minus its direct children. GC bytes are attributed to the marker that
called the allocation. A recursive marker's total counts only its outermost call. `Semaphore.WaitForSignal`,
`Idle` and `GC.Alloc` are left out.

#### With `domain gpu`

Aggregates GPU time per marker instead of CPU time. `thread` defaults to every thread that has GPU samples (both
`Main Thread` and `all` mean that); name another thread to walk only it. `sort` is `self` or `total`; `gc` and `calls`
are refused. `filter`, `exclude` and `limit` work as for CPU.

**Returns:** one record per marker, ranked by `sort`. The analyzed range, session, GPU threads, GPU frame-time statistics and
the `exclude` names that matched a marker are logged at info level.
A marker is `{ name, description, threads, framesPresent, selfPercent, gpuSelfMs, gpuTotalMs, heaviest }`, where `gpuSelfMs`
and `gpuTotalMs` are `{ sum, mean, median, max, maxFrame }` over the frames the marker appears in. A total counts only the
outermost occurrence of a name. `selfPercent` is the marker's share of the GPU self time of all markers left after
exclusions, before `filter` and `limit`. `heaviest` is `{ frame, thread, sampleId, gpuSelfMs }`, the single instance with
the most GPU self time; pass it to `diagnostics profiler_sample`. Exits `Failed` when this Editor's
Profiler API has no GPU hierarchy columns.

**Notes:** The capture needs the GPU Usage module enabled (see `areas` on `diagnostics profiler_record`) and a graphics API
that supports GPU recording; without GPU samples no record is written and a warning says so. GPU time per sample only exists in
hierarchy views, so the command builds one per frame per thread: a range over 300 frames is slow, and a warning says so.
Narrow `first-frame` and `last-frame`, or set `thread`.

### `diagnostics profiler_sample`
Read one sample (a node of the merged call tree) of a capture session (`diagnostics profiler_session`), found by
marker name path or `sampleId`, with its children and their share of it, for one frame and thread.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId` from `diagnostics profiler_session`. |
| `frame` | no | last frame | Frame index to read. An out-of-range value fails, and the error gives the available frame range. |
| `thread` | no | main thread | Thread name to read, e.g. `Main Thread`. An unknown name fails, and the error lists the available thread names. |
| `marker` | no | thread root | Marker name path to drill into, `/`-separated, e.g. `PlayerLoop/Update.ScriptRunBehaviourUpdate`. The default returns the top-level markers. |
| `max-children` | no | `25` | Maximum child markers to return under the selected node, sorted by total time descending. |
| `sample-id` | no | `–` | A `sampleId` (raw sample index) from `diagnostics profiler_frame`, `diagnostics profiler_markers` or an earlier `diagnostics profiler_sample`, for the same frame and thread. Overrides `marker`. |
| `callstacks` | no | `false` | Also return the GC allocations (bytes and callstack) made directly under the selected marker. |
| `related-thread` | no | `–` | Also return the samples on this thread that overlap in time with the selected marker. Needs a marker or `sample-id`. |

**Returns:** `object` — `{ sessionId, capture, loaded, frame, frameRange: { first, last }, thread, threadMs, marker, children }`.
`threadMs` is the thread's total time in the frame. `marker` is `null` when reading the thread root. `marker` and each
`children` entry carry `sampleId` (the first raw sample of that node), `instances` (how many same-marker samples were
merged into it), `name`, `totalMs`, `selfMs`, `calls`, `gcAllocBytes`, `percentOfThread`, `hasChildren`, and
`description` (the Profiler's own note on what the marker means and what to check next; omitted when it has none).
`percentOfThread` is the node's `totalMs` over `threadMs`. Each child also has `percentOfParent`: its `totalMs` over the
selected node's, or over `threadMs` when reading the thread root (`marker` has `percentOfParent: null`). Follow the
child with the largest `percentOfParent` to walk down to the cost.

`objects` lists up to 5 distinct Unity objects the sample ran for, as `{ entityId, name, type, gameObject: { entityId, name } }`;
`objectCount` gives how many there were. Unity records the object on the sample that ran for it, such as a script's
`Update`. Samples below it inherit that sample's objects, and `objectsFrom` gives the path of the sample they came from.
Names and types are read from the capture. `entityId` is the id in the recording session: a capture loaded into another
Editor session has different ids, so find the object in the project by `gameObject.name`, `name` and `type`, not by
`entityId`. Both fields are omitted when neither the sample nor any parent has an object.

Lookups that miss write no result and exit `NotFound`, with an error on stderr: it gives the frame range for an
out-of-range frame, the available threads for an unknown thread, and the child names where the path stopped for an
unknown marker. A `sample-id` that is not in that frame and thread, and an unknown session, also exit `NotFound`; the
command exits `Failed` while a Profiler recording is in progress.

With `callstacks`, the response also has `allocations: [{ bytes, callstack }]` (up to 20 GC allocations made directly under the
selected marker), `allocationsTotal`, `allocationsBytes`, and `callstackNote` when none of them has a callstack (enable
Call Stacks > GC.Alloc in the Profiler window and record again). With `related-thread`, it also has
`related: { thread, samples: [{ name, description, depth, startOffsetMs, durationMs, overlapMs }] }`: the depth 1-2 samples on that thread
that overlap in time with the selected marker's instances, most overlap first (up to `max-children`), with `startOffsetMs` measured from
the first instance's start. `related-thread` without a marker or `sample-id`, or with an unknown thread, returns `{ error }`
(plus `availableThreads` for the latter).

**Notes:** An Editor capture's main thread starts at `EditorLoop` (and `PlayerLoop` in Play mode), so a marker
path for one begins there. See [Capture sessions](#capture-sessions) for what reading does to the Profiler.

### Analysis workflow

1. **Which frames hiccup or allocate?** `diagnostics profiler_summary --sort cpu|gc|gpu|main|render` ranks the frames it lists in `spikes` and flags hiccups.
2. **Why is this frame slow?** `diagnostics profiler_frame --frame N` returns `hotPaths`, the chain of heaviest
   calls down to the leaf that dominates each thread. Pass a step's `sampleId` to `diagnostics profiler_sample`.
   Add `--domain gpu` to trace GPU time instead.
3. **What costs the most over many frames?** `diagnostics profiler_markers --thread all --exclude <waits>` ranks
   leaf (self) time across a frame range; follow `heaviest.sampleId` to the worst instance. For GPU add
   `--domain gpu`, which has the same `exclude`, `selfPercent` and `heaviest`.

### `diagnostics profiler_counters`
List the counters recorded in a capture, or summarize chosen counters over a frame range.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `session` | **yes** | `–` | A `sessionId`. |
| `counters` | no | `–` | Comma-separated counter names, e.g. `GC Allocated In Frame,CPU Main Thread Active Time`. A name shared by several categories returns each; write `Category/Name` to pick one. Empty lists the available counters. |
| `first-frame` | no | second frame | First frame to analyze. |
| `last-frame` | no | last frame | Last frame to analyze. |
| `per-frame` | no | `false` | Also return each counter's value on every analyzed frame, up to the first 500 frames of the range. |

**Returns:** `object` — listing: `{ sessionId, loaded, counters: [{ name, category, unit }] }`, sorted by category then name.
Statistics: `{ sessionId, loaded, analyzedRange, counters: [{ name, category, unit, stats, values? }], unknown }`, where
`stats` is `{ min, p25, median, p75, p95, max, mean }`, `values` is `[{ frame, value }]` with `valuesTotal` and
`valuesTruncated` beside it, and `unknown` lists requested names the capture does not have, each also logged as a warning. `stats` always covers the whole range.

**Notes:** Time counters are converted to `ms`; other units (`Bytes`, `Count`, ...) are reported as recorded. Values are
read from the main thread, where Unity's built-in counters are emitted.

## Memory snapshot

### `diagnostics memory_snapshot`
Capture a memory snapshot (`.snap`) of the connected target. Returns immediately. Poll
`diagnostics memory_snapshot_status` until status is `completed`, then read the path.

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `flags` | no | see below | Comma-separated capture flags: `ManagedObjects`, `NativeObjects`, `NativeAllocations`, `NativeAllocationSites`, `NativeStackTraces`. Defaults to the Memory Profiler package's configured flags, else `ManagedObjects,NativeObjects`. |
| `output-path` | no | `–` | Output `.snap` path (absolute, or relative to the project root). Defaults to `<snapshot folder>/<productName>_<timestamp>_<snapshotId>.snap`. Always written on the Editor machine, even for a Player. |
| `collect-garbage` | no | `true` | Run `GC.Collect` before capturing, so entirely empty managed heap pages get unmapped. Editor only. |
| `timeout-seconds` | no | `600` | Give up waiting for the capture after this long. Raise it for slow remote connections. |

**Returns:** `object` — `{ status: "queued", snapshotId, path, target }`. Exits `InvalidUsage` for an unknown flag
or a non-positive `timeout-seconds`, and `Failed` while a recording or another snapshot runs.

**Notes:** An unknown flag name is rejected with an error listing the valid ones. The callstack flags
capture nothing unless you started the target Editor or Player with `-enable-memoryprofiler-callstacks`.
`collect-garbage` is ignored for a Player, where it would collect the **Editor's** heap instead; the
`capturing` status says so in `message`. Snapshots are large, so expect hundreds of megabytes for a real
project. A snapshot does not run alongside a Profiler recording; the second call fails.

### `diagnostics memory_snapshot_status`
Get the status of the last memory snapshot: `idle` | `queued` | `capturing` | `completed` | `failed` |
`interrupted`.

No parameters.

**Returns:** `MemorySnapshotStatus` (JSON) — `path` and `sizeBytes` are populated when `completed`,
`error` when `failed`.

**Notes:** `MainThreadRequired = false`. A snapshot that passes `timeout-seconds` reports `failed`, but
because the underlying operation may still be running, further snapshots, Profiler recordings and
`diagnostics profiler_connect` stay refused until it finishes or a domain reload clears it. If it then
finishes, the status changes to its real result. `interrupted` means a domain reload landed while a
snapshot was `queued` or `capturing`.

## When the connection moves

The Profiler window can change the connection at any time, and nothing here can stop that. A snapshot
re-checks the connection immediately before it captures. A recording re-checks it before it starts, on
every poll while it records, and again immediately before the file is written. A connection that moved
fails the command, discarding any frames recorded so far, rather than saving another process's data
under the target that was asked for.

---

See [Creating commands](../creating-commands.md) and [Connectivity](../connectivity.md).
