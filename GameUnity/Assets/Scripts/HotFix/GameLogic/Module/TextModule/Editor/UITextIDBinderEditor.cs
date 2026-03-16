#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
#if TextMeshPro
using TMPro;
#endif

namespace GameLogic
{
    [CustomEditor(typeof(UITextIDBinder))]
    [CanEditMultipleObjects]
    public class UITextIDBinderEditor : Editor
    {
        private SerializedProperty m_textIDProp;
        private SerializedProperty m_previewLanguageProp;
        private SerializedProperty m_previewTextProp;
        private UITextIDBinder m_binder;

        private void OnEnable()
        {
            m_binder = target as UITextIDBinder;
            m_textIDProp = serializedObject.FindProperty("m_textID");
            m_previewLanguageProp = serializedObject.FindProperty("m_previewLanguage");
            m_previewTextProp = serializedObject.FindProperty("m_previewText");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            // 绘制默认字段
            // m_binder.m_textID = EditorGUILayout.IntField("文本配置ID", m_binder.m_textID);
            EditorGUILayout.PropertyField(m_textIDProp, new GUIContent("文本配置ID"));
            EditorGUILayout.PropertyField(m_previewLanguageProp, new GUIContent("预览语言"));

            // 实时更新预览
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // 显示预览文本（只读）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("预览文本", m_previewTextProp.stringValue);
            EditorGUI.EndDisabledGroup();

            // 添加刷新按钮
            if (GUILayout.Button("刷新预览"))
            {
                EditorConfigLoader.ReloadTables();
                EditorUtility.SetDirty(target);
            }

            // 显示警告信息
            if (m_binder.TextID == 0)
            {
                EditorGUILayout.HelpBox("TextID 为 0，请设置有效的文本ID", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            m_binder.UpdateTextContent();

            if (m_binder.TextID <= 0)
            {
                m_binder.TextID = 0;
            }
        }
    }
}

#endif