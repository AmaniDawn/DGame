using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace GameLogic
{
    public sealed class BattleNetMgr : Singleton<BattleNetMgr>
    {
        private bool m_hasSentBattleLoadDone;

        protected override void OnInit()
        {
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_NotifyBattleLoading, NotifyBattleLoading);
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_NotifyEnterBattle, NotifyEnterBattle);
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.S2C_BroadcastFrameData, BroadcastFrameData);
        }

        private void BroadcastFrameData(IMessage msg)
        {
            if (msg is not S2C_BroadcastFrameData)
            {
                return;
            }
        }

        private void NotifyBattleLoading(IMessage msg)
        {
            if (msg is not S2C_NotifyBattleLoading)
            {
                return;
            }

            GameModule.UIModule.CloseAllWindows();
            GameModule.UIModule.ShowWindowAsync<LoadingUI>();
            if (m_hasSentBattleLoadDone)
            {
                return;
            }

            m_hasSentBattleLoadDone = true;
            BattleLoadDoneRequest().Coroutine();
        }

        private void NotifyEnterBattle(IMessage msg)
        {
            if (msg is not S2C_NotifyEnterBattle message)
            {
                return;
            }

            m_hasSentBattleLoadDone = false;
            GameModule.UIModule.CloseWindow<LoadingUI>();
            BattleEnterHelper.EnterBattle(message);
        }

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
            }
        }

        private async FTask BattleLoadDoneRequest()
        {
            var response = await GameClient.Instance.Call(new C2S_BattleLoadDoneRequest());
            OnBattleLoadDoneResponse(response);
        }

        private void OnBattleLoadDoneResponse(IMessage message)
        {
            if (message is not S2C_BattleLoadDoneResponse response)
            {
                return;
            }

            if (response.ErrorCode == 0)
            {
                return;
            }

            m_hasSentBattleLoadDone = false;
            GameModule.UIModule.ShowTipsUI(response.ErrorCode);
        }

        public void BattleFinClientRequest()
        {
            GameClient.Instance.Send(new S2C_BattleFinClientDataReq());
        }

        public void SyncFrameDataRequest()
        {
            GameClient.Instance.Send(new C2S_SyncFrameDataReq());
        }
    }
}
