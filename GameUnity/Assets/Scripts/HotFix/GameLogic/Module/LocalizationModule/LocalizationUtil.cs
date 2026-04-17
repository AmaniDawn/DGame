using System.Collections.Generic;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    public static class LocalizationUtil
    {
        private static readonly Dictionary<LocalAreaType, string> m_languageMap = new Dictionary<LocalAreaType, string>();
        private static readonly Dictionary<string, LocalAreaType> m_languageStrMap = new Dictionary<string, LocalAreaType>();

        private static void RegisterLanguageMap(LocalAreaType language, string str = "")
        {
            if (string.IsNullOrEmpty(str))
            {
                str = language.ToString();
            }

            m_languageMap[language] = str;
            m_languageStrMap[str] = language;
        }

        public static string GetLanguage(LocalAreaType language)
        {
            if (m_languageMap.TryGetValue(language, out var languageStr))
            {
                return languageStr;
            }

            language = LocalAreaType.EN;
            return m_languageMap.TryGetValue(language, out languageStr) ? languageStr : language.ToString();
        }

        public static LocalAreaType SystemLanguage
            => Application.systemLanguage switch
            {
                UnityEngine.SystemLanguage.ChineseSimplified => LocalAreaType.CN,
                UnityEngine.SystemLanguage.ChineseTraditional => LocalAreaType.GAT,
                UnityEngine.SystemLanguage.English => LocalAreaType.EN,
                UnityEngine.SystemLanguage.Japanese => LocalAreaType.JP,
                UnityEngine.SystemLanguage.Korean => LocalAreaType.KR,
                UnityEngine.SystemLanguage.Indonesian => LocalAreaType.INDO,
                UnityEngine.SystemLanguage.Vietnamese => LocalAreaType.VN,
                _ => LocalAreaType.EN
            };
    }
}