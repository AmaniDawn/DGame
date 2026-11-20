using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Authoring
{
    /// <summary>
    /// Resolves an agent-supplied <see cref="ObjectRef"/> handle to a live UnityEngine.Object, and
    /// describes any object back into a canonical <see cref="AuthoringResult"/> identity.
    /// Backed by GlobalObjectId, which addresses both assets (GUID + fileId) and scene objects.
    /// </summary>
    public static class ObjectResolver
    {
        /// <summary>
        /// Try to resolve a handle to a loaded object. Returns false with an <paramref name="error"/>
        /// when the handle is empty or does not resolve.
        /// </summary>
        /// <param name="handle">The agent-supplied object reference.</param>
        /// <param name="obj">The resolved object, or null on failure.</param>
        /// <param name="error">A human-readable reason on failure, or null on success.</param>
        /// <returns>True if the handle resolved.</returns>
        public static bool TryResolve(ObjectRef handle, out Object obj, out string error)
        {
            obj = null;
            error = null;

            if (handle == null || handle.IsEmpty)
            {
                error = "Empty object reference.";
                return false;
            }

            // 1. Canonical GlobalObjectId.
            if (!string.IsNullOrEmpty(handle.GlobalId))
            {
                if (!GlobalObjectId.TryParse(handle.GlobalId, out var gid))
                {
                    error = $"Invalid globalId '{handle.GlobalId}'.";
                    return false;
                }

                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj != null)
                    return true;

                error = $"globalId '{handle.GlobalId}' did not resolve to a loaded object.";
                return false;
            }

            // 2. Asset path. Explicit "Assets/"/"Packages/"-rooted paths load as-is; a bare relative
            // path (e.g. "Materials/Floor.mat") is normalized under the authoring root first. If it
            // does not resolve to an asset, fall back to a hierarchy lookup so dotted GameObject names
            // (e.g. "Cube.001") still resolve, and report every strategy that was tried.
            if (!string.IsNullOrEmpty(handle.Path))
            {
                var raw = handle.Path;

                // "<asset>:<child path>" addresses a GameObject inside a prefab (or model) asset, but only
                // when the part before ':' loads as an asset. Otherwise the whole string resolves as
                // usual, so a scene object literally named "Cube.001:Child" still resolves.
                var hasChildPath = TrySplitChildPath(raw, out var assetPart, out var childPath);
                if (hasChildPath)
                {
                    var container = LoadAsset(assetPart, out _);
                    if (container != null)
                        return TryFindChild(container, assetPart, childPath, out obj, out error);
                }

                var explicitlyRooted = IsExplicitlyRooted(raw);
                obj = LoadAsset(raw, out var assetPath);
                if (obj != null)
                    return true;

                if (explicitlyRooted)
                {
                    error = hasChildPath
                        ? $"No asset at path '{assetPart}' (from '{raw}')."
                        : $"No asset at path '{raw}'.";
                    return false;
                }

                // A bare relative string may actually be a scene hierarchy path with a dotted name.
                var go = FindByHierarchyPath(raw);
                if (go != null)
                {
                    obj = go;
                    return true;
                }

                error = $"Could not resolve '{raw}': no asset at '{assetPath}', no GameObject at hierarchy path '{raw}'.";
                return false;
            }

            // 3. GUID (+ optional fileId for sub-assets).
            if (!string.IsNullOrEmpty(handle.Guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(handle.Guid);
                if (string.IsNullOrEmpty(path))
                {
                    error = $"Unknown GUID '{handle.Guid}'.";
                    return false;
                }

                if (handle.FileId.HasValue)
                {
                    foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (candidate != null &&
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long localId) &&
                            localId == handle.FileId.Value)
                        {
                            obj = candidate;
                            return true;
                        }
                    }

                    error = $"No sub-asset with fileId {handle.FileId} in '{path}'.";
                    return false;
                }

                obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null)
                    return true;

                error = $"Could not load asset '{path}'.";
                return false;
            }

            // 4. Instance id (loaded/scene object).
            if (handle.InstanceId.HasValue)
            {
                obj = PipelineUtils.IdToObject(handle.InstanceId.Value);
                if (obj != null)
                    return true;

                error = $"No loaded object with instanceId {handle.InstanceId}.";
                return false;
            }

            // 5. Scene hierarchy path.
            if (!string.IsNullOrEmpty(handle.HierarchyPath))
            {
                var go = FindByHierarchyPath(handle.HierarchyPath);
                if (go != null)
                {
                    obj = go;
                    return true;
                }

                error = $"No GameObject at hierarchy path '{handle.HierarchyPath}'.";
                return false;
            }

            error = "Empty object reference.";
            return false;
        }

        /// <summary>
        /// Produce the canonical identity for an object (assets get path/guid/fileId; loaded objects
        /// get instanceId/hierarchyPath). Returns null for a null object.
        /// </summary>
        /// <param name="obj">The object to describe.</param>
        /// <returns>The object's canonical identity, or null if <paramref name="obj"/> is null.</returns>
        public static AuthoringResult Describe(Object obj)
        {
            if (obj == null)
                return null;

            var result = new AuthoringResult { Type = obj.GetType().Name };

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                result.AssetPath = assetPath;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId))
                {
                    result.Guid = guid;
                    result.FileId = localId;
                }
            }
            else
            {
                result.InstanceId = PipelineUtils.GetObjectId(obj);
                var go = obj as GameObject ?? (obj as Component)?.gameObject;
                if (go != null)
                    result.HierarchyPath = GetHierarchyPath(go);
            }

            // Not every object has a global id (e.g. transient objects); ignore failures.
            try
            {
                result.GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
            }
            catch
            {
                // leave GlobalId null
            }

            return result;
        }

        /// <summary>True when a path is explicitly rooted at "Assets/" or "Packages/" (or is exactly one of those).</summary>
        private static bool IsExplicitlyRooted(string path) =>
            path == "Assets" || path == "Packages"
            || path.StartsWith("Assets/", System.StringComparison.Ordinal)
            || path.StartsWith("Packages/", System.StringComparison.Ordinal);

        /// <summary>
        /// Load the main asset at a path. An "Assets/"/"Packages/"-rooted path loads as-is; a bare
        /// relative one is normalized under the authoring root first. Null when nothing is there.
        /// </summary>
        private static Object LoadAsset(string raw, out string assetPath)
        {
            assetPath = raw;
            if (!IsExplicitlyRooted(raw))
            {
                var resolved = ProjectPaths.Resolve(raw, out _);
                if (!string.IsNullOrEmpty(resolved))
                    assetPath = resolved;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        /// <summary>
        /// Split "Assets/UI/Screen.prefab:Canvas/Header" at the first ':' that follows a file name with
        /// an extension. A drive letter ("C:/...") has no extension before its ':' and is left alone.
        /// </summary>
        private static bool TrySplitChildPath(string path, out string assetPath, out string childPath)
        {
            assetPath = childPath = null;
            var separator = path.IndexOf(':');
            if (separator <= 0)
                return false;

            var candidate = path.Substring(0, separator);
            if (!System.IO.Path.HasExtension(candidate))
                return false;

            assetPath = candidate;
            childPath = path.Substring(separator + 1);
            return true;
        }

        /// <summary>
        /// Find a GameObject inside a prefab or model asset. The path is relative to the asset's root;
        /// it may also start with the root's own name, and naming only the root returns the root.
        /// </summary>
        private static bool TryFindChild(Object asset, string assetPath, string childPath, out Object obj, out string error)
        {
            obj = null;
            error = null;
            if (!(asset is GameObject root))
            {
                error = $"'{assetPath}' is a {asset.GetType().Name}, not a prefab; " +
                        "a ':<child path>' suffix addresses a GameObject inside a prefab or model asset.";
                return false;
            }

            var trimmed = childPath.Trim('/');
            Transform found = null;
            if (trimmed.Length == 0 || trimmed == root.name)
                found = root.transform;
            else
                found = root.transform.Find(trimmed)
                    ?? (trimmed.StartsWith(root.name + "/", System.StringComparison.Ordinal)
                        ? root.transform.Find(trimmed.Substring(root.name.Length + 1))
                        : null);

            if (found == null)
            {
                var children = new System.Collections.Generic.List<string>();
                foreach (Transform child in root.transform)
                    children.Add(child.name);
                error = $"No GameObject at '{childPath}' in '{assetPath}' (root '{root.name}'; its children: " +
                        $"{(children.Count > 0 ? string.Join(", ", children) : "none")}).";
                return false;
            }

            obj = found.gameObject;
            return true;
        }

        private static GameObject FindByHierarchyPath(string path)
        {
            var parts = path.Trim('/').Split('/');
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                return null;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name != parts[0])
                        continue;

                    var current = root.transform;
                    var matched = true;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var child = current.Find(parts[i]);
                        if (child == null)
                        {
                            matched = false;
                            break;
                        }

                        current = child;
                    }

                    if (matched)
                        return current.gameObject;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var sb = new System.Text.StringBuilder(go.name);
            var parent = go.transform.parent;
            while (parent != null)
            {
                sb.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return "/" + sb;
        }
    }
}
