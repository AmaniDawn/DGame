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
            UIModule.Instance.GetWindowAsync<GameMainUI>(ui =>
            {
                ui?.RefreshFuncOpenState();
            });
        }

        private void OnFuncOpen(FuncType funcType)
        {
            if (TbFuncOpenConfig.TryGetValue((int)funcType, out var cfg) && cfg.UseOpenTips && cfg.OpenTipsID > 0)
            {
                UIModule.Instance.ShowTipsUI((uint)cfg.OpenTipsID);
            }

            UIModule.Instance.GetWindowAsync<GameMainUI>(ui =>
            {
                ui?.RefreshFuncOpenState();
            });
        }
    }
}
