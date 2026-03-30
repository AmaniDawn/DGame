using System.Diagnostics;

namespace GameLogic
{
    public abstract class Singleton<T> : ISingleton where T : Singleton<T>, new()
    {
        protected static T m_instance = null;

        /// <summary>
        /// 获取单例实例
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
        /// 单例实例是否有效
        /// </summary>
        public static bool IsValid => m_instance != null;

        protected Singleton()
        {
#if UNITY_EDITOR

            string st = new StackTrace().ToString();
            // using const string to compare simply
            if (!st.Contains("GameLogic.Singleton`1[T].get_Instance"))
            {
                DGame.DLogger.Error($"请必须通过Instance方法来实例化{typeof(T).FullName}类");
            }

#endif
        }

        /// <summary>
        /// 激活单例
        /// </summary>
        public virtual void Active()
        {
        }

        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 销毁单例
        /// </summary>
        public virtual void Destroy()
        {
            OnDestroy();

            if (m_instance != null)
            {
                SingletonSystem.DestroySingleton(m_instance);
                m_instance = null;
            }
        }

        protected virtual void OnDestroy()
        {

        }
    }
}