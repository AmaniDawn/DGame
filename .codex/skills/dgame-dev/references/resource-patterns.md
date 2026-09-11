# 资源管理模式

> **适用场景**：资源生命周期、UI 资源、缓存、泄漏排查、低内存处理 | **关联文档**：[resource-api.md](resource-api.md)、[ui-lifecycle.md](ui-lifecycle.md)

## 生命周期归属

| 资源类型 | 加载方式 | 释放方式 |
|----------|----------|----------|
| UI 窗口 Prefab | `UIModule.ResourceLoader.LoadGameObject` | `UIWindow.Destroy` 销毁实例 |
| 已存在节点上的 Widget | `CreateWidget<T>(goPath)` / `CreateWidget<T>(GameObject)` | `UIWidget.Destroy` |
| 动态加载的 Widget Prefab | `CreateWidgetByPath<T>` / `CreateWidgetByPathAsync<T>` | `UIWidget.Destroy` |
| TextAsset/Material/Sprite 等普通资源 | `LoadAssetAsync<T>` | 拥有者 `UnloadAsset` |
| AudioClip 池化资源 | AudioModule 内部 handle 池 | AudioModule 管理 |
| DLL TextAsset | `LoadAssemblyProcedure` | 加载 Assembly 后 `UnloadAsset` |

时序陷阱：`UIWidget.OnDestroy` 先于父 `Window.OnDestroy` 执行。父窗口若在 Widget 销毁后访问其资源会出现空引用，跨节点释放顺序需按此排布。

## UI 中加载资源

```csharp
private TextAsset m_configAsset;

protected override void OnCreate()
{
    LoadConfigAsync().Forget();
}

private async UniTaskVoid LoadConfigAsync()
{
    m_configAsset = await GameModule.ResourceModule.LoadAssetAsync<TextAsset>(
        "MyConfig", gameObject.GetCancellationTokenOnDestroy());
}

protected override void OnDestroy()
{
    GameModule.ResourceModule.UnloadAsset(m_configAsset);
    m_configAsset = null;
}
```

## 资源地址

DGame 常见热更资源目录：

- `BundleAssets/UI`
- `BundleAssets/UIRaw/Atlas`
- `BundleAssets/Audios`
- `BundleAssets/Materials`
- `BundleAssets/Fonts`
- `BundleAssets/DLL`

UI 默认 `AssetLocation` 是类型名，例如 `MainWindow` 加载 `MainWindow`。

## Widget 创建方式

`CreateWidgetByPathAsync<T>` 确实存在，定义在 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIBase.cs`。不要把它删成不存在，也不要把它用于已在窗口 Prefab 里的节点。

真实签名：

```csharp
public T CreateWidget<T>(string goPath, bool visible = true) where T : UIWidget, new()
public T CreateWidget<T>(Transform parentTrans, string goPath, bool visible = true) where T : UIWidget, new()
public T CreateWidget<T>(GameObject goRoot, bool visible = true) where T : UIWidget, new()
public T CreateWidgetByPath<T>(Transform parentTrans, string assetLocation, bool visible = true) where T : UIWidget, new()
public async UniTask<T> CreateWidgetByPathAsync<T>(Transform parentTrans, string assetLocation, bool visible = true) where T : UIWidget, new()
public T CreateWidgetByPrefab<T>(GameObject goPrefab, Transform parentTrans = null, bool visible = true) where T : UIWidget, new()
public T CreateWidgetByType<T>(Transform parentTrans, bool visible = true) where T : UIWidget, new()
public async UniTask<T> CreateWidgetByTypeAsync<T>(Transform parentTrans, bool visible = true) where T : UIWidget, new()
```

选择规则：

| 场景 | 推荐 API |
|------|----------|
| Widget 根节点已经在当前窗口 Prefab 中 | `CreateWidget<T>(goPath)` 或 `CreateWidget<T>(GameObject)` |
| Widget Prefab 需要按资源地址同步实例化 | `CreateWidgetByPath<T>(parentTrans, assetLocation)` |
| Widget Prefab 需要按资源地址异步实例化 | `CreateWidgetByPathAsync<T>(parentTrans, assetLocation)` |
| 已持有 Prefab 对象 | `CreateWidgetByPrefab<T>(goPrefab, parentTrans)` |
| 资源地址等于 Widget 类型名 | `CreateWidgetByType<T>` / `CreateWidgetByTypeAsync<T>` |

```csharp
var nodeWidget = CreateWidget<ItemWidget>("m_tfItemRoot");
var prefabWidget = await CreateWidgetByPathAsync<ItemWidget>(
    m_tfContent,
    "ItemWidget");
