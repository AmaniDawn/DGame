# Profiler capture commands: working notes

Overview for people: [README.md](README.md). Parameters: `Documentation~/commands/profiling-and-memory.md`.

## Keep current
Change a command, parameter, response field, threading rule or any fact below? Update this file,
README.md and the docs page in the same change. Delete facts that stop being true; don't annotate them.

## Sessions
- The Editor has one Profiler history and `ProfilerDriver.LoadProfile` replaces it. Many sessions can be open, one is resident; using another evicts and reloads. Managed caches survive eviction.
- Every capture command returns `busy` while `profiler_record` runs.
- `sampleId` = `RawFrameDataView` sample index within one (frame, thread). Stable across reloads; valid for the session's lifetime. Objects carrying it also carry `frame` and `thread`.
- First load needs `confirm`; reloads between own sessions are silent; reloading over unowned frames fails.
- The first frame is skipped by default (usually partial) and logged at info level.

## Threading
- Async commands run Profiler API calls (views, `MakeResident`; throws off the main thread) on the main thread in 8 ms slices (`AnalysisSlicer`), with an Editor tick between.
- Only `RawFrameDataView.GetSample*` is thread safe. `MarkerAggregator.WalkThread` and `RawCallTree.Build` run on workers.
- Hierarchy views are not thread safe: GPU walks and `profiler_sample` stay on the main thread.
- After every resume the slicer checks closed, stale, residency, load generation and recording. Any change fails the command ("rerun"); never return or cache partial results.
- `CommandContext.Current` is `[ThreadStatic]`: capture it before the first `await`.
- `BatchCommand` rejects `Task`-returning commands.

## Data quirks
- `CPU Main/Render Thread Active Time` counters have no unit metadata but hold nanoseconds. Other units come from `metadataInfo[0].unit`.
- GPU frame time is read at frame+4. Per-sample GPU time exists only in hierarchy columns.
- A GPU marker needs `CommandBuffer.BeginSample(ProfilerMarker)` with `MarkerFlags.SampleGPU`; `BeginSample(string)` records none.
- `HierarchyFrameDataView` default mode doesn't merge siblings; sessions use `MergeSamplesWithTheSameName`. A merged item's `sampleId` is its first raw sample; `instances` counts the merged ones.
- Marker descriptions come from `MarkersInformationProvider` by reflection. Null is omitted, never serialized.
- Sample objects: id is `entityId` (not `instanceId`); name and type come from `GetUnityObjectInfo`, because capture ids differ from the live Editor. A sample without an object inherits the nearest ancestor's (`objectsFrom`). The below-6000.4 branch of `SampleObjects` is compiled only by CI.
- GC callstacks print CoreCLR style: `Type::Method()`.
- `selfPercent` is rounded to 3 decimals (1 decimal sums to 97.4 over ~770 markers).
- Percentiles use the lower index, no interpolation; per-marker stats cover present frames only.

## Contracts and checks
- Response field names are frozen once the `analyze-profiler-captures` skill ships; add fields, don't rename.
- Commands are named in the skill's reference, so run `CommandNamesTests`, `ParameterNamesTests`, `CommandEffectTests` after any rename.
- Tests: `unity command test run --filter Profiler` (in `com.unity.pipeline.tests`, `Tests/Editor/Profiling/`).
- Verify live on a fixture capture:
  - A second `profiler_session` on the same file returns `loaded false`.
  - `sampleId` from `profiler_frame` or `profiler_markers` round-trips through `profiler_sample --sample-id`.
  - `profiler_markers --thread all` output is unchanged by aggregator changes.
  - `profiler_sample` at the thread root has `threadMs` > 0 and each child's `percentOfParent` equals its `percentOfThread`.
