using System.Diagnostics;

namespace GameBattle
{
    /// <summary>
    /// 全局常驻单例基类。
    /// <remarks>生命周期独立于战斗上下文。</remarks>
    /// </summary>
    /// <typeparam name="T">具体的全局单例类型。</typeparam>
    public class Singleton<T> : ISingleton where T : Singleton<T>, new()
    {
        /// <summary>
        /// 全局唯一实例。
        /// </summary>
        protected static T m_instance;

        /// <summary>
        /// 获取全局单例实例。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new T();
                    m_instance.OnInit();
                    SingletonSystem.Register(m_instance);
                }
                return m_instance;
            }
        }

        /// <summary>
        /// 初始化全局单例基类，并在编辑器环境下校验实例化入口。
        /// </summary>
        protected Singleton()
        {
#if UNITY_EDITOR
            string st = new StackTrace().ToString();
            if (!st.Contains("GameBattle.Singleton`1[T].get_Instance"))
            {
                DGame.DLogger.Error($"Singleton<{typeof(T).FullName}> should be instantiated via Instance only.");
            }
#endif
        }

        /// <summary>
        /// 当前单例实例是否已创建。
        /// </summary>
        public static bool IsValid => m_instance != null;

        /// <summary>
        /// 单例首次创建后触发的初始化回调。
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 销毁当前全局单例实例。
        /// </summary>
        public void Destroy()
        {
            OnDestroy();

            if (m_instance != null)
            {
                SingletonSystem.DestroySingleton(m_instance);
                m_instance = null;
            }
        }

        /// <summary>
        /// 单例销毁前触发的清理回调。
        /// </summary>
        protected virtual void OnDestroy() { }
    }
}