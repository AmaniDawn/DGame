#if !UNITY_6000_3_OR_NEWER

using UnityEditor;

namespace DGame
{
    [InitializeOnLoad]
    public partial class UnityToolbarExtendRight
    {
        static UnityToolbarExtendRight()
        {
            UnityToolbarExtend.RIGHT_TOOLBAR_GUI.Add(OnToolbarGUI_SceneSwitcher);
            EditorApplication.projectChanged += UpdateScenes;
            UpdateScenes();
            UnityToolbarExtend.RIGHT_TOOLBAR_GUI.Add(OnToolbarGUI_EditorPlayMode);
            m_resourcesModeIndex = EditorPrefs.GetInt("EditorPlayMode", 0);
        }
    }
}

#endif