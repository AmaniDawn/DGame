#if UNITY_6000_0_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Creates a preview build profile: a build profile for the active platform whose only scene is the
    /// preview scene. The preview player is built from it, and the importers strip previews for its scenes.
    /// </summary>
    static class PreviewBuildProfileSetup
    {
        internal const string DefaultAssetPath = "Assets/Settings/Build Profiles/ScenePreview.asset";

        /// <summary>Create a profile for the open scene. Returns its path, or null with an error logged.</summary>
        internal static string CreateForActiveScene()
        {
            var scenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("Scene Preview: open and save the preview scene, then create the preview build profile.");
                return null;
            }
            return Create(scenePath, DefaultAssetPath);
        }

        /// <summary>
        /// Create a profile whose only scene is <paramref name="scenePath"/> at
        /// <paramref name="assetPath"/>, or at a free path next to it if that one is taken.
        /// </summary>
        internal static string Create(string scenePath, string assetPath)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var profile = ScenePreviewEngine.CreateBuildProfile(target);
            if (profile == null)
                return null;

            profile.overrideGlobalScenes = true;
            profile.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            ScenePreviewSettings.AddScriptingDefine(profile);

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.Refresh();
            var path = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Scene Preview: created a {target} preview build profile for '{scenePath}' at '{path}'.");
            return path;
        }
    }
}
#endif
