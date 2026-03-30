using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 主模型挂点缓存。
    /// 用于在模型加载完成后缓存常用骨骼点/挂点，避免频繁递归查找 Transform。
    /// </summary>
    public class DummyPointCache
    {
        /// <summary>
        /// 挂点名称到 Transform 的缓存表。
        /// </summary>
        private readonly Dictionary<string, Transform> m_dummyPoints = new();

        /// <summary>
        /// 当前缓存所对应的模型根节点。
        /// </summary>
        private Transform m_root;

        /// <summary>
        /// 当前缓存根节点。
        /// </summary>
        public Transform Root => m_root;

        /// <summary>
        /// 以指定节点为根，重建整棵挂点缓存树。
        /// 一般在主模型创建完成或切换模型后调用。
        /// </summary>
        /// <param name="root">模型根节点。</param>
        public void Refresh(Transform root)
        {
            m_root = root;
            m_dummyPoints.Clear();
            if (m_root == null)
            {
                return;
            }

            CacheRecursive(m_root);
        }

        /// <summary>
        /// 根据挂点名称获取 Transform。
        /// 先查缓存，缓存未命中时再尝试从根节点查找并回填缓存。
        /// </summary>
        /// <param name="dummyName">挂点名称。</param>
        /// <returns>查找到的挂点；不存在时返回 null。</returns>
        public Transform GetDummyPoint(string dummyName)
        {
            if (string.IsNullOrEmpty(dummyName))
            {
                return null;
            }

            if (m_dummyPoints.TryGetValue(dummyName, out var point) && point != null)
            {
                return point;
            }

            if (m_root == null)
            {
                return null;
            }

            point = m_root.Find(dummyName);
            if (point != null)
            {
                m_dummyPoints[dummyName] = point;
            }

            return point;
        }

        /// <summary>
        /// 尝试获取指定名称的挂点。
        /// </summary>
        /// <param name="dummyName">挂点名称。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public bool TryGetDummyPoint(string dummyName, out Transform point)
        {
            point = GetDummyPoint(dummyName);
            return point != null;
        }

        /// <summary>
        /// 清空当前挂点缓存。
        /// 一般在模型销毁或切换前调用。
        /// </summary>
        public void Clear()
        {
            m_root = null;
            m_dummyPoints.Clear();
        }

        /// <summary>
        /// 递归扫描整棵节点树并缓存所有节点名称。
        /// </summary>
        /// <param name="node">当前扫描节点。</param>
        private void CacheRecursive(Transform node)
        {
            if (node == null)
            {
                return;
            }

            if (!m_dummyPoints.ContainsKey(node.name))
            {
                m_dummyPoints.Add(node.name, node);
            }

            for (var i = 0; i < node.childCount; i++)
            {
                CacheRecursive(node.GetChild(i));
            }
        }
    }
}