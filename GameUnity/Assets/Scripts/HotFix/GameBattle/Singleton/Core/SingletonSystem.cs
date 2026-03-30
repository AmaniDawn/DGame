using System.Collections.Generic;

namespace GameBattle
{
    /// <summary>
    /// 管理全局常驻单例的统一注册与销毁。
    /// </summary>
    public static class SingletonSystem
    {
        /// <summary>
        /// 当前已登记的全局单例列表。
        /// </summary>
        private static readonly List<ISingleton> m_singletons = new List<ISingleton>();

        /// <summary>
        /// 注册一个全局单例。
        /// </summary>
        /// <param name="singleton">需要登记的单例对象。</param>
        public static void Register(ISingleton singleton)
        {
            if (!m_singletons.Contains(singleton))
            {
                m_singletons.Add(singleton);
            }
        }

        /// <summary>
        /// 将指定单例从全局管理列表中移除。
        /// </summary>
        /// <param name="singleton">需要移除的单例对象。</param>
        public static void DestroySingleton(ISingleton singleton)
        {
            if (m_singletons != null && m_singletons.Contains(singleton))
            {
                m_singletons?.Remove(singleton);
            }
        }

        /// <summary>
        /// 销毁并清空当前登记的全部全局单例。
        /// </summary>
        public static void Destroy()
        {
            if (m_singletons != null)
            {
                for (int i = m_singletons.Count - 1; i >= 0; i--)
                {
                    m_singletons[i].Destroy();
                }
                m_singletons.Clear();
            }
        }
    }
}