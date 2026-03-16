using DGame;
using UnityEngine;

namespace GameLogic
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    System.Type thisType = typeof(T);
                    string instName = thisType.Name;
                    GameObject go = SingletonSystem.GetGameObject(instName);

                    if (go == null)
                    {
                        go = GameObject.Find($"/{instName}");

                        if (go == null)
                        {
                            go = new GameObject(instName);
                            go.transform.position = Vector3.zero;
                        }
                    }
                    else
                    {
                        m_instance = go.GetComponent<T>();

                        if (m_instance == null)
                        {
                            m_instance = go.AddComponent<T>();
                        }
                    }

                    if (m_instance == null)
                    {
                        DLogger.Fatal($"无法创建 MonoSingleton<{typeof(T)}>");
                    }
                    SingletonSystem.Register(go, m_instance);
                }
                return m_instance;
            }
        }

        public static bool IsValid => m_instance != null;

        private void Awake()
        {
            if (CheckInstance())
            {
                OnInit();
            }
        }

        protected virtual void OnInit()
        {
        }

        private bool CheckInstance()
        {
            if (this == Instance)
            {
                return true;
            }
            Object.Destroy(gameObject);
            return false;
        }

        private void OnDestroy()
        {
            if (this == Instance)
            {
                Release();
            }
        }

        private void Release()
        {
            if (m_instance != null)
            {
                SingletonSystem.DestroySingleton(m_instance.gameObject, m_instance);
                m_instance = null;
            }
        }
    }
}