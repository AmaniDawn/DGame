using System.IO;
using Unity.Pipeline.ScenePreview;
using UnityEditor.Build.Content;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>Writes the <see cref="ContentArchiveManifest"/> packed into every Scene Preview archive.</summary>
    static class ContentArchiveManifestWriter
    {
        /// <summary>Write the manifest into <paramref name="outputFolder"/> and return it as an archive entry.</summary>
        internal static ResourceFile Write(string outputFolder, string contentFileName, bool requiresDefaultResources,
            int version = ContentArchiveManifest.CurrentVersion)
        {
            var manifest = new ContentArchiveManifest(contentFileName, requiresDefaultResources, version);
            var path = Path.Combine(outputFolder, ContentArchiveManifest.FileName);
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            return new ResourceFile { fileName = path, fileAlias = ContentArchiveManifest.FileName };
        }
    }
}
