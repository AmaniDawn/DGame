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

            // 2. Asset path.
            if (!string.IsNullOrEmpty(handle.Path))
            {
                obj = AssetDatabase.LoadMainAssetAtPath(handle.Path);
                if (obj != null)
                    return true;

                error = $"No asset at path '{handle.Path}'.";
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
