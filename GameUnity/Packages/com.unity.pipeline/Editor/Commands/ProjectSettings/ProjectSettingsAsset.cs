using System;
using UnityEditor;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Loads a <see cref="SerializedObject"/> for a singleton settings asset under
    /// <c>ProjectSettings/</c> (e.g. <c>InputManager.asset</c>, <c>TagManager.asset</c>,
    /// <c>AudioManager.asset</c>) that has no first-class scripting API. Used by the settings-group
    /// commands that must read/write those managers via serialized properties.
    /// </summary>
    internal static class ProjectSettingsAsset
    {
        public static SerializedObject Load(string assetPath)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (objects == null || objects.Length == 0 || objects[0] == null)
                throw new InvalidOperationException($"Could not load settings asset at '{assetPath}'.");

            return new SerializedObject(objects[0]);
        }
    }
}
