using Luban;
using SimpleJSON;

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
            // 在这里编写服务器加载配置的逻辑
            var configPath = GenerateConfigPath(file);
            var jsonText = File.ReadAllText(configPath);
            return JSONNode.Parse(jsonText);
        }

        /// <summary>
        /// 生成配置表存放路径。
        /// </summary>
        /// <param name="file">FileName</param>
        /// <returns>configPath</returns>
        private string GenerateConfigPath(string file)
            => $"{AppContext.BaseDirectory}/../../../../../Configs/Json/{file}.json";
    }
}