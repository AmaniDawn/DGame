#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameLogic
{
    public class UIImageDrawEditor
    {
        [MenuItem("GameObject/UI/UIImage", priority = 32)]
        public static void CreateUIImage()
        {
            UIImage uiImage = new GameObject("UIImage", typeof(RectTransform), typeof(UIImage)).GetComponent<UIImage>();
            UnityEditorUtil.ResetInCanvasFor(uiImage.rectTransform);
            uiImage.rectTransform.localPosition = Vector3.zero;
        }

        #region 图片不规则形状

        public static void DrawImageMaskGUI(string title, ref bool isPanelOpen, SerializedProperty isUseImageMask,
            SerializedProperty fillPercent, SerializedProperty fill, SerializedProperty ringWidth,
            SerializedProperty segement, SerializedProperty isUsePercentVert, SerializedProperty verticesDistances, SerializedProperty rotation, SerializedProperty isUseRoundedCorner)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseImageMask, new GUIContent("开启不规则图形"));
                if (isUseImageMask.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(rotation, new GUIContent("旋转角度"));
                    EditorGUILayout.PropertyField(fillPercent, new GUIContent("填充比例"));
                    EditorGUILayout.PropertyField(fill, new GUIContent("是否填充满图形"));
                    if (!fill.boolValue)
                    {
                        EditorGUILayout.PropertyField(ringWidth, new GUIContent("圆环的宽度"));
                    }
                    EditorGUILayout.PropertyField(segement, new GUIContent("图形角的个数"));
                    EditorGUILayout.PropertyField(isUsePercentVert, new GUIContent("开启百分比顶点距离"));
                    if (isUsePercentVert.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(verticesDistances, new GUIContent("顶点百分比距离"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;

                    if (isUseRoundedCorner.boolValue)
                    {
                        isUseRoundedCorner.boolValue = false;
                    }
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 圆角

        public static void DrawImageRoundedCornersGUI(string title, ref bool isPanelOpen, SerializedProperty isUseRoundedCorner,
            SerializedProperty radius, SerializedProperty triangleNum, SerializedProperty isUseImageMask)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseRoundedCorner, new GUIContent("开启圆角图形"));
                if (isUseRoundedCorner.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(radius, new GUIContent("图形半径"));
                    EditorGUILayout.PropertyField(triangleNum, new GUIContent("三角形个数"));
                    EditorGUI.indentLevel--;

                    if (isUseImageMask.boolValue)
                    {
                        isUseImageMask.boolValue = false;
                    }
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 多边形

        public static void DrawImagePolygonGUI(string title, ref bool isPanelOpen, SerializedProperty isUsePolygon,
            SerializedProperty texture, SerializedProperty fill, SerializedProperty sides, SerializedProperty rotation,
            SerializedProperty verticesDistances, SerializedProperty size, SerializedProperty fillPercent, SerializedProperty ringWidth,
            SerializedProperty segement,SerializedProperty isUseImageMask, SerializedProperty isUseRoundedCorner)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUsePolygon, new GUIContent("开启多边形图形"));
                if (isUsePolygon.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(texture, new GUIContent("贴图"));
                    EditorGUILayout.PropertyField(fill, new GUIContent("是否填充"));
                    EditorGUILayout.PropertyField(rotation, new GUIContent("旋转"));
                    EditorGUILayout.PropertyField(size, new GUIContent("大小"));
                    EditorGUILayout.PropertyField(sides, new GUIContent("边数"));
                    EditorGUILayout.PropertyField(verticesDistances, new GUIContent("顶点数量"));
                    EditorGUILayout.PropertyField(fillPercent, new GUIContent("填充比例"));
                    if (!fill.boolValue)
                    {
                        EditorGUILayout.PropertyField(ringWidth, new GUIContent("圆环的宽度"));
                    }
                    EditorGUILayout.PropertyField(segement, new GUIContent("图形角的个数"));
                    EditorGUI.indentLevel--;

                    if (isUseImageMask.boolValue)
                    {
                        isUseImageMask.boolValue = false;
                    }
                    if (isUseRoundedCorner.boolValue)
                    {
                        isUseRoundedCorner.boolValue = false;
                    }
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region Mirror

        public static void DrawImageMirrorGUI(string title, ref bool isPanelOpen, SerializedProperty isUseMirror,
            SerializedProperty mirrorType)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseMirror, new GUIContent("开启镜像模式"));
                if (isUseMirror.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(mirrorType, new GUIContent("镜像模式"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion
    }
}

#endif