using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// AssetBundle 包版本生成方式
    /// </summary>
    public enum PackageVersionMode
    {
        /// <summary>
        /// 自动使用当前时间生成唯一版本号
        /// </summary>
        [InspectorName("自动时间戳")]
        AutoTimestamp = 0,

        /// <summary>
        /// 手动指定版本号
        /// </summary>
        [InspectorName("手动输入")]
        Manual = 1,
    }

    /// <summary>
    /// 强制更新类型
    /// </summary>
    public enum UpdateStyle
    {
        /// <summary>
        /// 强制更新(不更新无法进入游戏)
        /// </summary>
        [InspectorName("强制更新(不更新无法进入游戏)")]
        Force = 0,

        /// <summary>
        /// 非强制(不更新可以进入游戏)
        /// </summary>
        [InspectorName("非强制(不更新可以进入游戏)")]
        Optional = 1,
    }

    /// <summary>
    /// 是否有提示更新
    /// </summary>
    public enum UpdateNotice
    {
        /// <summary>
        /// 更新存在提示
        /// </summary>
        [InspectorName("更新存在提示")]
        Notice = 0,

        /// <summary>
        /// 更新非提示
        /// </summary>
        [InspectorName("更新非提示")]
        NoNotice = 1,
    }

    [CreateAssetMenu(fileName = "UpdateSettings", menuName = "DGame/UpdateSettings")]
    public class UpdateSettings : ScriptableObject
    {
        [SerializeField]
        private string projectName = "Demo";

        public bool Enable
        {
             get
             {
#if ENABLE_HYBRIDCLR
                return true;
#else
                return false;
#endif
             }
        }

        [Header("自动同步 [HybridCLRGlobalSettings]")]
        public List<string> HotUpdateAssemblies = new List<string>() { "GameProto.dll", "GameLogic.dll" };
        public List<string> AOTMetaAssemblies = new List<string>() { "mscorlib.dll", "System.dll", "System.Core.dll", "DGame.Runtime.dll" ,"UniTask.dll", "YooAsset.dll" };

        /// <summary>
        /// 主热更DLL
        /// </summary>
        public string LogicMainDllName = "GameLogic.dll";

        /// <summary>
        /// 程序集文本资产打包Asset后缀名
        /// </summary>
        public string AssemblyTextAssetExtension = ".bytes";

        /// <summary>
        /// 程序集文本AB资产资源目录
        /// </summary>
        public string AssemblyTextAssetPath = "BundleAssets/DLL";

        [Header("更新设置")]
        public UpdateStyle UpdateStyle = UpdateStyle.Force;

        public UpdateNotice UpdateNotice = UpdateNotice.Notice;

        [SerializeField]
        private string m_resDownloadPath = "https://127.0.0.1:8081";

        [SerializeField]
        private string m_fallbackResDownloadPath = "https://127.0.0.1:8082";

        [SerializeField]
        private LoadResWayWebGL m_loadResWayWebGL = LoadResWayWebGL.Remote;

        /// <summary>
        /// WebGL平台加载本地资源/加载远程资源
        /// </summary>
        public LoadResWayWebGL GetLoadResWayWebGL() => m_loadResWayWebGL;

        [SerializeField]
        private bool m_isAutoAssetCopyToBuildAddress = false;

        /// <summary>
        /// 是否自动将打包资源复制到打包后的StreamingAssets地址和buildAddress地址
        /// </summary>
        public bool IsAutoAssetCopyToBuildAddress() => m_isAutoAssetCopyToBuildAddress;

        [SerializeField]
        private string m_buildAddress = "../../Builds/Unity_Data/StreamingAssets";

        [SerializeField]
        private bool m_replaceAssetPathWithAddress = false;

        [SerializeField]
        private bool m_forceGenerateAtlas = false;

        [SerializeField]
        private PackageVersionMode m_packageVersionMode = PackageVersionMode.AutoTimestamp;

        [SerializeField]
        private string m_manualBuildVersion = "1.0";

        /// <summary>
        /// 打包程序资源缓存地址
        /// </summary>
        public string GetBuildAddress() => m_buildAddress;

        /// <summary>
        /// 获取 AB 包版本生成模式
        /// </summary>
        public PackageVersionMode GetPackageVersionMode() => m_packageVersionMode;

        /// <summary>
        /// 获取手动指定的 AB 包版本号
        /// </summary>
        public string GetManualBuildVersion() => m_manualBuildVersion;

        /// <summary>
        /// 获取本次打包使用的资源包版本号
        /// </summary>
        public string GetBuildPackageVersion()
        {
            if (m_packageVersionMode == PackageVersionMode.Manual)
            {
                string manualVersion = m_manualBuildVersion?.Trim();
                if (!string.IsNullOrEmpty(manualVersion))
                {
                    return manualVersion;
                }
            }

            return GetAutoBuildPackageVersion();
        }

        /// <summary>
        /// 是否使用可寻址资源代替资源路径
        /// <remarks>说明：开启此项可以节省运行时清单占用的内存！</remarks>
        /// </summary>
        public bool GetReplaceAssetPathWithAddress()
                => m_replaceAssetPathWithAddress;

        /// <summary>
        /// 是否打包的时候强制重新生成图集
        /// </summary>
        public bool ForceGenerateAtlas => m_forceGenerateAtlas;

        /// <summary>
        /// 获取资源服务器地址。https://127.0.0.1:8081/Demo/Android
        /// </summary>
        public string GetResDownloadPath() =>
                Path.Combine(m_resDownloadPath, projectName, GetPlatformName()).Replace("\\", "/");

        /// <summary>
        /// 获取资源服务备用地址 https://127.0.0.1:8081/Demo/Android
        /// </summary>
        public string GetFallbackResDownloadPath()
                => Path.Combine(m_fallbackResDownloadPath, projectName, GetPlatformName()).Replace("\\", "/");

        [SerializeField]
        public string packageName = "DefaultPackage";

        /// <summary>
        /// 资源包名
        /// </summary>
        public string PackageName { get => packageName; set => packageName = value; }

        public string updateUIDefineConfigPath = "Config/UpdateUIDefine";

        /// <summary>
        /// 热更新流程文本配置路径
        /// </summary>
        public string UpdateUIDefineConfigPath => updateUIDefineConfigPath;

        [SerializeField] private bool m_enableAddressable = true;

        /// <summary>
        /// AB资源是否支持可寻址
        /// </summary>
        public bool EnableAddressable => m_enableAddressable;

        /// <summary>
        /// 自动生成构建包版本号
        /// 格式：yyyy-MM-dd-分钟段（每10分钟一个段）
        /// </summary>
        private static string GetAutoBuildPackageVersion()
        {
            int totalMinutes = DateTime.Now.Hour * 6 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 获取当前平台名称
        /// </summary>
        /// <returns></returns>
        public static string GetPlatformName()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "IOS";
#elif UNITY_WEBGL
            return "WebGL";
#else
            switch (Application.platform)
            {
                    case RuntimePlatform.WindowsEditor:
                            return "Windows64";
                    case RuntimePlatform.WindowsPlayer:
                            return "Windows64";
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                            return "MacOS";
                    case RuntimePlatform.IPhonePlayer:
                            return "IOS";
                    case RuntimePlatform.Android:
                            return "Android";
                    case RuntimePlatform.WebGLPlayer:
                            return "WebGL";
                    case RuntimePlatform.PS5:
                            return "PS5";
                    default:
                            throw new NotSupportedException($"没有支持平台: '{Application.platform.ToString()}'");
            }
#endif
        }
    }
}