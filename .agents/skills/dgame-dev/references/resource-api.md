# 资源加载与卸载 API

> **适用场景**：`GameModule.ResourceModule`、`LoadAssetAsync`、`LoadGameObjectAsync`、`UnloadAsset`、`SetSprite`、YooAsset 多包和热更下载 | **关联文档**：[resource-patterns.md](resource-patterns.md)、[hotpatch-workflow.md](hotpatch-workflow.md)

## 核心入口

业务层统一使用：

```csharp
GameModule.ResourceModule
```

接口文件：`GameUnity/Assets/DGame/Runtime/Module/ResourceModule/IResourceModule.cs`。

不要写 `GameModule.Resource`，DGame 当前业务入口是 `GameModule.ResourceModule`。

---

## 资源加载

### 普通资源

```csharp
var textAsset = await GameModule.ResourceModule.LoadAssetAsync<TextAsset>(
    location,
    cancellationToken);

GameModule.ResourceModule.UnloadAsset(textAsset);
```

同步 API 存在，但新增业务优先异步：

```csharp
var material = GameModule.ResourceModule.LoadAsset<Material>("UIMat");
GameModule.ResourceModule.UnloadAsset(material);
```

### 非泛型（Type）重载

资源类型在运行时才确定时，用 `Type` 参数的非泛型重载，返回 `UnityEngine.Object`：

```csharp
UniTask<UnityEngine.Object> LoadAssetAsync(string location, Type assetType,
    CancellationToken cancellationToken = default, string packageName = "");

UnityEngine.Object LoadAsset(string location, Type assetType, string packageName = "");
```

同样按持有关系 `UnloadAsset`；实例化 GameObject 仍用 `LoadGameObject(Async)`。

### 回调式重载

除 `UniTask<T>` 外，`IResourceModule` 还保留回调式异步加载：

```csharp
UniTaskVoid LoadAssetAsync<T>(string location, Action<T> callback,
    string packageName = "")
    where T : UnityEngine.Object;
```

示例：

```csharp
GameModule.ResourceModule.LoadAssetAsync<Material>("UIMat", material =>
{
    if (material == null)
    {
        DLogger.Error("Load UIMat failed.");
        return;
    }

    // 使用完成后仍按持有关系 UnloadAsset。
}, packageName: "");
```

需要成功/失败/进度/userData 时使用 `LoadAssetCallbacks` 重载：

```csharp
void LoadAssetAsync(string location, int priority,
    LoadAssetCallbacks loadAssetCallbacks, object userData,
    string packageName = "");

void LoadAssetAsync(string location, Type assetType, int priority,
    LoadAssetCallbacks loadAssetCallbacks, object userData,
    string packageName = "");
```

`LoadAssetCallbacks` 委托签名：

| 回调 | 签名 |
|------|------|
| 成功 | `LoadAssetSuccessCallback(string assetName, object asset, float duration, object userData)` |
| 失败 | `LoadAssetFailureCallback(string assetName, LoadResourceStatus status, string errorMsg, object userData)` |
| 进度 | `LoadAssetUpdateCallback(string assetName, float progress, object userData)` |

新增业务仍优先使用 `UniTask<T>` + `CancellationToken`；回调式 API 主要用于兼容旧调用、需要进度回调或接入已有回调管线。

### GameObject 实例

```csharp
var go = await GameModule.ResourceModule.LoadGameObjectAsync(location, parent, cancellationToken);
var go2 = GameModule.ResourceModule.LoadGameObject(location, parent);
```

`LoadGameObject/LoadGameObjectAsync` 会实例化到场景。销毁实例时走 Unity `Destroy` 或 UI 生命周期；不要对实例调用 `UnloadAsset`。

### AssetHandle

返回 `AssetHandle` 的加载方式，适合需要精细控制加载过程的场景，四个句柄签名（同步/异步 × 泛型/非泛型）：

```csharp
// 同步句柄
AssetHandle LoadAssetSyncHandle<T>(string location, string packageName = "");
AssetHandle LoadAssetSyncHandle(string location, Type assetType, string packageName = "");

// 异步句柄
AssetHandle LoadAssetAsyncHandle<T>(string location, string packageName = "");
AssetHandle LoadAssetAsyncHandle(string location, Type assetType, string packageName = "");
```

使用示例（`using` + `IsValid` + `AssetObject` + `.Task`）：

