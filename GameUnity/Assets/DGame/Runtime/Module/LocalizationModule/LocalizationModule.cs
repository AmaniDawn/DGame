namespace DGame
{
    public class LocalizationModule : Module, ILocalizationModule
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

        public Language CurrentLanguage => m_localizationHelper != null ? m_localizationHelper.CurrentLanguage : Language.CN;

        public Language SystemLanguage => m_localizationHelper.SystemLanguage;

        public bool ContainsLanguage(Language language)
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

        public bool SetLanguage(Language language)
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