using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if ENABLE_HYBRIDCLR

using HybridCLR;
using HybridCLR.Commands;

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
            if (!TryBuildAndCopyDlls())
            {
                throw new InvalidOperationException("编译或复制热更新 DLL 失败，请检查控制台日志");
            }
        }

        public static bool TryBuildAndCopyDlls()
        {
#if ENABLE_HYBRIDCLR
            try
            {
                BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                CompileDllCommand.CompileDll(buildTarget);
                return CopyAotAndHotUpdateDlls(buildTarget);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
#else
            return true;
#endif
        }

        private static bool CopyAotAndHotUpdateDlls(BuildTarget buildTarget)
        {
            bool success = CopyAotAssembliesToAssetPath(buildTarget);
            success &= CopyHotUpdateAssembliesToAssetPath(buildTarget);

#if ENABLE_HYBRIDCLR && ENABLE_OBFUZ
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
            else
            {
                Debug.LogError($"[CompileAndObfuscate] DLL 不存在: {srcFile}");
                success = false;
            }
        }
#endif

            AssetDatabase.Refresh();
            return success;
        }

        /// <summary>
        /// 将AOT（预编译）程序集复制到资源路径中，供HybridCLR热更新使用
        /// AOT程序集是IL2CPP编译后生成的裁剪过的原生程序集，用于补充元数据
        /// </summary>
        private static bool CopyAotAssembliesToAssetPath(BuildTarget target)
        {
#if ENABLE_HYBRIDCLR
            // 获取AOT程序集的源目录（IL2CPP裁剪后的输出目录）
            string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            // 构建目标目录路径，程序集将作为TextAsset资源存储在此处
            string aotAssembliesDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);
            Directory.CreateDirectory(aotAssembliesDstDir);
            bool success = true;

            // 遍历配置中指定的所有AOT元数据程序集
            foreach (var aotDll in DGame.Settings.UpdateSettings.AOTMetaAssemblies)
            {
                // 构建源DLL文件的完整路径
                string srcDllPath = $"{aotAssembliesSrcDir}/{aotDll}";

                // 检查源文件是否存在，如果不存在则报错并跳过
                if (!File.Exists(srcDllPath))
                {
                    Debug.LogError($"[CopyAotAssembliesToAssetPath] ab中添加AOT补充元数据DLL：{srcDllPath} 时发生错误，文件不存在。裁剪后的AOT程序集在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包");
                    success = false;
                    continue;
                }
                // 构建目标文件路径，添加文件扩展名使其可作为Unity的TextAsset加载
                string dllBytesPath = $"{aotAssembliesDstDir}/{aotDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                // 复制DLL文件到目标路径，覆盖已存在的文件
                File.Copy(srcDllPath, dllBytesPath, true);
                // 记录复制操作日志，便于调试和追踪
                Debug.Log($"[CopyAotAssembliesToAssetPath] copy AOT DLL {srcDllPath} -> {dllBytesPath}]");
            }
            return success;
#else
            return true;
#endif
        }

        /// <summary>
        /// 将热更新程序集复制到资源路径中，供HybridCLR运行时加载使用
        /// 热更新程序集包含需要动态更新的业务逻辑代码
        /// </summary>
        private static bool CopyHotUpdateAssembliesToAssetPath(BuildTarget target)
        {
#if ENABLE_HYBRIDCLR
            // 获取指定平台的热更新DLL输出目录
            // 这个目录包含了编译后的热更新程序集文件
            string hotFixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            // 构建目标目录路径，热更新程序集将作为TextAsset资源存储在此处
            string hotFixDllDstDir = Path.Combine(Application.dataPath, DGame.Settings.UpdateSettings.AssemblyTextAssetPath);
            Directory.CreateDirectory(hotFixDllDstDir);
            bool success = true;
            // 遍历所有热更新程序集文件（排除保留的程序集）
            // 这些是需要在运行时动态加载和更新的业务逻辑代码
            foreach (var hotFixDll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                // 构建源DLL文件的完整路径
                string dllPath = Path.Combine(hotFixDllSrcDir, hotFixDll);
                // 构建目标文件路径，添加文件扩展名使其可作为Unity的TextAsset加载
                // 通常扩展名为".bytes"，便于Unity识别和打包
                string dllBytesPath = $"{hotFixDllDstDir}/{hotFixDll}{DGame.Settings.UpdateSettings.AssemblyTextAssetExtension}";
                if (!File.Exists(dllPath))
                {
                    Debug.LogError($"[CopyHotUpdateAssembliesToAssetPath] 热更新 DLL 不存在: {dllPath}");
                    success = false;
                    continue;
                }
                // 复制热更新DLL文件到目标路径，覆盖已存在的文件
                // 这样热更新代码就可以作为资源文件被打包和加载
                File.Copy(dllPath, dllBytesPath, true);
                // 记录复制操作日志，便于调试和追踪热更新文件的准备过程
                Debug.Log($"[CopyHotUpdateAssembliesToAssetPath] copy HotUpdate DLL {dllPath} -> {dllBytesPath}]");
            }
            return success;
#else
            return true;
#endif
        }

    }
}
