#if UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Builds a Scene Preview archive locally, without Project Service, so a preview player's loading
    /// can be tested offline.
    /// </summary>
    public static class PreviewProducer
    {
        /// <summary>Build the archive for an asset and copy it to <paramref name="outputPath"/>.</summary>
        /// <param name="assetPath">Project-relative path of the asset.</param>
        /// <param name="outputPath">Where to write the .ca file.</param>
        /// <returns>True when the archive was written.</returns>
        public static bool ProduceToFile(string assetPath, string outputPath)
        {
            if (!PreviewSceneArtifactImporter.Produce(assetPath))
                return false;

            var archivePath = PreviewSceneArtifactImporter.GetArchivePath(assetPath);
            if (string.IsNullOrEmpty(archivePath))
            {
                Debug.LogError($"Scene Preview: the import of '{assetPath}' produced no archive.");
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                FileUtil.ReplaceFile(archivePath, outputPath);
                Debug.Log($"Scene Preview: wrote '{outputPath}' ({new FileInfo(outputPath).Length} bytes) for '{assetPath}'.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Scene Preview: could not write '{outputPath}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Batch-mode entry point: <c>-executeMethod Unity.Pipeline.Editor.Previews.PreviewProducer.ProduceFromCommandLine
        /// -previewAsset &lt;assetPath&gt; -previewOut &lt;file&gt;</c>. Exits with 0 on success, 1 on failure.
        /// </summary>
        public static void ProduceFromCommandLine()
        {
            var assetPath = GetArgument("-previewAsset");
            var outputPath = GetArgument("-previewOut");
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("Scene Preview: pass -previewAsset <assetPath> -previewOut <file>.");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(ProduceToFile(assetPath, outputPath) ? 0 : 1);
        }

        static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            }
            return null;
        }
    }
}
#endif
