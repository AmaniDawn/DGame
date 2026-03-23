using Cysharp.Threading.Tasks;
using DGame;

namespace GameLogic
{
    public class CommonUIController : IUIController
    {
        public void RegUIMessage()
        {
            GameEvent.AddEventListener<uint, uint, System.Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
        }

        #region ShowWaitingUI

        private void OnShowWaitingUI(uint waitFuncID, uint textID, System.Action callback)
        {
            OnShowWaitingUIAsync(waitFuncID, textID, callback).Forget();
        }

        private async UniTaskVoid OnShowWaitingUIAsync(uint waitFuncID, uint textID, System.Action callback)
        {
            var ui = await GameModule.UIModule.ShowWindowAsyncAwait<WaitingUI>();
            ui?.Init(waitFuncID, textID, callback);
        }

        #endregion
    }
}