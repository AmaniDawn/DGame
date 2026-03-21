using System;
using Cysharp.Threading.Tasks;
using DGame;

namespace GameLogic
{
    public class CommonUIController : IUIController
    {
        public void RegUIMessage()
        {
            GameEvent.AddEventListener<uint, string, System.Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
            GameEvent.AddEventListener<uint>(ICommonUI_Event.FinishWaiting, OnFinishWaiting);
            GameEvent.AddEventListener<string, string, bool>(ICommonUI_Event.ShowComTipsUI, OnShowComTipsUI);
            GameEvent.AddEventListener(ILoginUI_Event.OnLoginGateSuccess, OnLoginGateSuccess);
        }

        #region OnLoginGateSuccess

        private void OnLoginGateSuccess()
        {
            UIModule.Instance.ShowWindowAsync<GameMainUI>();
        }

        #endregion
        
        #region FinishWaitingUI

        private void OnFinishWaiting(uint waitFuncID)
        {
            UIModule.Instance.GetWindowAsync<WaitingUI>(ui =>
            {
                if (ui != null && ui.FinishWaiting(waitFuncID))
                {
                    ui.Close();
                }
            });
        }

        #endregion

        #region ShowWaitingUI

        private void OnShowWaitingUI(uint waitFuncID, string tips, System.Action callback)
        {
            OnShowWaitingUIAsync(waitFuncID, tips, callback).Forget();
        }

        private async UniTaskVoid OnShowWaitingUIAsync(uint waitFuncID, string tips, System.Action callback)
        {
            var ui = await UIModule.Instance.ShowWindowAsyncAwait<WaitingUI>();
            ui?.Init(waitFuncID, tips, callback);
        }

        #endregion
        
        #region OnShowComTipsUI

        private void OnShowComTipsUI(string title, string content, bool isUserPrivacy)
        {
            OnOnShowComTipsUIAsync(title, content, isUserPrivacy).Forget();
        }

        private async UniTaskVoid OnOnShowComTipsUIAsync(string title, string content, bool isUserPrivacy)
        {
            var ui = await UIModule.Instance.ShowWindowAsyncAwait<ComTipsUI>();
            ui?.Init(title, content, isUserPrivacy);
        }

        #endregion
    }
}