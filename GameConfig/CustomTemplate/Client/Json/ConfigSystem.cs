using Luban;
using DGame;
using SimpleJSON;
using UnityEngine;

namespace GameProto
{
    /// <summary>
    /// 配置加载器。
    /// </summary>
    public class ConfigSystem
    {
        private static ConfigSystem m_instance;

        public static ConfigSystem Instance => m_instance != null ? m_instance : m_instance = new ConfigSystem();

        private bool m_init = false;

        private Tables m_tables;

        public Tables Tables
        {
            get
            {
                if (!m_init)
                {
                    Load();
                }

                return m_tables;
            }
        }

        private IResourceModule m_resourceModule;

        /// <summary>
        /// 重载配置。
        /// </summary>
        public void Reload()
        {
            if (m_init)
            {
                m_tables.Reload();
            }
        }

        /// <summary>
        /// 加载配置。
        /// </summary>
        public void Load()
        {
            m_tables = new Tables(LoadJsonNode);
            m_init = true;
        }

        /// <summary>
        /// 加载Json配置。
        /// </summary>
        /// <param name="file">FileName</param>
        /// <returns>ByteBuf</returns>
        private JSONNode LoadJsonNode(string file)
        {
            if (m_resourceModule == null)
            {
                m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            }
            TextAsset textAsset = m_resourceModule.LoadAsset<TextAsset>(file);
            return JsonUtility.FromJson<JSONNode>(textAsset.text);
        }
    }
}