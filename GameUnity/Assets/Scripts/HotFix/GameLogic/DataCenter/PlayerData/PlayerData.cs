using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class PlayerData : BasePlayerData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        public ulong RoleID { get => m_roleID; private set => m_roleID = value; }

        /// <summary>
        /// 性别
        /// </summary>
        public RoleBodyType BodyType { get => m_bodyType; private set => m_bodyType = value; }

        /// <summary>
        /// RoleNo
        /// </summary>
        public uint RoleNo { get; private set; }

        /// <summary>
        /// 所在主服
        /// </summary>
        public uint WorldID { get; private set; }

        /// <summary>
        /// 创角时间
        /// </summary>
        public uint CreateTime { get; private set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; private set; }

        /// <summary>
        /// 等级
        /// </summary>
        public uint Level { get; private set; }

        /// <summary>
        /// 旧等级
        /// </summary>
        public uint OldLevel { get; private set; }

        /// <summary>
        /// 角色所有的货币和数量
        /// </summary>
        private Dictionary<int, uint> m_allCurrencyDict = new Dictionary<int, uint>();

        /// <summary>
        /// 角色体力
        /// </summary>
        public uint Stam { get; private set; }

        /// <summary>
        /// 上次增加体力的时间
        /// </summary>
        public long LastAddStamTime { get; private set; }

        /// <summary>
        /// 每日购买体力次数
        /// </summary>
        public int DailyBuyStamCount { get; private set; }

        /// <summary>
        /// 角色战斗力
        /// </summary>
        public uint FightValue { get; private set; }

        /// <summary>
        /// 角色个性签名
        /// </summary>
        public string Sign { get; private set; }

        /// <summary>
        /// 是否完成新手引导
        /// </summary>
        public string IsFinGuide { get; private set; }

        /// <summary>
        /// 累计充值金额
        /// </summary>
        public uint TotalRmb { get; private set; }

        /// <summary>
        /// 是否登录成功，完成数据初始化
        /// </summary>
        public bool IsInit { get; private set; }

        #region 货币

        public bool CheckCurrencyEnough()
        {

        }

        #endregion
    }
}