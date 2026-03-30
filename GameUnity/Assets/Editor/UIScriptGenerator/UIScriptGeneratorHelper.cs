using System;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class UIScriptGeneratorHelper : EditorWindow
    {
        [MenuItem("GameObject/ScriptGenerator/About", priority = 88)]
        public static void ShowAboutWindow()
        {
            // UIScriptGeneratorHelper welcomeWindow = (UIScriptGeneratorHelper)EditorWindow.GetWindow(typeof(UIScriptGeneratorHelper), false, "About");
            var window = GetWindow<UIScriptGeneratorHelper>();
            window.titleContent = new GUIContent("About", EditorGUIUtility.IconContent("_Help").image);
            window.minSize = new Vector2(400, 400);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            foreach (var rule in UIScriptGeneratorSettings.GetScriptGenerateRulers())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(rule.uiElementRegex, GUILayout.Width(150));
                GUILayout.Label("<=>", GUILayout.Width(50));
                GUILayout.Label(rule.componentName.ToString(), GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}