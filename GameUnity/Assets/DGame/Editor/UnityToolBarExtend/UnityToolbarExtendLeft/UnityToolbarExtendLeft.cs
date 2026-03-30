#if !UNITY_6000_3_OR_NEWER

using UnityEditor;

namespace DGame
{
    [InitializeOnLoad]
    public partial class UnityToolbarExtendLeft
    {
        static UnityToolbarExtendLeft()
        {
            UnityToolbarExtend.LEFT_TOOLBAR_GUI.Add(OnToolbarGUI_ScreenLauncher);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuit;
        }
    }
}

#endif