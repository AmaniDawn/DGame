using System;
using System.Collections.Generic;
using System.Text;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public sealed class RedDotModule : Singleton<RedDotModule>
    {
        #region Constants

        public const char PATH_SEPARATOR = '/';
        public const string ROOT_PATH = "Root";
        public const int ROOT_ID = 0;

        #endregion

        #region Fields

        private readonly Dictionary<int, RedDotNode> m_nodeDict = new Dictionary<int, RedDotNode>(128);
        private readonly Dictionary<string, int> m_pathToIdMap = new Dictionary<string, int>(128);
        private readonly List<string> m_pathCache = new List<string>(8);
        private readonly StringBuilder m_stringBuilder = new StringBuilder(128);
        private int m_nextAutoId = 10000; // 自动分配 ID 的起始值（用于兼容模式）

        #endregion

        #region Properties

        public RedDotNode Root { get; private set; }

        public bool IsInitialized { get; private set; }

        public int NodeCount => m_nodeDict.Count;

        #endregion

        #region Initialization

        public void Initialize()
        {
            RedDotPathDefine_Gen.RegisterAll();
        }

        protected override void OnInit()
        {
            if (IsInitialized)
                return;

            Root = new RedDotNode(ROOT_ID, ROOT_PATH, ROOT_PATH);
            m_nodeDict.Add(ROOT_ID, Root);
            m_pathToIdMap.Add(ROOT_PATH, ROOT_ID);
            IsInitialized = true;

            // DLogger.Info("[RedDotModule] Initialized");
        }

        public void Reset()
        {
            m_nodeDict.Clear();
            m_pathToIdMap.Clear();
            m_nextAutoId = 10000;
            Root = new RedDotNode(ROOT_ID, ROOT_PATH, ROOT_PATH);
            m_nodeDict.Add(ROOT_ID, Root);
            m_pathToIdMap.Add(ROOT_PATH, ROOT_ID);

            // DLogger.Info("[RedDotModule] Reset");
        }

        protected override void OnDestroy()
        {
            Reset();
            IsInitialized = false;
        }

        #endregion

        #region Node Operations

        /// <summary>
        /// 注册红点节点（使用预分配的 ID，推荐）
        /// </summary>
        public RedDotNode Register(int id, string path, string[] segments,
            RedDotType type = RedDotType.Dot, RedDotAggregateStrategy strategy = RedDotAggregateStrategy.Or)
        {
            if (string.IsNullOrEmpty(path) || segments == null || segments.Length == 0)
            {
                DLogger.Error("[RedDotModule] Path and segments cannot be empty");
                return null;
            }

            if (m_nodeDict.TryGetValue(id, out var existingNode))
            {
                return existingNode;
            }

            return InternalRegister(id, path, segments, type, strategy);
        }

        /// <summary>
        /// 注册红点节点（兼容模式，自动分配 ID）
        /// </summary>
        public RedDotNode Register(string path, RedDotType type = RedDotType.Dot,
            RedDotAggregateStrategy strategy = RedDotAggregateStrategy.Or)
        {
            if (string.IsNullOrEmpty(path))
            {
                DLogger.Error("[RedDotModule] Path cannot be empty");
                return null;
            }

            // 如果路径已注册，直接返回
            if (m_pathToIdMap.TryGetValue(path, out int existingId))
            {
                return m_nodeDict[existingId];
            }

            SplitPath(path, m_pathCache);

            return InternalRegisterCompat(path, m_pathCache, type, strategy);
        }

        /// <summary>
        /// 注册红点节点（使用预计算的路径分段，兼容模式）
        /// </summary>
        public RedDotNode Register(string path, string[] segments, RedDotType type = RedDotType.Dot,
            RedDotAggregateStrategy strategy = RedDotAggregateStrategy.Or)
        {
            if (string.IsNullOrEmpty(path) || segments == null || segments.Length == 0)
            {
                DLogger.Error("[RedDotModule] Path and segments cannot be empty");
                return null;
            }

            if (m_pathToIdMap.TryGetValue(path, out int existingId))
            {
                return m_nodeDict[existingId];
            }

            return InternalRegisterCompat(path, segments, type, strategy);
        }

        private RedDotNode InternalRegister(int id, string fullPath, IList<string> segments,
            RedDotType type, RedDotAggregateStrategy strategy)
        {
            RedDotNode parent = Root;
            m_stringBuilder.Clear();

            for (int i = 0; i < segments.Count; i++)
            {
                string segment = segments[i];

                if (m_stringBuilder.Length > 0)
                {
                    m_stringBuilder.Append(PATH_SEPARATOR);
                }

                m_stringBuilder.Append(segment);

                string currentPath = m_stringBuilder.ToString();
                bool isLast = i == segments.Count - 1;
                int currentId = isLast ? id : m_nextAutoId++;

                if (!m_pathToIdMap.TryGetValue(currentPath, out int nodeId))
                {
                    var node = new RedDotNode(currentId, currentPath, segment);
                    m_nodeDict.Add(currentId, node);
                    m_pathToIdMap.Add(currentPath, currentId);
                    parent.AddChild(node);

                    if (isLast)
                    {
                        node.Type = type;
                        node.AggregateStrategy = strategy;
                    }

                    parent = node;
                }
                else
                {
                    parent = m_nodeDict[nodeId];

                    if (isLast)
                    {
                        parent.Type = type;
                        parent.AggregateStrategy = strategy;
                    }
                }
            }

            return parent;
        }

        private RedDotNode InternalRegisterCompat(string fullPath, IList<string> segments,
            RedDotType type, RedDotAggregateStrategy strategy)
        {
            RedDotNode parent = Root;
            m_stringBuilder.Clear();

            for (int i = 0; i < segments.Count; i++)
            {
                string segment = segments[i];

                if (m_stringBuilder.Length > 0)
                {
                    m_stringBuilder.Append(PATH_SEPARATOR);
                }

                m_stringBuilder.Append(segment);
                bool isLast = i == segments.Count - 1;

                string currentPath = m_stringBuilder.ToString();

                if (!m_pathToIdMap.TryGetValue(currentPath, out int nodeId))
                {
                    int newId = m_nextAutoId++;
                    var node = new RedDotNode(newId, currentPath, segment);
                    m_nodeDict.Add(newId, node);
                    m_pathToIdMap.Add(currentPath, newId);
                    parent.AddChild(node);
                    parent = node;
                }
                else
                {
                    parent = m_nodeDict[nodeId];
                }

                if (isLast)
                {
                    parent.Type = type;
                    parent.AggregateStrategy = strategy;
                }
            }

            return parent;
        }

        /// <summary>
        /// 获取节点（通过 ID）
        /// </summary>
        public RedDotNode GetNode(int id) => m_nodeDict.GetValueOrDefault(id);

        /// <summary>
        /// 获取节点（通过路径，兼容模式）
        /// </summary>
        public RedDotNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (m_pathToIdMap.TryGetValue(path, out int id))
            {
                return GetNode(id);
            }

            return null;
        }

        /// <summary>
        /// 检查节点是否存在（通过 ID）
        /// </summary>
        public bool HasNode(int id) => m_nodeDict.ContainsKey(id);

        /// <summary>
        /// 检查节点是否存在（通过路径，兼容模式）
        /// </summary>
        public bool HasNode(string path)
            => !string.IsNullOrEmpty(path) && m_pathToIdMap.ContainsKey(path);

        /// <summary>
        /// 注销节点（通过 ID）
        /// </summary>
        public void Unregister(int id)
        {
            if (!m_nodeDict.TryGetValue(id, out var node))
            {
                return;
            }

            node.RemoveAllListeners();
            node.Parent?.RemoveChild(node);
            m_pathToIdMap.Remove(node.Path);
            m_nodeDict.Remove(id);
        }

        /// <summary>
        /// 注销节点（通过路径，兼容模式）
        /// </summary>
        public void Unregister(string path)
        {
            if (m_pathToIdMap.TryGetValue(path, out int id))
            {
                Unregister(id);
            }
        }

        #endregion

        #region Value Operations

        /// <summary>
        /// 设置红点数值（通过 ID）
        /// </summary>
        public void SetValue(int id, int value)
        {
            RedDotNode node = GetNode(id);

            if (node == null)
            {
                DLogger.Warning($"[RedDotModule] Node not found: {id}");
                return;
            }

            node.SetValue(value);
        }

        /// <summary>
        /// 设置红点数值（通过路径，兼容模式）
        /// </summary>
        public void SetValue(string path, int value)
        {
            RedDotNode node = GetNode(path);

            if (node == null)
            {
                DLogger.Warning($"[RedDotModule] Node not found: {path}");
                return;
            }

            node.SetValue(value);
        }

        /// <summary>
        /// 增加红点数值（通过 ID）
        /// </summary>
        public void AddValue(int id, int delta)
        {
            RedDotNode node = GetNode(id);

            if (node == null)
            {
                DLogger.Warning($"[RedDotModule] Node not found: {id}");
                return;
            }

            node.AddValue(delta);
        }

        /// <summary>
        /// 增加红点数值（通过路径，兼容模式）
        /// </summary>
        public void AddValue(string path, int delta)
        {
            RedDotNode node = GetNode(path);

            if (node == null)
            {
                DLogger.Warning($"[RedDotModule] Node not found: {path}");
                return;
            }

            node.AddValue(delta);
        }

        /// <summary>
        /// 获取红点数值（通过 ID）
        /// </summary>
        public int GetValue(int id)
        {
            RedDotNode node = GetNode(id);
            return node != null ? node.Value : 0;
        }

        /// <summary>
        /// 获取红点数值（通过路径，兼容模式）
        /// </summary>
        public int GetValue(string path)
        {
            RedDotNode node = GetNode(path);
            return node != null ? node.Value : 0;
        }

        /// <summary>
        /// 检查是否显示红点（通过 ID）
        /// </summary>
        public bool IsShow(int id) => GetValue(id) > 0;

        /// <summary>
        /// 检查是否显示红点（通过路径，兼容模式）
        /// </summary>
        public bool IsShow(string path) => GetValue(path) > 0;

        /// <summary>
        /// 清除红点（通过 ID）
        /// </summary>
        public void ClearNodeValue(int id) => SetValue(id, 0);

        /// <summary>
        /// 清除红点（通过路径，兼容模式）
        /// </summary>
        public void ClearNodeValue(string path) => SetValue(path, 0);

        /// <summary>
        /// 清除所有红点
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in m_nodeDict)
            {
                if (kvp.Value.IsLeaf)
                {
                    kvp.Value.SetValue(0);
                }
            }
        }

        #endregion

        #region Listener Operations

        /// <summary>
        /// 添加监听（通过 ID）
        /// </summary>
        public void AddListener(int id, Action<RedDotNode> callback)
        {
            RedDotNode node = GetNode(id);
            node?.AddListener(callback);
        }

        /// <summary>
        /// 添加监听（通过路径，兼容模式）
        /// </summary>
        public void AddListener(string path, Action<RedDotNode> callback)
        {
            RedDotNode node = GetNode(path);
            node?.AddListener(callback);
        }

        /// <summary>
        /// 移除监听（通过 ID）
        /// </summary>
        public void RemoveListener(int id, Action<RedDotNode> callback)
        {
            RedDotNode node = GetNode(id);
            node?.RemoveListener(callback);
        }

        /// <summary>
        /// 移除监听（通过路径，兼容模式）
        /// </summary>
        public void RemoveListener(string path, Action<RedDotNode> callback)
        {
            RedDotNode node = GetNode(path);
            node?.RemoveListener(callback);
        }

        #endregion

        #region Utility

        private void SplitPath(string path, List<string> result)
        {
            result.Clear();

            int start = 0;

            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == PATH_SEPARATOR)
                {
                    if (i > start)
                    {
                        result.Add(path.Substring(start, i - start));
                    }

                    start = i + 1;
                }
            }

            if (start < path.Length)
            {
                result.Add(path.Substring(start));
            }
        }

        /// <summary>
        /// 获取所有叶子节点
        /// </summary>
        public List<RedDotNode> GetAllLeafNodes()
        {
            List<RedDotNode> result = new List<RedDotNode>();

            foreach (var kvp in m_nodeDict)
            {
                if (kvp.Value.IsLeaf)
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        public IEnumerable<RedDotNode> GetAllNodes() => m_nodeDict.Values;

#if UNITY_EDITOR

        /// <summary>
        /// 打印树形结构（调试用）
        /// </summary>
        public void PrintTree()
        {
            DLogger.Log("[RedDotModule] RedDot Tree Structure:");
            PrintNode(Root, 0);
        }

        private void PrintNode(RedDotNode node, int depth)
        {
            string indent = new string('-', depth * 2);
            DLogger.Log($"{indent}{node.Name} [Value={node.Value}, Type={node.Type}]");

            for (int i = 0; i < node.ChildCount; i++)
            {
                PrintNode(node.GetChild(i), depth + 1);
            }
        }

#endif

        #endregion
    }
}