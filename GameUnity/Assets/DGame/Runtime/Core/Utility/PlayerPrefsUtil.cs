namespace DGame
{
    public static partial class Utility
    {
        public static class PlayerPrefsUtil
        {
            private static bool m_enable = true;
            private static string m_userID;

            public static void DeleteAll()
            {
                if (!m_enable)
                {
                    return;
                }
                UnityEngine.PlayerPrefs.DeleteAll();
            }

            public static void DeleteKey(string key)
            {
                if (!m_enable)
                {
                    return;
                }
                UnityEngine.PlayerPrefs.DeleteKey(key);
            }

            public static float GetFloat(string key)
                => m_enable ? UnityEngine.PlayerPrefs.GetFloat(key) : -1f;

            public static float GetFloat(string key, float defaultValue)
                => m_enable ? UnityEngine.PlayerPrefs.GetFloat(key, defaultValue) : -1f;

            public static int GetInt(string key)
                => m_enable ? UnityEngine.PlayerPrefs.GetInt(key) : -1;

            public static int GetInt(string key, int defaultValue)
                => m_enable ? UnityEngine.PlayerPrefs.GetInt(key, defaultValue) : -1;

            public static string GetString(string key)
                => m_enable ? UnityEngine.PlayerPrefs.GetString(key) : string.Empty;

            public static string GetString(string key, string defaultValue)
                => m_enable ? UnityEngine.PlayerPrefs.GetString(key, defaultValue) : string.Empty;

            public static bool HasKey(string key)
                => m_enable && UnityEngine.PlayerPrefs.HasKey(key);

            public static void SetFloat(string key, float value, bool save = true)
            {
                if (!m_enable)
                {
                    return;
                }
                UnityEngine.PlayerPrefs.SetFloat(key, value);
                if (save)
                {
                    UnityEngine.PlayerPrefs.Save();
                }
            }

            public static void SetInt(string key, int value, bool save = true)
            {
                if (!m_enable)
                {
                    return;
                }
                UnityEngine.PlayerPrefs.SetInt(key, value);
                if (save)
                {
                    UnityEngine.PlayerPrefs.Save();
                }
            }

            public static void SetString(string key, string value, bool save = true)
            {
                if (!m_enable)
                {
                    return;
                }
                UnityEngine.PlayerPrefs.SetString(key, value);
                if (save)
                {
                    UnityEngine.PlayerPrefs.Save();
                }
            }

            public static void SetBool(string key, bool value)
            {
                if (!m_enable)
                {
                    return;
                }
                SetInt(key, value ? 1 : 0);
            }

            public static bool GetBool(string key)
                => m_enable && GetInt(key) == 1;

            public static bool GetBool(string key, bool defaultValue)
                => m_enable ? GetInt(key, defaultValue ? 1 : 0) == 1 : defaultValue;

            public static string GetUserKey(string key)
                => $"{m_userID}_{key}";

            public static void SetUserID(string id)
            {
                if (!m_enable)
                {
                    return;
                }
                m_userID = id;
            }

            public static bool HasUserKey(string key)
                => m_enable && HasKey(GetUserKey(key));

            public static void SetUserInt(string key, int value)
            {
                if (!m_enable)
                {
                    return;
                }
                SetInt(GetUserKey(key), value);
            }

            public static int GetUserInt(string key, int defaultValue)
                => m_enable ? GetInt(GetUserKey(key), defaultValue) : defaultValue;

            public static void SetUserFloat(string key, float value)
            {
                if (!m_enable)
                {
                    return;
                }
                SetFloat(GetUserKey(key), value);
            }

            public static float GetUserFloat(string key, float defaultValue)
                => m_enable ? GetFloat(GetUserKey(key), defaultValue) : defaultValue;

            public static void SetUserString(string key, string value)
            {
                if (!m_enable)
                {
                    return;
                }
                SetString(GetUserKey(key), value);
            }

            public static string GetUserString(string key, string defaultValue)
                => m_enable ? GetString(GetUserKey(key), defaultValue) : defaultValue;

            public static void SetUserBool(string key, bool value)
            {
                if (!m_enable)
                {
                    return;
                }
                SetBool(GetUserKey(key), value);
            }

            public static bool GetUserBool(string key, bool defaultValue)
                => m_enable ? GetBool(GetUserKey(key), defaultValue) : defaultValue;

            public static bool HasSetting(string settingName)
                => UnityEngine.PlayerPrefs.HasKey(settingName);

            public static bool Save()
            {
                UnityEngine.PlayerPrefs.Save();
                return true;
            }
        }
    }
}