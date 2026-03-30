#if false
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(MemoryPoolSetting))]
    public class MemoryPoolSettingInSpector : DGameInspector
    {
        private SerializedProperty m_memoryStrictCheckType = null;
        private int m_memoryStrictCheckTypeIndex = 0;
        private string[] m_memoryStrictCheckTypeNames = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            int memoryStrictCheckTypeIndex = EditorGUILayout.Popup("内存池强制检查开启模式", m_memoryStrictCheckTypeIndex, m_memoryStrictCheckTypeNames);
            if (memoryStrictCheckTypeIndex != m_memoryStrictCheckTypeIndex)
            {
                m_memoryStrictCheckTypeIndex = memoryStrictCheckTypeIndex;
                m_memoryStrictCheckType.enumValueIndex = memoryStrictCheckTypeIndex <= 0 ? 0 : memoryStrictCheckTypeIndex;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        private void OnEnable()
        {
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }
            m_memoryStrictCheckType = serializedObject?.FindProperty("m_memoryStrictCheckType");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            List<string> tempList = new List<string>();
            System.Type enumType = typeof(MemoryStrictCheckType);
            // Array enumValues = Enum.GetValues(typeof(MemoryStrictCheckType));
            string[] enumNames = Enum.GetNames(enumType);
            for (int i = 0; i < enumNames.Length; i++)
            {
                var enumName = enumNames[i];

                if (enumName == "AlwaysEnable")
                {
                    tempList.Add("总是启用");
                }
                else if (enumName == "OnlyEnableWhenDevelopment")
                {
                    tempList.Add("仅在开发模式启用");
                }
                else if (enumName == "OnlyEnableInEditor")
                {
                    tempList.Add("仅在编辑器中启用");
                }
                else if (enumName == "AlwaysDisable")
                {
                    tempList.Add("总是禁用");
                }
            }

            m_memoryStrictCheckTypeNames = tempList.ToArray();
            m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex;
            if (m_memoryStrictCheckType.enumValueIndex <= 0)
            {
                m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex  = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif