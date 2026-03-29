using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 订阅作用域组件。
    /// </summary>
    public sealed class SubscriptionScopeComponent : Entity
    {
        private readonly List<Action> m_clearActions = new();
        private bool m_disposed;

        /// <summary>
        /// 添加一条清理动作。
        /// </summary>
        /// <param name="clearAction">清理动作。</param>
        public void Add(Action clearAction)
        {
            if (m_disposed)
            {
                clearAction?.Invoke();
                return;
            }

            if (clearAction != null)
            {
                m_clearActions.Add(clearAction);
            }
        }

        /// <summary>
        /// 销毁当前作用域并执行全部清理动作。
        /// </summary>
        public void Destroy()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            for (var i = m_clearActions.Count - 1; i >= 0; i--)
            {
                m_clearActions[i]?.Invoke();
            }
            m_clearActions.Clear();
        }
    }
}