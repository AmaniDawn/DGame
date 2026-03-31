using System;
using System.Collections.Generic;
using GameBattle;
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
        private readonly Dictionary<DummyPointType, Transform> m_dummyPoints = new();
        
        /// <summary>
        /// 挂点名称到挂点类型的静态映射表。
        /// 该映射在类型首次加载时构建一次，后续所有实例共享。
        /// </summary>
        private static readonly Dictionary<string, DummyPointType> NAME_TO_TYPE_MAP = BuildNameMap();

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

            if (!NAME_TO_TYPE_MAP.TryGetValue(dummyName, out var pointType))
            {
                return null;
            }

            return GetDummyPoint(pointType);
        }
        
        /// <summary>
        /// 根据挂点类型获取 Transform。
        /// </summary>
        /// <param name="pointType">挂点类型。</param>
        /// <returns>查找到的挂点；不存在时返回 null。</returns>
        public Transform GetDummyPoint(DummyPointType pointType)
        {
            if (pointType == DummyPointType.DM_NONE || pointType == DummyPointType.DM_MAX)
            {
                return null;
            }

            return m_dummyPoints.GetValueOrDefault(pointType, null);
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
        /// 尝试按挂点类型获取挂点。
        /// </summary>
        /// <param name="pointType">挂点类型。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public bool TryGetDummyPoint(DummyPointType pointType, out Transform point)
        {
            point = GetDummyPoint(pointType);
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
        /// 构建挂点名称到挂点类型的静态映射。
        /// 仅收录实际定义的挂点枚举值，并过滤掉哨兵项。
        /// </summary>
        /// <returns>挂点名称到挂点类型的映射表。</returns>
        private static Dictionary<string, DummyPointType> BuildNameMap()
        {
            var map = new Dictionary<string, DummyPointType>(StringComparer.Ordinal);

            foreach (DummyPointType value in Enum.GetValues(typeof(DummyPointType)))
            {
                if (value == DummyPointType.DM_NONE || value == DummyPointType.DM_MAX)
                {
                    continue;
                }

                var name = value.ToString();
                if (!map.TryAdd(name, value))
                {
                    throw new InvalidOperationException($"Duplicate DummyPointType name: {name}");
                }
            }

            return map;
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

            if (NAME_TO_TYPE_MAP.TryGetValue(node.name, out var pointType) &&
                pointType != DummyPointType.DM_NONE &&
                pointType != DummyPointType.DM_MAX)
            {
                m_dummyPoints[pointType] = node;
            }

            for (var i = 0; i < node.childCount; i++)
            {
                CacheRecursive(node.GetChild(i));
            }
        }
    }
}