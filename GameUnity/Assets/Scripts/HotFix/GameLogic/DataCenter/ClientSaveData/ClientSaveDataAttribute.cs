namespace GameLogic
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ClientSaveDataAttribute : System.Attribute
    {
        /// <summary>
        /// 保存数据的键名
        /// </summary>
        public string SaveKey { get; private set; }

        /// <summary>
        /// 是否按角色ID区分存储
        /// </summary>
        public bool PerRoleID { get; private set; }

        /// <summary>
        /// 构造客户端保存数据特性
        /// </summary>
        /// <param name="saveKey">保存数据的键名</param>
        /// <param name="perRoleID">是否按角色ID区分存储，默认为false</param>
        public ClientSaveDataAttribute(string saveKey, bool perRoleID = false)
        {
            SaveKey = saveKey;
            PerRoleID = perRoleID;
        }
    }
}