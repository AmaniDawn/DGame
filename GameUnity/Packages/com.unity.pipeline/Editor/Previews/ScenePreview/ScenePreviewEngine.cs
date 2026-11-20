#if UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Build.Content;
using UnityEditor.Build.Profile;
using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Every engine call the Scene Preview importers make that is internal or differs between Unity
    /// versions, kept in one place so the next engine change is a one-file fix.
    /// </summary>
    static class ScenePreviewEngine
    {
        /// <summary>Produce a non-primary import result now. True when it is valid.</summary>
        internal static bool ProduceArtifact(ArtifactKey key)
        {
            // var: the result type is ArtifactID before 6000.4 and ImportResultID from it.
            var id = AssetDatabaseExperimental.ProduceArtifact(key);
            return id.isValid;
        }

        /// <summary>Paths of an already-produced import result's files, or null when there is none.</summary>
        internal static string[] GetArtifactPaths(ArtifactKey key)
        {
            var id = AssetDatabaseExperimental.LookupArtifact(key);
            if (!id.isValid)
                return null;
            return AssetDatabaseExperimental.GetArtifactPaths(id, out var paths) ? paths : null;
        }

        /// <summary>Write a named file of the import result.</summary>
        internal static void WriteArtifact(AssetImportContext ctx, string name, byte[] bytes)
        {
#if UNITY_6000_5_OR_NEWER
            ctx.SetOutputArtifactData(name, bytes);
#else
            File.WriteAllBytes(ctx.GetOutputArtifactFilePath(name), bytes);
#endif
        }

        /// <summary>
        /// Run <paramref name="build"/> with the editor's shader and compute-shader build callbacks
        /// registered, so render pipeline variant stripping applies to what it writes. When a render
        /// pipeline's callbacks cannot run inside an import, build without them and warn.
        /// </summary>
        internal static byte[] WithShaderStrippingCallbacks(AssetImportContext ctx, Func<byte[]> build)
        {
            var buildPipelineType = Type.GetType("UnityEditor.Build.BuildPipelineInterfaces, UnityEditor");
            var initialize = buildPipelineType?.GetMethod("InitializeBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
            var cleanup = buildPipelineType?.GetMethod("CleanupBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
            if (initialize == null || cleanup == null)
            {
                ctx.LogImportWarning("Building without render pipeline shader variant stripping: the editor's build callbacks are not available.");
                return build();
            }

            try
            {
                initialize.Invoke(null, new object[] { 272 }); // ShaderProcessors | ComputeShaderProcessors
            }
            catch (TargetInvocationException e)
            {
                // URP versions that save their asset while gathering shader features cannot run inside an import.
                cleanup.Invoke(null, null);
                ctx.LogImportWarning($"Building without render pipeline shader variant stripping: {e.GetBaseException().Message}");
            }

            try
            {
                return build();
            }
            finally
            {
                cleanup.Invoke(null, null);
            }
        }

        /// <summary>Create a build profile asset for a platform, or null with an error logged.</summary>
        internal static BuildProfile CreateBuildProfile(BuildTarget target)
        {
            var subtarget = BuildPipeline.GetBuildTargetGroup(target) == BuildTargetGroup.Standalone
                ? StandaloneBuildSubtarget.Player
                : StandaloneBuildSubtarget.Default;
            var create = typeof(BuildProfile).GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(BuildTarget), typeof(StandaloneBuildSubtarget) }, null);
            if (create == null)
            {
                Debug.LogError("Scene Preview: this Unity version has no BuildProfile.CreateInstance(BuildTarget, StandaloneBuildSubtarget); create the build profile from the Build Profiles window.");
                return null;
            }

            try
            {
                return (BuildProfile)create.Invoke(null, new object[] { target, subtarget });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogError($"Scene Preview: could not create a {target} build profile (is its build support installed?): {e.GetBaseException().Message}");
                return null;
            }
        }

        /// <summary>The active build profile, or null when the classic platform settings are active.</summary>
        internal static BuildProfile GetActiveBuildProfile() => BuildProfile.GetActiveBuildProfile();
    }
}
#endif
