using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// The Scene Preview settings, stored at <see cref="ScenePreviewSettings.AssetPath"/>: the preview build
    /// profile and the assemblies a preview may contain. Edited from Project Settings > Pipeline > Editor.
    /// </summary>
    /// <remarks>
    /// An asset rather than a ProjectSettings file, so every Scene Preview import depends on it and is redone
    /// whenever it changes, including through version control and Project Service revisions.
    /// </remarks>
    sealed class ScenePreviewSettingsAsset : ScriptableObject
    {
        [SerializeField] string m_BuildProfileGuid = "";
        [SerializeField] List<string> m_PreservedAssemblies = new List<string>();

        /// <summary>GUID of the preview build profile, or an empty string.</summary>
        internal string BuildProfileGuid
        {
            get => m_BuildProfileGuid ?? string.Empty;
            set => m_BuildProfileGuid = value ?? string.Empty;
        }

        /// <summary>The assemblies a preview may contain and the preview player preserves. Empty means no filtering.</summary>
        internal IReadOnlyList<string> PreservedAssemblies => m_PreservedAssemblies;

        internal void SetPreservedAssemblies(IEnumerable<string> assemblies) =>
            m_PreservedAssemblies = assemblies.Distinct().OrderBy(name => name, System.StringComparer.Ordinal).ToList();
    }
}
