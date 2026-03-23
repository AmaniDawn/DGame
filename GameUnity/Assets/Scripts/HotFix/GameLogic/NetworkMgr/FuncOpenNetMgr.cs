using System.Collections.Generic;
using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;
using GameProto;

namespace GameLogic
{
    public class FuncOpenNetMgr : DataCenterModule<FuncOpenNetMgr>
    {
        public override void OnInit()
        {
            GameClient.Instance.RegisterMsgHandler(OuterOpcode.G2C_FuncOpenNotify, OnFuncOpenNotify);
            GameEvent.AddEventListener(ILoginUI_Event.OnLoginGateSuccess, OnLoginGateSuccess);
            GameEvent.AddEventListener(IPlayerLogicEvent_Event.OnMainPlayerLevelChange, OnMainPlayerLevelChange);
        }

        public override void OnRoleLogout()
        {
            FuncOpenMgr.Instance.Clear();
        }

        private void OnLoginGateSuccess()
        {
            RequestFuncOpenList().Coroutine();
        }

        private void OnMainPlayerLevelChange()
        {
            RequestFuncOpenList().Coroutine();
        }

        private void OnFuncOpenNotify(IMessage message)
        {
            if (message is not G2C_FuncOpenNotify notify)
            {
                return;
            }

            FuncOpenMgr.Instance.SyncOpenFuncList(notify.NewOpenFuncList, true);
        }

        public async FTask RequestFuncOpenList()
        {
            if (!GameClient.Instance.IsStatusEnter)
            {
                return;
            }

            var response = await GameClient.Instance.Call(new C2G_QueryFuncOpenListRequest());
            if (response is not G2C_QueryFuncOpenListResponse funcOpenResponse)
            {
                return;
            }

            if (funcOpenResponse.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(funcOpenResponse.ErrorCode);
                return;
            }

            FuncOpenMgr.Instance.SyncOpenFuncList(funcOpenResponse.OpenFuncList ?? new List<int>(), true);
        }
    }
}