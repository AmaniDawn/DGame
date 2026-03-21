using System.Collections.Generic;
using Fantasy;

namespace GameLogic
{
    public class LoginDataMgr : Singleton<LoginDataMgr>
    {
        public List<CSServerInfo> ServerInfoList { get; private set; } = new List<CSServerInfo>();
        public List<CSRecentServerRoleInfo> RecentServerRoleInfoList { get; private set; } = new List<CSRecentServerRoleInfo>();
        
        private LoginSaveData m_loginSaveData;
        private QuickAuthSaveData m_quickAuthSaveData;

        public CSServerInfo CurServerInfo { get; set; } = new CSServerInfo();

        /// <summary>
        /// 是否第一次进入游戏打开用户隐私界面
        /// </summary>
        public bool IsFirstOpenUserPrivacy
        {
            get
            {
                return m_loginSaveData.IsFirstOpenUserPrivacy > 0;
            }
        }
        
        /// <summary>
        /// 是否设置同意隐私协议
        /// </summary>
        public bool IsAgreeUserPrivacy
        {
            get
            {
                return m_loginSaveData.SetAgreeUserPrivacy > 0;
            }
        }
        
        protected override void OnInit()
        {
            m_loginSaveData = ClientSaveDataMgr.Instance.GetSaveData<LoginSaveData>();
            m_quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
        }

        public void OnRoleLogout()
        {
            ServerInfoList.Clear();
            RecentServerRoleInfoList.Clear();
        }

        #region 服务器相关

        public void UpdateServerInfos(A2C_LoginResponse response)
        {
            if (response == null)
            {
                return;
            }
            ServerInfoList.AddRange(response.ServerInfoList);

            if (response.RecentServerRoleInfoList != null)
            {
                RecentServerRoleInfoList.AddRange(response.RecentServerRoleInfoList);
            }
            
            if (m_quickAuthSaveData != null)
            {
                m_quickAuthSaveData.Token = response.Token;
            }

            if (RecentServerRoleInfoList.Count > 0)
            {
                var recentServerRoleInfo = RecentServerRoleInfoList[0];
                var recentServerInfo = GetServerInfo(recentServerRoleInfo.ServerID);
                if (recentServerInfo != null)
                {
                    CurServerInfo.Address = recentServerInfo.Address;
                    CurServerInfo.Port = recentServerInfo.Port;
                    CurServerInfo.Name = recentServerInfo.Name;
                    CurServerInfo.State = recentServerInfo.State;
                    CurServerInfo.Group = recentServerInfo.Group;
                    CurServerInfo.Recommend = recentServerInfo.Recommend;
                    CurServerInfo.ServerID = recentServerInfo.ServerID;
                }
            }
            else if (ServerInfoList.Count > 0)
            {
                var serverInfo = ServerInfoList[0];
                if (serverInfo != null)
                {
                    CurServerInfo.Address = serverInfo.Address;
                    CurServerInfo.Port = serverInfo.Port;
                    CurServerInfo.Name = serverInfo.Name;
                    CurServerInfo.State = serverInfo.State;
                    CurServerInfo.Group = serverInfo.Group;
                    CurServerInfo.Recommend = serverInfo.Recommend;
                    CurServerInfo.ServerID = serverInfo.ServerID;
                }
            }
        }

        #endregion

        public CSServerInfo GetServerInfo(int serverId)
        {
            if (ServerInfoList == null)
            {
                return null;
            }
            for (int i = 0; i < ServerInfoList.Count; i++)
            {
                if (ServerInfoList[i].ServerID == serverId)
                {
                    return ServerInfoList[i];
                }
            }
            return null;
        }
        
        /// <summary>
        /// 设置第一次登录显示用户协议
        /// </summary>
        /// <param name="state"></param>
        public void SetFirstOpenUserPrivacy(bool state)
        {
            m_loginSaveData.IsFirstOpenUserPrivacy = state ? 1 : 0;
            m_loginSaveData.Save();
        }
        
        /// <summary>
        /// 设置同意用户隐私协议
        /// </summary>
        /// <param name="state"></param>
        public void SetAgreeUserPrivacy(bool state)
        {
            m_loginSaveData.SetAgreeUserPrivacy = state ? 1 : 0;
            m_loginSaveData.Save();
        }
    }
}