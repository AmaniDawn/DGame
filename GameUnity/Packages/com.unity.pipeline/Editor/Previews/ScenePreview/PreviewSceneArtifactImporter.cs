#if UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Pipeline.ScenePreview;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Build.Content;
using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Builds a Scene Preview content archive (.ca) for one asset of any type, to show in a preview
    /// player's scene: a single content file holding the
    /// asset and what it references, plus a <see cref="ContentArchiveManifest"/>, for the platform the
    /// import runs under. Project Service runs it on demand by its type name,
    /// <c>T:{guid}+Unity.Pipeline.Editor.Previews.PreviewSceneArtifactImporter</c>, artifact name
    /// <c>ca</c>; <see cref="ContentArchiveLoader"/> loads the result in a player.
    /// </summary>
    /// <remarks>
    /// Shader variants are stripped for the preview build profile's scenes, and the content keeps only
    /// types from the assemblies the Scene Preview settings preserve, so the preview player can always load it.
    /// </remarks>
    // Non-primary: its only extension is override-only, so it never imports a file and never conflicts with another importer's extension.
    [ScriptedImporter(Version, exts: new string[0], overrideExts: new[] { Extension }, AllowCaching = true)]
    public sealed class PreviewSceneArtifactImporter : ScriptedImporter
    {
        internal const string Extension = "unitypipeline_previewscene";

        internal const int Version = 4;

        /// <summary>Name of the import result file holding the archive.</summary>
        public const string ArchiveArtifactName = "ca";

        /// <summary>Build the archive. Called by the AssetDatabase.</summary>
        /// <param name="ctx">The import context.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Recorded by path, so the import is also redone when the settings asset first appears.
            ctx.DependsOnSourceAsset(ScenePreviewSettings.AssetPath);

            var assetPath = ctx.assetPath;
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (assetGuid.Empty())
            {
                ctx.LogImportError($"Asset not found: {assetPath}");
                return;
            }
            ctx.DependsOnArtifact(assetGuid);

            // Object identifiers come from the file's headers, so an asset that fails to load would still be archived, as a null object.
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                ctx.LogImportError($"The asset could not be loaded: {assetPath}");
                return;
            }

            // Reading the target also records it as a dependency, so platforms never share a result.
            var buildTarget = ctx.selectedBuildTarget;

            var profilePath = ScenePreviewSettings.ResolveBuildProfilePath(out var isFallback);
            var profileGuid = string.IsNullOrEmpty(profilePath) ? default : AssetDatabase.GUIDFromAssetPath(profilePath);
            if (profileGuid.Empty())
            {
                ctx.LogImportError(isFallback
                    ? "No preview build profile: set one in Project Settings > Pipeline > Editor > Scene Preview."
                    : $"Preview build profile not found: '{profilePath}'.");
                return;
            }
            if (isFallback)
                ctx.LogImportWarning($"No preview build profile is set; using the active build profile '{profilePath}'.");

            var globalUsage = PreviewSceneUsage.Compute(ctx, profilePath);
            if (globalUsage == null)
                return;
            var archive = ScenePreviewEngine.WithShaderStrippingCallbacks(ctx,
                () => BuildContentArchive(ctx, assetPath, assetGuid, buildTarget, globalUsage.Value));
            if (archive == null)
                return;

            ScenePreviewEngine.WriteArtifact(ctx, ArchiveArtifactName, archive);
        }

        static byte[] BuildContentArchive(AssetImportContext ctx, string assetPath, GUID assetGuid,
            BuildTarget target, BuildUsageTagGlobal globalUsage)
        {
            var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(assetGuid, target);
            if (includedObjects.Length == 0)
            {
                ctx.LogImportError($"No objects found in asset: {assetPath}");
                return null;
            }

            var dependencies = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, target, null);

            // Default resources ship with every player: map them rather than serialize them, and tell
            // the loader through the manifest that it must supply the global table.
            var filteredDependencies = dependencies.Where(o => !o.filePath.Contains("unity default resources")).ToArray();
            var defaultDependencies = dependencies.Where(o => o.filePath.Contains("unity default resources")).ToArray();

            // A referenced asset changing does not reimport the asset referencing it, so depend on each one.
            foreach (var dependencyGuid in filteredDependencies.Select(o => o.guid).Distinct())
            {
                if (dependencyGuid.Empty() || dependencyGuid == assetGuid)
                    continue;
                var dependencyPath = AssetDatabase.GUIDToAssetPath(dependencyGuid);
                if (string.IsNullOrEmpty(dependencyPath) || dependencyPath.StartsWith("Resources/"))
                    continue;
                ctx.DependsOnArtifact(dependencyGuid);
                // The AssetDatabase drops an artifact dependency the import never loads.
                AssetDatabase.LoadMainAssetAtPath(dependencyPath);
            }

            var objects = new List<ObjectIdentifier>(includedObjects);
            objects.AddRange(filteredDependencies);

            var allowedAssemblies = PreviewLinkXml.ReadAllowList();
            if (allowedAssemblies.Count > 0)
            {
                objects = objects.Where(o =>
                {
                    var type = ContentBuildInterface.GetTypeForObject(o);
                    return type != null && allowedAssemblies.Contains(type.Assembly.GetName().Name);
                }).ToList();
            }

            // internalName and fileName must match, or the archived .resS alias differs from the one the content file references.
            var internalName = SanitizeName(Path.GetFileNameWithoutExtension(assetPath));
            var command = new WriteCommand
            {
                internalName = internalName,
                fileName = internalName,
                serializeObjects = new List<SerializationInfo>()
            };

            // Serialization index 0 is reserved for null references.
            for (int i = 0; i < objects.Count; i++)
                command.serializeObjects.Add(new SerializationInfo { serializationObject = objects[i], serializationIndex = i + 1 });

            var objectIds = objects.ToArray();
            var usageSet = new BuildUsageTagSet();
            ContentBuildInterface.CalculateBuildUsageTags(objectIds, objectIds, globalUsage, usageSet);

            var referenceMap = new BuildReferenceMap();
            referenceMap.AddMappings(internalName, command.serializeObjects.ToArray());
            foreach (var dependency in defaultDependencies)
                referenceMap.AddMapping(dependency.filePath, dependency.localIdentifierInFile, dependency);

            var outputFolder = Path.Combine("Temp", "ScenePreview_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(outputFolder);
            try
            {
                var writeResult = ContentBuildInterface.WriteSerializedFile(outputFolder, new WriteParameters
                {
                    writeCommand = command,
                    settings = new BuildSettings { target = target, group = BuildPipeline.GetBuildTargetGroup(target), typeDB = null },
                    globalUsage = globalUsage,
                    usageSet = usageSet,
                    referenceMap = referenceMap,
                    bundleInfo = null
                });

                var resourceFiles = writeResult.resourceFiles.ToList();
                resourceFiles.Add(ContentArchiveManifestWriter.Write(outputFolder, internalName, defaultDependencies.Length > 0));

                var missing = resourceFiles.Where(r => !File.Exists(r.fileName)).ToList();
                if (missing.Count > 0)
                {
                    ctx.LogImportError("Cannot archive; resource files not on disk: " + string.Join(", ", missing.Select(m => Path.GetFileName(m.fileName))));
                    return null;
                }

                var archivePath = Path.Combine(outputFolder, internalName + ".ca");
                if (ContentBuildInterface.ArchiveAndCompress(resourceFiles.ToArray(), archivePath, UnityEngine.BuildCompression.LZ4) == 0)
                {
                    ctx.LogImportError("Failed to archive the content file.");
                    return null;
                }

                return File.ReadAllBytes(archivePath);
            }
            finally
            {
                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, true);
            }
        }

        /// <summary>
        /// Produce the archive for an asset now, in this editor. The user's open scenes are left as they are.
        /// </summary>
        /// <param name="assetPath">Project-relative path of the asset.</param>
        /// <returns>True when the archive was produced.</returns>
        public static bool Produce(string assetPath)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty())
            {
                Debug.LogError($"Scene Preview: asset not found: '{assetPath}'.");
                return false;
            }

            if (!ScenePreviewEngine.ProduceArtifact(new ArtifactKey(guid, typeof(PreviewSceneArtifactImporter))))
            {
                Debug.LogError($"Scene Preview: could not build the content archive for '{assetPath}'.");
                return false;
            }
            return true;
        }

        /// <summary>Path of an asset's produced archive, or null if it has not been produced.</summary>
        /// <param name="assetPath">Project-relative path of the asset.</param>
        /// <returns>The archive's path, which may be virtual: copy it out with <c>FileUtil.ReplaceFile</c>.</returns>
        public static string GetArchivePath(string assetPath)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty())
                return null;

            var paths = ScenePreviewEngine.GetArtifactPaths(new ArtifactKey(guid, typeof(PreviewSceneArtifactImporter)));
            return paths?.FirstOrDefault(p => p.EndsWith("." + ArchiveArtifactName, StringComparison.OrdinalIgnoreCase));
        }

        // Content file names are aliases inside the archive, so keep them lower-case and path-safe.
        static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "previewcontent";
            return new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
        }
    }
}
#endif
