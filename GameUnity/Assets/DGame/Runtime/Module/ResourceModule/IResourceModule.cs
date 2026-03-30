using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace DGame
{
    /// <summary>
    /// 资源管理器接口
    /// </summary>
    public interface IResourceModule
    {
        /// <summary>
        /// 当前资源适用的游戏版本号
        /// </summary>
        string ApplicableGameVersion { get; }

        /// <summary>
        /// 当前内部资源版本号
        /// </summary>
        int InternalResourceVersion { get; }

        /// <summary>
        /// 运行模式
        /// </summary>
        EPlayMode PlayMode { get; set; }

        /// <summary>
        /// 资源加密方式
        /// </summary>
        EncryptionType EncryptionType { get; set; }

        /// <summary>
        /// 边玩边下
        /// </summary>
        bool UpdatableWhilePlaying { get; set; }

        /// <summary>
        /// 同时下载的最大数量
        /// </summary>
        int DownloadingMaxNum { get; set; }

        /// <summary>
        /// 失败重试的最大次数
        /// </summary>
        int FailedTryAgainNum { get; set; }

        /// <summary>
        /// 初始化YooAssets系统
        /// </summary>
        void Initialize();

        /// <summary>
        /// 异步初始化YooAssets资源包操作
        /// </summary>
        /// <param name="packageName">资源包名</param>
        /// <param name="needInitMainFest">是否需要直接初始化资源清单 （单机OtherPackage使用）</param>
        /// <returns></returns>
        UniTask<InitializationOperation> InitPackage(string packageName, bool needInitMainFest = false);

        /// <summary>
        /// 默认资源包名
        /// </summary>
        string DefaultPackageName { get; set; }

        /// <summary>
        /// 异步系统参数 每帧执行消耗的最大时间切片（毫秒）
        /// </summary>
        long Milliseconds  { get; set; }

        /// <summary>
        /// 自动释放资源引用计数为0的资源包
        /// </summary>
        bool AutoUnloadBundleWhenUnused { get; set; }

        /// <summary>
        /// 热更服务器地址URL
        /// </summary>
        string HostServerURL { get; set; }

        /// <summary>
        /// 备用热更服务器地址URL
        /// </summary>
        string FallbackHostServerURL { get; set; }

        /// <summary>
        /// WebGL平台加载资源的方式
        /// </summary>
        LoadResWayWebGL LoadResWayWebGL { get; set; }

        /// <summary>
        /// 资源对象池自动释放可释放对象的时间间隔（秒）
        /// </summary>
        float AssetPoolAutoReleaseInterval { get; set; }

        /// <summary>
        /// 资源对象池容量
        /// </summary>
        int AssetPoolCapacity { get; set; }

        /// <summary>
        /// 资源过期时间（秒）
        /// </summary>
        float AssetExpireTime { get; set; }

        /// <summary>
        /// 资源对象池优先级
        /// </summary>
        int AssetPoolPriority { get; set; }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="asset">需要卸载的资源对象</param>
        void UnloadAsset(object asset);

        /// <summary>
        /// 资源回收（卸载所有引用计数为0的资源对象）
        /// </summary>
        void UnloadUnusedAssets();

        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        void ForceUnloadAllAssets();

        /// <summary>
        /// 强制释放所有未被使用的资源对象
        /// </summary>
        /// <param name="performGCCollect">是否执行垃圾回收GC操作</param>
        void ForceUnloadUnusedAssets(bool performGCCollect);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="packageName">资源包名，为空使用默认资源包</param>
        /// <returns></returns>
        CheckAssetStatus ContainsAsset(string location, string packageName = "");

        /// <summary>
        /// 检查资源地址是否有效
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="packageName">资源包名，为空使用默认资源包</param>
        /// <returns></returns>
        bool CheckLocationValid(string location, string packageName = "");

        #region GetAssetInfo

        /// <summary>
        /// 通过tag获取资源信息列表
        /// </summary>
        /// <param name="resTag">资源标签</param>
        /// <param name="packageName">资源包名，为空使用默认资源包</param>
        /// <returns></returns>
        AssetInfo[] GetAssetInfos(string resTag, string packageName = "");

        /// <summary>
        /// 通过tags获取资源信息列表
        /// </summary>
        /// <param name="tags">资源标签列表</param>
        /// <param name="packageName">资源包名，为空使用默认资源包</param>
        /// <returns></returns>
        AssetInfo[] GetAssetInfos(string[] tags, string packageName = "");

        /// <summary>
        /// 通过资源地址获取资源信息
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="packageName">资源包名，为空使用默认资源包</param>
        /// <returns></returns>
        AssetInfo GetAssetInfo(string location, string packageName = "");

        #endregion

        #region LoadAssetAsync

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载资源的优先级</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集</param>
        /// <param name="userData">用户自定义数据</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        void LoadAssetAsync(string location, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData, string packageName = "");

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="assetType">要加载的资源类型</param>
        /// <param name="priority">加载资源的优先级</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集</param>
        /// <param name="userData">用户自定义数据</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        void LoadAssetAsync(string location, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData, string packageName = "");

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="callback">回调函数</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        UniTaskVoid LoadAssetAsync<T>(string location, Action<T> callback, string packageName = "")
            where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="cancellationToken">取消操作Token</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        UniTask<T> LoadAssetAsync<T>(string location, CancellationToken cancellationToken = default,
            string packageName = "") where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="assetType">要加载的资源类型</param>
        /// <param name="cancellationToken">取消操作Token</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        UniTask<UnityEngine.Object> LoadAssetAsync(string location, Type assetType, CancellationToken cancellationToken = default,
            string packageName = "");

        #endregion

        #region LoadAssetSync

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="packageName">资源包的名称 传使用默认资源包</param>
        /// <typeparam name="T">要加载资源的类型</typeparam>
        /// <returns>资源实例对象</returns>
        T LoadAsset<T>(string location, string packageName = "") where T : UnityEngine.Object;

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="packageName">资源包的名称 传使用默认资源包</param>
        /// <param name="assetType">要加载资源的类型</param>
        /// <returns>资源实例对象</returns>
        UnityEngine.Object LoadAsset(string location, Type assetType, string packageName = "");
        
        #endregion

        #region LoadGameObject

        /// <summary>
        /// 同步加载游戏物体并实例化
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="parent">资源实例父节点</param>
        /// <param name="packageName">资源包的名称 不传使用默认资源包</param>
        /// <returns>资源实例</returns>
        /// <remarks>会实例化资源到场景 无需主动UnloadAsset Destroy时自动UnloadAsset</remarks>
        GameObject LoadGameObject(string location, Transform parent = null, string packageName = "");

        /// <summary>
        /// 异步加载游戏物体并实例化
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="parent">资源实例父节点</param>
        /// <param name="cancellationToken">取消操作Token</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns>资源实例</returns>
        /// <remarks>会实例化资源到场景 无需主动UnloadAsset Destroy时自动UnloadAsset</remarks>
        UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null,
            CancellationToken cancellationToken = default, string packageName = "");

        #endregion

        #region LoadAssetHandle

        /// <summary>
        /// 获取同步加载的资源操作句柄
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="packageName">资源包名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源操作句柄</returns>
        AssetHandle LoadAssetSyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object;

        /// <summary>
        /// 获取同步加载的资源操作句柄
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetType">资源类型</param>
        /// <returns>资源操作句柄</returns>
        AssetHandle LoadAssetSyncHandle(string location, Type assetType, string packageName = "");

        /// <summary>
        /// 获取异步加载的资源操作句柄
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="packageName">资源包名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源操作句柄</returns>
        AssetHandle LoadAssetAsyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object;

        /// <summary>
        /// 获取异步加载的资源操作句柄
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetType">资源类型</param>
        /// <returns>资源操作句柄</returns>
        AssetHandle LoadAssetAsyncHandle(string location, Type assetType, string packageName = "");

        #endregion

        /// <summary>
        /// 清理资源包中未使用的缓存文件
        /// </summary>
        /// <param name="clearMode">文件清理方式</param>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        ClearCacheFilesOperation ClearCacheFilesAsync(EFileClearMode clearMode = EFileClearMode.ClearUnusedBundleFiles,
            string customPackageName = "");

        /// <summary>
        /// 清理所有AB文件
        /// </summary>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        void ClearAllBundleFiles(string customPackageName = "");

        /// <summary>
        /// 资源下载器 用于下载当前资源版本所有的资源包文件
        /// </summary>
        ResourceDownloaderOperation Downloader { get; set; }

        /// <summary>
        /// 创建资源下载器，用于下载当前资源版本所有的资源包文件
        /// </summary>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        ResourceDownloaderOperation CreateResourceDownloader(string customPackageName = "");

        /// <summary>
        /// 当前最新的资源包版本
        /// </summary>
        string PackageVersion { get; set; }

        /// <summary>
        /// 获取当前资源包版本
        /// </summary>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        string GetPackageVersion(string customPackageName = "");

        /// <summary>
        /// 异步请求资源清单的版本信息
        /// </summary>
        /// <param name="appendTimeTicks">请求URL是否需要带时间戳</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        RequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks = false, int timeout = 60,
            string customPackageName = "");

        /// <summary>
        /// 异步请求更新资源清单文件
        /// </summary>
        /// <param name="packageVersion">更新的包裹版本</param>
        /// <param name="timeout">超时时间（默认值：60秒）</param>
        /// <param name="customPackageName">指定资源包的名称。不传使用默认资源包</param>
        UpdatePackageManifestOperation UpdatePackageManifestAsync(string packageVersion, int timeout = 60,
            string customPackageName = "");

        /// <summary>
        /// 设置服务器URL
        /// </summary>
        /// <param name="defaultRemoteServerURL">默认远端资源地址</param>
        /// <param name="fallbackHostServerURL">备用远端资源地址</param>
        void SetRemoteServerURL(string defaultRemoteServerURL, string fallbackHostServerURL);

        /// <summary>
        /// 低内存行为
        /// </summary>
        void OnLowMemory();

        /// <summary>
        /// 低内存回调保护 设置低内存时强制卸载未使用的资源回调
        /// </summary>
        /// <param name="action">低内存行为，true进行GC</param>
        void SetForceUnloadUnusedAssetsAction(Action<bool> action);
    }


    /// <summary>
    /// WebGL平台下加载资源的方式
    /// </summary>
    public enum LoadResWayWebGL
    {
        /// <summary>
        /// 访问远程资源
        /// </summary>
        [InspectorName("访问远程资源")]
        Remote = 0,

        /// <summary>
        /// 跳过远程下载资源直接访问StreamingAssets
        /// </summary>
        [InspectorName("访问StreamingAssets")]
        StreamingAssets = 1,
    }
}