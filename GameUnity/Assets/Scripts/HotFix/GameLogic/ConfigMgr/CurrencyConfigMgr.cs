using GameProto;

namespace GameLogic
{
    public class CurrencyConfigMgr : Singleton<CurrencyConfigMgr>
    {
        public CurrencyConfig GetOrDefault(int currencyType)
            => TbCurrencyConfig.GetOrDefault((CurrencyType)currencyType);

        public bool TryGetValue(int currencyType, out CurrencyConfig cfg)
            => TbCurrencyConfig.TryGetValue((CurrencyType)currencyType, out cfg);

        public bool ContainsKey(int currencyType) => TbCurrencyConfig.ContainsKey((CurrencyType)currencyType);

        public CurrencyConfig GetOrDefault(CurrencyType currencyType) => TbCurrencyConfig.GetOrDefault(currencyType);

        public bool TryGetValue(CurrencyType currencyType, out CurrencyConfig cfg)
            => TbCurrencyConfig.TryGetValue(currencyType, out cfg);

        public bool ContainsKey(CurrencyType currencyType) => TbCurrencyConfig.ContainsKey(currencyType);
    }
}