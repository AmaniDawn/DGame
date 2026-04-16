using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DGame
{
    public class UnityEditorUtil
    {
        /// <summary>
        /// 设置新建的 UI 组件作为 Canvas的子物体
        /// </summary>
        /// <param name="rectTransform"></param>
        public static void ResetInCanvasFor(RectTransform rectTransform)
        {
            rectTransform.SetParent(Selection.activeTransform);

            if (!ParentHasCanvas(rectTransform))
            {
                // 如果不存在具有 canvas 组件的父物体 就查找或创建一个场景中存在的
                Transform canvas = GetOrCreateCanvas();
                rectTransform.SetParent(canvas);
            }

            bool isFind = DGame.Utility.UnityUtil.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (!isFind)
            {
                // 创建 EventSystem 组件
                GameObject eventSystem = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0);
            Selection.activeGameObject = rectTransform.gameObject;
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
        public static void LayoutFoldoutBox(System.Action action, string label, ref bool isOpen, bool isBox = false)
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
                GUIStyle style = new GUIStyle(GUI.skin.box);
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
                GUIStyle style = new GUIStyle(GUI.skin.box)
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

        #region 脚本替换工具

        private static Dictionary<Type, int> m_classIdCache = new Dictionary<Type, int>();

        public static int GetClassIDCached(System.Type type)
        {
            if (!m_classIdCache.TryGetValue(type, out int classId))
            {
                classId = GetClassID(type);
                m_classIdCache[type] = classId;
            }
            return classId;
        }

        /// <summary>
        /// 获取 type 类型脚本的 classID
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static int GetClassID(System.Type type)
        {
            if (!typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                throw new ArgumentException($"类型 {type} 必须是MonoBehaviour类型或子类型");
            }

            GameObject go = null;

            try
            {
                go = EditorUtility.CreateGameObjectWithHideFlags("Temp", HideFlags.HideAndDontSave);
                Component uiSprite = go.AddComponent(type);
                SerializedObject ob = new SerializedObject(uiSprite);
                int classID = ob.FindProperty("m_Script").objectReferenceInstanceIDValue;
                return classID;
            }
            finally
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        /// <summary>
        /// 根据泛型获取脚本的 classID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int GetClassID<T>() where T : MonoBehaviour
        {
            return GetClassIDCached(typeof(T));
        }

        /// <summary>
        /// 根据脚本类型ID 替换脚本
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SerializedObject ReplaceClass(MonoBehaviour mb, System.Type type)
        {
            int id = GetClassIDCached(type);
            SerializedObject ob = new SerializedObject(mb);
            ob.Update();
            ob.FindProperty("m_Script").objectReferenceInstanceIDValue = id;
            ob.ApplyModifiedProperties();
            ob.Update();
            return ob;
        }

        /// <summary>
        /// 根据脚本类型ID 替换脚本
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public static SerializedObject ReplaceClass<T>(MonoBehaviour mb) where T : MonoBehaviour
        {
            int id = GetClassID<T>();
            SerializedObject ob = new SerializedObject(mb);
            ob.Update();
            ob.FindProperty("m_Script").objectReferenceInstanceIDValue = id;
            ob.ApplyModifiedProperties();
            ob.Update();
            return ob;
        }

        #endregion

        #region 绘制GUI

        public static void DrawChineseEnumPopup<T>(SerializedProperty prop, string popupName) where T : Enum
        {
            var formats = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var chineseNames = formats.Select(f => f.GetDescription()).ToArray();
            int currentValue = prop.enumValueIndex;
            int selectedIndex = Array.IndexOf(formats.Cast<int>().ToArray(), currentValue);
            selectedIndex = EditorGUILayout.Popup(popupName, selectedIndex, chineseNames);
            if (selectedIndex >= 0 && selectedIndex < formats.Length)
            {
                prop.enumValueIndex = selectedIndex;
            }
        }

        public static string DrawFolderField(string label, string labelIcon, string path)
        {
            using var horizontalScope = new EditorGUILayout.HorizontalScope();

            var buttonGUIContent = new GUIContent("选择", EditorGUIUtility.IconContent("Folder Icon").image);

            if (!string.IsNullOrEmpty(labelIcon))
            {
                var labelGUIContent = new GUIContent(" " + label, EditorGUIUtility.IconContent(labelIcon).image);
                path = EditorGUILayout.TextField(labelGUIContent, path);
            }
            else
            {
                path = EditorGUILayout.TextField(label, path);
            }

            if (GUILayout.Button(buttonGUIContent, GUILayout.Width(60), GUILayout.Height(20)))
            {
                var newPath = EditorUtility.OpenFolderPanel(label, path, string.Empty);
                newPath = newPath.Replace(Application.dataPath, "Assets");
                if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(Application.dataPath))
                {
                    path = "Assets" + newPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("路径不在Unity项目内: " + newPath);
                }
            }
            return path;
        }

        public void DrawPathArrItem(string label, string itemLabel, string iconName, ref string[] paths, ref bool isShow)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            isShow = EditorGUILayout.BeginFoldoutHeaderGroup(isShow, label);
            // GUILayout.Label("", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            if (isShow)
            {
                GUILayout.Label("数量:", GUILayout.ExpandWidth(false));
                int newSize = EditorGUILayout.IntField(paths.Length, GUILayout.Width(40));
                newSize = Mathf.Max(0, newSize);
                if (newSize != paths.Length)
                {
                    Array.Resize(ref paths, newSize);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(25), GUILayout.Height(20)))
                {
                    Array.Resize(ref paths, paths.Length + 1);
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"), GUILayout.Width(25), GUILayout.Height(20)) && paths.Length > 0)
                {
                    Array.Resize(ref paths, paths.Length - 1);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (isShow)
            {
                EditorGUILayout.BeginVertical("box");
                for (int i = 0; i < paths.Length; i++)
                {
                    paths[i] = DrawFolderField($"{itemLabel}[{i}]", iconName, paths[i]);
                    // var keywordsContent = new GUIContent($" 关键词 [{i}]", EditorGUIUtility.IconContent("FilterByLabel").image);
                    // config.excludeKeywords[i] = EditorGUILayout.TextField(keywordsContent, config.excludeKeywords[i]);
                }
                GUILayout.Space(2);
                if (GUILayout.Button(new GUIContent(" 清空", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), GUILayout.Height(25)))
                {
                    paths = Array.Empty<string>();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion

        #region Path相关

        public static List<string> GetSelectedObjectFolderPaths(bool includeChild = false)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < Selection.assetGUIDs.Length; i++)
            {
                var selectPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[i]);

                if (Directory.Exists(selectPath))
                {
                    paths.Add(selectPath);

                    if (includeChild)
                    {
                        paths.AddRange(Directory.GetDirectories(selectPath, "*", SearchOption.AllDirectories));
                    }
                }
            }
            return paths;
        }

        public static string GetAssetPath(string filePath)
        {
            string assetsDir = "Assets";
            string dataPath = Application.dataPath;
            filePath = filePath.Replace("\\", "/");
            if (filePath.Contains(dataPath))
            {
                return filePath.Replace(dataPath, assetsDir);
            }
            return filePath;
        }

        public static void DoActionWithSelectedTargets(Action<string> action)
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                return;
            }

            foreach (var guid in Selection.assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (Directory.Exists(path))
                {
                    action?.Invoke(path);
                }
                else if (File.Exists(path))
                {
                    action?.Invoke(path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region File

        public static void GetAllFilesFromPath(List<string> listFilePath, string dirPath, string searchPattern = "*.*")
        {
            if (!Directory.Exists(dirPath))
            {
                return;
            }
            var dirInfo = new DirectoryInfo(dirPath);
            var files = dirInfo.GetFiles(searchPattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string filePath = GetAssetPath(file.FullName);
                listFilePath.Add(filePath);
            }
        }

        static public string AssetPathToGUID(string assetPath)
        {
            if (assetPath.EndsWith("/"))
            {
                assetPath = assetPath.Remove(assetPath.Length - 1, 1);
            }

            if (File.Exists(assetPath) || Directory.Exists(assetPath))
            {
                return AssetDatabase.AssetPathToGUID(assetPath);
            }

            Debug.LogError("资产不存在:" + assetPath);

            return null;
        }

        #endregion

        #region AssetDatabase.GetAssetPath

        public static string GetAssetPath(int instanceID)
            =>
#if UNITY_6000_0_OR_NEWER
                AssetDatabase.GetAssetPath((EntityId)instanceID);
#else
                AssetDatabase.GetAssetPath(instanceID);
#endif

        #endregion
    }
}