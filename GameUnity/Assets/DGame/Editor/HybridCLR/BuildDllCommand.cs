using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if ENABLE_HYBRIDCLR

using HybridCLR.Editor;
using HybridCLR.Editor.Commands;

#endif

#if ENABLE_OBFUZ

using Obfuz4HybridCLR;
using Obfuz.Settings;

#endif

namespace DGame
{
    public static class BuildDllCommand
    {
        [MenuItem("DGame Tools/Build/Build Dll And CopyTo AssemblyTextAssetPath", priority = 150)]
        public static void BuildAndCopyDlls()
        {
#if ENABLE_HYBRIDCLR
            // 当前平台
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(buildTarget);
            CopyAotAndHotUpdateDlls(buildTarget);
#endif
        }

        private static void CopyAotAndHotUpdateDlls(BuildTarget buildTarget)
        {
            CopyAotAssembliesToAssetPath(buildTarget);
            CopyHotUpdateAssembliesToAssetPath(buildTarget);

#if ENABLE_HYBRIDCLR && ENABLE_OBFUZ
        CompileDllCommand.CompileDll(buildTarget);

        string obfuscatedHotUpdateDllPath = PrebuildCommandExt.GetObfuscatedHotUpdateAssemblyOutputPath(buildTarget);
        ObfuscateUtil.ObfuscateHotUpdateAssemblies(buildTarget, obfuscatedHotUpdateDllPath);

        Directory.CreateDirectory(Application.streamingAssetsPath);

        string hotUpdateDllPath = $"{SettingsUtil.GetHotUpdateDllsOutputDirByTarget(buildTarget)}";
        List<string> obfuscationRelativeAssemblyNames = ObfuzSettings.Instance.assemblySettings.GetObfuscationRelativeAssemblyNames();

        foreach (string assName in SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
        {
            string srcDir = obfuscationRelativeAssemblyNames.Contains(assName) ? obfuscatedHotUpdateDllPath : hotUpdateDllPath;
            string srcFile = $"{srcDir}/{assName}.dll";
            string dstFile = Application.dataPath +"/"+ DGame.Settings.UpdateSettings.AssemblyTextAssetPath  + $"/{assName}.dll.bytes";
            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, dstFile, true);
                Debug.Log($"[CompileAndObfuscate] Copy {srcFile} to {dstFile}");
            }
        }
#endif

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将AOT（预编译）程序集复制到资源路径中，供HybridCLR热更新使用
        /// AOT程序集是IL2CPP编译后生成的裁剪过的原生程序集，用于补充元数据
        /// </summary>
        private static void CopyAotAssembliesToAssetPath()
        {
#if ENABLE_HYBRIDCLR
            var target = EditorUserBuildSettings.activeBuildTarget;
            string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string aotAssembliesDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);

            foreach (var aotDll in SettingsUtil.AOTAssemblyNames)
            {
                string srcDllPath = $"{aotAssembliesSrcDir}/{aotDll}";

                if (!File.Exists(srcDllPath))
                {
                    Debug.LogError($"[CopyAotAssembliesToAssetPath] ab中添加AOT补充元数据DLL：{srcDllPath} 时发生错误，文件不存在。裁剪后的AOT程序集在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包");
                    continue;
                }
                string dllBytesPath = $"{aotAssembliesDstDir}/{aotDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                File.Copy(srcDllPath, dllBytesPath, true);
                Debug.Log($"[CopyAotAssembliesToAssetPath] copy AOT DLL {srcDllPath} -> {dllBytesPath}]");
            }
#endif
        }

        /// <summary>
        /// 将AOT（预编译）程序集复制到资源路径中，供HybridCLR热更新使用
        /// AOT程序集是IL2CPP编译后生成的裁剪过的原生程序集，用于补充元数据
        /// </summary>
        private static void CopyAotAssembliesToAssetPath(BuildTarget target)
        {
#if ENABLE_HYBRIDCLR
            // 获取AOT程序集的源目录（IL2CPP裁剪后的输出目录）
            string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            // 构建目标目录路径，程序集将作为TextAsset资源存储在此处
            string aotAssembliesDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);

            // 遍历配置中指定的所有AOT元数据程序集
            foreach (var aotDll in SettingsUtil.AOTAssemblyNames)
            {
                // 构建源DLL文件的完整路径
                string srcDllPath = $"{aotAssembliesSrcDir}/{aotDll}";

                // 检查源文件是否存在，如果不存在则报错并跳过
                if (!File.Exists(srcDllPath))
                {
                    Debug.LogError($"[CopyAotAssembliesToAssetPath] ab中添加AOT补充元数据DLL：{srcDllPath} 时发生错误，文件不存在。裁剪后的AOT程序集在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包");
                    continue;
                }
                // 构建目标文件路径，添加文件扩展名使其可作为Unity的TextAsset加载
                string dllBytesPath = $"{aotAssembliesDstDir}/{aotDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                // 复制DLL文件到目标路径，覆盖已存在的文件
                File.Copy(srcDllPath, dllBytesPath, true);
                // 记录复制操作日志，便于调试和追踪
                Debug.Log($"[CopyAotAssembliesToAssetPath] copy AOT DLL {srcDllPath} -> {dllBytesPath}]");
            }
#endif
        }

        /// <summary>
        /// 将热更新程序集复制到资源路径中，供HybridCLR运行时加载使用
        /// 热更新程序集包含需要动态更新的业务逻辑代码
        /// </summary>
        private static void CopyHotUpdateAssembliesToAssetPath(BuildTarget target)
        {
#if ENABLE_HYBRIDCLR
            // 获取指定平台的热更新DLL输出目录
            // 这个目录包含了编译后的热更新程序集文件
            string hotFixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            // 构建目标目录路径，热更新程序集将作为TextAsset资源存储在此处
            string hotFixDllDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);
            // 遍历所有热更新程序集文件（排除保留的程序集）
            // 这些是需要在运行时动态加载和更新的业务逻辑代码
            foreach (var hotFixDll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                // 构建源DLL文件的完整路径
                string dllPath = Path.Combine(hotFixDllSrcDir, hotFixDll);
                // 构建目标文件路径，添加文件扩展名使其可作为Unity的TextAsset加载
                // 通常扩展名为".bytes"，便于Unity识别和打包
                string dllBytesPath = $"{hotFixDllDstDir}/{hotFixDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                // 复制热更新DLL文件到目标路径，覆盖已存在的文件
                // 这样热更新代码就可以作为资源文件被打包和加载
                File.Copy(dllPath, dllBytesPath, true);
                // 记录复制操作日志，便于调试和追踪热更新文件的准备过程
                Debug.Log($"[CopyHotUpdateAssembliesToAssetPath] copy HotUpdate DLL {dllPath} -> {dllBytesPath}]");
            }
#endif
        }

        /// <summary>
        /// 将热更新程序集复制到资源路径中，供HybridCLR运行时加载使用
        /// 热更新程序集包含需要动态更新的业务逻辑代码
        /// </summary>
        private static void CopyHotUpdateAssembliesToAssetPath()
        {
#if ENABLE_HYBRIDCLR
            var target = EditorUserBuildSettings.activeBuildTarget;
            string hotFixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            string hotFixDllDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);
            foreach (var hotFixDll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                string dllPath = Path.Combine(hotFixDllSrcDir, hotFixDll);
                string dllBytesPath = $"{hotFixDllDstDir}/{hotFixDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                File.Copy(dllPath, dllBytesPath, true);
                Debug.Log($"[CopyHotUpdateAssembliesToAssetPath] copy HotUpdate DLL {dllPath} -> {dllBytesPath}]");
            }
#endif
        }
    }
}