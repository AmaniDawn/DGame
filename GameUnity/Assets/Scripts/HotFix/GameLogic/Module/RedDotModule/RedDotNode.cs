using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 红点节点 - 核心数据结构
    /// </summary>
    public sealed class RedDotNode
    {
        #region Fields

        private readonly List<RedDotNode> m_children;

        private Action<RedDotNode> m_onValueChanged;

        #endregion

        #region Properties

        /// <summary>
        /// 节点唯一标识
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 节点完整路径（如 "Main/Mail/System"）
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// 节点名称（路径最后一段）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 红点类型
        /// </summary>
        public RedDotType Type { get; set; }

        /// <summary>
        /// 聚合策略
        /// </summary>
        public RedDotAggregateStrategy AggregateStrategy { get; set; }

        /// <summary>
        /// 红点数值（0表示无红点）
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// 是否显示红点
        /// </summary>
        public bool IsShow => Value > 0;

        /// <summary>
        /// 是否为叶子节点
        /// </summary>
        public bool IsLeaf { get; private set; }

        /// <summary>
        /// 父节点
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 子节点数量
        /// </summary>
        public int ChildCount => m_children.Count;

        /// <summary>
        /// 是否需要更新
        /// </summary>
        public bool IsDirty { get; private set; }

        #endregion

        #region Constructor

        public RedDotNode(int id, string path, string name)
        {
            Id = id;
            Path = path;
            Name = name;
            Type = RedDotType.Dot;
            AggregateStrategy = RedDotAggregateStrategy.Or;
            m_children = new List<RedDotNode>(4);
            Value = 0;
            IsDirty = false;
            IsLeaf = true;
        }

        #endregion

        #region Tree Operations

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(RedDotNode child)
        {
            if (child == null || m_children.Contains(child))
            {
                return;
            }

            m_children.Add(child);
            child.Parent = this;
            IsLeaf = false;

            MarkDirty();
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public void RemoveChild(RedDotNode child)
        {
            if (child == null)
            {
                return;
            }

            if (m_children.Remove(child))
            {
                child.Parent = null;
                IsLeaf = m_children.Count == 0;
                MarkDirty();
            }
        }

        /// <summary>
        /// 获取子节点
        /// </summary>
        public RedDotNode GetChild(int index)
        {
            if (index < 0 || index >= m_children.Count)
            {
                return null;
            }
            return m_children[index];
        }

        /// <summary>
        /// 根据名称获取子节点
        /// </summary>
        public RedDotNode GetChildByName(string name)
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Name == name)
                {
                    return m_children[i];
                }
            }
            return null;
        }

        #endregion

        #region Value Operations

        /// <summary>
        /// 设置红点数值（仅叶子节点可调用）
        /// </summary>
        public void SetValue(int value)
        {
            if (!IsLeaf)
            {
                DGame.DLogger.Warning($"[RedDot] 只有叶子节点可以直接设置数值: {Path}");
                return;
            }

            value = Math.Max(0, value);
            if (Value != value)
            {
                Value = value;
                NotifyValueChanged();
                PropagateToParent();
            }
        }

        /// <summary>
        /// 增加红点数值
        /// </summary>
        public void AddValue(int delta)
        {
            SetValue(Value + delta);
        }

        /// <summary>
        /// 清除红点
        /// </summary>
        public void Clear()
        {
            SetValue(0);
        }

        #endregion

        #region Dirty Flag and Update

        /// <summary>
        /// 标记为脏
        /// </summary>
        private void MarkDirty()
        {
            if (IsDirty)
            {
                return;
            }

            IsDirty = true;
            Parent?.MarkDirty();
        }

        /// <summary>
        /// 重新计算数值（仅非叶子节点）
        /// </summary>
        public void Recalculate()
        {
            if (!IsDirty)
            {
                return;
            }

            if (IsLeaf)
            {
                IsDirty = false;
                return;
            }

            for (int i = 0; i < m_children.Count; i++)
            {
                m_children[i].Recalculate();
            }

            int newValue = CalculateAggregatedValue();

            if (Value != newValue)
            {
                Value = newValue;
                NotifyValueChanged();
            }

            IsDirty = false;
        }

        /// <summary>
        /// 根据聚合策略计算数值
        /// </summary>
        private int CalculateAggregatedValue()
        {
            switch (AggregateStrategy)
            {
                case RedDotAggregateStrategy.Sum:
                    int sum = 0;
                    for (int i = 0; i < m_children.Count; i++)
                    {
                        sum += m_children[i].Value;
                    }
                    return sum;

                case RedDotAggregateStrategy.Or:
                    for (int i = 0; i < m_children.Count; i++)
                    {
                        if (m_children[i].Value > 0)
                        {
                            return 1;
                        }
                    }
                    return 0;

                case RedDotAggregateStrategy.Max:
                    int max = 0;

                    for (int i = 0; i < m_children.Count; i++)
                    {
                        max = Math.Max(max, m_children[i].Value);
                    }
                    return max;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 向上冒泡更新
        /// </summary>
        private void PropagateToParent()
        {
            if (Parent == null)
            {
                return;
            }

            int newParentValue = Parent.CalculateAggregatedValue();
            if (Parent.Value != newParentValue)
            {
                Parent.Value = newParentValue;
                Parent.NotifyValueChanged();
                Parent.PropagateToParent();
            }
        }

        #endregion

        #region Event Listeners

        /// <summary>
        /// 添加值变化监听
        /// </summary>
        public void AddListener(Action<RedDotNode> callback)
        {
            m_onValueChanged += callback;
        }

        /// <summary>
        /// 移除值变化监听
        /// </summary>
        public void RemoveListener(Action<RedDotNode> callback)
        {
            m_onValueChanged -= callback;
        }

        /// <summary>
        /// 移除所有监听
        /// </summary>
        public void RemoveAllListeners()
        {
            m_onValueChanged = null;
        }

        /// <summary>
        /// 通知值变化
        /// </summary>
        private void NotifyValueChanged()
        {
            m_onValueChanged?.Invoke(this);
        }

        #endregion

        #region Utility

        public override string ToString()
        {
            return $"[RedDotNode] Path={Path}, Value={Value}, Type={Type}, IsLeaf={IsLeaf}, ChildCount={m_children.Count}";
        }

        #endregion
    }
}