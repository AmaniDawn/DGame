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
    public class DataCenterSys : Singleton<DataCenterSys>, IUpdate
    {
        private readonly List<IDataCenterModule> m_dataCenterModuleList = new List<IDataCenterModule>();

        protected override void OnInit()
        {
            RegCmdHandle();
            InitModule();
            InitOtherModule();
        }

        private void RegCmdHandle()
        {

        }

        #region 网络操作

        /// <summary>
        /// 注册新账号。
        /// </summary>
        /// <param name="address">服务器地址</param>
        /// <param name="port">服务器端口</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        public async FTask Register(string address, int port, string userName, string password)
        {
            await GameClient.Instance.ConnectAsync(address, port);
            GameClient.Instance.Status = GameClientStatus.StatusRegister;
            var response = (A2C_RegisterResponse)await GameClient.Instance.Call(new C2A_RegisterRequest()
            {
                UserName = userName,
                Password = password
            });
            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                DLogger.Warning($"Error: {response.ErrorCode}");
                return;
            }
            DLogger.Info("Registered Successfully");
            GameEvent.Get<ILoginUI>().OnRegisterSuccess();
        }

        /// <summary>
        /// 登录账号并连接到 Gate 服务器。
        /// </summary>
        /// <param name="address">认证服务器地址</param>
        /// <param name="port">认证服务器端口</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        public async FTask Login(string address, int port, string userName, string password)
        {
            await GameClient.Instance.ConnectAsync(address, port);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = (A2C_LoginResponse)await GameClient.Instance.Call(new C2A_LoginRequest()
            {
                UserName = userName,
                Password = password,
                LoginType = 1
            });

            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                DLogger.Warning($"Error: {response.ErrorCode}");
                return;
            }
            DLogger.Info($"Login Successfully Token: {response.Token}");

            if (JwtParseHelper.Parse(response.Token, out var payload))
            {
                DLogger.Info($"Login Success: UID: {payload.uid} address: {payload.address} port: {payload.port}");
                // 根据Token返回的Address登录到Gate服务器
                GameClient.Instance.Status = GameClientStatus.StatusInit;
                GameClient.Instance.Disconnect();
                await GameClient.Instance.ConnectAsync(payload.address, payload.port);
                GameClient.Instance.Status = GameClientStatus.StatusLogin;
                // 发送登录请求到Gate服务器
                var loginResponse = (G2C_LoginResponse)await GameClient.Instance.Call(new C2G_LoginRequest()
                {
                    Token = response.Token
                });

                if (loginResponse.ErrorCode != 0)
                {
                    DLogger.Error($"登录到Gate服务器发生错误: Error: {loginResponse.ErrorCode}");
                    return;
                }
                GameClient.Instance.Status = GameClientStatus.StatusEnter;
                DLogger.Info($"Login Gate Successfully.");
                // DLogger.Info($"CreateTime: {loginResponse.GameAccountInfo.CreateTime} LoginTime: {loginResponse.GameAccountInfo.LoginTime}");
            }
        }

        #endregion

        #region Module相关

        private void InitOtherModule()
        {
        }

        private void InitModule()
        {
            RegisterModule(LoginNetMgr.Instance);
        }

        /// <summary>
        /// 注册数据中心模块。
        /// </summary>
        /// <param name="module">要注册的模块</param>
        public void RegisterModule(IDataCenterModule module)
        {
            if (m_dataCenterModuleList.Contains(module))
            {
                return;
            }

            module.OnInit();
            m_dataCenterModuleList.Add(module);
        }

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
                UIModule.Instance.CloseAllWindows();
                for (int i = 0; i < m_dataCenterModuleList.Count; i++)
                {
                    m_dataCenterModuleList[i].OnRoleLogout();
                }
            }
        }
    }
}