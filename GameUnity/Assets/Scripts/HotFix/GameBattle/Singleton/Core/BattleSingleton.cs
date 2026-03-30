using System.Diagnostics;

namespace GameBattle
{
    /// <summary>
    /// 战斗域单例基类。
    /// <remarks>仅在当前战斗上下文存在时允许创建实例</remarks>
    /// </summary>
    /// <typeparam name="T">具体的战斗域单例类型。</typeparam>
    public abstract class BattleSingleton<T> : ISingleton where T : BattleSingleton<T>, new()
    {
        /// <summary>
        /// 当前战斗上下文中的唯一实例。
        /// </summary>
        protected static T m_instance;

        /// <summary>
        /// 获取当前战斗上下文中的单例实例。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    if (BattleManager.CurBattleContextComponent?.SingletonManager == null)
                    {
                        DGame.DLogger.Error($"BattleSingleton<{typeof(T).Name}> access failed: BattleContext or SingletonManager is null.");
                    }
                    else
                    {
                        m_instance = new T();
                        m_instance.OnInit();
                        BattleManager.CurBattleContextComponent.SingletonManager.Register(m_instance);
                    }
                }
                return m_instance;
            }
        }

        /// <summary>
        /// 初始化战斗域单例基类，并在编辑器环境下校验实例化入口。
        /// </summary>
        protected BattleSingleton()
        {
#if UNITY_EDITOR
            string st = new StackTrace().ToString();
            if (!st.Contains("GameBattle.BattleSingleton`1[T].get_Instance"))
            {
                DGame.DLogger.Error($"BattleSingleton<{typeof(T).FullName}> should be instantiated via Instance only.");
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
        /// 销毁当前战斗域单例实例。
        /// </summary>
        public void Destroy()
        {
            OnDestroy();
            m_instance = null;
        }

        /// <summary>
        /// 单例销毁前触发的清理回调。
        /// </summary>
        protected virtual void OnDestroy() { }
    }
}