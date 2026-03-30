using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 管理当前战斗上下文内创建的战斗域单例。
    /// </summary>
    public sealed class SingletonManagerComponent : Entity
    {
        /// <summary>
        /// 当前战斗上下文登记的单例列表。
        /// </summary>
        private readonly List<ISingleton> m_singletons = new List<ISingleton>();

        /// <summary>
        /// 注册一个战斗域单例。
        /// </summary>
        /// <param name="singleton">需要登记的单例对象。</param>
        public void Register(ISingleton singleton)
        {
            if (!m_singletons.Contains(singleton))
            {
                m_singletons.Add(singleton);
            }
        }

        /// <summary>
        /// 销毁并清空当前战斗上下文中登记的全部单例。
        /// </summary>
        public void Destroy()
        {
            if (m_singletons != null)
            {
                for (int i = m_singletons.Count - 1; i >= 0; i--)
                {
                    m_singletons[i].Destroy();
                    m_singletons[i] = null;
                }
                m_singletons.Clear();
            }
        }
    }
}