```csharp
using var syncHandle = GameModule.ResourceModule.LoadAssetSyncHandle<AudioClip>(location);
if (syncHandle.IsValid) { var clip = syncHandle.AssetObject as AudioClip; }

using var asyncHandle = GameModule.ResourceModule.LoadAssetAsyncHandle<AudioClip>(location);
await asyncHandle.Task;
if (asyncHandle.IsValid) { var clip = asyncHandle.AssetObject as AudioClip; }
```

持有 `AssetHandle` 的代码必须明确 `Dispose` 责任（`using` 或手动 `handle.Dispose()`）。

### 加载方式选择

| 资源类型 / 场景 | 推荐 API | 是否手动释放 |
|-----------------|----------|--------------|
| `TextAsset` / `Material` / 直接持有的 `Sprite` | `LoadAssetAsync<T>(location, cancellationToken)` | 是，拥有者在 `OnDestroy/Dispose` 调 `UnloadAsset(asset)` |
| 需要成功/失败/进度回调的普通资源 | `LoadAssetAsync(location, priority, LoadAssetCallbacks, userData, packageName)` | 是，成功回调拿到的资源按持有关系 `UnloadAsset` |
| 手动持有 YooAsset 句柄 | `LoadAssetAsyncHandle<T>(location, packageName)` | 是，持有者调用 `handle.Dispose()` |
| 实例化 GameObject Prefab | `LoadGameObjectAsync(location, parent, cancellationToken)` / `LoadGameObject(location, parent)` | 否，不对实例调 `UnloadAsset`，销毁实例 GameObject |
| UI `Image` / `SpriteRenderer` 设置图片 | `image.SetSprite(...)` / `spriteRenderer.SetSprite(...)` | 否，不存在 `ReleaseSprite`；传取消令牌，组件链路管理已设置资源 |
| 图集子图 | `SetSubSprite(image, atlasLocation, spriteName, ...)` | 否，不存在 `ReleaseSprite`；窗口销毁或重新设置时由组件链路处理 |
| UI Widget Prefab | `CreateWidgetByPath<T>` / `CreateWidgetByPathAsync<T>` | 否，走 `UIWidget.Destroy` / UI 生命周期 |
| AudioClip 播放 | AudioModule 相关播放接口 | 通常否，由 AudioModule handle 池和停止逻辑管理 |

---

## Sprite 扩展方法

文件：`GameUnity/Assets/DGame/Runtime/Module/ResourceModule/Utility/Sprite/SetSpriteExtensions.cs`。

```csharp
public static void SetSprite(this Image image, string location,
    bool setNativeSize = false,
    Action<Image> callback = null,
    CancellationToken cancellationToken = default)

public static void SetSprite(this SpriteRenderer spriteRenderer, string location,
    Action<SpriteRenderer> callback = null,
    CancellationToken cancellationToken = default)

public static void SetSubSprite(this Image image, string location, string spriteName,
    bool setNativeSize = false,
    CancellationToken cancellationToken = default)

public static void SetSubSprite(this SpriteRenderer spriteRenderer, string location, string spriteName,
    CancellationToken cancellationToken = default)
```

注意：

- `Image.SetSprite` 的 callback 是 `Action<Image>`，不是 `Action<Sprite>`。
- `SpriteRenderer.SetSprite` 的 callback 是 `Action<SpriteRenderer>`。
- `SetSubSprite` 当前没有 callback 参数。
- UI 中传入 `gameObject.GetCancellationTokenOnDestroy()`，避免窗口销毁后继续设置 Sprite。

---

## 资源查询 API

DGame 不使用 `HasAsset`。查询资源存在性使用 `ContainsAsset`：

```csharp
bool valid = GameModule.ResourceModule.CheckLocationValid(location, packageName);
CheckAssetStatus status = GameModule.ResourceModule.ContainsAsset(location, packageName);
AssetInfo assetInfo = GameModule.ResourceModule.GetAssetInfo(location, packageName);
AssetInfo[] tagAssets = GameModule.ResourceModule.GetAssetInfos("PRELOAD", packageName);
AssetInfo[] multiTagAssets = GameModule.ResourceModule.GetAssetInfos(new[] { "UI", "PRELOAD" }, packageName);
```

`ContainsAsset` 返回 `CheckAssetStatus`：

