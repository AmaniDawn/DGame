#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(UIText), true)]
    [CanEditMultipleObjects]
    public class UITextEditor : GraphicEditor
    {
        private static bool m_textSpacingPanelOpen = false;
        private static bool m_vertexColorPanelOpen = false;
        private static bool m_textShadowPanelOpen = false;
        private static bool m_textOutLineAndGradientPanelOpen = false;
        private static bool m_textBestFitFontPanelOpen = false;
        private static bool m_textFontPanelOpen = true;
        private static bool m_textGradientColorPanelOpen = true;
        private static bool m_textCirclePanelOpen = true;
        private GradientColorEditor m_gradientColorEditor;
        private UIText m_uiText;

        private SerializedProperty m_text;
        private SerializedProperty m_fontData;

        // 字符间距
        private SerializedProperty m_isUseTextSpacing;
        private SerializedProperty m_textSpacing;

        // 顶点颜色
        private SerializedProperty m_isUseVertexColor;
        private SerializedProperty m_colorFilterType;
        private SerializedProperty m_vertexTopLeftColor;
        private SerializedProperty m_vertexTopRightColor;
        private SerializedProperty m_vertexBottomLeftColor;
        private SerializedProperty m_vertexBottomRightColor;
        private SerializedProperty m_vertexColorOffset;

        // 阴影
        private SerializedProperty m_isUseTextShadow;
        private SerializedProperty m_shadowTopLeftColor;
        private SerializedProperty m_shadowTopRightColor;
        private SerializedProperty m_shadowBottomLeftColor;
        private SerializedProperty m_shadowBottomRightColor;
        private SerializedProperty m_effectDistance;

        // 描边和渐变
        private SerializedProperty m_isUseTextOutline;
        private SerializedProperty m_outLineWidth;
        private SerializedProperty m_outLineColor;
        private SerializedProperty m_alpha;

        // 颜色渐变
        private SerializedProperty m_isUseGradientColor;

        // 字体自适应
        private SerializedProperty m_isUseBestFitFont;
        private SerializedProperty m_resizeTextForBestFit;

        // 环形
        private SerializedProperty m_useTextCircle;
        private SerializedProperty m_radius;
        private SerializedProperty m_spaceCoff;
        private SerializedProperty m_angleOffset;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_uiText = (UIText)this.target;
            m_gradientColorEditor = new GradientColorEditor();
            m_gradientColorEditor?.OnEnable(serializedObject, m_uiText);
            FindAllFonts();
            m_textSpacingPanelOpen = EditorPrefs.GetBool("UIText.m_textSpacingPanelOpen", m_textSpacingPanelOpen);
            m_vertexColorPanelOpen = EditorPrefs.GetBool("UIText.m_vertexColorPanelOpen", m_vertexColorPanelOpen);
            m_textShadowPanelOpen = EditorPrefs.GetBool("UIText.m_textShadowPanelOpen", m_textShadowPanelOpen);
            m_textOutLineAndGradientPanelOpen = EditorPrefs.GetBool("UIText.m_textOutLineAndGradientPanelOpen", m_textOutLineAndGradientPanelOpen);
            m_textBestFitFontPanelOpen = EditorPrefs.GetBool("UIText.m_textBestFitFontPanelOpen", m_textBestFitFontPanelOpen);
            m_textFontPanelOpen = EditorPrefs.GetBool("UIText.m_textFontPanelOpen", m_textFontPanelOpen);
            m_textGradientColorPanelOpen = EditorPrefs.GetBool("UIText.m_textGradientColorPanelOpen", m_textGradientColorPanelOpen);
            m_textCirclePanelOpen = EditorPrefs.GetBool("UIText.m_textCirclePanelOpen", m_textCirclePanelOpen);

            m_uiText.UITextOutlineExtend.Initialize(m_uiText);
            m_uiText.UITextShadowExtend.Initialize(m_uiText);
            m_uiText.UITextGradientColorExtend.Initialize(m_uiText);
            m_text = serializedObject.FindProperty("m_Text");
            m_fontData = serializedObject.FindProperty("m_FontData");

            // 字符间距
            {
                m_isUseTextSpacing = serializedObject.FindProperty("m_uiTextSpacingExtend.m_isUseTextSpacing");
                m_textSpacing = serializedObject.FindProperty("m_uiTextSpacingExtend.m_textSpacing");
            }

            // 颜色渐变
            {
                m_isUseVertexColor = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_isUseVertexColor");
                m_colorFilterType = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_colorFilterType");
                m_vertexTopLeftColor = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_vertexTopLeftColor");
                m_vertexTopRightColor = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_vertexTopRightColor");
                m_vertexBottomLeftColor = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_vertexBottomLeftColor");
                m_vertexBottomRightColor = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_vertexBottomRightColor");
                m_vertexColorOffset = serializedObject.FindProperty("m_uiTextVertexColorExtend.m_vertexColorOffset");
            }

            // 阴影
            {
                m_isUseTextShadow = serializedObject.FindProperty("m_uiTextShadowExtend.m_isUseTextShadow");
                m_shadowTopLeftColor = serializedObject.FindProperty("m_uiTextShadowExtend.m_shadowTopLeftColor");
                m_shadowTopRightColor = serializedObject.FindProperty("m_uiTextShadowExtend.m_shadowTopRightColor");
                m_shadowBottomLeftColor = serializedObject.FindProperty("m_uiTextShadowExtend.m_shadowBottomLeftColor");
                m_shadowBottomRightColor = serializedObject.FindProperty("m_uiTextShadowExtend.m_shadowBottomRightColor");
                m_effectDistance = serializedObject.FindProperty("m_uiTextShadowExtend.m_effectDistance");
            }

            // 描边
            {
                m_isUseTextOutline = serializedObject.FindProperty("m_uiTextOutlineExtend.m_isUseTextOutline");
                m_outLineWidth = serializedObject.FindProperty("m_uiTextOutlineExtend.m_outLineWidth");
                m_outLineColor = serializedObject.FindProperty("m_uiTextOutlineExtend.m_outLineColor");
                m_alpha = serializedObject.FindProperty("m_uiTextOutlineExtend.m_alpha");
            }

            // 渐变
            {
                m_isUseGradientColor = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_isUseGradientColor");
            }

            // 环形字体
            {
                m_useTextCircle = serializedObject.FindProperty("m_uiTextCircleExtend.m_useTextCircle");
                m_radius = serializedObject.FindProperty("m_uiTextCircleExtend.m_radius");
                m_spaceCoff = serializedObject.FindProperty("m_uiTextCircleExtend.m_spaceCoff");
                m_angleOffset = serializedObject.FindProperty("m_uiTextCircleExtend.m_angleOffset");
            }

            // 字体自适应
            {
                m_isUseBestFitFont = serializedObject.FindProperty("m_isUseBestFitFont");
                m_resizeTextForBestFit = serializedObject.FindProperty("m_FontData.m_BestFit");
            }
        }

        protected override void OnDisable()
        {
            m_gradientColorEditor?.OnDisable();
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UITextGUI();
            EditorGUILayout.PropertyField(m_text);
            EditorGUILayout.PropertyField(m_fontData);
            // AppearanceControlsGUI();
            // RaycastControlsGUI();
            // 绘制其他所有属性，除了 m_Script
            // DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void UITextGUI()
        {
            // var outlineExtend = ((UIText)target)?.UITextOutlineExtend;
            // GUI.enabled = false;
            // if (outlineExtend?.TextEffect != null)
            // {
            //     EditorGUILayout.ObjectField("Graphic", outlineExtend.TextEffect.TextGraphic, typeof(Text), false);
            // }
            // GUI.enabled = true;

            UITextDrawEditor.DrawTextFontsGUI("字体选择",ref m_textFontPanelOpen, m_uiText, m_allFonts, OnClickFontBtnAction);
            UITextDrawEditor.DrawTextSpacingGUI("字符间距",ref m_textSpacingPanelOpen, m_isUseTextSpacing, m_textSpacing);
            UITextDrawEditor.DrawTextVertexColorGUI("顶点颜色",ref m_vertexColorPanelOpen, m_isUseVertexColor, m_colorFilterType,
                m_vertexTopLeftColor, m_vertexTopRightColor, m_vertexBottomLeftColor, m_vertexBottomRightColor, m_vertexColorOffset);
            UITextDrawEditor.DrawTextShadowGUI("字体阴影", ref m_textShadowPanelOpen, m_isUseTextShadow,
                m_shadowTopLeftColor, m_shadowTopRightColor, m_shadowBottomLeftColor, m_shadowBottomRightColor, m_effectDistance);
            UITextDrawEditor.DrawTextOutLineGUI("字体描边", ref m_textOutLineAndGradientPanelOpen, m_isUseTextOutline,
                m_outLineColor, m_outLineWidth, m_alpha);
            UITextDrawEditor.DrawTextGradientColorGUI("字体渐变", ref m_textGradientColorPanelOpen, m_isUseGradientColor, m_gradientColorEditor);
            UITextDrawEditor.DrawTextCircleGUI("环形字体", ref m_textCirclePanelOpen, m_useTextCircle, m_radius,
                m_spaceCoff, m_angleOffset);
            // UITextDrawEditor.DrawTextLocalizationGUI("多语言", ref m_textLocalizationPanelOpen, m_useI2Localization, m_hasParams,
            //     m_textDefine, m_uiText.UITextLocalizationExtend, serializedObject);
            UITextDrawEditor.DrawTextBestFitGUI("字体自适应", ref m_textBestFitFontPanelOpen, m_isUseBestFitFont, m_resizeTextForBestFit);

            if (GUI.changed)
            {
                EditorPrefs.SetBool("UIText.m_textSpacingPanelOpen", m_textSpacingPanelOpen);
                EditorPrefs.SetBool("UIText.m_vertexColorPanelOpen", m_vertexColorPanelOpen);
                EditorPrefs.SetBool("UIText.m_textShadowPanelOpen", m_textShadowPanelOpen);
                EditorPrefs.SetBool("UIText.m_textOutLineAndGradientPanelOpen", m_textOutLineAndGradientPanelOpen);
                EditorPrefs.SetBool("UIText.m_textBestFitFontPanelOpen", m_textBestFitFontPanelOpen);
                EditorPrefs.SetBool("UIText.m_textFontPanelOpen", m_textFontPanelOpen);
                EditorPrefs.SetBool("UIText.m_textGradientColorPanelOpen", m_textGradientColorPanelOpen);
                EditorPrefs.SetBool("UIText.m_textCirclePanelOpen", m_textCirclePanelOpen);
            }
        }

        #region 字体快捷按钮

        private readonly string[] m_searchInFolders = { "Assets/BundleAssets/Fonts" };
        private List<Font> m_allFonts = new List<Font>();

        private void FindAllFonts()
        {
            m_allFonts.Clear();
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", m_searchInFolders);
            foreach (string guid in fontGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);

                if (font != null)
                {
                    m_allFonts.Add(font);
                }
            }
        }

        private void OnClickFontBtnAction(Font font)
        {
            UIText uiText = (UIText)this.target;

            if (uiText != null)
            {
                uiText.font = font;
                EditorUtility.SetDirty(uiText);
            }
        }

        #endregion
    }
}

#endif