using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;
using GameProto;

namespace GameLogic
{
    public class LoginNetMgr : DataCenterModule<LoginNetMgr>
    {
        public override void OnInit()
        {
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.G2C_RepeatLogin, OnRepeatLoginNotify);
        }

        #region 服务器下发消息

        private void OnRepeatLoginNotify(IMessage obj)
        {
            if (obj is not G2C_RepeatLogin)
            {
                return;
            }

            GameModule.UIModule.ShowErrorTipsUI(G.R("账号在别处登录"));
            // GameClient.Instance.Disconnect();
            GameModule.UIModule.CloseAllWindows();
            GameModule.UIModule.ShowWindowAsync<MainLoginUI>();
        }

        #endregion

        #region RegisterRequest

        public async FTask RegisterRequest(string username, string password)
        {
            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            GameClient.Instance.Status = GameClientStatus.StatusRegister;
            var response = await GameClient.Instance.Call(new C2A_RegisterRequest
            {
                UserName = username,
                Password = password
            });
            OnRegisterResponse(response);
        }

        private void OnRegisterResponse(IMessage message)
        {
            if (message is not A2C_RegisterResponse response)
            {
                return;
            }

            GameClient.Instance.Disconnect();
            if (response.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(response.ErrorCode);
                return;
            }

            GameModule.UIModule.ShowTipsUI(G.R("注册成功"));
        }

        #endregion

        #region LoginAuthRequest

        public async FTask LoginAuthRequest(string username, string password)
        {
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = await GameClient.Instance.Call(new C2A_LoginRequest
            {
                UserName = username,
                Password = password
            });

            OnLoginAuthResponse(response);
        }

        private void OnLoginAuthResponse(IMessage message)
        {
            if (message is not A2C_LoginResponse response)
            {
                return;
            }

            GameClient.Instance.Disconnect();
            if (response.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(response.ErrorCode);
                return;
            }

            LoginDataMgr.Instance.UpdateServerInfos(response);
            GameEvent.Get<ILoginUI>().OnLoginAuthSuccess();
        }

        #endregion

        #region LoginGateRequest

        public async FTask LoginGateRequest()
        {
            var quickAuthSaveData = QuickAuthSaveData.Get;
            if (quickAuthSaveData == null)
            {
                return;
            }

            if (quickAuthSaveData.Token == null)
            {
                GameModule.UIModule.ShowTipsUI(G.R("登录异常，请重新登录"));
                return;
            }

            var curSveInfo = LoginDataMgr.Instance.CurServerInfo;
            if (curSveInfo == null)
            {
                GameModule.UIModule.ShowTipsUI(G.R("请先选择服务器"));
                return;
            }

            await GameClient.Instance.ConnectAsync(curSveInfo.Address, curSveInfo.Port);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = (G2C_LoginResponse)await GameClient.Instance.Call(new C2G_LoginRequest
            {
                Token = quickAuthSaveData.Token,
                ServerID = curSveInfo.ServerID
            });
            OnLoginGateResponse(response);
        }

        private void OnLoginGateResponse(IMessage message)
        {
            if (message is not G2C_LoginResponse response)
            {
                return;
            }

            if (response.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(response.ErrorCode);
                return;
            }

            GameClient.Instance.StartHeartbeat();
            GameClient.Instance.Status = GameClientStatus.StatusEnter;
            DataCenterSys.Instance.SetCurPlayerData(response.PlayerData);
            GameEvent.Get<ILoginUI>().OnLoginGateSuccess();
        }

        #endregion
    }
}