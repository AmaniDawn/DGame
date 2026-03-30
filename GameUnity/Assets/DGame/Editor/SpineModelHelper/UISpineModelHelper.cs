#if UNITY_EDITOR && SPINE_UNITY && SPINE_CSHARP

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static partial class SpineModelHelper
    {
        // 获取SceneHierarchyWindow的实例（缓存优化）
        private static object m_sceneHierarchyCache = null;

        #region Spine资源路径

        private const string SPINE_PATH = "Assets/BundleAssets/Actor/Spine";
        private const string UI_SPINE_SAVE_PATH = "Assets/BundleAssets/Prefabs/UISpineModel";
        private const string SPINE_SAVE_PATH = "Assets/BundleAssets/Prefabs/Model";

        #endregion

        [MenuItem("Assets/Spine/导入Spine资源到项目")]
        private static void ImportSpineAssets()
        {
            // 选择源文件夹
            var sourcePath = EditorUtility.OpenFolderPanel("选择Spine资源文件夹", "", "");
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            // 确认导入
            if (!EditorUtility.DisplayDialog("确认导入",
                $"将文件夹 \"{Path.GetFileName(sourcePath)}\" 中的Spine资源导入到:\n{SPINE_PATH}\n\n是否继续?",
                "导入", "取消"))
            {
                return;
            }

            // 获取源文件夹中所有文件
            var sourceFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            var dirName = Path.GetFileNameWithoutExtension(sourceFiles.First(f => f.EndsWith(".png")));
            // 获取源文件夹名称
            // var folderName = Path.GetFileName(sourcePath);
            var targetPath = $"{SPINE_PATH}/{dirName}";
            // 创建目标目录
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            var importedCount = 0;

            foreach (var file in sourceFiles)
            {
                // 跳过.meta文件
                if (file.EndsWith(".meta")) continue;

                var fileName = Path.GetFileName(file);
                var targetFile = $"{targetPath}/{fileName}";

                // 复制文件
                File.Copy(file, targetFile, true);
                importedCount++;
            }

            // 刷新资源数据库
            AssetDatabase.Refresh();

            Debug.Log($"Spine资源导入完成! 导入路径: {targetPath}, 文件数: {importedCount}");

            // 选中新导入的文件夹
            var importedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetPath);
            Selection.activeObject = importedFolder;
            EditorGUIUtility.PingObject(importedFolder);
        }

        [MenuItem("Assets/Spine/处理UISpine模型")]
        private static void GenUISpineModel()
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < Selection.assetGUIDs.Length; i++)
            {
                var guid = Selection.assetGUIDs[i];
                var onePath = AssetDatabase.GUIDToAssetPath(guid);
                Debug.LogFormat("处理UISpine模型: {0}", onePath);

                if (File.Exists(onePath))
                {
                    var skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(onePath);

                    // 获取实际的SkeletonData对象，参数true表示不使用缓存，强制重新加载
                    if (skeletonDataAsset.GetSkeletonData(true) == null)
                    {
                        EditorUtility.DisplayDialog("Invalid SkeletonDataAsset",
                            "Unable to create Spine GameObject.\n\nPlease check your SkeletonDataAsset.", "Ok");
                        return;
                    }

                    if (skeletonDataAsset != null)
                    {
                        var assetName = skeletonDataAsset.name.Replace(AssetUtility.SkeletonDataSuffix, "");
                        var animationName = onePath.StartsWith(SPINE_PATH) ? "idle" : "run";

                        GameObject canvasGameObject = new GameObject("Canvas");
                        Canvas canvas = canvasGameObject.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                        GameObject parentGameObject = new GameObject();
                        parentGameObject.name = assetName;
                        parentGameObject.layer = LayerMask.NameToLayer("UI");
                        parentGameObject.transform.SetParent(canvas.transform, false);
                        parentGameObject.AddComponent<RectTransform>();

                        GameObject shaPanGameObject = new GameObject("m_tfUISpineRoot");
                        shaPanGameObject.transform.SetParent(parentGameObject.transform, false);
                        shaPanGameObject.AddComponent<RectTransform>();

                        GameObject effGameObject = new GameObject("m_tfEffRoot");
                        effGameObject.transform.SetParent(parentGameObject.transform, false);
                        effGameObject.AddComponent<RectTransform>();

                        /*var skeletonGraphicInspectorType =
                            System.Type.GetType("Spine.Unity.Editor.SkeletonGraphicInspector");
                        var graphicInstantiateDelegate =
                            skeletonGraphicInspectorType?.GetMethod("SpawnSkeletonGraphicFromDrop",
                                BindingFlags.Static | BindingFlags.Public);
                        var instantiateDelegate =
                            System.Delegate.CreateDelegate(typeof(EditorInstantiation.InstantiateDelegate),
                                graphicInstantiateDelegate) as EditorInstantiation.InstantiateDelegate;
                        SkeletonGraphic newSkeletonComponent =
                            (SkeletonGraphic)instantiateDelegate.Invoke(skeletonDataAsset);
                        newSkeletonComponent.startingAnimation = animationName;
                        newSkeletonComponent.raycastTarget = false;
                        // var material =
                        //     AssetDatabase.LoadAssetAtPath<Material>(
                        //         "Assets/Plugins/Spine/Runtime/spine-unity/Materials/UI-PMATexture/SkeletonGraphicDefault.mat");
                        // newSkeletonComponent.material = material;
                        newSkeletonComponent.MeshGenerator.settings.useClipping = false;
                        newSkeletonComponent.allowMultipleCanvasRenderers = true;
                        GameObject newGameObject = newSkeletonComponent.gameObject;
                        newGameObject.transform.position = Vector3.zero;
                        newGameObject.name = "m_goSpineModel";
                        newGameObject.transform.SetParent(shaPanGameObject.transform, false);*/

                        GameObject spineGameObject = new GameObject("m_goSpineModel");
                        SkeletonGraphic newSkeletonComponent = spineGameObject.AddComponent<SkeletonGraphic>();
                        newSkeletonComponent.skeletonDataAsset = skeletonDataAsset;
                        newSkeletonComponent.startingAnimation = animationName;
                        newSkeletonComponent.raycastTarget = false;
                        // newSkeletonComponent.material = null; // 使用默认材质
                        newSkeletonComponent.MeshGenerator.settings.useClipping = true;
                        newSkeletonComponent.allowMultipleCanvasRenderers = true;
                        spineGameObject.transform.SetParent(shaPanGameObject.transform, false);

                        SetExpandedRecursive(canvasGameObject, true);
                        //Selection.activeGameObject = newGameObject;

                        string savePath = GetSavePath(onePath, true);
                        savePath += $"/{assetName}";
                        if (string.IsNullOrEmpty(savePath))
                        {
                            Debug.Log("savePath error.");
                            return;
                        }

                        if (!Directory.Exists(savePath))
                        {
                            Directory.CreateDirectory(savePath);
                        }

                        savePath = $"{savePath}/{assetName}.prefab";
                        PrefabUtility.SaveAsPrefabAsset(parentGameObject, savePath);
                        Debug.LogFormat("UISpine模型生成结束. {0}", savePath);
                        //GameObject.DestroyImmediate(newGameObject);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static string GetSavePath(string originalPath, bool isUI = false)
        {
            var tPath = isUI ? UI_SPINE_SAVE_PATH : SPINE_SAVE_PATH;
            var savePathDir = tPath;
            return savePathDir;

        }

        /// <summary>
        /// 递归展开指定的GameObject及其所有子物体
        /// </summary>
        /// <param name="go"></param>
        /// <param name="expand"></param>
        private static void SetExpandedRecursive(GameObject go, bool expand)
        {
            if (go == null) return;
            // 使用栈替代递归，避免深层层级导致的栈溢出
            var stack = new Stack<Transform>();
            stack.Push(go.transform);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                SetExpanded(current.gameObject, expand);

                foreach (Transform child in current)
                {
                    stack.Push(child);
                }
            }
        }

        /// <summary>
        /// 展开指定的GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <param name="expand"></param>
        private static void SetExpanded(GameObject go, bool expand)
        {
            if (go == null) return;

            try
            {
                var sceneHierarchy = GetSceneHierarchyWindow();
                if (sceneHierarchy == null) return;

                var methodInfo = sceneHierarchy.GetType().GetMethod("ExpandTreeViewItem",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo?.Invoke(sceneHierarchy, new object[] { go.GetInstanceID(), expand });
            }
            catch
            {
                // Unity版本更新可能导致反射失效，静默处理
            }
        }

        /// <summary>
        /// 获取SceneHierarchyWindow的实例
        /// </summary>
        /// <returns></returns>
        private static object GetSceneHierarchyWindow()
        {
            if (m_sceneHierarchyCache != null) return m_sceneHierarchyCache;
            var hierarchyWindow = GetHierarchyWindow();
            if (hierarchyWindow == null) return null;
            m_sceneHierarchyCache = typeof(EditorWindow).Assembly
                .GetType("UnityEditor.SceneHierarchyWindow")
                ?.GetProperty("sceneHierarchy")
                ?.GetValue(hierarchyWindow);
            return m_sceneHierarchyCache;
        }

        /// <summary>
        /// 获取当前的Hierarchy窗口
        /// </summary>
        /// <returns></returns>
        private static EditorWindow GetHierarchyWindow()
            => Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(w => w.GetType().FullName == "UnityEditor.SceneHierarchyWindow");
    }
}

#endif