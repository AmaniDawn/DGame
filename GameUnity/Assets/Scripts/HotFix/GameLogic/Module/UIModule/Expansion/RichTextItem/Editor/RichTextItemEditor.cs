#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(RichTextItem))]
    [CanEditMultipleObjects]
    public class RichTextItemEditor : UnityEditor.Editor
    {
        #region Serialized Properties

        // Text Settings
        private SerializedProperty m_font;
        private SerializedProperty m_fontSize;
        private SerializedProperty m_fontColor;
        private SerializedProperty m_supportRichText;

        // Icon Settings
        private SerializedProperty m_iconSize;
        private SerializedProperty m_iconOffset;
        private SerializedProperty m_iconAlignment;

        // Layout Settings
        private SerializedProperty m_alignment;
        private SerializedProperty m_characterSpacing;
        private SerializedProperty m_lineSpacing;
        private SerializedProperty m_horizontalOverflow;
        private SerializedProperty m_verticalOverflow;

        // Shadow Settings
        private SerializedProperty m_enableShadow;
        private SerializedProperty m_shadowEffectDistance;
        private SerializedProperty m_shadowTopLeftColor;
        private SerializedProperty m_shadowTopRightColor;
        private SerializedProperty m_shadowBottomLeftColor;
        private SerializedProperty m_shadowBottomRightColor;

        // Outline Settings
        private SerializedProperty m_enableOutline;
        private SerializedProperty m_outlineColor;
        private SerializedProperty m_outlineWidth;

        // Animation
        private SerializedProperty m_emojiFPS;

        // Link Settings
        private SerializedProperty m_defaultLinkColor;
        private SerializedProperty m_underlineHeight;

        // Async Rendering
        private SerializedProperty m_enableAsyncRendering;
        private SerializedProperty m_elementsPerFrame;
        private SerializedProperty m_asyncThreshold;

        #endregion

        #region Styles

        private static GUIStyle s_boxStyle;
        private static GUIStyle s_headerStyle;
        private static GUIStyle s_exampleCodeStyle;

        #endregion

        private void OnEnable()
        {
            // Text Settings
            m_font = serializedObject.FindProperty("m_font");
            m_fontSize = serializedObject.FindProperty("m_fontSize");
            m_fontColor = serializedObject.FindProperty("m_fontColor");
            m_supportRichText = serializedObject.FindProperty("m_supportRichText");

            // Icon Settings
            m_iconSize = serializedObject.FindProperty("m_iconSize");
            m_iconOffset = serializedObject.FindProperty("m_iconOffset");
            m_iconAlignment = serializedObject.FindProperty("m_iconAlignment");

            // Layout Settings
            m_alignment = serializedObject.FindProperty("m_alignment");
            m_characterSpacing = serializedObject.FindProperty("m_characterSpacing");
            m_lineSpacing = serializedObject.FindProperty("m_lineSpacing");
            m_horizontalOverflow = serializedObject.FindProperty("m_horizontalOverflow");
            m_verticalOverflow = serializedObject.FindProperty("m_verticalOverflow");

            // Shadow Settings
            m_enableShadow = serializedObject.FindProperty("m_enableShadow");
            m_shadowEffectDistance = serializedObject.FindProperty("m_shadowEffectDistance");
            m_shadowTopLeftColor = serializedObject.FindProperty("m_shadowTopLeftColor");
            m_shadowTopRightColor = serializedObject.FindProperty("m_shadowTopRightColor");
            m_shadowBottomLeftColor = serializedObject.FindProperty("m_shadowBottomLeftColor");
            m_shadowBottomRightColor = serializedObject.FindProperty("m_shadowBottomRightColor");

            // Outline Settings
            m_enableOutline = serializedObject.FindProperty("m_enableOutline");
            m_outlineColor = serializedObject.FindProperty("m_outlineColor");
            m_outlineWidth = serializedObject.FindProperty("m_outlineWidth");

            // Animation
            m_emojiFPS = serializedObject.FindProperty("m_emojiFPS");

            // Link Settings
            m_defaultLinkColor = serializedObject.FindProperty("m_defaultLinkColor");
            m_underlineHeight = serializedObject.FindProperty("m_underlineHeight");

            // Async Rendering
            m_enableAsyncRendering = serializedObject.FindProperty("m_enableAsyncRendering");
            m_elementsPerFrame = serializedObject.FindProperty("m_elementsPerFrame");
            m_asyncThreshold = serializedObject.FindProperty("m_asyncThreshold");
        }

        private void InitStyles()
        {
            if (s_boxStyle == null)
            {
                s_boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(0, 0, 4, 4)
                };
            }

            if (s_headerStyle == null)
            {
                s_headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
            }

            if (s_exampleCodeStyle == null)
            {
                s_exampleCodeStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(0, 0, 2, 2)
                };
            }
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(5);

            DrawTextSettings();
            DrawIconSettings();
            DrawLayoutSettings();
            DrawShadowSettings();
            DrawOutlineSettings();
            DrawAnimationSettings();
            DrawLinkSettings();
            DrawAsyncSettings();
            DrawExamplesSection();

            serializedObject.ApplyModifiedProperties();
        }

        #region Draw Sections

        private void DrawTextSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("文本设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawObjectField("字体", m_font, typeof(Font));
            DrawIntField("字体大小", m_fontSize);
            DrawColorField("字体颜色", m_fontColor);
            DrawToggleField("支持富文本", m_supportRichText);

            EditorGUILayout.EndVertical();
        }

        private void DrawIconSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("图标设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawIntField("图标尺寸", m_iconSize);
            DrawVector2Field("图标偏移", m_iconOffset);
            DrawPopupField("垂直对齐", m_iconAlignment, new[] { "居中", "底部", "顶部" });

            EditorGUILayout.EndVertical();
        }

        private void DrawLayoutSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("布局设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawPopupField("文本对齐", m_alignment, new[] { "左对齐", "居中", "右对齐" });
            DrawFloatField("字符间距", m_characterSpacing);
            DrawFloatField("行间距", m_lineSpacing);

            EditorGUILayout.Space(3);

            DrawPopupField("水平溢出", m_horizontalOverflow, new[] { "自动换行", "溢出显示" });
            DrawPopupField("垂直溢出", m_verticalOverflow, new[] { "截断", "溢出显示" });

            EditorGUILayout.EndVertical();
        }

        private void DrawShadowSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("阴影设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawToggleField("启用阴影", m_enableShadow);

            if (m_enableShadow.boolValue)
            {
                DrawVector2Field("阴影偏移", m_shadowEffectDistance);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("四角阴影颜色", EditorStyles.miniLabel);

                float labelWidth = 30f;
                float colorWidth = (EditorGUIUtility.currentViewWidth - 40f - labelWidth * 2 - 10f) / 2f;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("左上", GUILayout.Width(labelWidth));
                m_shadowTopLeftColor.colorValue = EditorGUILayout.ColorField(GUIContent.none, m_shadowTopLeftColor.colorValue, true, true, false, GUILayout.MinWidth(colorWidth));
                EditorGUILayout.LabelField("右上", GUILayout.Width(labelWidth));
                m_shadowTopRightColor.colorValue = EditorGUILayout.ColorField(GUIContent.none, m_shadowTopRightColor.colorValue, true, true, false, GUILayout.MinWidth(colorWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("左下", GUILayout.Width(labelWidth));
                m_shadowBottomLeftColor.colorValue = EditorGUILayout.ColorField(GUIContent.none, m_shadowBottomLeftColor.colorValue, true, true, false, GUILayout.MinWidth(colorWidth));
                EditorGUILayout.LabelField("右下", GUILayout.Width(labelWidth));
                m_shadowBottomRightColor.colorValue = EditorGUILayout.ColorField(GUIContent.none, m_shadowBottomRightColor.colorValue, true, true, false, GUILayout.MinWidth(colorWidth));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOutlineSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("描边设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawToggleField("启用描边", m_enableOutline);

            if (m_enableOutline.boolValue)
            {
                DrawColorField("描边颜色", m_outlineColor);
                DrawIntSlider("描边宽度", m_outlineWidth, 1, 10);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("动画设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawIntSlider("表情帧率", m_emojiFPS, 1, 60);

            float frameInterval = 1f / m_emojiFPS.intValue;
            EditorGUILayout.LabelField($"每帧间隔: {frameInterval * 1000:F1} 毫秒", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawLinkSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("链接设置", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawColorField("默认链接颜色", m_defaultLinkColor);
            DrawFloatField("下划线高度", m_underlineHeight);

            EditorGUILayout.EndVertical();
        }

        private void DrawAsyncSettings()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("异步渲染", s_headerStyle);
            EditorGUILayout.Space(2);

            DrawToggleField("启用自动分帧", m_enableAsyncRendering);

            if (m_enableAsyncRendering.boolValue)
            {
                DrawIntSlider("每帧处理数", m_elementsPerFrame, 1, 50);
                DrawIntSlider("分帧阈值", m_asyncThreshold, 5, 100);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExamplesSection()
        {
            EditorGUILayout.BeginVertical(s_boxStyle);
            EditorGUILayout.LabelField("规则实例", s_headerStyle);
            EditorGUILayout.Space(4);

            DrawExampleItem("颜色标签", "<color=#FF0000>红色文字</color>", "支持十六进制颜色值，可嵌套使用");
            EditorGUILayout.Space(4);

            DrawExampleItem("图标标签", "[icon:diamond]", "显示名为 diamond 的图片资源");
            EditorGUILayout.Space(4);

            DrawExampleItem("表情标签", "[emoji_001]", "播放已注册的表情动画\n需先调用 RichTextConfig.RegisterEmoji 注册");
            EditorGUILayout.Space(4);

            DrawExampleItem("超链接标签", "[link:1|点击这里|#00BFFF|underline]", "格式: [link:ID|显示文本|颜色|样式]\n样式可选: normal / underline");

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Field Helpers

        private void DrawObjectField(string label, SerializedProperty property, System.Type objType)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.objectReferenceValue = EditorGUI.ObjectField(rect, label, property.objectReferenceValue, objType, false);
            EditorGUI.EndProperty();
        }

        private void DrawIntField(string label, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.intValue = EditorGUI.IntField(rect, label, property.intValue);
            EditorGUI.EndProperty();
        }

        private void DrawFloatField(string label, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.floatValue = EditorGUI.FloatField(rect, label, property.floatValue);
            EditorGUI.EndProperty();
        }

        private void DrawColorField(string label, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.colorValue = EditorGUI.ColorField(rect, label, property.colorValue);
            EditorGUI.EndProperty();
        }

        private void DrawToggleField(string label, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.boolValue = EditorGUI.Toggle(rect, label, property.boolValue);
            EditorGUI.EndProperty();
        }

        private void DrawVector2Field(string label, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.vector2Value = EditorGUI.Vector2Field(rect, label, property.vector2Value);
            EditorGUI.EndProperty();
        }

        private void DrawPopupField(string label, SerializedProperty property, string[] options)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.enumValueIndex = EditorGUI.Popup(rect, label, property.enumValueIndex, options);
            EditorGUI.EndProperty();
        }

        private void DrawIntSlider(string label, SerializedProperty property, int min, int max)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), property);
            property.intValue = EditorGUI.IntSlider(rect, label, property.intValue, min, max);
            EditorGUI.EndProperty();
        }

        private void DrawExampleItem(string title, string code, string description)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(code, s_exampleCodeStyle, GUILayout.Height(22));
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
        }

        #endregion
    }
}

#endif
