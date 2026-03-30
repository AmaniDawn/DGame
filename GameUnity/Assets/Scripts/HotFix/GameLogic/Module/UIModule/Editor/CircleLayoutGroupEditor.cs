#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(CircleLayoutGroup))]
    public class CircleLayoutGroupEditor : Editor
    {
        private SerializedProperty mode;
        private SerializedProperty radius;
        private SerializedProperty initAngle;
        private SerializedProperty keepRadLen;
        private SerializedProperty keepRadLenVal;
        private SerializedProperty sectorAngle;
        private SerializedProperty sectorAlignCenter;
        private SerializedProperty sectorClockwise;
        private SerializedProperty circleCenter;
        private SerializedProperty controlChildSize;
        private SerializedProperty childSize;

        private bool showBasicSettings = true;
        private bool showSectorSettings = false;
        private bool showAdvancedSettings = false;

        private void OnEnable()
        {
            mode = serializedObject.FindProperty("mode");
            radius = serializedObject.FindProperty("radius");
            initAngle = serializedObject.FindProperty("initAngle");
            keepRadLen = serializedObject.FindProperty("keepRadLen");
            keepRadLenVal = serializedObject.FindProperty("keepRadLenVal");
            sectorAngle = serializedObject.FindProperty("sectorAngle");
            sectorAlignCenter = serializedObject.FindProperty("sectorAlignCenter");
            sectorClockwise = serializedObject.FindProperty("sectorClockwise");
            circleCenter = serializedObject.FindProperty("circleCenter");
            controlChildSize = serializedObject.FindProperty("controlChildSize");
            childSize = serializedObject.FindProperty("childSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(5);
            // 绘制标题和基本信息
            DrawInspectorHeader();
            // 基础设置
            DrawBasicSettings();

            // 扇形模式专属设置
            if (mode.enumValueIndex == (int)CircleLayoutGroup.LayoutMode.Sector)
            {
                DrawSectorSettings();
            }

            // 高级设置
            DrawAdvancedSettings();

            // 操作按钮
            DrawActionButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInspectorHeader()
        {
            // 标题样式
            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            var label = new GUIContent(" 圆形布局组", EditorGUIUtility.IconContent("_Popup").image);
            EditorGUILayout.LabelField(label, titleStyle);
            EditorGUILayout.Space(3);

            // 模式选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("布局模式", GUILayout.Width(80));
            var oldMode = mode.enumValueIndex;
            mode.enumValueIndex = GUILayout.Toolbar(mode.enumValueIndex, new[] { "圆形分布", "扇形分布" });

            if (oldMode != mode.enumValueIndex)
            {
                // 模式切换时重置一些参数
                if (mode.enumValueIndex == (int)CircleLayoutGroup.LayoutMode.Circle)
                {
                    sectorAlignCenter.boolValue = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
        }

        private void DrawBasicSettings()
        {
            showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "基础设置", true, EditorStyles.foldoutHeader);
            if (!showBasicSettings) return;

            EditorGUILayout.BeginVertical("HelpBox");

            // 半径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("半径", GUILayout.Width(60));
            radius.floatValue = EditorGUILayout.FloatField(radius.floatValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("重置", GUILayout.Width(40)))
                radius.floatValue = 100f;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Slider(radius, 0, 500, GUIContent.none);

            // 起始角度
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("起始角度", GUILayout.Width(60));
            initAngle.floatValue = EditorGUILayout.FloatField(initAngle.floatValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("重置", GUILayout.Width(40)))
                initAngle.floatValue = 0f;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Slider(initAngle, -360, 360, GUIContent.none);

            // 圆心偏移
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("圆心偏移", GUILayout.Width(60));
            circleCenter.vector2Value =
                EditorGUILayout.Vector2Field("", circleCenter.vector2Value, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("重置", GUILayout.Width(40)))
                circleCenter.vector2Value = Vector2.zero;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawSectorSettings()
        {
            showSectorSettings =
                EditorGUILayout.Foldout(showSectorSettings, "扇形设置", true, EditorStyles.foldoutHeader);
            if (!showSectorSettings) return;

            EditorGUILayout.BeginVertical("HelpBox");

            // 扇形角度
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("扇形角度", GUILayout.Width(80));
            sectorAngle.floatValue = EditorGUILayout.Slider(sectorAngle.floatValue, 0, 360);
            EditorGUILayout.EndHorizontal();

            // 保持弧长
            EditorGUILayout.BeginHorizontal();
            keepRadLen.boolValue = EditorGUILayout.ToggleLeft("保持弧长不变", keepRadLen.boolValue, GUILayout.Width(120));

            if (keepRadLen.boolValue)
            {
                keepRadLenVal.floatValue = EditorGUILayout.FloatField("弧长值", keepRadLenVal.floatValue);
            }

            EditorGUILayout.EndHorizontal();

            if (!keepRadLen.boolValue)
            {
                // 居中对齐
                EditorGUILayout.BeginHorizontal();
                sectorAlignCenter.boolValue =
                    EditorGUILayout.ToggleLeft("居中对齐", sectorAlignCenter.boolValue, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            // 方向
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("方向", GUILayout.Width(40));
            string[] directions = new string[] { "顺时针", "逆时针" };
            int directionIndex = sectorClockwise.boolValue ? 0 : 1;
            directionIndex = GUILayout.Toolbar(directionIndex, directions);
            sectorClockwise.boolValue = directionIndex == 0;
            EditorGUILayout.EndHorizontal();

            // 预览信息
            if (Application.isPlaying)
            {
                var layoutGroup = target as CircleLayoutGroup;
                int childCount = layoutGroup.transform.childCount;
                float actualAngle = childCount <= 1
                    ? 0
                    : sectorAngle.floatValue / (sectorAlignCenter.boolValue ? (childCount + 1) : (childCount - 1));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"子物体数量: {childCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"实际角度间隔: {actualAngle:F1}°", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawAdvancedSettings()
        {
            showAdvancedSettings =
                EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true, EditorStyles.foldoutHeader);
            if (!showAdvancedSettings) return;

            EditorGUILayout.BeginVertical("HelpBox");

            // 控制子物体大小
            EditorGUILayout.BeginHorizontal();
            controlChildSize.boolValue =
                EditorGUILayout.ToggleLeft("控制子物体大小", controlChildSize.boolValue, GUILayout.Width(120));

            if (controlChildSize.boolValue)
            {
                childSize.vector2Value = EditorGUILayout.Vector2Field("", childSize.vector2Value);
            }

            EditorGUILayout.EndHorizontal();

            if (controlChildSize.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("宽度", GUILayout.Width(40));
                childSize.vector2Value = new Vector2(
                    EditorGUILayout.FloatField(childSize.vector2Value.x),
                    childSize.vector2Value.y
                );
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("高度", GUILayout.Width(40));
                childSize.vector2Value = new Vector2(
                    childSize.vector2Value.x,
                    EditorGUILayout.FloatField(childSize.vector2Value.y)
                );
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5),
                fontSize = 11
            };

            // 立即应用按钮
            if (GUILayout.Button("立即刷新", buttonStyle))
            {
                var layoutGroup = target as CircleLayoutGroup;

                if (layoutGroup != null)
                {
                    layoutGroup.CalculateLayoutInputHorizontal();
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    SceneView.RepaintAll();
                }
            }

            // 重置按钮
            if (GUILayout.Button("重置参数", buttonStyle))
            {
                radius.floatValue = 100f;
                initAngle.floatValue = 0f;
                circleCenter.vector2Value = Vector2.zero;
                sectorAngle.floatValue = 90f;
                keepRadLen.boolValue = false;
                keepRadLenVal.floatValue = 50f;
                sectorAlignCenter.boolValue = false;
                sectorClockwise.boolValue = true;
                controlChildSize.boolValue = false;
                childSize.vector2Value = Vector2.one * 100f;
            }

            EditorGUILayout.EndHorizontal();

            // 状态显示
            EditorGUILayout.Space(3);
            var layoutGroupTarget = target as CircleLayoutGroup;

            if (layoutGroupTarget != null)
            {
                int activeChildren = 0;

                foreach (Transform child in layoutGroupTarget.transform)
                {
                    if (child.gameObject.activeSelf) activeChildren++;
                }

                EditorGUILayout.LabelField($"子物体: {activeChildren} 活跃 / {layoutGroupTarget.transform.childCount} 总计",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            }
        }

        // 在Scene视图中绘制辅助线
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawGizmos(CircleLayoutGroup layoutGroup, GizmoType gizmoType)
        {
            if (!layoutGroup.enabled) return;

            // 绘制圆形轮廓
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Vector3 center = layoutGroup.transform.TransformPoint(layoutGroup.circleCenter);
            float worldRadius = layoutGroup.radius * GetWorldScale(layoutGroup.transform);

            Handles.DrawWireDisc(center, Vector3.forward, worldRadius);

            // 如果是扇形模式，绘制扇形区域
            if (layoutGroup.mode == CircleLayoutGroup.LayoutMode.Sector && layoutGroup.sectorAngle > 0)
            {
                Handles.color = new Color(0.2f, 0.8f, 1f, 0.1f);
                float startAngle = layoutGroup.initAngle;
                float endAngle = startAngle +
                                 (layoutGroup.sectorClockwise ? layoutGroup.sectorAngle : -layoutGroup.sectorAngle);

                Vector3 startDir = Quaternion.Euler(0, 0, startAngle) * Vector3.right;
                Vector3 endDir = Quaternion.Euler(0, 0, endAngle) * Vector3.right;

                Handles.DrawSolidArc(center, Vector3.forward, startDir,
                    layoutGroup.sectorAngle * (layoutGroup.sectorClockwise ? 1 : -1), worldRadius);

                // 绘制扇形边界线
                Handles.color = new Color(1f, 0.5f, 0.2f, 0.6f);
                Handles.DrawLine(center, center + startDir * worldRadius);
                Handles.DrawLine(center, center + endDir * worldRadius);
            }
        }

        private static float GetWorldScale(Transform transform)
        {
            Vector3 lossyScale = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), 0.001f);
        }
    }
}

#endif