```

列表重建使用 `AdjustItemNum` 系列：

```csharp
AdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab);
for (int i = 0; i < data.Count; i++)
{
    m_items[i].Refresh(data[i]);
}

await AsyncAwaitAdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab,
    maxNumPerFrame: 5,
    updateAction: (item, index) => item.Refresh(data[index]));
```

## CancellationToken 使用

DGame 资源异步 API 支持取消令牌：

```csharp
var token = gameObject.GetCancellationTokenOnDestroy();
var config = await GameModule.ResourceModule.LoadAssetAsync<TextAsset>("LevelData", token);
```

规则：

- UI 里优先传 `gameObject.GetCancellationTokenOnDestroy()`。
- `CreateWidgetByPathAsync<T>` 内部已经把当前 UI 的 `gameObject.GetCancellationTokenOnDestroy()` 传给 `UIModule.ResourceLoader.LoadGameObjectAsync`。
- 自己创建 `CancellationTokenSource` 时必须在 `OnDestroy/Dispose` 中 `Cancel()` + `Dispose()`。
- 取消只阻止后续回调继续使用结果，不等价于释放已持有资源；已成功持有的 `UnityEngine.Object` 仍按拥有者调用 `UnloadAsset`。
- await 返回后仍要检查窗口、组件、关键字段是否还有效。

```csharp
private CancellationTokenSource m_cts;
private TextAsset m_asset;

protected override void OnCreate()
{
    m_cts = CancellationTokenSource.CreateLinkedTokenSource(
        gameObject.GetCancellationTokenOnDestroy());
    LoadConfigAsync(m_cts.Token).Forget();
}

private async UniTaskVoid LoadConfigAsync(CancellationToken token)
{
    m_asset = await GameModule.ResourceModule.LoadAssetAsync<TextAsset>("MyConfig", token);
    if (token.IsCancellationRequested || m_asset == null || this == null)
    {
        return;
    }
}

protected override void OnDestroy()
{
    m_cts?.Cancel();
    m_cts?.Dispose();
    m_cts = null;

    GameModule.ResourceModule.UnloadAsset(m_asset);
    m_asset = null;
}
```

## 并发与预加载

`ResourceModule` 内部用 `m_loadingAssetList` 避免同一资源并发重复加载；第二个请求会等待首个加载完成。业务层仍应避免无意义的批量重复请求。

推荐模式：

```csharp
var iconTask = GameModule.ResourceModule.LoadAssetAsync<Sprite>("IconSword", token);
var textTask = GameModule.ResourceModule.LoadAssetAsync<TextAsset>("ItemText", token);

// UniTask<T> 只能消费一次，用 WhenAll 的元组返回值解构，不要再对单个 task 调 GetResult
var (icon, text) = await UniTask.WhenAll(iconTask, textTask);
```

列表资源分帧创建：

```csharp
await AsyncAwaitAdjustItemNum(m_items, data.Count, m_tfContent,
    prefab: m_itemPrefab,
    maxNumPerFrame: 5,
    updateAction: (item, index) => item.Refresh(data[index]));
```

启动预加载：

- AOT `PreloadProcedure` 会读取 YooAsset 标签 `PRELOAD`。
- WebGL 下还会读取 `WEBGL_PRELOAD`。
- 只把启动强依赖、小体量资源加入 PRELOAD；大 UI、大模型、可延迟资源不要进启动预加载。

## 清理策略

普通资源释放后不一定立刻卸载 Bundle；由资源模块、YooAsset 和 `ResourceModuleDriver` 按引用计数与时间间隔清理。

低内存或切场景可调用：

```csharp
GameModule.ResourceModule.ForceUnloadUnusedAssets(true);
```

## 场景切换资源整理

切场景前后先处理业务持有关系，再触发资源模块整理，不要把 `ForceUnloadUnusedAssets(true)` 当成替代释放的万能入口。前置断引用：

1. 关闭不应跨场景保留的窗口，确保窗口 `OnDestroy` 里释放 `LoadAssetAsync<T>` 持有的资源。
2. 停止或回收本场景创建的 GameObject、Widget、对象池实例，避免仍有场景对象引用旧资源。
3. 清掉模块级或静态缓存中持有的 `UnityEngine.Object`、`AssetHandle`。

完整切换流程（API 已核实）：

```csharp
public async UniTask SwitchToBattleScene()
{
    // 1. 关闭全部窗口，触发各 UIWindow.OnDestroy 释放资源
    GameModule.UIModule.CloseAllWindows();

    // 2. Single 模式加载新场景，自动卸载旧场景
    await GameModule.SceneModule.LoadSceneAsync("BattleScene", LoadSceneMode.Single,
        progressCallBack: p => { /* 显示进度条 */ });

    // 3. 引用断开后整理未使用资源
    GameModule.ResourceModule.UnloadUnusedAssets();

    // 4. 内存压力大时再强制整理 + 可选 GC
    // GameModule.ResourceModule.ForceUnloadUnusedAssets(true);
}
```

## 叠加场景

叠加场景在卸载时必须显式 `UnloadAsync`，再整理其释放出的资源：

```csharp
// 叠加加载
await GameModule.SceneModule.LoadSceneAsync("MinigameScene", LoadSceneMode.Additive);

