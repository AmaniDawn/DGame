#if UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Reads and writes the Scene Preview settings asset (<see cref="ScenePreviewSettingsAsset"/>) the
    /// importers depend on: the preview build profile and the assemblies a preview may contain.
    /// </summary>
    static class ScenePreviewSettings
    {
        internal const string AssetPath = "Assets/Settings/ScenePreview/ScenePreviewSettings.asset";

        /// <summary>
        /// Scripting define of the preview build profile. The Scene Preview runtime assemblies compile
        /// into a player only when it is set, so the game's own builds leave them out.
        /// </summary>
        internal const string ScriptingDefine = "ENABLE_PIPELINE_SCENE_PREVIEW";

        static ScenePreviewSettingsAsset Load() => AssetDatabase.LoadAssetAtPath<ScenePreviewSettingsAsset>(AssetPath);

        static ScenePreviewSettingsAsset LoadOrCreate()
        {
            var settings = Load();
            if (settings != null)
                return settings;

            CreateFolders(Path.GetDirectoryName(AssetPath).Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<ScenePreviewSettingsAsset>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            return settings;
        }

        static void CreateFolders(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            CreateFolders(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static void Save(ScenePreviewSettingsAsset settings)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        /// <summary>The configured preview build profile's path, or an empty string when none is set or it no longer exists.</summary>
        internal static string ConfiguredBuildProfilePath
        {
            get
            {
                var settings = Load();
                return settings == null || string.IsNullOrEmpty(settings.BuildProfileGuid)
                    ? string.Empty
                    : AssetDatabase.GUIDToAssetPath(settings.BuildProfileGuid);
            }
        }

        /// <summary>
        /// The build profile previews are built against: the configured one, else the active build
        /// profile. Empty when neither exists.
        /// </summary>
        internal static string ResolveBuildProfilePath(out bool isFallback)
        {
            var configured = ConfiguredBuildProfilePath;
            isFallback = string.IsNullOrEmpty(configured);
            if (!isFallback)
                return configured;

            var active = ScenePreviewEngine.GetActiveBuildProfile();
            return active != null ? AssetDatabase.GetAssetPath(active) : string.Empty;
        }

        /// <summary>Set the preview build profile, creating the settings asset if needed. Empty clears it.</summary>
        internal static void SetBuildProfilePath(string buildProfilePath)
        {
            var settings = LoadOrCreate();
            settings.BuildProfileGuid = string.IsNullOrEmpty(buildProfilePath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(buildProfilePath);
            Save(settings);
        }

        /// <summary>The assemblies a preview may contain. Empty, meaning no filtering, when none are set.</summary>
        internal static IReadOnlyList<string> PreservedAssemblies =>
            Load()?.PreservedAssemblies ?? Array.Empty<string>();

        /// <summary>Set the assemblies a preview may contain, creating the settings asset if needed.</summary>
        internal static void SetPreservedAssemblies(IEnumerable<string> assemblies)
        {
            var settings = LoadOrCreate();
            settings.SetPreservedAssemblies(assemblies);
            Save(settings);
        }

        /// <summary>The active build profile when it is the configured preview build profile, else null.</summary>
        internal static BuildProfile ActivePreviewBuildProfile()
        {
            var configured = ConfiguredBuildProfilePath;
            if (string.IsNullOrEmpty(configured))
                return null;
            var active = ScenePreviewEngine.GetActiveBuildProfile();
            return active != null && AssetDatabase.GetAssetPath(active) == configured ? active : null;
        }

        internal static bool HasScriptingDefine(BuildProfile profile) =>
            profile.scriptingDefines != null && profile.scriptingDefines.Contains(ScriptingDefine);

        /// <summary>Add <see cref="ScriptingDefine"/> to the profile. Scripts recompile when the profile is active.</summary>
        internal static void AddScriptingDefine(BuildProfile profile)
        {
            if (!HasScriptingDefine(profile))
                profile.scriptingDefines = (profile.scriptingDefines ?? Array.Empty<string>()).Append(ScriptingDefine).ToArray();
        }
    }
}
#endif