| 值 | 含义 |
|----|------|
| `NotExist` | 资源不存在 |
| `AssetOnline` | 资源需要从远端下载 |
| `AssetOnDisk` | 资源在磁盘缓存 |
| `AssetOnFileSystem` | 资源在文件系统 |
| `BinaryOnDisk` | 二进制资源在磁盘缓存 |
| `BinaryOnFileSystem` | 二进制资源在文件系统 |
| `Invalid` | 无效资源地址 |

---

## 多包加载

```csharp
var status = GameModule.ResourceModule.ContainsAsset(location, "DLC_Chapter2");
var asset = await GameModule.ResourceModule.LoadAssetAsync<TextAsset>(
    location,
    packageName: "DLC_Chapter2");
```

默认包名由 `ResourceModuleDriver.PackageName` 和 `Settings.UpdateSettings.PackageName` 控制，当前默认 `DefaultPackage`。

---

## 热更下载 API

资源热更仍通过 `GameModule.ResourceModule` 进入 YooAsset：

```csharp
string version = GameModule.ResourceModule.GetPackageVersion();

var requestVersionOp = GameModule.ResourceModule.RequestPackageVersionAsync(
    appendTimeTicks: false,
    timeout: 60,
    customPackageName: "");

var updateManifestOp = GameModule.ResourceModule.UpdatePackageManifestAsync(
    packageVersion,
    timeout: 60,
    customPackageName: "");

var downloader = GameModule.ResourceModule.CreateResourceDownloader(customPackageName: "");
```

缓存清理和远端地址：

```csharp
GameModule.ResourceModule.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
GameModule.ResourceModule.ClearAllBundleFiles();
GameModule.ResourceModule.SetRemoteServerURL(defaultRemoteServerURL, fallbackHostServerURL);
```

方法名是 `SetRemoteServerURL`，不是 `SetRemoteServicesUrl`。

---

## 资源清理

```csharp
GameModule.ResourceModule.UnloadUnusedAssets();
GameModule.ResourceModule.ForceUnloadUnusedAssets(true);
GameModule.ResourceModule.ForceUnloadAllAssets();
```

`ResourceModuleDriver` 会根据时间间隔、强制标记和低内存回调触发清理。

---

## 路径与地址

- 资源文件落位在 `GameUnity/Assets/BundleAssets/...`。
- `UpdateSettings.AssemblyTextAssetPath` 当前是 `BundleAssets/DLL`。
- location 通常由 YooAsset 收集器地址决定。当前项目启用 Addressable 时，DLL 加载地址可直接是 `GameLogic.dll`；未启用时会拼为 `Assets/BundleAssets/DLL/GameLogic.dll.bytes`。
- 热更资源统一放 `Assets/BundleAssets/...`（经 YooAsset 收集，走 `ResourceModule` 加载）。
- 非热更资源放 `Assets/AssetArt/...`，按资源类型划分子目录（如 `Atlas`）。

---

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| 新增业务使用 `Resources.Load` | 使用 `GameModule.ResourceModule` |
| 写 `GameModule.Resource` | 写 `GameModule.ResourceModule` |
| 写 `HasAsset(location)` | 写 `ContainsAsset(location)` 并处理 `CheckAssetStatus` |
| `LoadAssetAsync<T>` 后长期持有不释放 | 在拥有者 `OnDestroy/Dispose` 中 `UnloadAsset` |
| 对实例化 GameObject 调 `UnloadAsset` | 销毁实例 GameObject |
| 写 `SetSpriteAsync(...)` | DGame 只有 `SetSprite(...)` / `SetSubSprite(...)` 扩展方法 |
| 写 `ReleaseSprite(...)` | DGame 不存在该 API；直接加载的 `Sprite` 用 `UnloadAsset`，`SetSprite` 链路不手动释放 |
| UI 异步加载不传取消令牌 | 使用 `gameObject.GetCancellationTokenOnDestroy()` |
| 热更下载和加载传不同包名 | 检查、下载、加载传同一个 `packageName` |

## 交叉引用

| 关联主题 | 文档 | 说明 |
|---------|------|------|
| 加载/释放模式 | resource-patterns.md | 生命周期归属、场景切换、并发预加载、CancellationToken |
| 热更资源包 | hotpatch-workflow.md | 包版本、下载器、缓存清理 |
| 构建打包 | build-pipeline.md | AB 构建与 `BundleAssets` 资源落位 |