// 卸载叠加场景并清理
await GameModule.SceneModule.UnloadAsync("MinigameScene");
GameModule.ResourceModule.UnloadUnusedAssets();
```

## 分包下载门控

分包资源使用前先用 `ContainsAsset` 判断是否需要从远端下载，`AssetOnline` 时先下载再加载：

```csharp
public async UniTask EnsureDLCReady(string location, string packageName)
{
    // AssetOnline 表示资源需要从远端下载
    if (GameModule.ResourceModule.ContainsAsset(location, packageName) != CheckAssetStatus.AssetOnline)
    {
        return;
    }

    var downloader = GameModule.ResourceModule.CreateResourceDownloader(packageName);
    if (downloader.TotalDownloadCount > 0)
    {
        downloader.BeginDownload();
        await downloader.Task;
    }
}
```

下载、检查、加载必须传同一个 `packageName`。

## 泄漏根因分类

| 根因 | 典型表现 | 处理 |
|------|----------|------|
| 资源持有者未释放 | `LoadAssetAsync<T>` 后窗口关闭仍占用 | 在拥有者 `OnDestroy/Dispose` 调 `UnloadAsset` |
| AssetHandle 未 Dispose | 手动 `LoadAssetAsyncHandle` 后引用计数不降 | 持有方负责 `handle.Dispose()` |
| 异步回调晚于生命周期 | 窗口关闭后继续设置 Image/Text | 传 `CancellationToken`，await 后检查对象有效性 |
| 隐藏窗口仍存活 | UI 只是 Hide，资源持续存在 | 理解 `HideTimeToClose`，需要立即释放时 Close |
| 对象池持有实例 | 场景切换后池内对象仍保留 | `Recycle/Remove/DestroyPool` 区分使用 |
| 静态字段缓存资源 | 切场景或热更销毁后资源仍被静态引用 | 不用静态缓存 Unity 资源；必须缓存时提供显式 Clear |
| 重复列表重建 | 每次刷新都 Destroy/Instantiate | 使用 `AdjustItemNum` 或 SuperScrollView |

## 静态缓存反模式

不要用静态字段长期持有 `TextAsset`、`Sprite`、`Material`、`GameObject` Prefab 或 `AssetHandle`：

```csharp
// 错误：静态缓存绕过拥有者生命周期
private static Sprite s_icon;

// 正确：由窗口/模块持有，并在生命周期结束释放
private Sprite m_icon;

protected override void OnCreate()
{
    LoadIconAsync().Forget();
}

private async UniTaskVoid LoadIconAsync()
{
    m_icon = await GameModule.ResourceModule.LoadAssetAsync<Sprite>(
        "IconSword",
        gameObject.GetCancellationTokenOnDestroy());
}

protected override void OnDestroy()
{
    GameModule.ResourceModule.UnloadAsset(m_icon);
    m_icon = null;
}
```

允许的静态缓存通常只缓存纯数据、ID、路径字符串或配置查询结果。若确实需要模块级资源缓存，模块必须提供明确的 `Clear/Dispose`，并在热更销毁、切账号或切场景时调用。

## 泄漏排查

1. 搜索 `LoadAsset` 是否有对应 `UnloadAsset`。
2. 检查是否持有 `AssetHandle` 未 Dispose。
3. 检查 UI 是否隐藏而不是关闭，隐藏窗口会在 `HideTimeToClose` 后关闭。
4. 检查对象池是否仍持有实例。
5. 检查是否有静态字段持有 `UnityEngine.Object` 或 `AssetHandle`。
6. 检查异步加载是否传了取消令牌。
7. 使用 Debugger/资源日志观察引用计数。
