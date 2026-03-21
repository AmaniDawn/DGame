using DGame;
using Fantasy;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void OnRegisterSuccess();

        void OnLoginGateSuccess();
        
        void OnLoginAuthSuccess();

        void ShowLoginUI();

        void GetAccountInfo();
    }
}