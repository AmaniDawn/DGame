using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace GameLogic
{
    /// <summary>
    /// 战斗网络管理器。
    /// 负责注册战斗相关消息处理器，并封装客户端发起的战斗网络请求。
    /// </summary>
    public sealed class BattleNetMgr : Singleton<BattleNetMgr>
    {
        /// <summary>
        /// 初始化战斗网络管理器并注册战斗相关消息处理器。
        /// </summary>
        protected override void OnInit()
        {
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_NotifyEnterBattle, NotifyEnterBattle);
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_BroadcastFrameData, BroadcastFrameData);
        }

        #region 服务器下发消息

        /// <summary>
        /// 处理服务器下发的帧广播数据。
        /// </summary>
        /// <param name="msg">收到的网络消息。</param>
        private void BroadcastFrameData(IMessage msg)
        {
            if (msg is not S2C_BroadcastFrameData message)
            {
                return;
            }
        }

        /// <summary>
        /// 处理服务器下发的进入战斗通知。
        /// </summary>
        /// <param name="msg">收到的网络消息。</param>
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

        /// <summary>
        /// 发送开始战斗请求并处理服务器响应。
        /// </summary>
        /// <returns>表示异步请求流程的任务。</returns>
        public async FTask StartBattleRequest()
        {
            var response = await GameClient.Instance.Call(new C2S_StartBattleRequest());
            OnStartBattleResponse(response);
        }

        /// <summary>
        /// 处理开始战斗请求的服务器响应。
        /// </summary>
        /// <param name="message">服务器返回的网络消息。</param>
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

        /// <summary>
        /// 向服务器发送客户端结束战斗数据请求。
        /// </summary>
        public void BattleFinClientRequest()
        {
            GameClient.Instance.Send(new S2C_BattleFinClientDataReq()
            {
            });
        }

        #endregion

        #region 客户端同步帧数据

        /// <summary>
        /// 向服务器发送帧同步数据请求。
        /// </summary>
        public void SyncFrameDataRequest()
        {
            GameClient.Instance.Send(new C2S_SyncFrameDataReq()
            {
            });
        }

        #endregion
    }
}