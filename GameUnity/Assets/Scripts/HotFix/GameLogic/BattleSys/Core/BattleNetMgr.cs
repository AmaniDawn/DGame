using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace GameLogic
{
    /// <summary>
    /// Battle 网络层相关
    /// </summary>
    public sealed class BattleNetMgr : Singleton<BattleNetMgr>
    {
        protected override void OnInit()
        {
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_NotifyEnterBattle, NotifyEnterBattle);
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_BroadcastFrameData, BroadcastFrameData);
        }

        #region 服务器下发消息
        
        private void BroadcastFrameData(IMessage msg)
        {
            if (msg is not S2C_BroadcastFrameData message)
            {
                return;
            }
        }
        
        private void NotifyEnterBattle(IMessage msg)
        {
            if (msg is not S2C_NotifyEnterBattle message)
            {
                return;
            }
            // 通知开始战斗
        }
        
        #endregion

        #region 开始战斗请求

        public async FTask StartBattleRequest()
        {
            var response = await GameClient.Instance.Call(new C2S_StartBattleRequest());
            OnStartBattleResponse(response);
        }
        
        private void OnStartBattleResponse(IMessage message)
        {
            if (message is not S2C_StartBattleResponse response)
            {
                return;
            }
            if (response.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(response.ErrorCode);
                return;
            }
        }

        #endregion
        
        #region 客户端结束战斗

        public void BattleFinClientRequest()
        {
            GameClient.Instance.Send(new S2C_BattleFinClientDataReq()
            {
                
            });
        }

        #endregion
        
        #region 客户端同步帧数据

        public void SyncFrameDataRequest()
        {
            GameClient.Instance.Send(new C2S_SyncFrameDataReq()
            {
                
            });
        }

        #endregion
    }
}