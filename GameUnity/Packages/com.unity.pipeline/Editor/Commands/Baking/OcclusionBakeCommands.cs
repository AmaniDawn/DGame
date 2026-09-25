using System;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.Baking
{
    /// <summary>
    /// CLI-215 Group C — Occlusion Culling bake commands. Trigger an async occlusion bake of the open
    /// scene(s) via <see cref="StaticOcclusionCulling.GenerateInBackground"/>, poll its status, cancel
    /// it, and clear the baked occlusion data.
    ///
    /// Async pattern mirrors <see cref="BuildCommand"/> / <see cref="LightingBakeCommands"/>: trigger
    /// returns immediately; completion is detected by <see cref="StaticOcclusionCulling.isRunning"/>
    /// flipping false (polled in <see cref="EditorApplication.update"/>); in-flight state is held in
    /// static fields and reconciled against a Temp/ status file across domain reloads.
    /// </summary>
    [InitializeOnLoad]
    public static class OcclusionBakeCommands
    {
        const string StatusFile = "Temp/pipeline_occlusion_bake_status.json";

        // Sentinel for "argument omitted" on the float bake parameters. float.NaN is not a
        // compile-time constant (so it can't be a default parameter value), but float.MinValue is, and
        // it is not a plausible real value for any of these positive bake parameters.
        const float Unset = float.MinValue;

        static readonly object s_Lock = new object();
        static bool s_Tracking;
        static string s_BakeId;
        static DateTime s_StartedUtc;

        static OcclusionBakeCommands()
        {
            OnReload();
            EditorApplication.update += OnUpdate;
        }

        #region bake_occlusion_culling

        [CliCommand("bake_occlusion_culling", "Trigger an async occlusion-culling bake of the open scene(s) via StaticOcclusionCulling.GenerateInBackground(). Returns immediately; poll occlusion_bake_status until completed.")]
        public static object BakeOcclusionCulling(
            [CliArg("smallest_occluder", "Smallest object that will occlude others (meters). Defaults to Unity's current value.")] float smallestOccluder = Unset,
            [CliArg("smallest_hole", "Smallest gap geometry can have that the view can see through (meters). Defaults to Unity's current value.")] float smallestHole = Unset,
            [CliArg("backface_threshold", "Backface threshold (1-100); lower trims more backfaces. Defaults to Unity's current value.")] float backfaceThreshold = Unset,
            [CliArg("confirm", "Accepted for parity (a bake overwrites existing occlusion data); not required.")] bool confirm = false,
            [CliArg("dry_run", "If true, validate there is an open scene and report the parameters that would be used without baking.")] bool dryRun = false)
        {
            if (!BakeSceneGuard.HasBakeableScene())
                return BakeSceneGuard.NoSceneResult();

            OnUpdate(); // reconcile any stale tracking flag first.

            if (StaticOcclusionCulling.isRunning)
                return new { code = "bake_in_progress", message = "An occlusion bake is already running. Poll occlusion_bake_status." };

            // Validate explicitly provided values before resolving. An omitted arg is the Unset
            // sentinel (float.MinValue); NaN is never <= Unset, so a NaN arg is treated as provided
            // and rejected here. smallest_occluder / smallest_hole must be positive; backface_threshold
            // is documented as 1-100.
            if (!IsOmitted(smallestOccluder) && !(smallestOccluder > 0f))
                throw new ArgumentException($"smallest_occluder must be greater than 0 (got {smallestOccluder}).");
            if (!IsOmitted(smallestHole) && !(smallestHole > 0f))
                throw new ArgumentException($"smallest_hole must be greater than 0 (got {smallestHole}).");
            if (!IsOmitted(backfaceThreshold) && !(backfaceThreshold >= 1f && backfaceThreshold <= 100f))
                throw new ArgumentException($"backface_threshold must be between 1 and 100 (got {backfaceThreshold}).");

            // Resolve effective parameters: an omitted arg (the Unset sentinel) keeps Unity's current value.
            var occluder = IsOmitted(smallestOccluder) ? StaticOcclusionCulling.smallestOccluder : smallestOccluder;
            var hole = IsOmitted(smallestHole) ? StaticOcclusionCulling.smallestHole : smallestHole;
            var backface = IsOmitted(backfaceThreshold) ? StaticOcclusionCulling.backfaceThreshold : backfaceThreshold;

            if (dryRun)
            {
                return new
                {
                    status = "dry_run",
                    wouldBake = true,
                    parameters = new { smallestOccluder = occluder, smallestHole = hole, backfaceThreshold = backface }
                };
            }

            // Apply the (possibly overridden) bake parameters before generating.
            StaticOcclusionCulling.smallestOccluder = occluder;
            StaticOcclusionCulling.smallestHole = hole;
            StaticOcclusionCulling.backfaceThreshold = backface;

            var bakeId = Guid.NewGuid().ToString("N");
            lock (s_Lock)
            {
                s_Tracking = true;
                s_BakeId = bakeId;
                s_StartedUtc = DateTime.UtcNow;
            }

            WriteStatus(new OcclusionBakeStatus
            {
                Status = "baking",
                BakeId = bakeId,
                StartedUtcTicks = s_StartedUtc.Ticks
            });

            StaticOcclusionCulling.GenerateInBackground();
            return new { status = "baking", bakeId };
        }

        [CliCommand("occlusion_bake_status", "Get the status of the last occlusion bake: idle | baking | completed.", MainThreadRequired = false)]
        public static string OcclusionBakeStatus()
        {
            if (File.Exists(StatusFile))
                return File.ReadAllText(StatusFile);
            return "{\"status\":\"idle\"}";
        }

        [CliCommand("cancel_occlusion_bake", "Cancel an in-progress occlusion bake (StaticOcclusionCulling.Cancel()).")]
        public static object CancelOcclusionBake()
        {
            var wasRunning = StaticOcclusionCulling.isRunning;
            if (wasRunning)
                StaticOcclusionCulling.Cancel();

            lock (s_Lock)
            {
                if (s_Tracking)
                {
                    var ms = (int)Math.Max(0, (DateTime.UtcNow - s_StartedUtc).TotalMilliseconds);
                    WriteStatus(new OcclusionBakeStatus
                    {
                        Status = "completed",
                        Result = "cancelled",
                        BakeId = s_BakeId,
                        BakeTimeMs = ms
                    });
                    s_Tracking = false;
                }
            }

            return new { cancelled = wasRunning };
        }

        [CliCommand("clear_occlusion_culling", "Clear baked occlusion-culling data for the open scene(s). Destructive: requires confirm=true.")]
        public static object ClearOcclusionCulling(
            [CliArg("confirm", "Must be true to actually clear (destructive, not undoable via Unity's Undo).")] bool confirm = false,
            [CliArg("dry_run", "If true, report what would be cleared without clearing.")] bool dryRun = false)
        {
            if (!confirm)
                throw new ArgumentException("Refusing to clear occlusion culling data. Pass confirm=true (destructive, not undoable via Unity's Undo).");

            if (dryRun)
                return new { status = "dry_run", wouldClear = true };

            StaticOcclusionCulling.Clear();
            return new { cleared = true };
        }

        // True when the caller omitted a float bake arg (left it at the Unset sentinel). NaN is never
        // <= Unset, so NaN counts as provided and is caught by the range validation above.
        static bool IsOmitted(float value) => value <= Unset;

        #endregion

        #region completion / reload bookkeeping

        static void OnUpdate()
        {
            lock (s_Lock)
            {
                if (s_Tracking && !StaticOcclusionCulling.isRunning)
                    Finish("succeeded");
            }
        }

        // Must be called under s_Lock.
        static void Finish(string result)
        {
            var ms = (int)Math.Max(0, (DateTime.UtcNow - s_StartedUtc).TotalMilliseconds);
            WriteStatus(new OcclusionBakeStatus
            {
                Status = "completed",
                Result = result,
                BakeId = s_BakeId,
                BakeTimeMs = ms,
                Stats = result == "succeeded"
                    ? new OcclusionBakeStats { UmbraDataSizeBytes = StaticOcclusionCulling.umbraDataSize }
                    : null
            });
            s_Tracking = false;
        }

        static void OnReload()
        {
            if (!File.Exists(StatusFile))
                return;

            OcclusionBakeStatus prior;
            try
            {
                prior = JsonConvert.DeserializeObject<OcclusionBakeStatus>(File.ReadAllText(StatusFile));
            }
            catch
            {
                return;
            }

            if (prior == null || prior.Status != "baking")
                return;

            lock (s_Lock)
            {
                s_BakeId = prior.BakeId;
                s_StartedUtc = prior.StartedUtcTicks > 0 ? new DateTime(prior.StartedUtcTicks, DateTimeKind.Utc) : DateTime.UtcNow;
                s_Tracking = true;
                if (!StaticOcclusionCulling.isRunning)
                    Finish("succeeded");
            }
        }

        #endregion

        static void WriteStatus(OcclusionBakeStatus status)
        {
            try
            {
                File.WriteAllText(StatusFile, JsonConvert.SerializeObject(status));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OcclusionBake] Failed to write status file: {ex.Message}");
            }
        }
    }

    /// <summary>Status/result payload for <c>bake_occlusion_culling</c> / <c>occlusion_bake_status</c>.</summary>
    [Serializable]
    public class OcclusionBakeStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("bakeId")]
        public string BakeId { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public string Result { get; set; }

        [JsonProperty("bakeTimeMs")]
        public int BakeTimeMs { get; set; }

        [JsonProperty("stats", NullValueHandling = NullValueHandling.Ignore)]
        public OcclusionBakeStats Stats { get; set; }

        [JsonProperty("startedUtcTicks")]
        public long StartedUtcTicks { get; set; }
    }

    /// <summary>Post-bake occlusion statistics (umbra data size in bytes).</summary>
    [Serializable]
    public class OcclusionBakeStats
    {
        [JsonProperty("umbraDataSizeBytes")]
        public long UmbraDataSizeBytes { get; set; }
    }
}
