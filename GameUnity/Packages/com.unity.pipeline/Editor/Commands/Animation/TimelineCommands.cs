using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.Pipeline.Editor.Commands.Animation
{
    /// <summary>
    /// Group C of CLI-214: author <c>TimelineAsset</c>s (tracks + clips). Timeline ships in the OPTIONAL
    /// <c>com.unity.timeline</c> package, so the Editor assembly does NOT reference <c>Unity.Timeline</c>
    /// (that would break compilation when the package is absent). Every command:
    ///
    /// 1. runs <see cref="TimelineGuard.IsInstalled"/>; if Timeline is absent it logs
    ///    <see cref="TimelineGuard.NotInstalledMessage"/> and exits <see cref="ExitCode.Failed"/>, and
    /// 2. reaches the Timeline API entirely through reflection.
    ///
    /// Type names used (all in <c>Unity.Timeline</c>): <c>UnityEngine.Timeline.TimelineAsset</c>,
    /// <c>TrackAsset</c>, <c>TimelineClip</c>, and the concrete track types
    /// (<c>AnimationTrack</c>, <c>AudioTrack</c>, <c>ActivationTrack</c>, <c>ControlTrack</c>,
    /// <c>PlayableTrack</c>, <c>SignalTrack</c>, <c>MarkerTrack</c>).
    ///
    /// Out of scope (v1): markers, signal emitters/receivers, custom playable tracks, and track bindings
    /// to scene objects.
    /// </summary>
#if UNITY_6000_5_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    static class TimelineCommands
    {
        const string Group = "animation.timeline";

        private const string Asm = "Unity.Timeline";

        // Friendly track-type name => concrete TrackAsset subclass full name.
        private static readonly Dictionary<string, string> TrackTypeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Animation", "UnityEngine.Timeline.AnimationTrack" },
            { "Audio", "UnityEngine.Timeline.AudioTrack" },
            { "Activation", "UnityEngine.Timeline.ActivationTrack" },
            { "Control", "UnityEngine.Timeline.ControlTrack" },
            { "Playable", "UnityEngine.Timeline.PlayableTrack" },
            { "Signal", "UnityEngine.Timeline.SignalTrack" },
            { "Marker", "UnityEngine.Timeline.MarkerTrack" },
        };

        [CliGroupCommand(Group,
            "Create a .playable TimelineAsset under the authoring root (optional frame rate). Requires the com.unity.timeline package.",
            MainThreadRequired = true,
            Tags = new[] { "animation/timeline" },
            Effect = CommandEffect.Destructive)]
        internal static ExitCode Create(CommandContext cmd,
            [CliArg("path", "Asset path ending in .playable, relative to the authoring root. The Assets/ prefix is optional.", Required = true)] string path,
            [CliArg("frame-rate", "Timeline frame rate (default 60).")] float frameRate = 60f,
            [CliArg("confirm", "Required (true) only when overwriting an existing asset at the path.")] bool confirm = false,
            [CliArg("dry-run", "If true, validate inputs and report what would be created without writing anything.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
            {
                cmd.Log.Error(TimelineGuard.NotInstalledMessage);
                return ExitCode.Failed;
            }

            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
            {
                cmd.Log.Error(error);
                return ExitCode.InvalidUsage;
            }

            if (!string.Equals(Path.GetExtension(normalized), ".playable", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Log.Error($"Timeline path '{normalized}' must end in .playable.");
                return ExitCode.InvalidUsage;
            }

            if (frameRate <= 0f)
            {
                cmd.Log.Error($"frame-rate must be positive (got {frameRate}).");
                return ExitCode.InvalidUsage;
            }

            var exists = AssetDatabase.LoadMainAssetAtPath(normalized) != null;
            if (exists && !confirm)
            {
                cmd.Log.Error($"An asset already exists at '{normalized}'. Pass confirm=true to overwrite it.");
                return ExitCode.InvalidUsage;
            }

            if (dryRun)
            {
                cmd.Result.Write(new AuthoringResult { AssetPath = normalized, Type = "TimelineAsset" });
                return ExitCode.Success;
            }

            EnsureParentFolder(normalized);

            var timelineType = TimelineGuard.ResolveTimelineAssetType();
            var timeline = ScriptableObject.CreateInstance(timelineType);
            SetFrameRate(timeline, timelineType, frameRate);

            if (exists)
                AssetDatabase.DeleteAsset(normalized);

            AssetDatabase.CreateAsset(timeline, normalized);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(normalized);

            var loaded = AssetDatabase.LoadMainAssetAtPath(normalized);
            var result = ObjectResolver.Describe(loaded) ?? new AuthoringResult { Type = "TimelineAsset" };
            result.AssetPath = normalized;
            cmd.Result.Write(result);
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Add a track (Animation | Audio | Activation | Control | Playable | Signal | Marker) to a TimelineAsset, optionally nested under a parent group/track. Requires the com.unity.timeline package.",
            MainThreadRequired = true,
            Tags = new[] { "animation/timeline" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode AddTrack(CommandContext cmd,
            [CliArg("timeline", "Reference to the TimelineAsset to edit (path / guid / globalId).", Required = true)] ObjectRef timeline,
            [CliArg("track-type", "Track type: Animation | Audio | Activation | Control | Playable | Signal | Marker.", Required = true)] string trackType = null,
            [CliArg("name", "Optional track display name.")] string name = null,
            [CliArg("parent-track", "Optional name of an existing group/track to nest the new track under.")] string parentTrack = null,
            [CliArg("dry-run", "If true, validate inputs without writing the track.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
            {
                cmd.Log.Error(TimelineGuard.NotInstalledMessage);
                return ExitCode.Failed;
            }

            if (!TryResolveTimeline(cmd, timeline, out var asset, out var assetPath, out var timelineType, out var failure))
                return failure;

            if (string.IsNullOrWhiteSpace(trackType) || !TrackTypeNames.TryGetValue(trackType, out var trackTypeFullName))
            {
                cmd.Log.Error($"Unknown track-type '{trackType}'. Use Animation | Audio | Activation | Control | Playable | Signal | Marker.");
                return ExitCode.InvalidUsage;
            }

            var concreteTrackType = Type.GetType($"{trackTypeFullName}, {Asm}", throwOnError: false);
            if (concreteTrackType == null)
            {
                cmd.Log.Error($"Track type '{trackTypeFullName}' is not available in this version of {TimelineGuard.PackageId}.");
                return ExitCode.InvalidUsage;
            }

            var trackName = string.IsNullOrEmpty(name) ? trackType : name;

            // Resolve an optional parent track by name BEFORE any write so a bad parent fails clean.
            object parent = null;
            if (!string.IsNullOrEmpty(parentTrack))
            {
                parent = FindTrackByName(asset, timelineType, parentTrack);
                if (parent == null)
                {
                    cmd.Log.Error($"Parent track '{parentTrack}' not found on the timeline.");
                    return ExitCode.NotFound;
                }
            }

            var result = new AddTimelineTrackResult
            {
                AssetPath = assetPath,
                Type = "TimelineAsset",
                Track = new TimelineTrackSummary { Name = trackName, TrackType = trackType }
            };

            if (dryRun)
            {
                cmd.Result.Write(result);
                return ExitCode.Success;
            }

            // TrackAsset CreateTrack(Type type, TrackAsset parent, string name)
            var createTrack = timelineType.GetMethod("CreateTrack", new[] { typeof(Type), GetTrackAssetType(), typeof(string) });
            if (createTrack == null)
            {
                cmd.Log.Error("TimelineAsset.CreateTrack(Type, TrackAsset, string) not found via reflection.");
                return ExitCode.Failed;
            }

            createTrack.Invoke(asset, new object[] { concreteTrackType, parent, trackName });

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            FillIdentity(result, asset);
            cmd.Result.Write(result);
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Add a clip to a named track on a TimelineAsset. For Animation tracks pass an AnimationClip asset; for Audio tracks an AudioClip. Requires the com.unity.timeline package.",
            MainThreadRequired = true,
            Tags = new[] { "animation/timeline" },
            Effect = CommandEffect.Additive)]
        internal static ExitCode AddClip(CommandContext cmd,
            [CliArg("timeline", "Reference to the TimelineAsset to edit (path / guid / globalId).", Required = true)] ObjectRef timeline,
            [CliArg("track", "Target track name.", Required = true)] string track = null,
            [CliArg("start", "Clip start time in seconds.", Required = true)] float start = 0f,
            [CliArg("duration", "Clip duration in seconds.", Required = true)] float duration = 0f,
            [CliArg("asset", "Source asset: for Animation tracks an AnimationClip; for Audio tracks an AudioClip. Required for those track types.")] ObjectRef asset = null,
            [CliArg("dry-run", "If true, validate inputs without writing the clip.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
            {
                cmd.Log.Error(TimelineGuard.NotInstalledMessage);
                return ExitCode.Failed;
            }

            if (!TryResolveTimeline(cmd, timeline, out var timelineAsset, out var assetPath, out var timelineType, out var failure))
                return failure;

            if (string.IsNullOrWhiteSpace(track))
            {
                cmd.Log.Error("track is required.");
                return ExitCode.InvalidUsage;
            }
            if (duration <= 0f)
            {
                cmd.Log.Error($"duration must be positive (got {duration}).");
                return ExitCode.InvalidUsage;
            }
            if (start < 0f)
            {
                cmd.Log.Error($"start must be >= 0 (got {start}).");
                return ExitCode.InvalidUsage;
            }

            var trackObj = FindTrackByName(timelineAsset, timelineType, track);
            if (trackObj == null)
            {
                cmd.Log.Error($"Track '{track}' not found on the timeline.");
                return ExitCode.NotFound;
            }

            // Resolve the optional clip-source asset BEFORE any write.
            Object sourceAsset = null;
            if (asset != null && !asset.IsEmpty)
            {
                if (!ObjectResolver.TryResolve(asset, out var srcObj, out var srcError))
                {
                    cmd.Log.Error($"Could not resolve clip asset: {srcError}");
                    return ExitCode.NotFound;
                }
                sourceAsset = srcObj;

                // Re-confine the clip source to the authoring root: a restricted root must not be
                // bypassed by referencing an AnimationClip/AudioClip that lives outside the sandbox.
                var sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    cmd.Log.Error($"Clip asset reference '{asset}' does not point at an on-disk asset.");
                    return ExitCode.InvalidUsage;
                }

                var confinedSource = ProjectPaths.Resolve(sourcePath, out var sourceConfineError);
                if (confinedSource == null)
                {
                    cmd.Log.Error($"Clip asset '{sourcePath}' is outside the authoring root '{ProjectPaths.AuthoringRoot}': {sourceConfineError}");
                    return ExitCode.InvalidUsage;
                }
            }

            var result = new AddTimelineClipResult
            {
                AssetPath = assetPath,
                Type = "TimelineAsset",
                Clip = new TimelineClipSummary { Track = track, Start = start, Duration = duration }
            };

            if (dryRun)
            {
                // Even in dry-run, validate that a clip can be created for this track/asset combination so
                // a missing-required-asset case fails fast and writes nothing.
                ValidateClipCreatable(trackObj, sourceAsset);
                cmd.Result.Write(result);
                return ExitCode.Success;
            }

            var clip = CreateClip(trackObj, sourceAsset);
            if (clip == null)
            {
                cmd.Log.Error($"Failed to create a clip on track '{track}'.");
                return ExitCode.Failed;
            }

            SetClipTiming(clip, start, duration);

            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssets();

            FillIdentity(result, timelineAsset);
            cmd.Result.Write(result);
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Read a TimelineAsset's structure: frame rate, duration, and its tracks with their clips. Requires the com.unity.timeline package.",
            MainThreadRequired = true,
            Tags = new[] { "animation/timeline" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Get(CommandContext cmd,
            [CliArg("timeline", "Reference to the TimelineAsset to read (path / guid / globalId).", Required = true)] ObjectRef timeline)
        {
            if (!TimelineGuard.IsInstalled())
            {
                cmd.Log.Error(TimelineGuard.NotInstalledMessage);
                return ExitCode.Failed;
            }

            if (!TryResolveTimeline(cmd, timeline, out var asset, out var assetPath, out var timelineType, out var failure))
                return failure;

            var info = new TimelineInfo
            {
                AssetPath = assetPath,
                FrameRate = GetFrameRate(asset, timelineType),
                Duration = GetDouble(asset, timelineType, "duration")
            };

            foreach (var trackObj in GetOutputTracks(asset, timelineType))
            {
                if (trackObj == null)
                    continue;

                var trackType = trackObj.GetType();
                var trackInfo = new TimelineTrackInfo
                {
                    Name = GetName(trackObj),
                    TrackType = FriendlyTrackType(trackType)
                };

                foreach (var clipObj in GetClips(trackObj))
                {
                    if (clipObj == null)
                        continue;
                    var clipType = clipObj.GetType();
                    var clipAsset = GetMember(clipObj, clipType, "asset") as Object;
                    trackInfo.Clips.Add(new TimelineClipInfo
                    {
                        Start = GetDoubleMember(clipObj, clipType, "start"),
                        Duration = GetDoubleMember(clipObj, clipType, "duration"),
                        Asset = clipAsset != null ? AssetDatabase.GetAssetPath(clipAsset) : null
                    });
                }

                info.Tracks.Add(trackInfo);
            }

            cmd.Result.Write(info);
            return ExitCode.Success;
        }

        // ---- reflection helpers ----

        private static Type GetTrackAssetType()
        {
            var t = Type.GetType($"UnityEngine.Timeline.TrackAsset, {Asm}", throwOnError: false);
            if (t == null)
                throw new InvalidOperationException("UnityEngine.Timeline.TrackAsset type not found.");
            return t;
        }

        /// <summary>
        /// Resolve a timeline handle to an on-disk TimelineAsset, its sandbox-confined path and the
        /// TimelineAsset type. Logs why and returns false, with the exit code to return, for a handle
        /// that resolves to nothing (<see cref="ExitCode.NotFound"/>), to something other than an
        /// on-disk TimelineAsset, or outside the authoring root (<see cref="ExitCode.InvalidUsage"/>).
        /// </summary>
        private static bool TryResolveTimeline(CommandContext cmd, ObjectRef timeline, out Object asset, out string assetPath, out Type timelineType, out ExitCode failure)
        {
            asset = null;
            assetPath = null;
            timelineType = null;
            failure = ExitCode.InvalidUsage;

            if (timeline == null || timeline.IsEmpty)
            {
                cmd.Log.Error("timeline is required.");
                return false;
            }

            if (!ObjectResolver.TryResolve(timeline, out var obj, out var error))
            {
                cmd.Log.Error(error);
                failure = ExitCode.NotFound;
                return false;
            }

            timelineType = TimelineGuard.ResolveTimelineAssetType();
            if (timelineType == null || !timelineType.IsInstanceOfType(obj))
            {
                cmd.Log.Error($"Reference '{timeline}' resolved to a {obj.GetType().Name}, not a TimelineAsset.");
                return false;
            }

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                cmd.Log.Error($"Reference '{timeline}' does not point at an on-disk TimelineAsset.");
                return false;
            }

            assetPath = ProjectPaths.Resolve(path, out var confineError);
            if (assetPath == null)
            {
                cmd.Log.Error($"Timeline '{path}' is outside the authoring root '{ProjectPaths.AuthoringRoot}': {confineError}");
                return false;
            }

            asset = obj;
            return true;
        }

        /// <summary>
        /// Set the timeline frame rate, preferring the current <c>editorSettings.frameRate</c> (double)
        /// and falling back to the obsolete <c>editorSettings.fps</c> (float) on older package versions.
        /// </summary>
        private static void SetFrameRate(object timeline, Type timelineType, float frameRate)
        {
            var editorSettings = GetMember(timeline, timelineType, "editorSettings");
            if (editorSettings == null)
                return;

            var settingsType = editorSettings.GetType();

            var frameRateProp = settingsType.GetProperty("frameRate", BindingFlags.Public | BindingFlags.Instance);
            if (frameRateProp != null && frameRateProp.CanWrite)
            {
                frameRateProp.SetValue(editorSettings, (double)frameRate);
                return;
            }

            var fpsProp = settingsType.GetProperty("fps", BindingFlags.Public | BindingFlags.Instance);
            if (fpsProp != null && fpsProp.CanWrite)
                fpsProp.SetValue(editorSettings, frameRate);
        }

        private static double GetFrameRate(object timeline, Type timelineType)
        {
            var editorSettings = GetMember(timeline, timelineType, "editorSettings");
            if (editorSettings == null)
                return 0d;

            var settingsType = editorSettings.GetType();

            var frameRateProp = settingsType.GetProperty("frameRate", BindingFlags.Public | BindingFlags.Instance);
            if (frameRateProp != null && frameRateProp.CanRead)
                return Convert.ToDouble(frameRateProp.GetValue(editorSettings));

            var fpsProp = settingsType.GetProperty("fps", BindingFlags.Public | BindingFlags.Instance);
            if (fpsProp != null && fpsProp.CanRead)
                return Convert.ToDouble(fpsProp.GetValue(editorSettings));

            return 0d;
        }

        private static IEnumerable<object> GetOutputTracks(object timeline, Type timelineType)
        {
            var method = timelineType.GetMethod("GetOutputTracks", Type.EmptyTypes);
            if (method == null)
                yield break;

            var enumerable = method.Invoke(timeline, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        private static object FindTrackByName(object timeline, Type timelineType, string name)
        {
            foreach (var track in GetOutputTracks(timeline, timelineType))
            {
                if (track != null && GetName(track) == name)
                    return track;
            }
            return null;
        }

        private static IEnumerable<object> GetClips(object track)
        {
            var method = track.GetType().GetMethod("GetClips", Type.EmptyTypes);
            if (method == null)
                yield break;

            var enumerable = method.Invoke(track, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        /// <summary>
        /// Find a <c>CreateClip</c> overload on the track that accepts the given source asset's type
        /// (e.g. AnimationTrack.CreateClip(AnimationClip), AudioTrack.CreateClip(AudioClip)). Returns
        /// null when no asset is given (caller then uses CreateDefaultClip).
        /// </summary>
        private static MethodInfo FindCreateClipForAsset(Type trackType, Object sourceAsset)
        {
            if (sourceAsset == null)
                return null;

            var assetType = sourceAsset.GetType();
            MethodInfo best = null;
            foreach (var m in trackType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.Name != "CreateClip")
                    continue;
                if (m.IsGenericMethodDefinition)
                    continue;
                var ps = m.GetParameters();
                if (ps.Length != 1)
                    continue;
                if (ps[0].ParameterType.IsAssignableFrom(assetType))
                {
                    // Prefer an exact match over a UnityEngine.Object-typed overload.
                    if (ps[0].ParameterType == assetType)
                        return m;
                    best = best ?? m;
                }
            }
            return best;
        }

        private static void ValidateClipCreatable(object track, Object sourceAsset)
        {
            var trackType = track.GetType();
            var isAnimation = trackType.FullName == "UnityEngine.Timeline.AnimationTrack";
            var isAudio = trackType.FullName == "UnityEngine.Timeline.AudioTrack";

            if ((isAnimation || isAudio) && sourceAsset == null)
                throw new ArgumentException($"Track type '{FriendlyTrackType(trackType)}' requires an 'asset' (AnimationClip / AudioClip) to create a clip.");

            if (sourceAsset != null && FindCreateClipForAsset(trackType, sourceAsset) == null)
                throw new ArgumentException(
                    $"Track type '{FriendlyTrackType(trackType)}' has no CreateClip overload accepting a {sourceAsset.GetType().Name}.");
        }

        private static object CreateClip(object track, Object sourceAsset)
        {
            var trackType = track.GetType();

            if (sourceAsset != null)
            {
                var createClip = FindCreateClipForAsset(trackType, sourceAsset);
                if (createClip == null)
                    throw new ArgumentException(
                        $"Track type '{FriendlyTrackType(trackType)}' has no CreateClip overload accepting a {sourceAsset.GetType().Name}.");
                return createClip.Invoke(track, new object[] { sourceAsset });
            }

            var isAnimation = trackType.FullName == "UnityEngine.Timeline.AnimationTrack";
            var isAudio = trackType.FullName == "UnityEngine.Timeline.AudioTrack";
            if (isAnimation || isAudio)
                throw new ArgumentException($"Track type '{FriendlyTrackType(trackType)}' requires an 'asset' (AnimationClip / AudioClip) to create a clip.");

            // For tracks that don't take a source asset, create a default (empty) clip.
            var createDefault = trackType.GetMethod("CreateDefaultClip", Type.EmptyTypes);
            if (createDefault != null)
                return createDefault.Invoke(track, null);

            throw new InvalidOperationException($"No CreateClip / CreateDefaultClip available on '{trackType.Name}'.");
        }

        private static void SetClipTiming(object clip, double start, double duration)
        {
            var clipType = clip.GetType();
            SetDoubleMember(clip, clipType, "start", start);
            SetDoubleMember(clip, clipType, "duration", duration);
        }

        private static string FriendlyTrackType(Type trackType)
        {
            foreach (var pair in TrackTypeNames)
            {
                if (pair.Value == trackType.FullName)
                    return pair.Key;
            }
            // Strip the trailing "Track" suffix for any unmapped subclass.
            var n = trackType.Name;
            return n.EndsWith("Track", StringComparison.Ordinal) ? n.Substring(0, n.Length - "Track".Length) : n;
        }

        private static string GetName(object obj)
        {
            var prop = obj.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj) as string;
        }

        private static object GetMember(object obj, Type type, string name)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
                return prop.GetValue(obj);
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        private static double GetDouble(object obj, Type type, string name)
        {
            var value = GetMember(obj, type, name);
            return value == null ? 0d : Convert.ToDouble(value);
        }

        private static double GetDoubleMember(object obj, Type type, string name) => GetDouble(obj, type, name);

        private static void SetDoubleMember(object obj, Type type, string name, double value)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
                return;
            }
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private static void FillIdentity(AuthoringResult result, Object asset)
        {
            var described = ObjectResolver.Describe(asset);
            if (described == null)
                return;
            result.Guid = described.Guid;
            result.FileId = described.FileId;
            result.GlobalId = described.GlobalId;
        }

        private static void EnsureParentFolder(string assetPath)
        {
            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || AssetDatabase.IsValidFolder(parent))
                return;
            CreateFolderRecursive(parent);
        }

        private static void CreateFolderRecursive(string assetsPath)
        {
            if (AssetDatabase.IsValidFolder(assetsPath))
                return;

            var parent = Path.GetDirectoryName(assetsPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid folder path '{assetsPath}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }

    // ---- result models ----

    [Serializable]
    class TimelineTrackSummary
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string TrackType { get; set; }
    }

    [Serializable]
    class AddTimelineTrackResult : AuthoringResult
    {
        [JsonProperty("track")]
        public TimelineTrackSummary Track { get; set; }
    }

    [Serializable]
    class TimelineClipSummary
    {
        [JsonProperty("track")]
        public string Track { get; set; }

        [JsonProperty("start")]
        public float Start { get; set; }

        [JsonProperty("duration")]
        public float Duration { get; set; }
    }

    [Serializable]
    class AddTimelineClipResult : AuthoringResult
    {
        [JsonProperty("clip")]
        public TimelineClipSummary Clip { get; set; }
    }

    [Serializable]
    class TimelineInfo
    {
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        [JsonProperty("frameRate")]
        public double FrameRate { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("tracks")]
        public List<TimelineTrackInfo> Tracks { get; set; } = new List<TimelineTrackInfo>();
    }

    [Serializable]
    class TimelineTrackInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string TrackType { get; set; }

        [JsonProperty("clips")]
        public List<TimelineClipInfo> Clips { get; set; } = new List<TimelineClipInfo>();
    }

    [Serializable]
    class TimelineClipInfo
    {
        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("asset")]
        public string Asset { get; set; }
    }
}
