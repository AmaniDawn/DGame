using Newtonsoft.Json;

namespace GameLogic
{
    public abstract class BaseClientSaveData
    {
        private string m_saveKey;

        /// <summary>
        /// 初始化保存数据
        /// </summary>
        /// <param name="saveKey">保存数据的键名</param>
        public void Init(string saveKey)
        {
            m_saveKey = saveKey;
            Load();
        }

        /// <summary>
        /// 加载数据
        /// <remarks>子类可重写实现解密</remarks>
        /// </summary>
        protected virtual void Load()
        {
            string jsonStr = DGame.Utility.PlayerPrefsUtil.GetString(m_saveKey);

            if (!string.IsNullOrEmpty(jsonStr))
            {
                JsonConvert.PopulateObject(jsonStr, this);
            }
        }

        /// <summary>
        /// 保存数据到本地存储
        /// <remarks>子类可重写实现加密</remarks>
        /// </summary>
        public virtual void Save()
            => DGame.Utility.PlayerPrefsUtil.SetString(m_saveKey,
                JsonConvert.SerializeObject(this, Formatting.None));

        protected static T Get<T>() where T : BaseClientSaveData, new()
            => ClientSaveDataMgr.Instance.GetSaveData<T>();
    }
}