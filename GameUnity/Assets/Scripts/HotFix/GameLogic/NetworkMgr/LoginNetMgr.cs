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

        private void OnRepeatLoginNotify(IMessage obj)
        {
            if (obj is not G2C_RepeatLogin)
            {
                return;
            }

            UIModule.Instance.ShowErrorTipsUI(G.R("账号在别处登录"));
            // GameClient.Instance.Disconnect();
            UIModule.Instance.CloseAllWindows();
            UIModule.Instance.ShowWindowAsync<MainLoginUI>();
        }

        public async FTask RegisterRequest(string username, string password)
        {
            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            GameClient.Instance.Status = GameClientStatus.StatusRegister;
            var response = (A2C_RegisterResponse)await GameClient.Instance.Call(new C2A_RegisterRequest
            {
                UserName = username,
                Password = password
            });

            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                return;
            }

            UIModule.Instance.ShowTipsUI(G.R("注册成功"));

            var quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
            if (quickAuthSaveData != null)
            {
                quickAuthSaveData.Uid = username;
                quickAuthSaveData.Pwd = password;
            }
        }

        public async FTask LoginRequest(string username, string password)
        {
            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = (A2C_LoginResponse)await GameClient.Instance.Call(new C2A_LoginRequest
            {
                UserName = username,
                Password = password
            });

            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                return;
            }

            LoginDataMgr.Instance.UpdateServerInfos(response);
            GameEvent.Get<ILoginUI>().OnLoginAuthSuccess();
        }

        public async FTask Login()
        {
            var quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
            if (quickAuthSaveData == null)
            {
                return;
            }

            if (quickAuthSaveData.Token == null)
            {
                UIModule.Instance.ShowTipsUI(G.R("登录异常，请重新登录"));
                return;
            }

            var curSveInfo = LoginDataMgr.Instance.CurServerInfo;
            if (curSveInfo == null)
            {
                UIModule.Instance.ShowTipsUI(G.R("请先选择服务器"));
                return;
            }

            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(curSveInfo.Address, curSveInfo.Port);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = (G2C_LoginResponse)await GameClient.Instance.Call(new C2G_LoginRequest
            {
                Token = quickAuthSaveData.Token,
                ServerID = curSveInfo.ServerID
            });

            if (response == null)
            {
                UIModule.Instance.ShowTipsUI(G.R("连接服务器失败"));
                return;
            }

            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                return;
            }

            GameClient.Instance.StartHeartbeat();
            GameClient.Instance.Status = GameClientStatus.StatusEnter;
            DataCenterSys.Instance.SetCurPlayerData(response.PlayerData);
            GameEvent.Get<ILoginUI>().OnLoginGateSuccess();
        }
    }
}
