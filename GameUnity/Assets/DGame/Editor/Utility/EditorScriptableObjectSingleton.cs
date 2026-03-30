using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class EditorScriptableObjectSingleton<T> : ScriptableObject where T : EditorScriptableObjectSingleton<T>
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        m_instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    }
                }
                return m_instance;
            }
        }
    }
}