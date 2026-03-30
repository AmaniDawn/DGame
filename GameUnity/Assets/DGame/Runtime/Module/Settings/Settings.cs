using UnityEngine;

namespace DGame
{
    public class Settings : MonoBehaviour
    {
        private static Settings m_instance;

        public static Settings Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = Utility.UnityUtil.FindObjectOfType<Settings>();

                    if (m_instance != null)
                    {
                        return m_instance;
                    }
                }
                return m_instance;
            }
        }

        [SerializeField] private ProcedureSettings m_procedureSettings;
        public static ProcedureSettings ProcedureSettings => Instance.m_procedureSettings;

        [SerializeField] private AudioSetting m_audioSetting;
        public static AudioSetting AudioSetting => Instance.m_audioSetting;

        [SerializeField] private UpdateSettings m_updateSettings;

        public static UpdateSettings UpdateSettings
        {
            get
            {
#if UNITY_EDITOR
                if (Instance == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:UpdateSettings");

                    if (guids.Length >= 1)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<UpdateSettings>(path);
                    }
                }
#endif
                return Instance.m_updateSettings;
            }
        }
    }
}