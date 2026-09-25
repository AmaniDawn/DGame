using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Editor.Authoring
{
    /// <summary>
    /// Project-path handling for authoring commands.
    ///
    /// - Paths resolve relative to the configurable <see cref="AuthoringRoot"/> (default "Assets"),
    ///   so callers can pass bare paths ("Gameplay/Enemies") without repeating the "Assets/" prefix.
    /// - Explicit "Assets/..." (or "Packages/...") paths are treated as project-relative as-is.
    /// - Every resolved path is confined to <see cref="AuthoringRoot"/> (the sandbox). With the
    ///   default root ("Assets") that means full Assets access; set a sub-folder to confine an agent.
    /// - "../" traversal and writes outside the project root are always rejected.
    ///
    /// NOTE: minimal seed for the shared safety policy (CAT-2509).
    /// </summary>
    public static class ProjectPaths
    {
        private const string DefaultRoot = "Assets";

        // Per-project EditorPrefs key so the root doesn't leak across projects on the same machine.
        private static string PrefKey => $"Unity.Pipeline.AuthoringRoot:{Application.dataPath}";

        /// <summary>Absolute path to the project root (the folder that contains Assets/).</summary>
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// Base folder (project-relative, under Assets/) that bare paths resolve against and that all
        /// authoring writes are confined to. Defaults to "Assets". Persisted per project via EditorPrefs.
        /// Setting an invalid value (outside Assets/ or containing "..") throws.
        /// </summary>
        public static string AuthoringRoot
        {
            get
            {
                var root = EditorPrefs.GetString(PrefKey, DefaultRoot);
                return string.IsNullOrEmpty(root) ? DefaultRoot : root;
            }
            set
            {
                var normalized = NormalizeRoot(value, out var error);
                if (normalized == null)
                    throw new ArgumentException(error);
                EditorPrefs.SetString(PrefKey, normalized);
            }
        }

        /// <summary>Reset the authoring root to the default ("Assets").</summary>
        public static void ResetAuthoringRoot() => EditorPrefs.DeleteKey(PrefKey);

        /// <summary>
        /// Resolve an agent-supplied path to a normalized, project-relative path confined to
        /// <see cref="AuthoringRoot"/>. Bare paths are taken relative to the root; explicit
        /// "Assets/..." paths and absolute paths under the project are honored. Returns null with an
        /// <paramref name="error"/> when the path escapes the root or the project.
        /// </summary>
        public static string Resolve(string path, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Path is required.";
                return null;
            }

            var root = AuthoringRoot;
            var normalized = path.Replace('\\', '/').Trim();

            // Absolute path: must live under the project root; convert to project-relative.
            if (Path.IsPathRooted(normalized))
            {
                var full = Path.GetFullPath(normalized).Replace('\\', '/');
                var projectRoot = ProjectRoot.Replace('\\', '/').TrimEnd('/');
                if (!full.Equals(projectRoot, StringComparison.OrdinalIgnoreCase) &&
                    !full.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Path '{path}' is outside the project root '{projectRoot}'.";
                    return null;
                }

                normalized = full.Length > projectRoot.Length ? full.Substring(projectRoot.Length + 1) : string.Empty;
            }

            normalized = normalized.TrimStart('/');

            if (normalized.Split('/').Contains(".."))
            {
                error = $"Path '{path}' must not contain '..'.";
                return null;
            }

            // Explicit project-relative paths ("Assets/..." / "Packages/...") are used as-is; anything
            // else is a bare path taken relative to the authoring root.
            var isProjectRelative =
                normalized == "Assets" || normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                normalized == "Packages" || normalized.StartsWith("Packages/", StringComparison.Ordinal);

            var resolved = (isProjectRelative ? normalized : CombineUnder(root, normalized)).TrimEnd('/');

            // Confine to the authoring root (default "Assets" => full Assets access).
            if (!(resolved == root || resolved.StartsWith(root + "/", StringComparison.Ordinal)))
            {
                error = $"Path '{path}' resolves to '{resolved}', which is outside the authoring root '{root}'.";
                return null;
            }

            return resolved;
        }

        private static string CombineUnder(string root, string relative)
        {
            relative = relative.TrimStart('/');
            return string.IsNullOrEmpty(relative) ? root : $"{root}/{relative}";
        }

        private static string NormalizeRoot(string value, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Authoring root is required.";
                return null;
            }

            var normalized = value.Replace('\\', '/').Trim().TrimStart('/').TrimEnd('/');
            if (normalized.Split('/').Contains(".."))
            {
                error = $"Authoring root '{value}' must not contain '..'.";
                return null;
            }

            if (!(normalized == "Assets" || normalized.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                error = $"Authoring root '{value}' must be under the project's Assets/ folder.";
                return null;
            }

            return normalized;
        }
    }
}
