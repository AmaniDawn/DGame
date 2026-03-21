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

        #region 重复登录

        private void OnRepeatLoginNotify(IMessage obj)
        {
            if (obj is not G2C_RepeatLogin msg)
            {
                return;
            }
            UIModule.Instance.ShowErrorTipsUI(G.R("账号在别处登录"));
            GameClient.Instance.Disconnect();
            UIModule.Instance.CloseAllWindows();
            UIModule.Instance.ShowWindowAsync<MainLoginUI>();
        }

        #endregion

        #region 注册Auth账号

        public async FTask RegisterRequest(string username, string password)
        {
            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            var response = (A2C_RegisterResponse)await GameClient.Instance.Call(new C2A_RegisterRequest()
            {
                UserName = username,
                Password = password
            });
            
            var quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
            if (quickAuthSaveData != null)
            {
                quickAuthSaveData.Uid = username;
                quickAuthSaveData.Pwd = password;
            }
        }

        #endregion
        
        #region 登录Auth账号

        public async FTask LoginRequest(string username, string password)
        {
            GameClient.Instance.Disconnect();
            await GameClient.Instance.ConnectAsync(TbFuncParamConfig.AuthenticationAddress, TbFuncParamConfig.AuthenticationPort);
            var response = (A2C_LoginResponse)await GameClient.Instance.Call(new C2A_LoginRequest()
            {
                UserName = username,
                Password = password
            });
            
            LoginDataMgr.Instance.UpdateServerInfos(response);
            GameEvent.Get<ILoginUI>().OnLoginAuthSuccess();
        }

        #endregion
        
        #region 登录Gate账号

        public void Login()
        {
            
        }

        #endregion
    }
}