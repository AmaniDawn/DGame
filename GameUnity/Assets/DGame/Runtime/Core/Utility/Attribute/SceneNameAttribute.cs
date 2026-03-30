using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace DGame
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneNameAttribute : PropertyAttribute
    {
        public bool IncludeNoneOption { get; set; } = true;

        public SceneNameAttribute() { }

        public SceneNameAttribute(bool includeNoneOption)
        {
            IncludeNoneOption = includeNoneOption;
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// 针对哪个类型的 Property 在 Inspector 进行绘制
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneNameAttribute))]
    public class SceneNameDrawer : PropertyDrawer
    {
        private static List<string> m_sceneNamesCache;
        private static double m_lastCacheTime;
        private const double CACHE_DURATION = 1.0; // 缓存1秒

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                // 绘制警告框
                Rect warningRect = position;
                warningRect.height = EditorGUIUtility.singleLineHeight + 5;
                EditorGUI.HelpBox(warningRect, "SceneName attribute can only be used with string fields.", MessageType.Error);
                return;
            }

            SceneNameAttribute sceneAttribute = attribute as SceneNameAttribute;
            List<string> sceneNames = GetSceneNames(sceneAttribute.IncludeNoneOption);

            // 显示当前值（如果不在列表中）
            string displayValue = property.stringValue;

            if (string.IsNullOrEmpty(property.stringValue) && sceneAttribute.IncludeNoneOption)
            {
                displayValue = "<None>";
            }

            int currentIndex = sceneNames.IndexOf(displayValue);

            if (currentIndex == -1)
            {
                // 如果当前值不在列表中，显示特殊选项
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    sceneNames.Insert(0, $"[Missing] {property.stringValue}");
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = 0;
                }
            }

            GUIContent sceneLabel = new GUIContent(
                " " + label.text,
                EditorGUIUtility.IconContent("SceneAsset Icon").image,
                "Choose a scene from the Build Settings"
            );

            // 计算控件位置
            float buttonWidth = 70f;
            float spacing = 5f;
            float labelWidth = EditorGUIUtility.labelWidth;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect popupRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth - buttonWidth - spacing, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, sceneLabel);
            int selectedIndex = EditorGUI.Popup(popupRect, currentIndex, sceneNames.ToArray());

            GUIContent refreshButton = new GUIContent(
                " Refresh",
                EditorGUIUtility.IconContent("Refresh").image,
                "Update scene list"
            );

            // 刷新按钮
            if (GUI.Button(buttonRect, refreshButton))
            {
                ClearCache();
                sceneNames = GetSceneNames(sceneAttribute.IncludeNoneOption);
                selectedIndex = sceneNames.IndexOf(displayValue);
                if (selectedIndex == -1) selectedIndex = 0;
            }

            if (selectedIndex >= 0 && selectedIndex < sceneNames.Count)
            {
                string selectedScene = sceneNames[selectedIndex];

                // 处理特殊选项
                if (selectedScene.StartsWith("[Missing]"))
                {
                    // 保持原值
                }
                else if (selectedScene == "<None>")
                {
                    property.stringValue = string.Empty;
                }
                else
                {
                    property.stringValue = selectedScene;
                }
            }
        }

        private List<string> GetSceneNames(bool includeNoneOption)
        {
            if (m_sceneNamesCache != null && (EditorApplication.timeSinceStartup - m_lastCacheTime) < CACHE_DURATION)
            {
                return new List<string>(m_sceneNamesCache);
            }

            List<string> sceneNames = new List<string>();

            if (includeNoneOption)
            {
                sceneNames.Add("<None>");
            }

            bool hasEnabledScenes = false;

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    sceneNames.Add(sceneName);
                    hasEnabledScenes = true;
                }
            }

            if (!hasEnabledScenes)
            {
                sceneNames.Add("No enabled scenes in Build Settings");
            }

            m_sceneNamesCache = new List<string>(sceneNames);
            m_lastCacheTime = EditorApplication.timeSinceStartup;

            return sceneNames;
        }

        private void ClearCache()
        {
            m_sceneNamesCache = null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

#endif
}