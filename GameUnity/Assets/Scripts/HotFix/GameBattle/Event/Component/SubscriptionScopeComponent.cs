using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    public sealed class SubscriptionScopeComponent : Entity
    {
        private readonly List<Action> m_clearActions = new();
        private bool m_disposed;

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