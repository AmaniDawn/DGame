using DGame;
using GameProto;

namespace GameLogic
{
    public class FuncOpenUIController : IUIController
    {
        public void RegUIMessage()
        {
            GameEvent.AddEventListener(IFuncOpenLogic_Event.OnFuncOpenDataChange, OnFuncOpenDataChange);
            GameEvent.AddEventListener<FuncType>(IFuncOpenLogic_Event.OnFuncOpen, OnFuncOpen);
        }

        private void OnFuncOpenDataChange()
        {
            GameModule.UIModule.GetWindowAsync<GameMainUI>(ui =>
            {
                ui?.RefreshFuncOpenState();
            });
        }

        private void OnFuncOpen(FuncType funcType)
        {
            if (FuncOpenConfigMgr.Instance.TryGetValue((int)funcType, out var cfg) && cfg.UseOpenTips && cfg.OpenTipsID > 0)
            {
                GameModule.UIModule.ShowTipsUI((uint)cfg.OpenTipsID);
            }

            GameModule.UIModule.GetWindowAsync<GameMainUI>(ui =>
            {
                ui?.RefreshFuncOpenState();
            });
        }
    }
}