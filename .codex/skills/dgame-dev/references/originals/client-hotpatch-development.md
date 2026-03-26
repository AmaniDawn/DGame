# DGame 热更资源包管理

当需求涉及热更资源包、资源版本更新、下载器、缓存清理、YooAsset 使用方式，或需要查询 `GameModule.ResourceModule` 相关 API 时，先阅读本文件。

本文档记录当前仓库中热更资源包管理的实际流程和资源模块 API。处理热更资源包时，优先沿用现有 `Procedure + GameModule.ResourceModule + YooAsset` 体系，不要另外造一套下载、版本或缓存管理链。

## 目录导航

- [DGame 热更资源包管理](#dgame-热更资源包管理)
  - [目录导航](#目录导航)
  - [核心概念](#核心概念)
  - [热更流程](#热更流程)
  - [YooAsset 相关资源包 API 速查（`GameModule.ResourceModule`）](#yooasset-相关资源包-api-速查gamemoduleresourcemodule)
  - [完整代码示例](#完整代码示例)
  - [使用原则](#使用原则)

## 核心概念

当前项目的热更资源包管理核心是：

- 主入口资源模块：`GameModule.ResourceModule`
- 底层实现：`DGame.Runtime/Module/ResourceModule`
- 资源系统：YooAsset
- 启动总流程：`DGame.AOT/Procedure/`

可以先按下面几条理解：

| 概念 | 当前项目含义 |
| --- | --- |
| Package | YooAsset 资源包，默认包名由 `DefaultPackageName` 决定，默认是 `DefaultPackage` |
| Package Version | 当前最新资源包版本号，流程里会先请求远端版本，再更新清单 |
| Manifest | 资源清单，更新流程里通过 `UpdatePackageManifestAsync` 更新 |
| Downloader | 资源下载器，通过 `CreateResourceDownloader()` 创建 |
| Clear Cache | 清理未使用或全部缓存包文件 |
| PlayMode | 资源运行模式，影响初始化与更新逻辑 |

当前 `IResourceModule` 已确认支持多种资源模式：

- `EditorSimulateMode`
- `OfflinePlayMode`
- `HostPlayMode`
- `WebPlayMode`

## 热更流程

当前热更资源包相关流程由 AOT Procedure 串起来，主链路如下：

1. `InitPackageProcedure`
2. `InitResourceProcedure`
3. `CreateDownloaderProcedure`
4. `DownloadFileProcedure`
5. `DownloadOverProcedure`
6. `ClearCacheProcedure`
7. `PreloadProcedure`

可先按下面方式理解每一步：

| Procedure | 当前职责 |
| --- | --- |
| `InitPackageProcedure` | 初始化 YooAsset Package，根据资源模式进入后续流程 |
| `InitResourceProcedure` | 请求资源版本、更新清单，确定是否需要走下载 |
| `CreateDownloaderProcedure` | 创建下载器，统计待下载文件数和总大小 |
| `DownloadFileProcedure` | 执行下载并更新下载进度、速度与剩余时间 |
| `DownloadOverProcedure` | 下载完成后的收尾逻辑 |
| `ClearCacheProcedure` | 清理未使用缓存文件 |
| `PreloadProcedure` | 进入正式游戏前的预加载 |

这里的关键点是：

- 热更资源流程属于 AOT Procedure 管理，不属于 HotFix 业务层自由发挥
- 业务代码如果只是“使用资源”，直接走 `GameModule.ResourceModule`
- 若需求是“修改版本更新、下载、缓存策略”，应改对应的 AOT Procedure

## YooAsset 相关资源包 API 速查（`GameModule.ResourceModule`）

当前项目里最常用的资源包管理 API 可先按下面表查：

| API | 作用 |
| --- | --- |
| `Initialize()` | 初始化 YooAsset 系统和默认资源包 |
| `InitPackage(packageName, needInitMainFest)` | 初始化指定资源包 |
| `RequestPackageVersionAsync(...)` | 请求远端资源包版本 |
| `UpdatePackageManifestAsync(packageVersion, ...)` | 根据版本更新资源清单 |
| `CreateResourceDownloader(...)` | 创建资源下载器 |
| `ClearCacheFilesAsync(...)` | 清理未使用缓存文件 |
| `ClearAllBundleFiles(...)` | 清理全部 Bundle 文件 |
| `GetPackageVersion(...)` | 获取当前资源包版本 |
| `LoadAsset<T>(location, packageName)` | 同步加载资源 |
| `LoadAssetAsync<T>(location, cancellationToken, packageName)` | 异步加载资源 |
| `LoadGameObject(location, parent, packageName)` | 同步加载并实例化 GameObject |
| `LoadGameObjectAsync(location, parent, cancellationToken, packageName)` | 异步加载并实例化 GameObject |
| `UnloadAsset(asset)` | 卸载资源对象 |
| `UnloadUnusedAssets()` | 卸载引用计数为 0 的资源 |
| `ForceUnloadAllAssets()` | 强制卸载所有资源 |

当前资源下载相关字段也很关键：

| 成员 | 作用 |
| --- | --- |
| `DefaultPackageName` | 默认资源包名 |
| `PackageVersion` | 当前最新资源版本号 |
| `Downloader` | 当前资源下载器实例 |
| `DownloadingMaxNum` | 最大并发下载数 |
| `FailedTryAgainNum` | 下载失败重试次数 |
| `HostServerURL` | 主热更服务器地址 |
| `FallbackHostServerURL` | 备用热更服务器地址 |
| `UpdatableWhilePlaying` | 是否支持边玩边下 |

## 完整代码示例

### 1. 初始化资源包并更新清单

```csharp
private async UniTask<bool> InitAndUpdatePackageAsync()
{
    var resource = GameModule.ResourceModule;

    // 初始化默认资源包
    var initOp = await resource.InitPackage(resource.DefaultPackageName);
    if (initOp == null || initOp.Status != EOperationStatus.Succeed)
    {
        DLogger.Error("InitPackage failed.");
        return false;
    }

    // 请求远端版本
    var versionOp = resource.RequestPackageVersionAsync();
    await versionOp.ToUniTask();
    if (versionOp.Status != EOperationStatus.Succeed)
    {
        DLogger.Error($"RequestPackageVersionAsync failed : {versionOp.Error}");
        return false;
    }

    resource.PackageVersion = versionOp.PackageVersion;

    // 更新清单
    var manifestOp = resource.UpdatePackageManifestAsync(resource.PackageVersion);
    await manifestOp.ToUniTask();
    if (manifestOp.Status != EOperationStatus.Succeed)
    {
        DLogger.Error($"UpdatePackageManifestAsync failed : {manifestOp.Error}");
        return false;
    }

    return true;
}
```

### 2. 创建下载器并执行下载

```csharp
private async UniTask<bool> DownloadPatchAsync()
{
    var resource = GameModule.ResourceModule;

    var downloader = resource.CreateResourceDownloader();
    if (downloader == null)
    {
        DLogger.Error("CreateResourceDownloader failed.");
        return false;
    }

    if (downloader.TotalDownloadCount == 0)
    {
        DLogger.Info("No patch files need download.");
        return true;
    }

    downloader.DownloadErrorCallback = data =>
    {
        DLogger.Error($"Download failed : {data.FileName}");
    };

    downloader.DownloadUpdateCallback = data =>
    {
        DLogger.Info($"Patch progress : {data.CurrentDownloadCount}/{data.TotalDownloadCount}");
    };

    downloader.BeginDownload();
    await downloader;

    return downloader.Status == EOperationStatus.Succeed;
}
```

### 3. 清理缓存文件

```csharp
private async UniTask ClearUnusedCacheAsync()
{
    var operation = GameModule.ResourceModule.ClearCacheFilesAsync();
    await operation.ToUniTask();
}
```

### 4. 业务层加载热更资源

```csharp
private async UniTask<GameObject> LoadRolePrefabAsync(string location, Transform parent, CancellationToken token)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync(location, parent, token);
}
```

### 5. UI 层通过封装资源加载器使用资源模块

当前 `UIResourceLoader` 其实也是代理到：

```csharp
GameModule.ResourceModule.LoadGameObject(...)
GameModule.ResourceModule.LoadGameObjectAsync(...)
```

因此 UI 里的资源加载，本质上仍然是统一走 `GameModule.ResourceModule`。

## 使用原则

处理热更资源包时，优先遵循以下原则：

1. 包初始化、版本更新、下载和清缓存优先沿用 AOT Procedure 现有链路。
2. 业务层只负责“使用资源”，不负责自建下载和版本管理流程。
3. 资源访问统一走 `GameModule.ResourceModule`。
4. UI 资源加载同样属于资源模块体系，不要另造一套加载接口。
5. 非必要不要手动绕过 `IResourceModule` 直接操作 YooAsset 包对象。
