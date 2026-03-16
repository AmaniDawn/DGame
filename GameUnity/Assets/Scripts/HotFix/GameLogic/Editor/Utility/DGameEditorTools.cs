#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;

namespace GameLogic
{
    public static class DGameEditorTools
    {
        #region 导出工具

        [MenuItem("Assets/导出Unity资源包", false, 20)]
        private static void ExportPackage()
        {
            if (Selection.objects.Length == 0)
            {
                return;
            }

            var assetPaths = new string[Selection.objects.Length];

            for (var i = 0; i < assetPaths.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GetAssetPath(Selection.objects[i]);
            }

            ExportPackage(assetPaths);
        }

        public static void ExportPackage(string[] assetPaths)
        {
            var path = EditorUtility.SaveFilePanel("导出Unity资源包", "", "", "unitypackage");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            assetPaths = AssetDatabase.GetDependencies(assetPaths);
            AssetDatabase.ExportPackage(assetPaths, path,
                ExportPackageOptions.Interactive | ExportPackageOptions.Recurse |
                ExportPackageOptions.IncludeDependencies);
        }

        [MenuItem("Assets/导出Unity资源包(不包含Shader和脚本)", false, 20)]
        private static void ExportPackageWithoutShaderAndScript()
        {
            if (Selection.objects.Length == 0)
            {
                return;
            }

            var path = EditorUtility.SaveFilePanel("导出Unity资源包(不包含Shader和脚本)", "", "", "unitypackage");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var assetPaths = new string[Selection.objects.Length];

            for (var i = 0; i < assetPaths.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GetAssetPath(Selection.objects[i]);
            }

            assetPaths = AssetDatabase.GetDependencies(assetPaths);
            List<string> listAssetPath = new List<string>(assetPaths);

            for (int i = 0; i < listAssetPath.Count; i++)
            {
                var assetPath = listAssetPath[i];

                if (assetPath.StartsWith("Assets/Scripts/") || assetPath.StartsWith("Assets/BundleAssets/Shader/")
                                                            || assetPath.StartsWith("Assets/DGame/"))
                {
                    listAssetPath.RemoveAt(i);
                    --i;
                }
            }

            AssetDatabase.ExportPackage(listAssetPath.ToArray(), path, ExportPackageOptions.Interactive);
        }

        #endregion
    }
}

#endif