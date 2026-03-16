using System.Collections.Generic;
using DGame;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic
{
    /// <summary>
    /// 统一管理全局的Singleton和MonoSingleton
    /// </summary>
    public static class SingletonSystem
    {
        private static IMonoDriver m_monoDriver;
        private static readonly List<ISingleton> m_singletons = new List<ISingleton>();
        private static readonly List<IUpdate> m_updates = new List<IUpdate>();
        private static readonly List<IFixedUpdate> m_fixedUpdates = new List<IFixedUpdate>();
        private static readonly List<ILateUpdate> m_lateUpdates = new List<ILateUpdate>();

        #if UNITY_EDITOR

        private static readonly List<IDrawGizmos> m_drawGizmos = new List<IDrawGizmos>();
        private static readonly List<IDrawGizmosSelected> m_drawGizmosSelecteds = new List<IDrawGizmosSelected>();

        #endif

        private static readonly Dictionary<string, GameObject> m_gameObjects = new Dictionary<string, GameObject>();

        public static void Register(ISingleton singleton)
        {
            CheckInit();
            m_singletons.Add(singleton);
            RegisterLifeCycle(singleton);
        }

        public static void Register(GameObject go, object singleton)
        {
            CheckInit();

            if (m_gameObjects.TryAdd(go.name, go))
            {
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(go);
                }
                RegisterLifeCycle(singleton);
            }
        }

        private static void RegisterLifeCycle(object singleton)
        {
            // Type iUpdate = typeof(IUpdate);
            //bool needUpdate = iUpdate.IsInstanceOfType(singleton);
            if (singleton is IUpdate update && !m_updates.Contains(update))
            {
                m_updates?.Add(update);
            }
            if (singleton is IFixedUpdate fixedUpdate && !m_fixedUpdates.Contains(fixedUpdate))
            {
                m_fixedUpdates?.Add(fixedUpdate);
            }
            if (singleton is ILateUpdate lateUpdate && !m_lateUpdates.Contains(lateUpdate))
            {
                m_lateUpdates?.Add(lateUpdate);
            }

#if UNITY_EDITOR

            if (singleton is IDrawGizmos drawGizmos && !m_drawGizmos.Contains(drawGizmos))
            {
                m_drawGizmos?.Add(drawGizmos);
            }

            if (singleton is IDrawGizmosSelected drawGizmosSelected && !m_drawGizmosSelecteds.Contains(drawGizmosSelected))
            {
                m_drawGizmosSelecteds?.Add(drawGizmosSelected);
            }

#endif
        }

        public static void DestroySingleton(ISingleton singleton)
        {
            if (m_singletons != null && m_singletons.Contains(singleton))
            {
                m_singletons?.Remove(singleton);
                DestroyLifeCycle(singleton);
            }
        }

        public static void DestroySingleton(GameObject go, object singleton)
        {
            if (m_gameObjects != null && m_gameObjects.ContainsKey(go.name))
            {
                m_gameObjects?.Remove(go.name);
                Object.Destroy(go);
                DestroyLifeCycle(singleton);
            }
        }

        private static void DestroyLifeCycle(object singleton)
        {
            if (singleton is IUpdate update && m_updates.Contains(update))
            {
                m_updates?.Remove(update);
            }
            if (singleton is IFixedUpdate fixedUpdate && m_fixedUpdates.Contains(fixedUpdate))
            {
                m_fixedUpdates?.Remove(fixedUpdate);
            }
            if (singleton is ILateUpdate lateUpdate && m_lateUpdates.Contains(lateUpdate))
            {
                m_lateUpdates?.Remove(lateUpdate);
            }

#if UNITY_EDITOR

            if (singleton is IDrawGizmos drawGizmos && m_drawGizmos.Contains(drawGizmos))
            {
                m_drawGizmos?.Remove(drawGizmos);
            }

            if (singleton is IDrawGizmosSelected drawGizmosSelected && m_drawGizmosSelecteds.Contains(drawGizmosSelected))
            {
                m_drawGizmosSelecteds?.Remove(drawGizmosSelected);
            }

#endif
        }

        public static void Destroy()
        {
            if (m_gameObjects != null)
            {
                foreach (var go in m_gameObjects.Values)
                {
                    Object.Destroy(go);
                }
                m_gameObjects.Clear();
            }

            if (m_singletons != null)
            {
                for (int i = m_singletons.Count - 1; i >= 0; i--)
                {
                    m_singletons[i].Destroy();
                }
                m_singletons.Clear();
            }
            Resources.UnloadUnusedAssets();
        }

        public static GameObject GetGameObject(string name)
        {
            if (m_gameObjects != null && m_gameObjects.TryGetValue(name, out GameObject go))
            {
                return go;
            }

            return null;
        }

        internal static bool ContainsKey(string goName)
        {
            if (m_gameObjects != null)
            {
                return m_gameObjects.ContainsKey(goName);
            }
            return false;
        }

        public static void Restart()
        {
            if (Camera.main != null)
            {
                Camera.main.gameObject.SetActive(false);
            }

            Destroy();
            SceneManager.LoadScene(0);
        }

        internal static ISingleton GetSingleton(string name)
        {
            for (int i = 0; i < m_singletons.Count; i++)
            {
                if (m_singletons[i].ToString() == name)
                {
                    return m_singletons[i];
                }
            }
            return null;
        }

        #region 生命周期

        private static bool m_initialized = false;

        private static void CheckInit()
        {
            if (m_initialized)
            {
                return;
            }

            m_initialized = true;

            if (m_monoDriver == null)
            {
                m_monoDriver = ModuleSystem.GetModule<IMonoDriver>();
            }
            m_monoDriver?.AddUpdateListener(OnUpdate);
            m_monoDriver?.AddFixedUpdateListener(OnFixedUpdate);
            m_monoDriver?.AddLateUpdateListener(OnLateUpdate);

#if UNITY_EDITOR

            m_monoDriver?.AddOnDrawGizmosListener(OnDrawGizmos);
            m_monoDriver?.AddOnDrawGizmosSelectedListener(OnDrawGizmosSelected);

#endif
        }

        private static void DeInit()
        {
            if (!m_initialized)
            {
                return;
            }

            m_initialized = false;

            if (m_monoDriver == null)
            {
                m_monoDriver = ModuleSystem.GetModule<IMonoDriver>();
            }
            m_monoDriver?.RemoveUpdateListener(OnUpdate);
            m_monoDriver?.RemoveFixedUpdateListener(OnFixedUpdate);
            m_monoDriver?.RemoveLateUpdateListener(OnLateUpdate);

#if UNITY_EDITOR

            m_monoDriver?.RemoveOnDrawGizmosListener(OnDrawGizmos);
            m_monoDriver?.RemoveOnDrawGizmosSelectedListener(OnDrawGizmosSelected);

#endif
        }

        private static void OnUpdate()
        {
            foreach (var update in m_updates)
            {
                update?.OnUpdate();
            }
        }

        private static void OnFixedUpdate()
        {
            foreach (var fixedUpdate in m_fixedUpdates)
            {
                fixedUpdate?.OnFixedUpdate();
            }
        }

        private static void OnLateUpdate()
        {
            foreach (var lateUpdate in m_lateUpdates)
            {
                lateUpdate?.OnLateUpdate();
            }
        }

        private static void OnDrawGizmos()
        {
#if UNITY_EDITOR

            foreach (var drawGizmo in m_drawGizmos)
            {
                drawGizmo?.OnDrawGizmos();
            }

#endif
        }

        private static void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR

            foreach (var drawGizmosSelected in m_drawGizmosSelecteds)
            {
                drawGizmosSelected?.OnDrawGizmosSelected();
            }

#endif
        }
        #endregion
    }
}