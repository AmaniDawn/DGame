# 热更资源包工作流

> **适用场景**：YooAsset 包版本、下载器、缓存清理、UpdateSettings、ResourceModuleDriver | **关联文档**：[resource-api.md](resource-api.md)

## 设置来源

| 文件 | 职责 |
|------|------|
| `DGame/Runtime/Module/Settings/UpdateSettings.cs` | 热更开关、包名、版本模式、服务器地址、DLL 列表 |
| `DGame/Runtime/Module/ResourceModule/ResourceModuleDriver.cs` | 运行模式、包名、下载参数、资源清理 |
| `DGame/Editor/ReleaseTools/ReleaseTools.cs` | AssetBundle 构建、整包构建菜单 |

ReleaseTools 菜单入口：

| 菜单 | 行为 |
|------|------|
| `DGame Tools/Build/一键打包AB` | 当前平台构建 AssetBundle，快捷键 `F8` |
| `DGame Tools/Build/AutoBuildWindow` | 构建 Windows AB 和整包 |
| `DGame Tools/Build/AutoBuildAndroid` | 构建 Android AB 和 APK |
| `DGame Tools/Build/AutoBuildIOS` | 构建 iOS AB 和 XCode 工程 |

## 版本模式

`PackageVersionMode`：

- `AutoTimestamp`：版本格式 `yyyy-MM-dd-分钟段`。
- `Manual`：使用 `m_manualBuildVersion`。

远端地址由 `GetResDownloadPath()` 和 `GetFallbackResDownloadPath()` 生成。

## 运行模式

`ResourceModuleDriver.PlayMode`：

- 编辑器内读取 `EditorPrefs["EditorPlayMode"]`。
- 非编辑器如果配置为 `EditorSimulateMode`，会改成 `OfflinePlayMode`。

## 下载器

```csharp
var downloader = GameModule.ResourceModule.CreateResourceDownloader(packageName);
GameModule.ResourceModule.Downloader = downloader;
```

下载参数与常用成员：

- `DownloadingMaxNum`
- `FailedTryAgainNum`
- `UpdatableWhilePlaying`
- `GameModule.ResourceModule.GetPackageVersion(customPackageName)`：读取本地资源包版本
- `downloader.TotalDownloadBytes`：待下载总字节
- `downloader.DownloadUpdateCallback`：下载进度回调（DGame 实际成员，非 `OnDownloadProgressCallback`）

## 端到端下载示例

资源更新链路按版本、清单、下载器、下载、清缓存顺序执行。多包时每一步都传同一个 `packageName`，不要只在最终加载资源时传包名：

```csharp
string packageName = "DLC_Chapter2";

var versionOp = GameModule.ResourceModule.RequestPackageVersionAsync(
    appendTimeTicks: false,
    timeout: 60,
    customPackageName: packageName);
await versionOp;
if (versionOp.Status != EOperationStatus.Succeed)
{
    return;
}

var manifestOp = GameModule.ResourceModule.UpdatePackageManifestAsync(
    versionOp.PackageVersion,
    timeout: 60,
    customPackageName: packageName);
await manifestOp;
if (manifestOp.Status != EOperationStatus.Succeed)
{
    return;
}

var downloader = GameModule.ResourceModule.CreateResourceDownloader(packageName);
GameModule.ResourceModule.Downloader = downloader;
if (downloader.TotalDownloadCount > 0)
{
    downloader.BeginDownload();
    await downloader;
    if (downloader.Status != EOperationStatus.Succeed)
    {
        return;
    }
}

var clearOp = GameModule.ResourceModule.ClearCacheFilesAsync(
    EFileClearMode.ClearUnusedBundleFiles,
    packageName);
await clearOp;
```

DGame AOT 启动链按后缀式命名（`XxxProcedure`）有序驱动，排障时按此顺序串起来看：

```
LaunchProcedure → SplashProcedure → InitPackageProcedure → InitResourceProcedure
→ CreateDownloaderProcedure → DownloadFileProcedure → DownloadOverProcedure
→ PreloadProcedure → LoadAssemblyProcedure → StartGameProcedure
```

上面的版本、清单、下载器、下载、清缓存步骤主要落在 `InitResourceProcedure`、`CreateDownloaderProcedure`、`DownloadFileProcedure`（进度回调用 `DownloadUpdateCallback`）与 `ClearCacheProcedure`。

## 缓存清理

```csharp
GameModule.ResourceModule.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles, packageName);
GameModule.ResourceModule.ClearAllBundleFiles(packageName);
```

## 日常开发步骤

```
1. Editor 直接 Play：编辑器模式下热更 DLL 编译进工程，无需打包
2. 模拟热更：HybridCLR → Generate All → ReleaseTools 打资源包 → BuildDllCommand 拷贝 DLL
3. 真机测试：出包 → 部署热更资源到 CDN → 启动触发热更下载
```

（代码域边界、程序集划分与 `GameStart.Entrance` 见 [hotfix-workflow.md](hotfix-workflow.md)，本文件只覆盖资源包侧。）

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| 只改 ResourceModuleDriver 的 URL | 优先改 `UpdateSettings` |
| 多包流程只加载时传 packageName | 清单、下载、检查、加载都传 |
| 手动版本为空 | `Manual` 模式必须设置版本 |
| 随意改 `BundleAssets/DLL` 文件名 | 必须匹配 `HotUpdateAssemblies` |
