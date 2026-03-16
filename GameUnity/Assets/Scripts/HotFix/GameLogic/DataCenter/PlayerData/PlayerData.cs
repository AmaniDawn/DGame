namespace GameLogic
{
    public class PlayerData : BasePlayerData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        public ulong RoleID { get => m_roleID; private set => m_roleID = value; }

        /// <summary>
        /// RoleNo
        /// </summary>
        public uint RoleNo { get; private set; }

        /// <summary>
        /// Uin
        /// </summary>
        public uint Uin { get; private set; }

        /// <summary>
        /// 创角时间
        /// </summary>
        public uint CreateTime { get; private set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; private set; }

        /// <summary>
        /// 是否登录成功，完成数据初始化
        /// </summary>
        public bool IsInit { get; private set; }
    }
}