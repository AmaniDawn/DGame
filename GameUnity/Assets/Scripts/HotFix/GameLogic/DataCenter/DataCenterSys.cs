using System.Collections.Generic;
using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 数据中心模块
    /// </summary>
    public partial class DataCenterSys : Singleton<DataCenterSys>, IUpdate
    {
        protected override void OnInit()
        {
            RegCmdHandle();
            InitModule();
            InitOtherModule();
        }

        private void RegCmdHandle()
        {

        }

        #region Module相关

        private readonly List<IDataCenterModule> m_dataCenterModuleList = new List<IDataCenterModule>();

        private void InitOtherModule()
        {
        }

        partial void InitModule();

        #endregion

        /// <summary>
        /// 每帧更新所有已注册的模块。
        /// </summary>
        public void OnUpdate()
        {
            foreach (var module in m_dataCenterModuleList)
            {
                module.OnUpdate();
            }
        }

        #region PlayerData相关

        /// <summary>
        /// 当前玩家数据
        /// </summary>
        public PlayerData CurPlayerData { get; private set; }

        /// <summary>
        /// 当前玩家RoleID
        /// </summary>
        public ulong CurRoleID => CurPlayerData != null ? CurPlayerData.RoleID : 0;

        /// <summary>
        /// 尝试获取当前玩家数据。
        /// </summary>
        /// <param name="playerData">输出玩家数据</param>
        /// <returns>是否存在当前玩家数据</returns>
        public bool TryGetCurPlayerData(out PlayerData playerData)
        {
            playerData = CurPlayerData;
            return playerData != null;
        }

        /// <summary>
        /// 尝试获取当前角色ID。
        /// </summary>
        /// <param name="roleID">输出角色ID</param>
        /// <returns>是否存在当前角色ID</returns>
        public bool TryGetCurRoleID(out ulong roleID)
        {
            roleID = CurRoleID;
            return roleID > 0;
        }

        /// <summary>
        /// 检查给定的角色ID是否为当前角色ID。
        /// </summary>
        /// <param name="roleID">要检查的角色ID</param>
        /// <returns>是否为当前角色</returns>
        public bool CheckIsSelfRoleID(ulong roleID) => roleID == CurRoleID;

        #endregion

        /// <summary>
        /// 清除客户端数据，关闭所有窗口并通知所有模块角色登出。
        /// </summary>
        public void ClearClientData()
        {
            if (CurPlayerData != null)
            {
                GameModule.UIModule.CloseAllWindows();
                for (int i = 0; i < m_dataCenterModuleList.Count; i++)
                {
                    m_dataCenterModuleList[i].OnRoleLogout();
                }
            }
            CurPlayerData = null;
            LoginDataMgr.Instance.OnRoleLogout();
        }

        public void SetCurPlayerData(CSPlayerData playerData, bool isDirty = false)
        {
            if (playerData == null)
            {
                return;
            }

            CurPlayerData ??= new PlayerData();
            CurPlayerData.UpdatePlayerData(playerData, isDirty);

            for (int i = 0; i < m_dataCenterModuleList.Count; i++)
            {
                m_dataCenterModuleList[i].OnRoleLogin();
            }
        }
    }
}
