using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    internal sealed class ReferenceFinderData
    {
        public enum AssetState : byte
        {
            Normal,
            Changed,
            Missing,
            Invalid
        }

        // 数据字典：GUID -> 资源描述
        public Dictionary<string, AssetDescription> assetDict = new Dictionary<string, AssetDescription>();

        // 引用计数缓存，避免重复计算
        private readonly Dictionary<(AssetDescription, AssetDescription), int> m_dictCache =
            new Dictionary<(AssetDescription, AssetDescription), int>();

        // 缓存文件路径
        private const string CachePath = "Library/ReferenceFinderCache";
        // 最小线程数
        public const int MinThreadCount = 8;
        // 单线程每次处理的资源数量
        private const int SingleThreadReadCount = 100;
        // 实际线程数：取CPU核心数和最小线程数的最大值
        private static readonly int ThreadCount = Math.Max(MinThreadCount, Environment.ProcessorCount);
        // 项目基础路径
        private static string m_basePath;

        // 需要读取文件内容进行依赖分析的文件类型
        private static readonly HashSet<string> FileExtension = new HashSet<string>
        {
            ".prefab",
            ".unity",
            ".mat",
            ".asset",
            ".anim",
            ".controller"
        };

        // 用于在文件中查找GUID的正则表达式
        private static readonly Regex GuidRegex = new Regex("guid: ([a-z0-9]{32})",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 每个线程独立的资源字典
        private readonly List<Dictionary<string, AssetDescription>> m_threadAssetDict =
            new List<Dictionary<string, AssetDescription>>();

        // 线程列表
        private readonly List<Thread> m_threadList = new List<Thread>();
        // 当前已读取的资源计数
        private int m_curReadAssetCount;
        // 总资源数量
        private int m_totalCount;
        // 所有资源路径数组
        public string[] allAssets;

        public void CollectDependenciesInfo()
        {
            try
            {
                m_basePath = Application.dataPath.Replace("/Assets", "");
                // 尝试从缓存读取
                ReadFromCache();
                // 获取所有资源路径
                allAssets = AssetDatabase.GetAllAssetPaths();
                m_totalCount = allAssets.Length;
                // 初始化多线程数据结构
                m_threadList.Clear();
                m_curReadAssetCount = 0;

                foreach (Dictionary<string, AssetDescription> i in m_threadAssetDict)
                {
                    i.Clear();
                }
                m_threadAssetDict.Clear();
                for (int i = 0; i < ThreadCount; i++)
                {
                    m_threadAssetDict.Add(new Dictionary<string, AssetDescription>());
                }
                bool allThreadFinish = false;

                // 启动多个线程处理资源
                for (int i = 0; i < ThreadCount; i++)
                {
                    ThreadStart method = ReadAssetInfo;
                    Thread readThread = new Thread(method);
                    m_threadList.Add(readThread);
                    readThread.Start();
                }

                // 等待所有线程完成，显示进度条
                while (!allThreadFinish)
                {
                    if (m_curReadAssetCount % 500 == 0 &&
                        EditorUtility.DisplayCancelableProgressBar("Updating", $"Handle {m_curReadAssetCount}",
                            (float)m_curReadAssetCount / m_totalCount))
                    {
                        // 用户取消操作
                        EditorUtility.ClearProgressBar();
                        foreach (Thread i in m_threadList)
                        {
                            i.Abort();
                        }
                        return;
                    }

                    allThreadFinish = true;

                    foreach (Thread i in m_threadList)
                    {
                        if (i.IsAlive)
                        {
                            allThreadFinish = false;
                            break;
                        }
                    }
                }

                // 合并各线程的数据
                foreach (Dictionary<string, AssetDescription> dict in m_threadAssetDict)
                {
                    foreach (KeyValuePair<string, AssetDescription> j in dict)
                    {
                        assetDict[j.Key] = j.Value;
                    }
                }
                // 写入缓存并更新引用信息
                EditorUtility.DisplayCancelableProgressBar("Updating", "Write cache", 1f);
                WriteToCache();
                EditorUtility.DisplayCancelableProgressBar("Updating", "Generate reference data", 1f);
                UpdateResourceReferenceInfo();
                EditorUtility.ClearProgressBar();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.ClearProgressBar();
            }
        }

        public bool ReadFromCache()
        {
            assetDict.Clear();
            ClearCache();

            if (File.Exists(CachePath))
            {
                // 从二进制文件读取缓存数据
                List<string> serializedGuid;
                List<string> serializedDependencyHash;
                List<int[]> serializedDependencies;

                using (FileStream fs = File.OpenRead(CachePath))
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    if (EditorUtility.DisplayCancelableProgressBar("Import Cache", "Reading Cache", 0))
                    {
                        EditorUtility.ClearProgressBar();
                        return false;
                    }

                    serializedGuid = (List<string>)bf.Deserialize(fs);
                    serializedDependencyHash = (List<string>)bf.Deserialize(fs);
                    serializedDependencies = (List<int[]>)bf.Deserialize(fs);
                    EditorUtility.ClearProgressBar();
                }

                // 重建依赖关系
                for (int i = 0; i < serializedGuid.Count; i++)
                {
                    string guid = serializedGuid[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (string.IsNullOrEmpty(path))
                    {
                        AssetDescription assetDescription = new AssetDescription()
                        {
                            name = Path.GetFileNameWithoutExtension(path),
                            path = path,
                            assetDependencyHashString = serializedDependencyHash[i]
                        };
                        assetDict.Add(guid, assetDescription);
                    }

                    if (assetDict.TryGetValue(guid, out var value))
                    {
                        List<string> guids = new List<string>();

                        foreach (var index in serializedDependencies[i])
                        {
                            string g = serializedGuid[index];

                            if (assetDict.ContainsKey(g))
                            {
                                guids.Add(g);
                            }
                        }

                        value.dependencies = guids;
                    }
                }

                UpdateResourceReferenceInfo();
                return true;
            }

            return false;
        }

        public void ReadAssetInfo()
        {
            int index = Thread.CurrentThread.ManagedThreadId % ThreadCount;
            // 每个线程处理的资源数量
            int intervalLength = m_totalCount / ThreadCount;
            int start = intervalLength * index;
            int end = start + intervalLength;
            if (m_totalCount - end < intervalLength)
            {
                end = m_totalCount;
            }
            int readAssetCount = 0;
            // 处理分配给该线程的资源范围
            for (int i = start; i < end; i++)
            {
                if (readAssetCount % SingleThreadReadCount == 0)
                {
                    m_curReadAssetCount += readAssetCount;
                    readAssetCount = 0;
                }

                GetAsset(m_basePath, allAssets[i]);
                readAssetCount++;
            }
        }

        public void GetAsset(string dataPath, string assetPath)
        {
            string extLowerStr = Path.GetExtension(assetPath).ToLower();
            // 判断是否需要读取文件内容
            bool needReadFile = FileExtension.Contains(extLowerStr);
            string fileName = $"{dataPath}/{assetPath}";
            string metaFile = $"{dataPath}/{assetPath}.meta";

            if (File.Exists(fileName) && File.Exists(metaFile))
            {
                // 从meta文件读取GUID
                string metaText = File.ReadAllText(metaFile, Encoding.UTF8);
                MatchCollection matchRs = GuidRegex.Matches(metaText);
                string selfGuid = matchRs[0].Groups[1].Value.ToLower();
                // 使用文件修改时间作为哈希
                string lastModifyTime = File.GetLastWriteTime(fileName).ToString(CultureInfo.InvariantCulture);
                MatchCollection guids = null;
                List<string> depend = new List<string>();
                // 如果需要，读取文件内容查找依赖的GUID
                if (needReadFile)
                {
                    string fileStr = File.ReadAllText(fileName, Encoding.UTF8);
                    guids = GuidRegex.Matches(fileStr);
                }

                int curListIndex = Thread.CurrentThread.ManagedThreadId % ThreadCount;
                Dictionary<string, AssetDescription> curDict = m_threadAssetDict[curListIndex];
                // 如果资源不存在或已更改，更新数据
                if (!curDict.ContainsKey(selfGuid) || curDict[selfGuid].assetDependencyHashString != lastModifyTime)
                {
                    if (guids != null)
                    {
                        for (int index = 0; index < guids.Count; ++index)
                        {
                            Match i = guids[index];
                            // 收集依赖的GUID
                            depend.Add(i.Groups[1].Value.ToLower());
                        }
                    }

                    AssetDescription ad = new AssetDescription
                    {
                        name = Path.GetFileNameWithoutExtension(assetPath),
                        path = assetPath,
                        assetDependencyHashString = lastModifyTime,
                        dependencies = depend
                    };

                    // 更新或添加资源描述
                    if (m_threadAssetDict[curListIndex].ContainsKey(selfGuid))
                    {
                        m_threadAssetDict[curListIndex][selfGuid] = ad;
                    }
                    else
                    {
                        m_threadAssetDict[curListIndex].Add(selfGuid, ad);
                    }
                }
            }
        }

        private void UpdateResourceReferenceInfo()
        {
            // 处理引用关系
            foreach (var asset in assetDict)
            {
                foreach (var assetGuid in asset.Value.dependencies)
                {
                    if (assetDict.ContainsKey(assetGuid))
                    {
                        assetDict[assetGuid].references.Add(asset.Key);
                    }
                }
            }
        }

        private void WriteToCache()
        {
            if (File.Exists(CachePath))
            {
                File.Delete(CachePath);
            }
            List<string> serializedGuid = new List<string>();
            List<string> serializedDependencyHash = new List<string>();
            List<int[]> serializedDependencies = new List<int[]>();
            Dictionary<string, int> guidIndex = new Dictionary<string, int>();
            using FileStream fs = File.OpenWrite(CachePath);

            // 建立GUID到索引的映射
            foreach (KeyValuePair<string, AssetDescription> pair in assetDict)
            {
                guidIndex.Add(pair.Key, guidIndex.Count);
                serializedGuid.Add(pair.Key);
                serializedDependencyHash.Add(pair.Value.assetDependencyHashString);
            }

            // 将依赖关系转换为索引数组
            foreach (string guid in serializedGuid)
            {
                List<int> res = new List<int>();

                foreach (string i in assetDict[guid].dependencies)
                {
                    if (guidIndex.TryGetValue(i, out var value))
                    {
                        res.Add(value);
                    }
                }

                int[] indexes = res.ToArray();
                serializedDependencies.Add(indexes);
            }

            // 序列化到文件
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, serializedGuid);
            bf.Serialize(fs, serializedDependencyHash);
            bf.Serialize(fs, serializedDependencies);
        }

        public void UpdateAssetState(string guid)
        {
            if (assetDict.TryGetValue(guid, out AssetDescription ad) && ad.state != AssetState.Invalid)
            {
                if (File.Exists(ad.path))
                {
                    ad.state = ad.assetDependencyHashString !=
                            File.GetLastWriteTime(ad.path).ToString(CultureInfo.InvariantCulture)
                        ? AssetState.Changed // 文件已修改
                        : AssetState.Normal;  // 文件未修改
                }
                else
                {
                    ad.state = AssetState.Missing; // 文件不存在
                }
            }
            else if (!assetDict.TryGetValue(guid, out ad))
            {
                // 新资源或无效资源
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ad = new AssetDescription
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    path = path,
                    state = AssetState.Invalid
                };
                assetDict.Add(guid, ad);
            }
        }

        public static string GetInfoByState(AssetState state)
        {
            if (state == AssetState.Changed)
                return "<color=red>缓存不匹配</color>";
            if (state == AssetState.Missing)
                return "<color=red>缓存丢失</color>";
            if (state == AssetState.Invalid)
                return "<color=yellow>缓存无效</color>";
            return "<color=green>缓存正常</color>";
        }

        private void ClearCache() => m_dictCache.Clear();

        internal sealed class AssetDescription
        {
            public string assetDependencyHashString;
            public List<string> dependencies = new List<string>();
            public string name = "";
            public string path = "";
            public List<string> references = new List<string>();
            public AssetState state = AssetState.Normal;
        }

        private int GetRefCount(string assetGUID, AssetDescription desc, List<string> guidStack)
        {
            // 检测循环引用
            if (guidStack.Contains(assetGUID))
            {
                Debug.Log("有循环引用, 计数可能不准确");
                return 0;
            }

            guidStack.Add(assetGUID);
            int total = 0;

            if (assetDict.TryGetValue(assetGUID, out AssetDescription value))
            {
                if (value.references.Count > 0)
                {
                    // 递归计算引用计数
                    Dictionary<string, int> cachedRefCount = new Dictionary<string, int>();

                    foreach (string refs in value.references)
                    {
                        if (!cachedRefCount.ContainsKey(refs))
                        {
                            int refCount = GetRefCount(refs, value, guidStack);
                            cachedRefCount[refs] = refCount;
                            total += refCount;
                        }
                    }
                }
                else
                {
                    total = 0;
                    // 处理直接依赖关系
                    if (desc != null)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(desc.path);

                        foreach (string deps in value.dependencies)
                        {
                            if (guid == deps)
                                total++;
                        }
                    }
                }
            }

            guidStack.RemoveAt(guidStack.Count - 1);
            return total;
        }

        public string GetRefCount(AssetDescription desc, AssetDescription parentDesc)
        {
            // 使用缓存避免重复计算
            if (m_dictCache.TryGetValue((desc, parentDesc), out int total))
                return total.ToString();
            string rootGUID = AssetDatabase.AssetPathToGUID(desc.path);
            List<string> guidInStack = new List<string> { rootGUID };
            Dictionary<string, int> cachedRefCount = new Dictionary<string, int>();
            // 计算所有引用的计数
            foreach (string refs in desc.references)
            {
                if (!cachedRefCount.ContainsKey(refs))
                {
                    int refCount = GetRefCount(refs, desc, guidInStack);
                    cachedRefCount[refs] = refCount;
                    total += refCount;
                }
            }
            // 处理没有引用但有直接依赖的情况
            if (desc.references.Count == 0 && parentDesc != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(desc.path);

                foreach (string refs in parentDesc.references)
                {
                    if (refs == guid)
                        total++;
                }
            }

            guidInStack.RemoveAt(guidInStack.Count - 1);
            m_dictCache.Add((desc, parentDesc), total);
            return total.ToString();
        }
    }
}