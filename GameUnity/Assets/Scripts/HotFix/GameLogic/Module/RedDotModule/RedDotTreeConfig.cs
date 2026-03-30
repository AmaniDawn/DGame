using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 红点树节点配置
    /// </summary>
    [Serializable]
    public sealed class RedDotNodeConfig
    {
        [Tooltip("节点名称（用于生成路径）")]
        public string name;

        [Tooltip("节点描述")]
        public string description;

        [Tooltip("红点类型")]
        public RedDotType type = RedDotType.Dot;

        [Tooltip("聚合策略")]
        public RedDotAggregateStrategy strategy = RedDotAggregateStrategy.Or;

        [Tooltip("子节点")]
        [SerializeReference]
        public List<RedDotNodeConfig> children = new List<RedDotNodeConfig>();

        [HideInInspector]
        public bool foldout = true;

        [HideInInspector]
        public string generatedPath;

        [HideInInspector]
        public int generatedId;

        public RedDotNodeConfig()
        {
            name = "NewNode";
        }

        public RedDotNodeConfig(string nodeName)
        {
            name = nodeName;
        }

        /// <summary>
        /// 递归生成路径
        /// </summary>
        public void GeneratePaths(string parentPath = "")
        {
            generatedPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";

            foreach (var child in children)
            {
                child.GeneratePaths(generatedPath);
            }
        }

        /// <summary>
        /// 获取所有节点（扁平化）
        /// </summary>
        public void GetAllNodes(List<RedDotNodeConfig> result)
        {
            result.Add(this);
            foreach (var child in children)
            {
                child.GetAllNodes(result);
            }
        }
    }

    /// <summary>
    /// 红点树配置 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "RedDotSystem/TreeConfig", fileName = "RedDotTreeConfig")]
    public sealed class RedDotTreeConfig : ScriptableObject
    {
        [Header("红点树根节点")]
        [SerializeField]
        private List<RedDotNodeConfig> m_rootNodes = new List<RedDotNodeConfig>();

        [Header("代码生成设置")]
        [Tooltip("生成的脚本路径（相对于Assets）")]
        [SerializeField]
        private string m_outputPath = "Scripts/HotFix/GameLogic/Module/RedDotModule/Gen/RedDotPathDefine_Gen.g.cs";

        [Tooltip("生成的命名空间")]
        [SerializeField]
        private string m_namespace = "GameLogic";

        [Tooltip("生成的类名")]
        [SerializeField]
        private string m_className = "RedDotPathDefine";

        /// <summary>
        /// 根节点列表
        /// </summary>
        public List<RedDotNodeConfig> RootNodes => m_rootNodes;

        /// <summary>
        /// 输出路径
        /// </summary>
        public string OutputPath => m_outputPath;

        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace => m_namespace;

        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName => m_className;

        /// <summary>
        /// 刷新所有节点的路径
        /// </summary>
        public void RefreshPaths()
        {
            foreach (var root in m_rootNodes)
            {
                root.GeneratePaths();
            }
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        public List<RedDotNodeConfig> GetAllNodes()
        {
            var result = new List<RedDotNodeConfig>();
            foreach (var root in m_rootNodes)
            {
                root.GetAllNodes(result);
            }
            return result;
        }

        /// <summary>
        /// 注册所有节点到红点管理器
        /// <remarks>不使用这个注册 使用RedDotPathDefine_Gen.RegisterAll</remarks>
        /// </summary>
        [Obsolete("不使用这个注册 使用RedDotPathDefine_Gen.RegisterAll")]
        public void RegisterAll()
        {
            RefreshPaths();
            var manager = RedDotModule.Instance;
            var allNodes = GetAllNodes();

            foreach (var node in allNodes)
            {
                if (!string.IsNullOrEmpty(node.generatedPath))
                {
                    manager.Register(node.generatedPath, node.type, node.strategy);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshPaths();
        }
#endif
    }
}