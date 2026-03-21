using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

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
            if (obj is not G2C_RepeatLogin msg)
            {
                return;
            }
            DLogger.Warning("客户端重复登录了");
            DLogger.Warning("客户端重复登录了 热更");
        }
    }
}