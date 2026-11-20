#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Build.Content;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Computes, inside a <see cref="PreviewSceneArtifactImporter"/> import, the shader feature usage (fog and
    /// lightmap modes, …) of the preview build profile's scenes and the render pipeline settings that
    /// variant stripping reads, and records every one of those inputs as a dependency of the import.
    /// </summary>
    static class PreviewSceneUsage
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        const string k_QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        /// <summary>
        /// Sync the settings singletons from disk, register the render pipeline dependencies, and return
        /// the global usage of the profile's scenes. Null, with an import error logged, when the profile
        /// cannot be read.
        /// </summary>
        internal static BuildUsageTagGlobal? Compute(AssetImportContext ctx, string profilePath)
        {
            ctx.DependsOnSourceAsset(k_GraphicsSettingsPath);
            ctx.DependsOnSourceAsset(k_QualitySettingsPath);
            SyncSettingsFromFile(ctx, "GraphicsSettings", k_GraphicsSettingsPath);
            SyncSettingsFromFile(ctx, "QualitySettings", k_QualitySettingsPath);
            DependOnRenderPipelineAssets(ctx);

            ctx.DependsOnSourceAsset(profilePath);
            var scenes = ReadProfileScenes(ctx, profilePath);
            if (scenes == null)
                return null;

            foreach (var scene in scenes)
                DependOnScene(ctx, scene);

            return AssetDatabase.IsAssetImportWorkerProcess()
                ? UsageInWorker(scenes)
                : UsageInEditor(scenes);
        }

        // Read the profile's file directly, so this import never loads the profile through the AssetDatabase.
        static List<string> ReadProfileScenes(AssetImportContext ctx, string profilePath)
        {
            var objects = InternalEditorUtility.LoadSerializedFileAndForget(profilePath);
            try
            {
                var profile = objects.OfType<BuildProfile>().FirstOrDefault();
                if (profile == null)
                {
                    ctx.LogImportError($"No build profile in '{profilePath}'.");
                    return null;
                }

                var scenes = new List<string>();
                foreach (var entry in profile.GetScenesForBuild())
                {
                    if (!entry.enabled)
                        continue;
                    if (string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path))
                    {
                        ctx.LogImportWarning($"Preview build profile scene not found: '{entry.path}'.");
                        continue;
                    }
                    scenes.Add(entry.path);
                }
                return scenes;
            }
            finally
            {
                foreach (var o in objects)
                {
                    if (o != null)
                        Object.DestroyImmediate(o, true);
                }
            }
        }

        // A scene's lightmap usage comes from its lighting settings and baked data, which are separate assets.
        static void DependOnScene(AssetImportContext ctx, string scenePath)
        {
            ctx.DependsOnSourceAsset(scenePath);
            foreach (var dependency in AssetDatabase.GetDependencies(scenePath, false))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(dependency);
                if (type == typeof(LightingSettings) || type == typeof(LightingDataAsset))
                    ctx.DependsOnSourceAsset(dependency);
            }
        }

        // An import worker keeps the scene of its previous job loaded, and global usage only accumulates.
        static BuildUsageTagGlobal UsageInWorker(List<string> scenes)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (scenes.Count == 0)
                return ContentBuildInterface.GetGlobalUsageFromGraphicsSettings();

            var usage = new BuildUsageTagGlobal();
            foreach (var scenePath in scenes)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                usage |= ContentBuildInterface.GetGlobalUsageFromGraphicsSettings();
            }
            return usage;
        }

        // In the user's editor, never close or replace their scenes: read each profile scene as the active one, additively.
        static BuildUsageTagGlobal UsageInEditor(List<string> scenes)
        {
            if (scenes.Count == 0)
                return ContentBuildInterface.GetGlobalUsageFromGraphicsSettings();

            var activeBefore = SceneManager.GetActiveScene();
            var usage = new BuildUsageTagGlobal();
            try
            {
                foreach (var scenePath in scenes)
                {
                    var loaded = SceneManager.GetSceneByPath(scenePath);
                    var scene = loaded.isLoaded ? loaded : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene);
                    usage |= ContentBuildInterface.GetGlobalUsageFromGraphicsSettings();
                    if (!loaded.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
            finally
            {
                if (activeBefore.IsValid() && activeBefore.isLoaded)
                    SceneManager.SetActiveScene(activeBefore);
            }
            return usage;
        }

        // Variant stripping reads the render pipeline assets in use and what they reference (renderer data, global settings).
        static void DependOnRenderPipelineAssets(AssetImportContext ctx)
        {
            var roots = new List<Object> { GraphicsSettings.defaultRenderPipeline };
            for (var i = 0; i < QualitySettings.count; i++)
                roots.Add(QualitySettings.GetRenderPipelineAssetAt(i));
            roots.Add(Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings"));

            var visited = new HashSet<string>();
            foreach (var root in roots.Where(r => r != null).Distinct())
            {
                var rootPath = AssetDatabase.GetAssetPath(root);
                if (IsProjectAsset(rootPath) && visited.Add(rootPath))
                    ctx.DependsOnSourceAsset(rootPath);

                var property = new SerializedObject(root).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || !(property.objectReferenceValue is ScriptableObject referenced))
                        continue;
                    var path = AssetDatabase.GetAssetPath(referenced);
                    if (IsProjectAsset(path) && visited.Add(path))
                        ctx.DependsOnSourceAsset(path);
                }
            }
        }

        static bool IsProjectAsset(string path) =>
            !string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.StartsWith("Packages/"));

        // An import worker loads the settings singletons once, at startup, and never reloads them, so make
        // them match the files this import depends on before reading usage or stripping from them.
        static void SyncSettingsFromFile(AssetImportContext ctx, string singletonName, string path)
        {
            var liveSettings = Unsupported.GetSerializedAssetInterfaceSingleton(singletonName);
            if (liveSettings == null)
            {
                ctx.LogImportError($"Could not get the {singletonName} singleton.");
                return;
            }

            // In the main editor the live settings are the freshest; copying the file would revert unsaved edits.
            if (!AssetDatabase.IsAssetImportWorkerProcess())
            {
                if (EditorUtility.IsDirty(liveSettings))
                    ctx.LogImportWarning(
                        $"{path} has unsaved changes, which import workers cannot see. " +
                        "Save Project Settings for previews that build the same everywhere.");
                return;
            }

            var fileSettings = InternalEditorUtility.LoadSerializedFileAndForget(path)?.FirstOrDefault(o => o != null);
            if (fileSettings == null)
            {
                ctx.LogImportError($"Could not load {path}.");
                return;
            }

            try
            {
                EditorUtility.CopySerialized(fileSettings, liveSettings);
            }
            finally
            {
                Object.DestroyImmediate(fileSettings, true);
            }
        }
    }
}
#endif
