using DGame;
using UnityEngine;

namespace GameLogic
{
    public static class GameModule
    {
        #region 模块

        private static RootModule m_rootModule;

        /// <summary>
        /// 基础模块
        /// </summary>
        public static RootModule RootModule
        {
            get => m_rootModule == null ? m_rootModule =
#if UNITY_6000_3_OR_NEWER
                Object.FindFirstObjectByType<RootModule>()
#else
                Object.FindObjectOfType<RootModule>()
#endif
                : m_rootModule;
            private set => m_rootModule = value;
        }

        private static IFsmModule m_fsmModule;

        /// <summary>
        /// 有限状态机模块
        /// </summary>
        public static IFsmModule FsmModule => m_fsmModule == null
            ? m_fsmModule = GetModule<IFsmModule>() : m_fsmModule;

        private static ISensitiveWordModule m_sensitiveWordModule;

        /// <summary>
        /// 敏感词模块
        /// </summary>
        public static ISensitiveWordModule SensitiveWordModule => m_sensitiveWordModule == null
            ? m_sensitiveWordModule = GetModule<ISensitiveWordModule>() : m_sensitiveWordModule;

        private static IAnimModule m_animModule;

        /// <summary>
        /// 动画机模块
        /// </summary>
        public static IAnimModule AnimModule => m_animModule == null
            ? m_animModule = GetModule<IAnimModule>() : m_animModule;

        private static IResourceModule m_resourceModule;

        /// <summary>
        /// 资源管理模块
        /// </summary>
        public static IResourceModule ResourceModule => m_resourceModule == null
            ? m_resourceModule = GetModule<IResourceModule>() : m_resourceModule;

        private static IAudioModule m_audioModule;

        /// <summary>
        /// 音频模块
        /// </summary>
        public static IAudioModule AudioModule => m_audioModule == null
            ? m_audioModule = GetModule<IAudioModule>() : m_audioModule;

        private static ISceneModule m_sceneModule;

        /// <summary>
        /// 场景管理模块
        /// </summary>
        public static ISceneModule SceneModule => m_sceneModule == null
            ? m_sceneModule = GetModule<ISceneModule>() : m_sceneModule;

        private static IGameTimerModule m_gameTimerModule;

        /// <summary>
        /// 计时器模块
        /// </summary>
        public static IGameTimerModule GameTimerModule => m_gameTimerModule == null
            ? m_gameTimerModule = GetModule<IGameTimerModule>() : m_gameTimerModule;

        private static DGame.IInputModule m_inputModule;

        /// <summary>
        /// 输入模块
        /// </summary>
        public static DGame.IInputModule InputModule => m_inputModule == null
            ? m_inputModule = GetModule<DGame.IInputModule>() : m_inputModule;

        private static GameLogic.IInputModule m_input;

        /// <summary>
        /// 新输入模块
        /// </summary>
        public static GameLogic.IInputModule Input => m_input == null
            ? m_input = GetModule<GameLogic.IInputModule>() : m_input;

        private static ILocalizationModule m_localizationModule;

        /// <summary>
        /// 多语言模块
        /// </summary>
        public static ILocalizationModule LocalizationModule => m_localizationModule == null
            ? m_localizationModule = GetModule<ILocalizationModule>() : m_localizationModule;

        private static IGameObjectPoolModule m_gameObjectPool;

        /// <summary>
        /// 游戏对象 对象池模块
        /// </summary>
        public static IGameObjectPoolModule GameObjectPool => m_gameObjectPool == null
            ? m_gameObjectPool = GetModule<IGameObjectPoolModule>() : m_gameObjectPool;
        
        private static UIModule m_uiModule;

        /// <summary>
        /// UI模块
        /// </summary>
        public static UIModule UIModule => m_uiModule == null ? m_uiModule = UIModule.Instance : m_uiModule;
        
        private static RedDotModule m_redDotModule;

        /// <summary>
        /// 红点模块
        /// </summary>
        public static RedDotModule RedDotModule => m_redDotModule == null 
            ? m_redDotModule = RedDotModule.Instance : m_redDotModule;

        #endregion

        /// <summary>
        /// 获取游戏模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T GetModule<T>() where T : class
        {
            T module = ModuleSystem.GetModule<T>();
            DLogger.Assert(condition: module != null, $"{typeof(T)} is null");
            return module;
        }

        public static void Destroy()
        {
            DLogger.Info("GameModule Destroy");
            m_rootModule = null;
            m_gameTimerModule = null;
            m_audioModule = null;
            m_sceneModule = null;
            m_animModule = null;
            m_resourceModule = null;
            m_fsmModule = null;
            m_inputModule = null;
            m_input = null;
            m_localizationModule = null;
            m_gameObjectPool = null;
            m_sensitiveWordModule = null;
            m_uiModule = null;
            m_redDotModule = null;
        }
    }
}