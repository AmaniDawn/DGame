using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace DGame
{
    public static class ReleaseTools
    {

        #region Build AssetBundle

        [MenuItem("DGame Tools/Build/一键打包AB _F8", priority = 151)]
        public static void BuildCurrentPlatformAB()
        {
            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            // 获取当前构建目标平台
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            // 执行AssetBundle构建
            BuildInternal(target, Application.dataPath + "/../Builds/", packageVersion:GetBuildPackageVersion());
            AssetDatabase.Refresh();
            //复制到打包后的StreamingAssets
            CopyStreamingAssetsFiles();
        }

        /// <summary>
        /// 复制StreamingAssets文件去打包目录
        /// </summary>
        private static void CopyStreamingAssetsFiles()
        {
            if (!Settings.UpdateSettings.IsAutoAssetCopyToBuildAddress())
            {
                Debug.Log("[CopyStreamingAssetsFiles] UpdateSettings.IsAutoAssetCopyToBuildAddress关闭，不会生产到打包目录中");
                return;
            }
            // 获取StreamingAssets路径
            string streamingAssetsPath = Application.streamingAssetsPath;
            // 目标路径 可以是任何目录路径
            string targetPath = Settings.UpdateSettings.GetBuildAddress();

            if (!System.IO.Path.IsPathRooted(targetPath))
            {
                // 如果是相对路径，结合 StreamingAssets 的路径进行合并
                targetPath = System.IO.Path.Combine(streamingAssetsPath, targetPath).Replace("\\","/");
            }

            if (!Directory.Exists(targetPath))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] 打包目录不存在，检查UpdateSettings.m_buildAddress: {targetPath}");
                return;
            }
            // 删除目标目录下的所有文件
            string[] deleteFiles = Directory.GetFiles(targetPath);
            foreach (var filePath in deleteFiles)
            {
                File.Delete(filePath);
                Debug.Log($"[CopyStreamingAssetsFiles] 删除文件: {filePath}");
            }

            // 删除目录下的所有子目录
            string[] directories = Directory.GetDirectories(targetPath);
            foreach (var directory in directories)
            {
                Directory.Delete(directory, true); // true：递归删除子目录及其内容
                Debug.Log($"[CopyStreamingAssetsFiles] 删除目录: {directory}");
            }

            // 获取StreamingAssets中的所有文件，排除.meta文件
            string[] files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }
                // 获取相对路径 用于目标目录中创建相同的文件夹结构
                string relativePath = file.Substring(streamingAssetsPath.Length + 1);
                string destinationPath = Path.Combine(targetPath, relativePath);
                // 确保目标文件夹存在
                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir) && !string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                // 复制文件
                File.Copy(file, destinationPath, true); // true：覆盖存在的文件
            }
            Debug.Log($"[CopyStreamingAssetsFiles] 复制文件成功: {targetPath}");
        }

        /// <summary>
        /// 生成构建包版本号
        /// 格式：yyyy-MM-dd-分钟段（每10分钟一个段）
        /// 例如：1997-01-01-919 表示1997年1月1日的第919个10分钟段
        /// </summary>
        /// <returns></returns>
        private static string GetBuildPackageVersion()
        {
            // 计算当天从0点开始的总分钟数，然后除以10得到段数
            int totalMinutes = DateTime.Now.Hour * 6 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 内部构建方法，执行具体的AssetBundle打包流程
        /// </summary>
        /// <param name="buildTarget">目标构建平台</param>
        /// <param name="outputRoot">输出根目录</param>
        /// <param name="packageVersion">包版本号</param>
        /// <param name="buildPipeline">构建管线类型，默认使用可编程构建管线</param>
        private static void BuildInternal(BuildTarget buildTarget, string outputRoot, string packageVersion = "1.0",
            EBuildPipeline buildPipeline = EBuildPipeline.ScriptableBuildPipeline)
        {
            if (Settings.UpdateSettings.ForceGenerateAtlas)
            {
                Debug.Log($"[BuildInternal] 强制重新生成所有图集");
                EditorSpriteSaveInfo.ForceGenerateAll(true);
            }

            Debug.Log($"[BuildInternal] 开始构建AssetBundle: {buildTarget}");

            IBuildPipeline pipeline = null;
            BuildParameters buildParameters = null;

            // 根据构建管线类型创建对应的参数和管线实例
            if (buildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                // 使用Unity内置构建管线
                BuiltinBuildParameters builtinParameters = new BuiltinBuildParameters();
                pipeline = new BuiltinBuildPipeline();
                buildParameters = builtinParameters;
                // 使用LZ4压缩，平衡压缩率和加载速度
                builtinParameters.CompressOption = ECompressOption.LZ4;
            }
            else
            {
                // 使用可编程构建管线（推荐，功能更强大）
                ScriptableBuildParameters scriptableBuildParameters = new ScriptableBuildParameters();
                pipeline = new ScriptableBuildPipeline();
                buildParameters = scriptableBuildParameters;
                scriptableBuildParameters.CompressOption = ECompressOption.LZ4;
                // 设置内置着色器资源包名称，避免重复打包着色器
                scriptableBuildParameters.BuiltinShadersBundleName = GetBuiltinShaderBundleName("DefaultPackage");
                scriptableBuildParameters.ReplaceAssetPathWithAddress = Settings.UpdateSettings.GetReplaceAssetPathWithAddress();
            }

            // 配置构建参数
            buildParameters.BuildOutputRoot = outputRoot; //AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(); // 构建输出目录
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(); // 内置文件根目录
            buildParameters.BuildPipeline = buildPipeline.ToString(); // 构建管线名称
            buildParameters.BuildTarget = buildTarget; // 目标平台
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle; // 资源包类型
            buildParameters.PackageName = "DefaultPackage"; // 包名称
            buildParameters.PackageVersion = packageVersion; // 包版本
            buildParameters.VerifyBuildingResult = true; // 验证构建结果
            // 启动共享资源打包
            buildParameters.EnableSharePackRule = true; // 启用共享资源打包规则
            buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName; // 文件名风格：包名_哈希值
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll; // 清理并复制所有内置文件
            buildParameters.BuildinFileCopyParams = string.Empty; // 内置文件复制参数
            buildParameters.EncryptionServices = GetEncryptionFromResourceModuleDriver(); // 加密服务
            buildParameters.ClearBuildCacheFiles = false; // 不清理构建缓存，启用增量构建，可以提高打包速度
            buildParameters.UseAssetDependencyDB = true; // 使用资源依赖关系数据库，可以提高打包速度

            // 执行构建流程
            var buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"[BuildInternal] AssetBundle资源构建成功: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"[BuildInternal] AssetBundle资源构建失败: {buildResult.ErrorInfo}");
            }
        }

        /// <summary>
        /// 创建加密服务实例
        /// 用于AssetBundle文件加密
        /// </summary>
        private static IEncryptionServices CreateEncryptionInstance(string packageName, EBuildPipeline buildPipeline)
        {
            // 从配置中获取加密类名
            var encryptionClassName = AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, buildPipeline.ToString());
            // 获取所有实现了IEncryptionServices接口的类型
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            // 查找匹配的加密类
            var classType = encryptionClassTypes.Find(x => x.FullName != null && x.FullName.Equals(encryptionClassName));

            if (classType != null)
            {
                Debug.Log($"[BuildInternal] Use Encryption: {classType}");
                // 创建加密服务实例
                return (IEncryptionServices)Activator.CreateInstance(classType);
            }

            return null;
        }

        /// <summary>
        /// 根据 ResourceModuleDriver 的 encryptionType 获取对应的加密服务
        /// </summary>
        private static IEncryptionServices GetEncryptionFromResourceModuleDriver()
        {
            // 通过名字查找 GameEntry 预制体
            var guids = AssetDatabase.FindAssets("t:Prefab GameEntry");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[BuildInternal] Failed to find GameEntry.prefab");
                return null;
            }

            var gameEntryPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var gameEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameEntryPath);
            if (gameEntryPrefab == null)
            {
                Debug.LogWarning("[BuildInternal] Failed to load GameEntry.prefab");
                return null;
            }

            var resourceModuleDriver = gameEntryPrefab.GetComponentInChildren<ResourceModuleDriver>();
            if (resourceModuleDriver == null)
            {
                Debug.LogWarning("[BuildInternal] ResourceModuleDriver not found in GameEntry.prefab");
                return null;
            }

            var encryptionType = resourceModuleDriver.EncryptionType;
            Debug.Log($"[BuildInternal] Use EncryptionType from ResourceModuleDriver: {encryptionType}");

            return encryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _ => null // EncryptionType.None
            };
        }

        /// <summary>
        /// 获取内置着色器资源包名称
        /// 注意：需要和自动收集的着色器资源包名保持一致
        /// 避免着色器被重复打包到多个AB中
        /// </summary>
        private static string GetBuiltinShaderBundleName(string packageName)
        {
            // 获取唯一包名设置
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            // 创建着色器打包规则结果
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            // 生成着色器资源包名称
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        #endregion

        #region GetBuildTarget

        public static BuildTarget GetBuildTarget(string platform)
            => platform switch
            {
                "Android" => BuildTarget.Android,
                "IOS" => BuildTarget.iOS,
                "Windows" => BuildTarget.StandaloneWindows64,
                "MacOS" => BuildTarget.StandaloneOSX,
                "Linux" => BuildTarget.StandaloneLinux64,
                "WebGL" => BuildTarget.WebGL,
                "Switch" => BuildTarget.Switch,
                "PS4" => BuildTarget.PS4,
                "PS5" => BuildTarget.PS5,
                _ => BuildTarget.NoTarget
            };

        #endregion

        #region Build

        [MenuItem("DGame Tools/Build/AutoBuildWindow", priority = 152)]
        public static void AutoBuildWindow()
        {
            BuildTarget target = BuildTarget.StandaloneWindows;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Windows", packageVersion:GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Windows/";
            BuildImp(BuildTargetGroup.Standalone, target, savePath + "Release_Windows.exe");
            OpenBuildSavePath(savePath);
        }

        private static void OpenBuildSavePath(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"构建目录不存在: {path}");
                return;
            }
            string absolutePath = Path.GetFullPath(path);
            EditorUtility.RevealInFinder(absolutePath);
        }

        [MenuItem("DGame Tools/Build/AutoBuildAndroid", priority = 153)]
        public static void AutoBuildAndroid()
        {
            BuildTarget target = BuildTarget.Android;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Android", packageVersion:GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Android/";
            BuildImp(BuildTargetGroup.Android, target, savePath + $"{GetBuildPackageVersion()}-Android.apk");
            OpenBuildSavePath(savePath);
        }

        [MenuItem("DGame Tools/Build/AutoBuildIOS", priority = 154)]
        public static void AutoBuildIOS()
        {
            BuildTarget target = BuildTarget.iOS;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/IOS", packageVersion:GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/IOS/";
            BuildImp(BuildTargetGroup.iOS, target, savePath + "XCode_Project");
            OpenBuildSavePath(savePath);
        }

        private static void BuildImp(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string locationPathName)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
            AssetDatabase.Refresh();
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = locationPathName,
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build {buildTarget.ToString()} Succeeded: {summary.totalSize / 1024 / 1024}MB");
            }
            else
            {
                Debug.LogError($"Build {buildTarget.ToString()} Failed: {summary.result}");
            }
        }

        #endregion
    }
}