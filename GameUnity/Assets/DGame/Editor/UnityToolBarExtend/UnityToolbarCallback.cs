#if !UNITY_6000_3_OR_NEWER

using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.UIElements;

namespace DGame
{
    public static class UnityToolbarCallback
    {
        private static Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static Type m_guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
        private static Type m_iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        private static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static ScriptableObject m_currentToolbar;

        public static Action OnToolbarGUI;
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static UnityToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (m_currentToolbar != null)
                {
                    var root = m_currentToolbar.GetType()
                        .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (root != null)
                    {
                        var rawRoot = root.GetValue(m_currentToolbar);
                        var mRoot = rawRoot as VisualElement;
                        RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
                        RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

                        void RegisterCallback(string rootName, Action cb)
                        {
                            var toolbarZone = mRoot.Q(rootName);

                            var parent = new VisualElement()
                            {
                                style =
                                {
                                    flexGrow = 1,
                                    flexDirection = FlexDirection.Row,
                                }
                            };
                            var container = new IMGUIContainer();
                            container.style.flexGrow = 1;
                            container.onGUIHandler += () => { cb?.Invoke(); };
                            parent.Add(container);
                            toolbarZone.Add(parent);
                        }
                    }
                }
            }
        }

        static void OnGUI()
        {
            OnToolbarGUI?.Invoke();
        }
    }
}

#endif