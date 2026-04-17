using DGame;
using GameProto;

namespace GameLogic
{
    public class DGameLocalizationHelper : ILocalizationHelper
    {
        public LocalAreaType CurrentLanguage { get; set; }

        public LocalAreaType SystemLanguage => LocalizationUtil.SystemLanguage;

        public bool ContainsLanguage(LocalAreaType language) => (int)language < (int)LocalAreaType.MAX;

        public bool ContainsLanguage(int language) => language < (int)LocalAreaType.MAX;

        public bool SetLanguage(LocalAreaType language)
        {
            if (ContainsLanguage(language) && language != CurrentLanguage)
            {
                CurrentLanguage = language;
                GameEvent.Get<ILocalization>().OnLanguageChanged((int)language);
                return true;
            }
            return false;
        }

        public bool SetLanguage(int language)
        {
            if (ContainsLanguage(language) && language != (int)CurrentLanguage)
            {
                CurrentLanguage = (LocalAreaType)language;
                GameEvent.Get<ILocalization>().OnLanguageChanged(language);
                return true;
            }
            return false;
        }
    }
}