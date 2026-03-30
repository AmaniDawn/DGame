#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class SpritePostprocessor : AssetPostprocessor
    {
        private static List<string> m_resourcesToDelete = new List<string>();

        #region 缓存相关

        // 文件名缓存：key=小写文件名(不含扩展名), value=完整路径列表
        private static Dictionary<string, HashSet<string>> s_fileNameCache;
        private static bool s_cacheInitialized = false;

        /// <summary>
        /// 初始化文件名缓存
        /// </summary>
        private static void EnsureCacheInitialized()
        {
            if (s_cacheInitialized && s_fileNameCache != null)
            {
                return;
            }

            s_fileNameCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var config = AtlasConfig.Instance;

            var tempRootDirArr = new List<string>(config.sourceAtlasRootDir);
            tempRootDirArr.AddRange(config.rootChildAtlasDir);

            foreach (var rootDir in tempRootDirArr)
            {
                if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) continue;

                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories)
                    .Where(CheckIsValidImageFile);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    var normalizedPath = Path.GetFullPath(file).Replace("\\", "/");

                    if (!s_fileNameCache.TryGetValue(fileName, out var pathSet))
                    {
                        pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        s_fileNameCache[fileName] = pathSet;
                    }
                    pathSet.Add(normalizedPath);
                }
            }

            s_cacheInitialized = true;
        }

        /// <summary>
        /// 重置缓存（配置变更时调用）
        /// </summary>
        public static void ResetCache()
        {
            s_cacheInitialized = false;
            s_fileNameCache?.Clear();
            s_fileNameCache = null;
        }

        /// <summary>
        /// 从缓存中移除文件
        /// </summary>
        private static void RemoveFromCache(string assetPath)
        {
            if (s_fileNameCache == null) return;

            var fileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            var normalizedPath = Path.GetFullPath(assetPath).Replace("\\", "/");

            if (s_fileNameCache.TryGetValue(fileName, out var pathSet))
            {
                pathSet.Remove(normalizedPath);
                if (pathSet.Count == 0)
                {
                    s_fileNameCache.Remove(fileName);
                }
            }
        }

        /// <summary>
        /// 添加文件到缓存
        /// </summary>
        private static void AddToCache(string assetPath)
        {
            if (s_fileNameCache == null) return;

            var fileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            var normalizedPath = Path.GetFullPath(assetPath).Replace("\\", "/");

            if (!s_fileNameCache.TryGetValue(fileName, out var pathSet))
            {
                pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                s_fileNameCache[fileName] = pathSet;
            }
            pathSet.Add(normalizedPath);
        }

        #endregion

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            m_resourcesToDelete.Clear();
            var config = AtlasConfig.Instance;

            if (!config.autoGenerate)
            {
                return;
            }

            // 计算需要处理的资源总数（用于进度显示）
            int totalAssets = (importedAssets?.Length ?? 0) + (deletedAssets?.Length ?? 0) + (movedAssets?.Length ?? 0);
            bool showProgress = totalAssets > 10; // 超过10个资源时显示进度条

            try
            {
                if (showProgress)
                {
                    EditorUtility.DisplayProgressBar("处理图集资源", "正在分析资源变更...", 0f);
                }

                ProcessAssetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
            catch (Exception e)
            {
                Debug.LogError($"图集生成失败：{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }

                bool isDelete = m_resourcesToDelete.Count > 0;
                foreach (var res in m_resourcesToDelete)
                {
                    AssetDatabase.DeleteAsset(res);
                }

                if (isDelete)
                {
                    Debug.LogError($"<color=red>针对 AtlasConfig.sourceAtlasRootDir 路径下资源</color>\n<color=red>移除了空格和同名资源，请检查重新合入相关资源</color>");
                }
                AssetDatabase.Refresh();
            }
        }

        private static void ProcessAssetChanges(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            // 处理删除的资源（先处理删除，再处理导入）
            ProcessAssets(deletedAssets, (path) =>
            {
                RemoveFromCache(path);
                EditorSpriteSaveInfo.OnDeleteSprite(path);
                LogProcessed("[Deleted]", path);
            }, true);

            // 处理导入的资源
            ProcessAssets(importedAssets, (path) =>
            {
                AddToCache(path);
                EditorSpriteSaveInfo.OnImportSprite(path);
                LogProcessed("[Added]", path);
            }, false);

            ProcessMovedAssets(movedFromAssetPaths, movedAssets);
        }

        private static void ProcessMovedAssets(string[] oldPaths, string[] newPaths)
        {
            if (oldPaths == null || newPaths == null)
            {
                return;
            }

            for (int i = 0; i < oldPaths.Length; i++)
            {
                if (ShouldProcessAsset(oldPaths[i]))
                {
                    RemoveFromCache(oldPaths[i]);
                    EditorSpriteSaveInfo.OnDeleteSprite(oldPaths[i]);
                    LogProcessed("[Moved From]", oldPaths[i]);
                    EditorSpriteSaveInfo.MarkParentAtlasesDirty(oldPaths[i], true);
                }

                if (ShouldProcessAsset(newPaths[i]))
                {
                    if (CheckFileNameContainsSpace(newPaths[i]) || CheckDuplicateAssetName(newPaths[i]) || ChangeSpriteTextureType(newPaths[i]))
                    {
                        continue;
                    }
                    AddToCache(newPaths[i]);
                    EditorSpriteSaveInfo.OnImportSprite(newPaths[i]);
                    LogProcessed("[Moved To]", newPaths[i]);
                    EditorSpriteSaveInfo.MarkParentAtlasesDirty(newPaths[i], false);
                }
            }
        }

        private static void LogProcessed(string operation, string path)
        {
            if (AtlasConfig.Instance.enableLogging)
            {
                Debug.Log($"<b>[{operation}]</b> {Path.GetFileName(path)}\nPath: {path}");
            }
        }

        private static void ProcessAssets(string[] importedAssets, Action<string> processor, bool isDelete = false)
        {
            if (importedAssets == null || importedAssets.Length <= 0)
            {
                return;
            }

            foreach (var path in importedAssets)
            {
                if (ShouldProcessAsset(path))
                {
                    if (!isDelete && (CheckFileNameContainsSpace(path) || CheckDuplicateAssetName(path) || ChangeSpriteTextureType(path)))
                    {
                        continue;
                    }
                    processor?.Invoke(path);
                }
            }
        }

        private static bool CheckFileNameContainsSpace(string assetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (fileName.Contains(" "))
            {
                m_resourcesToDelete.Add(assetPath);
                Debug.LogError($"<color=red>发现资源名存在空格: {assetPath}</color>");
                return true;
            }
            return false;
        }

        private static bool CheckDuplicateAssetName(string assetPath)
        {
            // 确保缓存已初始化
            EnsureCacheInitialized();

            var currentFileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            var normalizedCurrentPath = Path.GetFullPath(assetPath).Replace("\\", "/");

            // 使用缓存快速查找同名文件
            if (s_fileNameCache.TryGetValue(currentFileName, out var existingPaths))
            {
                // 收集需要移除的过期路径
                List<string> pathsToRemove = null;

                foreach (var existingPath in existingPaths)
                {
                    // 跳过自身
                    if (existingPath.Equals(normalizedCurrentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // 确保文件确实存在（防止缓存过期）
                    if (File.Exists(existingPath))
                    {
                        m_resourcesToDelete.Add(assetPath);
                        Debug.LogError($"<color=red>发现同名资源冲突: 合入资源: {assetPath} 存在资源: {existingPath}</color>");
                        return true;
                    }
                    else
                    {
                        // 文件不存在，标记为需要移除
                        pathsToRemove ??= new List<string>();
                        pathsToRemove.Add(existingPath);
                    }
                }

                // 遍历结束后再移除过期路径
                if (pathsToRemove != null)
                {
                    foreach (var path in pathsToRemove)
                    {
                        existingPaths.Remove(path);
                    }
                }
            }

            return false;
        }

        private static bool ChangeSpriteTextureType(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return false;
            }
            bool isChange = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                isChange = true;
            }

            if (AtlasConfig.Instance.checkMipmaps)
            {
                if (AtlasConfig.Instance.enableMipmaps && !importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    isChange = true;
                }
                else if (!AtlasConfig.Instance.enableMipmaps && importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    isChange = true;
                }
            }

            if (isChange)
            {
                LogProcessed("[Sprite Import Changed Reimport]", path);
                importer.SaveAndReimport();
            }
            return isChange;
        }

        private static bool ShouldProcessAsset(string assetPath)
        {
            var config = AtlasConfig.Instance;

            if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("Packages/")
                || !CheckIsShowProcessPath(assetPath)//!assetPath.StartsWith(config.sourceAtlasRootDir)
                || CheckIsExcludeFolder(assetPath)//assetPath.StartsWith(config.excludeFolder)
                || !CheckIsValidImageFile(assetPath))
            {
                return false;
            }

            foreach (var keyword in config.excludeKeywords)
            {
                if (assetPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckIsExcludeFolder(string assetPath)
        {
            foreach (var rootPath in AtlasConfig.Instance.excludeFolder)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (assetPath.StartsWith(tempPath + "/"))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckIsShowProcessPath(string assetPath)
        {
            var tempRootDirArr = new List<string>(AtlasConfig.Instance.sourceAtlasRootDir);
            tempRootDirArr.AddRange(AtlasConfig.Instance.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!assetPath.StartsWith(tempPath + "/"))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static bool CheckIsValidImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg");
        }
    }
}

#endif