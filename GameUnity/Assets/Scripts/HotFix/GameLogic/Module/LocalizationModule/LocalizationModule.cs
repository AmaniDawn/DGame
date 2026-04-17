using GameProto;

namespace GameLogic
{
    public class LocalizationModule : DGame.Module, ILocalizationModule
    {
        private ILocalizationHelper m_localizationHelper;

        public void SetLocalizationHelper(ILocalizationHelper localizationHelper)
        {
            m_localizationHelper = localizationHelper;
        }

        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {

        }

        public LocalAreaType CurrentLanguage => m_localizationHelper != null ? m_localizationHelper.CurrentLanguage : LocalAreaType.CN;

        public LocalAreaType SystemLanguage => m_localizationHelper.SystemLanguage;

        public bool ContainsLanguage(LocalAreaType language)
        {
            if (m_localizationHelper != null)
            {
                return m_localizationHelper.ContainsLanguage(language);
            }
            return false;
        }

        public bool ContainsLanguage(int language)
        {
            if (m_localizationHelper != null)
            {
                return m_localizationHelper.ContainsLanguage(language);
            }
            return false;
        }

        public bool SetLanguage(LocalAreaType language)
        {
            if (m_localizationHelper != null)
            {
                return m_localizationHelper.SetLanguage(language);
            }
            return false;
        }

        public bool SetLanguage(int language)
        {
            if (m_localizationHelper != null)
            {
                return m_localizationHelper.SetLanguage(language);
            }
            return false;
        }
    }
}