# DGame 热更资源包管理（精简版）

当需求涉及热更资源包、资源版本更新、下载器、缓存清理、YooAsset 或 `GameModule.ResourceModule` 的使用方式时，先读本文件。

目标：只保留 Codex 做热更资源包相关决策必须知道的信息。原始细节以 `references/originals/client-hotpatch-development.md` 为准。

## 核心边界

- 资源主入口：`GameModule.ResourceModule`
- 底层实现：`DGame.Runtime/Module/ResourceModule`
- 资源系统：YooAsset
- 热更资源总流程：`DGame.AOT/Procedure/`

结论：

- 包初始化、版本更新、下载、缓存清理属于 AOT Procedure
- 业务层只负责“使用资源”
- 不要另外造一套下载、版本或缓存管理链

## 核心概念

- `Package`：YooAsset 资源包，默认包名由 `DefaultPackageName` 决定，默认是 `DefaultPackage`
- `PackageVersion`：当前最新资源包版本号
- `Manifest`：资源清单，通过 `UpdatePackageManifestAsync` 更新
- `Downloader`：资源下载器，通过 `CreateResourceDownloader()` 创建
- `Clear Cache`：清理未使用或全部缓存包文件
- `PlayMode`：资源运行模式，影响初始化与更新逻辑

当前已确认支持的模式：

- `EditorSimulateMode`
- `OfflinePlayMode`
- `HostPlayMode`
- `WebPlayMode`

## 热更流程

当前 AOT 主链路：

1. `InitPackageProcedure`
2. `InitResourceProcedure`
3. `CreateDownloaderProcedure`
4. `DownloadFileProcedure`
5. `DownloadOverProcedure`
6. `ClearCacheProcedure`
7. `PreloadProcedure`

各阶段职责：

- `InitPackageProcedure`：初始化 YooAsset Package
- `InitResourceProcedure`：请求版本、更新清单、判断是否需要下载
- `CreateDownloaderProcedure`：创建下载器并统计待下载文件
- `DownloadFileProcedure`：执行下载并更新进度
- `DownloadOverProcedure`：下载完成后的收尾
- `ClearCacheProcedure`：清理未使用缓存
- `PreloadProcedure`：进入正式游戏前预加载

结论：

- 若需求是“改版本更新、下载、缓存策略”，改对应 AOT Procedure
- 若需求是“业务里加载资源”，直接走 `GameModule.ResourceModule`

## 常用 API

### 包管理

- `Initialize()`
- `InitPackage(packageName, needInitMainFest)`
- `RequestPackageVersionAsync(...)`
- `UpdatePackageManifestAsync(packageVersion, ...)`
- `CreateResourceDownloader(...)`
- `ClearCacheFilesAsync(...)`
- `ClearAllBundleFiles(...)`
- `GetPackageVersion(...)`

### 资源加载

- `LoadAsset<T>(location, packageName)`
- `LoadAssetAsync<T>(location, cancellationToken, packageName)`
- `LoadGameObject(location, parent, packageName)`
- `LoadGameObjectAsync(location, parent, cancellationToken, packageName)`
- `UnloadAsset(asset)`
- `UnloadUnusedAssets()`
- `ForceUnloadAllAssets()`

### 关键字段

- `DefaultPackageName`
- `PackageVersion`
- `Downloader`
- `DownloadingMaxNum`
- `FailedTryAgainNum`
- `HostServerURL`
- `FallbackHostServerURL`
- `UpdatableWhilePlaying`

## 最小使用样例

### 初始化包并更新清单

```csharp
var resource = GameModule.ResourceModule;

var initOp = await resource.InitPackage(resource.DefaultPackageName);
var versionOp = resource.RequestPackageVersionAsync();
await versionOp.ToUniTask();

resource.PackageVersion = versionOp.PackageVersion;

var manifestOp = resource.UpdatePackageManifestAsync(resource.PackageVersion);
await manifestOp.ToUniTask();
```

### 创建下载器

```csharp
var downloader = GameModule.ResourceModule.CreateResourceDownloader();
```

### 清理缓存

```csharp
var operation = GameModule.ResourceModule.ClearCacheFilesAsync();
await operation.ToUniTask();
```

### 业务层加载资源

```csharp
var go = await GameModule.ResourceModule.LoadGameObjectAsync(location, parent, token);
```

## UI 侧

- `UIResourceLoader` 本质上仍代理到 `GameModule.ResourceModule`
- UI 资源加载也属于统一资源模块体系
- 不要另造一套 UI 资源加载接口

## 使用原则

1. 包初始化、版本更新、下载、清缓存优先沿用 AOT Procedure 现有链路
2. 业务层只负责“使用资源”，不负责自建热更资源流程
3. 资源访问统一走 `GameModule.ResourceModule`
4. UI 资源加载同样走资源模块
5. 非必要不要绕过 `IResourceModule` 直接操作底层 YooAsset 包对象
