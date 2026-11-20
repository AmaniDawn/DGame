using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

#if ENABLE_HYBRIDCLR
using HybridCLR.Commands;
#endif

namespace DGame
{
    public static class ReleaseTools
    {
        #region CommandLine Helper

        private static string GetCommandLineArg(string argName)
        {
            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(argName) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                // 支持 -arg=value 格式
                if (args[i].StartsWith(argName + "="))
                {
                    string value = args[i].Substring(argName.Length + 1);
                    return value;
                }
            }

            return null;
        }

        #endregion

        #region Build AssetBundle

        [MenuItem("DGame Tools/Build/一键打包AB _F8", priority = 151)]
        public static void BuildCurrentPlatformAB()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildPipelineConfig config = CreateBuildConfig(target, GetBuildPackageVersion(), "Builds");
            RunBuildOrThrow(config, true, false);
        }

        /// <summary>
        /// 生成构建包版本号
        /// 格式：yyyy-MM-dd-分钟段（每10分钟一个段）
        /// 例如：1997-01-01-91 表示1997年1月1日的第91个10分钟段
        /// </summary>
        /// <returns></returns>
        private static string GetBuildPackageVersion()
        {
            if (Settings.UpdateSettings != null)
            {
                return Settings.UpdateSettings.GetBuildPackageVersion();
            }

            return GetAutoBuildPackageVersionFallback();
        }

        private static string GetAutoBuildPackageVersionFallback()
        {
            // 计算当天从0点开始的总分钟数，然后除以10得到段数
            int totalMinutes = DateTime.Now.Hour * 6 + DateTime.Now.Minute / 10;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 创建加密服务实例
        /// 用于AssetBundle文件加密
        /// </summary>
        private static IEncryptionServices CreateEncryptionInstance(string packageName, EBuildPipeline buildPipeline)
        {
            // 从配置中获取加密类名
            var encryptionClassName =
                AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, buildPipeline.ToString());
            // 获取所有实现了IEncryptionServices接口的类型
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            // 查找匹配的加密类
            var classType =
                encryptionClassTypes.Find(x => x.FullName != null && x.FullName.Equals(encryptionClassName));

            if (classType != null)
            {
                Debug.Log($"[BuildInternal] Use Encryption: {classType}");
                // 创建加密服务实例
                return (IEncryptionServices)Activator.CreateInstance(classType);
            }

            return null;
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

        #region Build Pipeline Window

        /// <summary>
        /// 为指定平台编译并复制热更新 DLL。
        /// </summary>
        public static bool BuildHotFixDll(BuildTarget buildTarget)
        {
            if (!SwitchActiveBuildTarget(buildTarget))
            {
                return false;
            }

            bool success = BuildDllCommand.TryBuildAndCopyDlls();
            if (success)
            {
                AssetDatabase.Refresh();
            }
            return success;
        }

        /// <summary>
        /// 使用窗口参数构建 AssetBundle。
        /// </summary>
        public static bool BuildAssetBundles(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, true, false);
        }

        /// <summary>
        /// 使用窗口参数仅构建 Player。
        /// </summary>
        public static bool BuildPlayer(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, false, true);
        }

        /// <summary>
        /// 使用窗口参数依次构建 AssetBundle 和 Player。
        /// </summary>
        public static bool BuildAll(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, true, true);
        }

        private static bool ExecuteBuild(BuildPipelineConfig config, bool buildAssetBundle, bool buildPlayer,
            bool runHybridClrGenerateAll = false)
        {
            if (!ValidateBuildConfig(config, buildAssetBundle, buildPlayer))
            {
                return false;
            }

            if (!SwitchActiveBuildTarget(config.BuildTarget))
            {
                return false;
            }

            if (runHybridClrGenerateAll && buildAssetBundle && buildPlayer && config.BuildHotFixDll
                && !PrepareHybridClrForFullBuild())
            {
                return false;
            }

            if (buildAssetBundle)
            {
                if (config.BuildHotFixDll)
                {
                    if (!BuildDllCommand.TryBuildAndCopyDlls())
                    {
                        return false;
                    }
                }

                AssetDatabase.Refresh();
                YooAsset.Editor.BuildResult buildResult = BuildInternal(config);

                if (!buildResult.Success)
                {
                    return false;
                }

                if (config.MinimalPackage
                    && !ProcessMinimalPackage(config, buildResult.OutputPackageDirectory))
                {
                    return false;
                }

                AssetDatabase.Refresh();

                if (config.CopyToBuildAddress && !CopyStreamingAssetsFiles(config))
                {
                    return false;
                }
            }

            if (buildPlayer)
            {
                string playerOutputPath = GetAbsoluteProjectPath(config.PlayerOutputPath);

                if (!BuildPlayerWithConfig(config.BuildTarget, playerOutputPath))
                {
                    return false;
                }
            }

            if (config.OpenOutputDirectory)
            {
                string outputPath = buildPlayer
                    ? GetPlayerOutputDirectory(GetAbsoluteProjectPath(config.PlayerOutputPath), config.BuildTarget)
                    : GetAbsoluteProjectPath(config.AssetBundleOutputRoot);
                OpenBuildSavePath(outputPath);
            }

            return true;
        }

        private static bool PrepareHybridClrForFullBuild()
        {
#if ENABLE_HYBRIDCLR
            try
            {
                Debug.Log("[ReleaseTools] 完整构建前执行 HybridCLR GenerateAll");
                PrebuildCommand.GenerateAll();
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ReleaseTools] HybridCLR GenerateAll 失败: {exception.Message}");
                Debug.LogException(exception);
                return false;
            }
