using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPlayerLogicEvent
    {
        void OnMainPlayerDiamondChange(uint oldValue, uint newValue);

        void OnMainPlayerGoldChange(uint oldValue, uint newValue);

        void OnMainPlayerStamChange(uint oldValue, uint newValue);

        void OnMainPlayerExpChange(uint oldValue, uint newValue);

        void OnMainPlayerCurrencyChange(GameProto.CurrencyType currencyType, uint oldValue, uint newValue);

        void OnMainPlayerFightValueChange(uint oldValue, uint newValue);

        void OnMainPlayerLevelChange();

        void OnMainPlayerNameChange();

        void OnMainPlayerLastAddStamTimeChange();

        void OnMainPlayerBuyStamCntChange();

        void OnMainPlayerSexDataChange();

        void OnMainPlayerSignDataChange();
    }
}