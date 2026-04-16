#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UnityEditorUtil
    {
        /// <summary>
        /// 设置新建的 UI 组件作为 Canvas的子物体
        /// </summary>
        /// <param name="rectTransform"></param>
        public static void ResetInCanvasFor(RectTransform rectTransform)
        {
            // 检查是否在预制体编辑模式
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null)
            {
                // 预制体模式
                PlaceUIElementInPrefabStage(rectTransform, prefabStage);
            }
            else
            {
                // 正常场景模式
                PlaceUIElementInScene(rectTransform);
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0);
            Selection.activeGameObject = rectTransform.gameObject;
        }

        /// <summary>
        /// 在预制体编辑模式下放置 UI 元素
        /// </summary>
        private static void PlaceUIElementInPrefabStage(RectTransform rectTransform, PrefabStage prefabStage)
        {
            rectTransform.SetParent(Selection.activeTransform);

            if (!ParentHasCanvas(rectTransform))
            {
                // 在预制体中查找 Canvas
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                Canvas canvas = prefabRoot.GetComponentInChildren<Canvas>();

                if (canvas != null)
                {
                    rectTransform.SetParent(canvas.transform);
                }
                else
                {
                    // 在预制体根物体下创建 Canvas
                    Canvas canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                    canvasObject.transform.SetParent(prefabRoot.transform, false);
                    canvasObject.renderMode = RenderMode.ScreenSpaceOverlay;
                    rectTransform.SetParent(canvasObject.transform);
                }
            }
        }

        /// <summary>
        /// 在场景模式下放置 UI 元素
        /// </summary>
        private static void PlaceUIElementInScene(RectTransform rectTransform)
        {
            rectTransform.SetParent(Selection.activeTransform);

            if (!ParentHasCanvas(rectTransform))
            {
                // 如果不存在具有 canvas 组件的父物体 就查找或创建一个场景中存在的
                Transform canvas = GetOrCreateCanvas();
                rectTransform.SetParent(canvas);
            }

            if (!DGame.Utility.UnityUtil.FindObjectOfType<UnityEngine.EventSystems.EventSystem>())
            {
                // 创建 EventSystem 组件
                GameObject eventSystem = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }
        }

        /// <summary>
        /// 获取或创建一个带有 Canvas 组件的物体
        /// </summary>
        /// <returns></returns>
        public static Transform GetOrCreateCanvas()
        {
            Canvas canvas = DGame.Utility.UnityUtil.FindObjectOfType<Canvas>();

            if (canvas != null)
            {
                return canvas.transform;
            }
            else
            {
                Canvas canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                canvasObject.renderMode = RenderMode.ScreenSpaceOverlay;

                GameObject uiRoot = GameObject.Find("UICanvas");

                if (uiRoot != null)
                {
                    canvasObject.transform.SetParent(uiRoot.transform, false);
                }
                return canvasObject.transform;
            }
        }

        /// <summary>
        /// 查询父物体是否存在 Canvas 组件
        /// </summary>
        /// <param name="transf">子物体</param>
        /// <returns></returns>
        public static bool ParentHasCanvas(Transform transf)
        {
            while (transf.parent)
            {
                transf = transf.parent;

                if (transf.TryGetComponent<Canvas>(out var canvas))
                {
                    return true;
                }
            }
            return false;
        }

        public static Rect GetGUIRect(float width, float height)
        {
            return GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(width <= 0), GUILayout.ExpandHeight(height <= 0));
        }

        /// <summary>
        /// 创建一个折叠框布局
        /// </summary>
        /// <param name="action">绘制事件</param>
        /// <param name="label">折叠框标题</param>
        /// <param name="isOpen">是否折叠</param>
        /// <param name="isBox">是否有装饰框</param>
        public static void LayoutFrameBox(System.Action action, string label, ref bool isOpen, bool isBox = false)
        {
            bool open = isOpen;
            LayoutVertical(() =>
            {
                open = GUILayout.Toggle(open, label, GUI.skin.GetStyle("foldout"), GUILayout.ExpandWidth(true), GUILayout.Height(18));

                if (open)
                {
                    action?.Invoke();
                }
            }, isBox);
            isOpen = open;
        }

        /// <summary>
        /// 创建水平布局区域
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isBox"></param>
        public static void LayoutHorizontal(System.Action action, bool isBox = false)
        {
            if (isBox)
            {
                GUIStyle style = new GUIStyle("HelpBox");
                GUILayout.BeginHorizontal(style);
            }
            else
            {
                GUILayout.BeginHorizontal();
            }
            action();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 创建水平布局区域
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isBox"></param>
        public static void LayoutVertical(System.Action action, bool isBox = false)
        {
            if (isBox)
            {
                GUIStyle style = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(2, 2, 2, 2)
                };
                GUILayout.BeginVertical(style);
            }
            else
            {
                GUILayout.BeginVertical();
            }
            action();
            GUILayout.EndVertical();
        }

        public static bool DrawAutoSizeButton(string text, float minWidth = 80f, float maxWidth = 400f)
        {
            // 创建根据文本内容调整的GUIStyle
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            // 计算文本所需的大小
            GUIContent content = new GUIContent(text);
            Vector2 textSize = buttonStyle.CalcSize(content);

            // 设置宽度，限制在最小和最大值之间
            float buttonWidth = Mathf.Clamp(textSize.x + 20f, minWidth, maxWidth);

            // 绘制按钮
            return GUILayout.Button(text, buttonStyle, GUILayout.Width(buttonWidth));
        }
    }
}

#endif