#else
            return true;
#endif
        }

        private static YooAsset.Editor.BuildResult BuildInternal(BuildPipelineConfig config)
        {
            if (config.ForceGenerateAtlas)
            {
                Debug.Log("[BuildInternal] 强制重新生成所有图集");
                EditorSpriteSaveInfo.RefreshAllForBuild();
            }

            Debug.Log($"[BuildInternal] 开始构建AssetBundle: {config.BuildTarget}");

            IBuildPipeline pipeline;
            BuildParameters buildParameters;

            if (config.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                var builtinBuildParameters = new BuiltinBuildParameters
                {
                    CompressOption = config.CompressOption,
                };
                pipeline = new BuiltinBuildPipeline();
                buildParameters = builtinBuildParameters;
            }
            else
            {
                var scriptableBuildParameters = new ScriptableBuildParameters
                {
                    CompressOption = config.CompressOption,
                    BuiltinShadersBundleName = GetBuiltinShaderBundleName(config.PackageName),
                    ReplaceAssetPathWithAddress = config.ReplaceAssetPathWithAddress,
                };
                pipeline = new ScriptableBuildPipeline();
                buildParameters = scriptableBuildParameters;
            }

            buildParameters.BuildOutputRoot = GetAbsoluteProjectPath(config.AssetBundleOutputRoot);
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = config.BuildPipeline.ToString();
            buildParameters.BuildTarget = config.BuildTarget;
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
            buildParameters.PackageName = config.PackageName.Trim();
            buildParameters.PackageVersion = config.PackageVersion.Trim();
            buildParameters.VerifyBuildingResult = config.VerifyBuildingResult;
            buildParameters.EnableSharePackRule = config.EnableSharePackRule;
            buildParameters.FileNameStyle = config.FileNameStyle;
            buildParameters.BuildinFileCopyOption = config.BuildinFileCopyOption;
            buildParameters.BuildinFileCopyParams = string.Empty;
            buildParameters.EncryptionServices = GetEncryptionFromType(config.EncryptionType);
            buildParameters.ClearBuildCacheFiles = config.ClearBuildCacheFiles;
            buildParameters.UseAssetDependencyDB = config.UseAssetDependencyDB;

            YooAsset.Editor.BuildResult buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"[BuildInternal] AssetBundle资源构建成功: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"[BuildInternal] AssetBundle资源构建失败: {buildResult.ErrorInfo}");
            }

            return buildResult;
        }

        private static bool ProcessMinimalPackage(BuildPipelineConfig config, string outputPackageDirectory)
        {
            string reportFileName = YooAssetSettingsData.GetBuildReportFileName(config.PackageName,
                config.PackageVersion);
            string reportPath = Path.Combine(outputPackageDirectory, reportFileName);
            if (!File.Exists(reportPath))
            {
                Debug.LogError($"[最小包] 未找到构建报告: {reportPath}");
                return false;
            }

            YooAsset.Editor.BuildReport buildReport;
            try
            {
                buildReport = YooAsset.Editor.BuildReport.Deserialize(File.ReadAllText(reportPath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[最小包] 读取构建报告失败: {exception.Message}");
                return false;
            }

            string[] retainTags = ParseRetainTags(config.RetainTags);
            var retainFileNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (retainTags.Length > 0)
            {
                foreach (ReportBundleInfo bundleInfo in buildReport.BundleInfos)
                {
                    if (bundleInfo.Tags != null && bundleInfo.Tags.Any(retainTags.Contains))
                    {
                        retainFileNames.Add(bundleInfo.FileName);
                    }
                }
            }

            string streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            string packageStreamingAssetsRoot = Path.Combine(streamingAssetsRoot, config.PackageName);
            if (!Directory.Exists(packageStreamingAssetsRoot))
            {
                Debug.LogError($"[最小包] 资源包 StreamingAssets 目录不存在: {packageStreamingAssetsRoot}");
                return false;
            }

            int deletedCount = 0;
            int retainedCount = 0;
            foreach (string bundleFile in Directory.GetFiles(packageStreamingAssetsRoot, "*.bundle",
                         SearchOption.AllDirectories))
            {
                if (retainFileNames.Contains(Path.GetFileName(bundleFile)))
                {
                    retainedCount++;
                    continue;
                }

                File.Delete(bundleFile);
                deletedCount++;
            }

            CleanEmptyDirectories(packageStreamingAssetsRoot);
            Debug.Log($"[最小包] 处理完成，删除 {deletedCount} 个 Bundle，保留 {retainedCount} 个 Bundle");
            return true;
        }

        private static string[] ParseRetainTags(string retainTags)
        {
            if (string.IsNullOrWhiteSpace(retainTags))
            {
                return Array.Empty<string>();
            }

            return retainTags.Split(',', '，')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToArray();
        }

        private static void CleanEmptyDirectories(string rootPath)
        {
            foreach (string directory in Directory.GetDirectories(rootPath))
            {
                CleanEmptyDirectories(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        private static bool ValidateBuildConfig(BuildPipelineConfig config, bool buildAssetBundle, bool buildPlayer)
        {
            if (config == null)
            {
                Debug.LogError("[ReleaseTools] 构建配置不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.PackageVersion))
            {
                Debug.LogError("[ReleaseTools] AssetBundle 版本号不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.PackageName))
            {
                Debug.LogError("[ReleaseTools] YooAsset 资源包名不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.AssetBundleOutputRoot))
            {
                Debug.LogError("[ReleaseTools] AssetBundle 输出目录不能为空");
                return false;
            }

            if (buildPlayer && string.IsNullOrWhiteSpace(config.PlayerOutputPath))
            {
                Debug.LogError("[ReleaseTools] Player 输出路径不能为空");
                return false;
            }

            if (buildAssetBundle && config.CopyToBuildAddress && string.IsNullOrWhiteSpace(config.BuildAddress))
            {
                Debug.LogError("[ReleaseTools] 启用内置资源同步时 BuildAddress 不能为空");
                return false;
            }

            return true;
        }

        private static bool SwitchActiveBuildTarget(BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget == buildTarget)
            {
                return true;
            }

            BuildTargetGroup buildTargetGroup = BuildPipelineConfig.GetBuildTargetGroup(buildTarget);
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
            {
                return true;
            }

            Debug.LogError($"[ReleaseTools] 切换构建平台失败: {buildTarget}");
            return false;
        }

        private static string GetAbsoluteProjectPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path).Replace('\\', '/');
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, path)).Replace('\\', '/');
        }

        private static string GetPlayerOutputDirectory(string playerOutputPath, BuildTarget buildTarget)
        {
            return buildTarget is BuildTarget.iOS or BuildTarget.WebGL
                ? playerOutputPath
                : Path.GetDirectoryName(playerOutputPath);
        }

        private static IEncryptionServices GetEncryptionFromType(EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _ => null,
            };
        }

        private static bool CopyStreamingAssetsFiles(BuildPipelineConfig config)
        {
            string streamingAssetsPath = Path.GetFullPath(Application.streamingAssetsPath);
            string targetPath = config.BuildAddress.Trim();

            if (!Directory.Exists(streamingAssetsPath))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] StreamingAssets 目录不存在: {streamingAssetsPath}");
                return false;
            }

            if (!Path.IsPathRooted(targetPath))
            {
                targetPath = Path.Combine(streamingAssetsPath, targetPath);
            }

            targetPath = Path.GetFullPath(targetPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] protectedProjectDirectories =
            {
                Application.dataPath,
                Path.Combine(projectRoot, "Packages"),
                Path.Combine(projectRoot, "ProjectSettings"),
                Path.Combine(projectRoot, "Library"),
            };
            if (IsSameOrSubPath(streamingAssetsPath, targetPath)
                || IsSameOrSubPath(targetPath, streamingAssetsPath)
                || IsSameOrSubPath(targetPath, projectRoot))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] BuildAddress 与工程或 StreamingAssets 路径存在危险包含关系: {targetPath}");
                return false;
            }

            if (protectedProjectDirectories.Any(directory => IsSameOrSubPath(directory, targetPath)))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] BuildAddress 位于受保护的工程目录中: {targetPath}");
                return false;
            }

            Directory.CreateDirectory(targetPath);

            foreach (string filePath in Directory.GetFiles(targetPath))
            {
                File.Delete(filePath);
            }

            foreach (string directory in Directory.GetDirectories(targetPath))
            {
                Directory.Delete(directory, true);
            }

            foreach (string filePath in Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = filePath.Substring(streamingAssetsPath.Length + 1);
                string destinationPath = Path.Combine(targetPath, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(filePath, destinationPath, true);
            }

            Debug.Log($"[CopyStreamingAssetsFiles] 复制文件成功: {targetPath}");
            return true;
        }

        private static bool IsSameOrSubPath(string parentPath, string childPath)
        {
            string normalizedParent = Path.GetFullPath(parentPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string normalizedChild = Path.GetFullPath(childPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return normalizedChild.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool BuildPlayerWithConfig(BuildTarget buildTarget, string locationPathName)
        {
            string outputDirectory = Path.GetDirectoryName(locationPathName);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildTargetGroup buildTargetGroup = BuildPipelineConfig.GetBuildTargetGroup(buildTarget);
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[ReleaseTools] Build Settings 中没有启用的场景");
                return false;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = BuildOptions.None,
            };
            BuildSummary summary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build {buildTarget} Succeeded: {summary.totalSize / 1024 / 1024}MB");
                return true;
            }

            Debug.LogError($"Build {buildTarget} Failed: {summary.result}");
            return false;
        }

        private static BuildPipelineConfig CreateBuildConfig(BuildTarget buildTarget, string packageVersion,
            string assetBundleOutputRoot, string playerOutputPath = null)
        {
            BuildPipelineConfig config = BuildPipelineConfig.CreateDefault(buildTarget);
            config.PackageVersion = packageVersion;
            config.AssetBundleOutputRoot = assetBundleOutputRoot;
            config.PlayerOutputPath = string.IsNullOrWhiteSpace(playerOutputPath)
                ? BuildPipelineConfig.GetDefaultPlayerOutputPath(buildTarget, packageVersion)
                : playerOutputPath;
            config.OpenOutputDirectory = !Application.isBatchMode;
            return config;
        }

        private static void RunBuildOrThrow(BuildPipelineConfig config, bool buildAssetBundle, bool buildPlayer,
            bool runHybridClrGenerateAll = false)
        {
            if (!ExecuteBuild(config, buildAssetBundle, buildPlayer, runHybridClrGenerateAll))
            {
                throw new InvalidOperationException(
                    $"构建失败：平台={config.BuildTarget}，版本={config.PackageVersion}");
            }
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
            string version = GetBuildPackageVersion();
            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.StandaloneWindows64, version,
                "Bundles/Windows", "Build/Windows/Release_Windows.exe");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
        }

        [MenuItem("DGame Tools/Build/AutoBuildAndroid", priority = 153)]
        public static void AutoBuildAndroid()
        {
            string version = GetBuildPackageVersion();
            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.Android, version, "Bundles/Android",
                $"Build/Android/{version}-Android.apk");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
        }

        [MenuItem("DGame Tools/Build/AutoBuildIOS", priority = 154)]
        public static void AutoBuildIOS()
        {
            string version = GetBuildPackageVersion();
            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.iOS, version, "Bundles/IOS",
                "Build/IOS/XCode_Project");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
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

        #endregion

        #region Build AssetBundle by Command

        public static void BuildWindowWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("[BuildWindowWithVersion] Please specify version using -version argument");
            }

            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.StandaloneWindows64, version,
                "Bundles/Windows", "Build/Windows/Release_Windows.exe");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
        }

        public static void BuildAndroidWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("[BuildAndroidWithVersion] Please specify version using -version argument");
            }

            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.Android, version, "Bundles/Android",
                $"Build/Android/{version}-Android.apk");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
        }

        public static void BuildIOSWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("[BuildIOSWithVersion] Please specify version using -version argument");
            }

            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.iOS, version, "Bundles/IOS",
                "Build/IOS/XCode_Project");
            config.CopyToBuildAddress = false;
            RunBuildOrThrow(config, true, true);
        }

        /// <summary>
        /// 打包安卓AB（自动版本号）
        /// </summary>
        public static void BuildAndroidAB()
        {
            string version = GetBuildPackageVersion();
            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.Android, version, "Bundles/Android");
            RunBuildOrThrow(config, true, false);
            Debug.Log("[BuildAndroidAB] Android AssetBundle build completed with auto version: " + version);
        }

        /// <summary>
        /// 打包安卓AB（手动版本号，通过命令行参数 -version 传入）
        /// </summary>
        public static void BuildAndroidABWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("[BuildAndroidABWithVersion] Please specify version using -version argument");
            }

            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.Android, version, "Bundles/Android");
            RunBuildOrThrow(config, true, false);
            Debug.Log($"[BuildAndroidABWithVersion] Android AssetBundle build completed with manual version: {version}");
        }

        /// <summary>
        /// 打包Windows AB（自动版本号）
        /// </summary>
        public static void BuildWindowsAB()
        {
            string version = GetBuildPackageVersion();
            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.StandaloneWindows64, version,
                "Bundles/Windows");
            RunBuildOrThrow(config, true, false);
            Debug.Log($"[BuildWindowsAB] Windows AssetBundle build completed with auto version: {version}");
        }

        /// <summary>
        /// 打包Windows AB（手动版本号，通过命令行参数 -version 传入）
        /// </summary>
        public static void BuildWindowsABWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("[BuildWindowsABWithVersion] Please specify version using -version argument");
            }

            BuildPipelineConfig config = CreateBuildConfig(BuildTarget.StandaloneWindows64, version,
                "Bundles/Windows");
            RunBuildOrThrow(config, true, false);
            Debug.Log($"[BuildWindowsABWithVersion] Windows AssetBundle build completed with manual version: {version}");
        }

        #endregion
    }
}
