#if UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// The assemblies a preview may contain, kept in the Scene Preview settings asset: generated from the
    /// settings page, read by <see cref="PreviewSceneArtifactImporter"/> as its allow-list, and handed to
    /// UnityLinker as a link.xml for builds of the preview build profile only, so the game's own builds keep
    /// their normal stripping.
    /// </summary>
    class PreviewLinkXml : IUnityLinkerProcessor
    {
        // "UnityEngine" covers every engine module, including uGUI and UI Toolkit. VFX Graph, the render
        // pipelines and TextMeshPro ship as their own package assemblies.
        static readonly string[] k_AssemblyPrefixes =
        {
            "UnityEngine",
            "Unity.RenderPipelines",
            "Unity.VisualEffectGraph",
            "Unity.TextMeshPro",
        };

        internal static string BuildLinkXmlAbsolutePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "ScenePreview", "link.xml"));

        /// <summary>Set the allow-list to the engine and package assemblies loaded in this editor.</summary>
        internal static void Generate()
        {
            var editorOnly = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Where(a => (a.flags & AssemblyFlags.EditorAssembly) != 0)
                .Select(a => a.name));
            ScenePreviewSettings.SetPreservedAssemblies(PipelineUtils.GetLoadedAssemblies()
                .Select(a => a.GetName().Name)
                .Where(name => !name.EndsWith(".Editor") && !editorOnly.Contains(name) && k_AssemblyPrefixes.Any(prefix => name.StartsWith(prefix))));
            Debug.Log($"Scene Preview: generated the preview assemblies in {ScenePreviewSettings.AssetPath}. Rebuild the preview player to apply them.");
        }

        /// <summary>The assemblies a preview may contain. Empty, meaning no filtering, when none are set.</summary>
        internal static HashSet<string> ReadAllowList() => new HashSet<string>(ScenePreviewSettings.PreservedAssemblies);

        internal static string BuildLinkXml(IEnumerable<string> assemblies)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<linker>");
            foreach (var assemblyName in assemblies)
                builder.AppendLine($"  <assembly fullname=\"{SecurityElement.Escape(assemblyName)}\" preserve=\"all\"/>");
            builder.AppendLine("</linker>");
            return builder.ToString();
        }

        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var assemblies = ScenePreviewSettings.PreservedAssemblies;
            // Building from a build profile makes it the active one for the duration of the build.
            if (assemblies.Count == 0 || ScenePreviewSettings.ActivePreviewBuildProfile() == null)
                return string.Empty;

            var path = BuildLinkXmlAbsolutePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, BuildLinkXml(assemblies));
            Debug.Log($"Scene Preview: preserving the {assemblies.Count} preview assemblies of {ScenePreviewSettings.AssetPath} in this preview player build.");
            return path;
        }
    }
}
#endif
