using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomPropertyDrawer(typeof(UIScriptGenerateRuler))]
    public class UIScriptGenerateRulerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var uiElementRegexRect = new Rect(position.x, position.y, 130, position.height);
            var componentNameRect = new Rect(position.x + 135, position.y, 180, position.height);
            var isUIWidgetRect = new Rect(position.x + 325, position.y, 50, position.height);
            EditorGUI.PropertyField(uiElementRegexRect, property.FindPropertyRelative("uiElementRegex"), GUIContent.none);
            EditorGUI.PropertyField(componentNameRect, property.FindPropertyRelative("componentName"), GUIContent.none);
            EditorGUI.PropertyField(isUIWidgetRect, property.FindPropertyRelative("isUIWidget"), GUIContent.none);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}