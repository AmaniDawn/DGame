using DGame;
using Fantasy;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void OnRegisterSuccess();

        void OnLoginSuccess();

        void ShowLoginUI();

        void GetAccountInfo();
    }
}