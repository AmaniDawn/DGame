#if !UNITY_6000_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [InitializeOnLoad]
    public static class UnityToolbarExtend
    {
        private static int TOOLBAR_COUNT;
        private static GUIStyle COMMAND_STYLE;

        public static readonly List<Action> LEFT_TOOLBAR_GUI = new List<Action>();
        public static readonly List<Action> RIGHT_TOOLBAR_GUI = new List<Action>();

        public const float SPACING = 8;
        public const float LARGE_SPACING = 20;
        public const float BUTTON_WIDTH = 32;
        public const float DROPDOWN_WIDTH = 80;
        public const float PLAY_PAUSE_STOP_WIDTH = 140;

        static UnityToolbarExtend()
        {
            System.Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

            string fileName = "k_ToolCount";

            FieldInfo toolIcons = toolbarType.GetField(fileName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            TOOLBAR_COUNT = toolIcons != null ? (int)toolIcons.GetValue(null) : 8;

            UnityToolbarCallback.OnToolbarGUI = OnGUI;
            UnityToolbarCallback.OnToolbarGUILeft = GUILeft;
            UnityToolbarCallback.OnToolbarGUIRight = GUIRight;
        }

        static void OnGUI()
        {
            if (COMMAND_STYLE == null)
            {
                COMMAND_STYLE = new GUIStyle("CommandLeft");
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;
            float playButtonsPosition = Mathf.RoundToInt((screenWidth - PLAY_PAUSE_STOP_WIDTH) / 2);
            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += SPACING;
            leftRect.xMin += BUTTON_WIDTH * TOOLBAR_COUNT;
            leftRect.xMin += SPACING;
            leftRect.xMin += 64 * 2;
            leftRect.xMax = playButtonsPosition;
            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += COMMAND_STYLE.fixedWidth * 3;
            rightRect.xMax = screenWidth;
            rightRect.xMax -= SPACING;
            rightRect.xMax -= DROPDOWN_WIDTH;
            rightRect.xMax -= SPACING;
            rightRect.xMax -= DROPDOWN_WIDTH;
            rightRect.xMax -= SPACING;
            rightRect.xMax -= DROPDOWN_WIDTH;
            rightRect.xMax -= SPACING;
            rightRect.xMax -= BUTTON_WIDTH;
            rightRect.xMax -= SPACING;
            rightRect.xMax -= 78;

            leftRect.xMin += SPACING;
            leftRect.xMax -= SPACING;
            rightRect.xMin += SPACING;
            rightRect.xMax -= SPACING;

            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;

            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in LEFT_TOOLBAR_GUI)
                {
                    handler();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUILayout.BeginHorizontal();
                foreach (var handler in RIGHT_TOOLBAR_GUI)
                {
                    handler();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();

            foreach (var handler in LEFT_TOOLBAR_GUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }

        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();

            foreach (var handler in RIGHT_TOOLBAR_GUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif