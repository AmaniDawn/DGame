# DGame 资源管理

当需求涉及资源寻址、Sprite 加载、GameObject 加载、普通 Asset 加载、资源卸载、资源信息查询、资源生命周期管理或 `GameModule.ResourceModule` 的使用方式时，先阅读本文件。

本文档记录当前仓库中资源管理模块的实际 API 和使用规则。处理资源加载时，统一以 `GameModule.ResourceModule` 为主入口；若是 UI 图片设置，优先使用 `SetSprite` 扩展，而不是自己再写一套异步图片加载流程。

## 目录导航

- [DGame 资源管理](#dgame-资源管理)
  - [目录导航](#目录导航)
  - [核心原则](#核心原则)
  - [资源寻址方式](#资源寻址方式)
  - [Sprite 加载](#sprite-加载)
  - [GameObject 加载](#gameobject-加载)
  - [其他 Asset 加载](#其他-asset-加载)
    - [同步加载](#同步加载)
    - [异步加载（推荐）](#异步加载推荐)
    - [回调风格](#回调风格)
    - [获取原始 Handle](#获取原始-handle)
  - [资源卸载](#资源卸载)
  - [资源信息查询](#资源信息查询)
  - [生命周期管理模式](#生命周期管理模式)
    - [UI 中加载 Sprite](#ui-中加载-sprite)
    - [加载 GameObject](#加载-gameobject)
    - [加载其他 Asset](#加载其他-asset)
  - [常用代码场景示例](#常用代码场景示例)
    - [设置 Image 图标](#设置-image-图标)
    - [动态加载并实例化 Prefab](#动态加载并实例化-prefab)
    - [预加载资源](#预加载资源)
    - [检查资源是否下载](#检查资源是否下载)
    - [游戏对象池](#游戏对象池)

## 核心原则

1. 资源访问统一走 `GameModule.ResourceModule`。
2. UI 图片优先使用 `SetSprite` / `SetSubSprite` 扩展。
3. 禁止直接使用 `Resources.Load`、`Resources.LoadAsync` 或其他绕过资源模块的 `Resources` 相关接口。
4. `GameObject` 不要先 `LoadAssetAsync<GameObject>` 再自己 `Instantiate`，应直接使用已封装好的 `LoadGameObject` / `LoadGameObjectAsync`。
5. `Sprite` 不要先 `LoadAssetAsync<Sprite>` 再自己设置到 `Image` 上，UI 图片应直接使用 `SetSprite` / `SetSubSprite`。
6. `LoadGameObject` / `LoadGameObjectAsync` 返回的实例由系统自动维护引用计数，不需要手动 `UnloadAsset`。
7. 非 GameObject 的普通 Asset 加载后，通常需要在不使用时主动 `UnloadAsset`。
8. 业务层优先异步加载，只有在确实可接受阻塞时才同步加载。

## 资源寻址方式

当前资源系统统一以 `location` 作为寻址方式。

常见 API 都是这种签名：

```csharp
LoadAsset<T>(string location, string packageName = "")
LoadAssetAsync<T>(string location, CancellationToken cancellationToken = default, string packageName = "")
LoadGameObject(string location, Transform parent = null, string packageName = "")
```

因此在当前项目里，资源寻址规则可以先理解为：

- 优先用资源定位地址 `location`
- 若不传 `packageName`，默认走 `DefaultPackage`
- UI 层资源加载也是同一套寻址方式

## Sprite 加载

当前项目里，Sprite 加载推荐使用：

- `SetSprite(this Image image, string location, ...)`
- `SetSprite(this SpriteRenderer spriteRenderer, string location, ...)`
- `SetSubSprite(...)`

相关实现位于：

- `GameUnity/Assets/DGame/Runtime/Module/ResourceModule/Utility/Sprite/SetSpriteExtensions.cs`

推荐原因：

- 封装了异步资源设置流程
- 统一通过 `ResourceExtComponent` 管理
- 更适合 UI 使用场景
- 避免自己手动管理 `Sprite` 资源引用和设置流程

推荐：

```csharp
m_imgIcon.SetSprite("item_1001");
```

如果是图集子图：

```csharp
m_imgIcon.SetSubSprite("CommonAtlas", "icon_mail");
```

不推荐：

```csharp
var sprite = await GameModule.ResourceModule.LoadAssetAsync<Sprite>("icon_mail", token);
m_imgIcon.sprite = sprite;
```

## GameObject 加载

当前资源模块提供：

- `LoadGameObject(...)`
- `LoadGameObjectAsync(...)`

接口注释已经明确：

- 会实例化资源到场景
- 无需主动 `UnloadAsset`
- `Destroy` 时自动 `UnloadAsset`

其背后依赖：

- `AssetReference`

`AssetReference` 会在实例销毁时自动释放关联资源引用。

因此对使用者来说可以直接记住：

- 动态实例化 Prefab 优先 `LoadGameObjectAsync`
- 这类实例通常不需要自己再手动 `UnloadAsset`
- 不要先 `LoadAssetAsync<GameObject>` 再自己 `Instantiate`

不推荐：

```csharp
var prefab = await GameModule.ResourceModule.LoadAssetAsync<GameObject>("TipsUI", token);
var go = UnityEngine.Object.Instantiate(prefab, parent);
```

推荐：

```csharp
var go = await GameModule.ResourceModule.LoadGameObjectAsync("TipsUI", parent, token);
```

## 其他 Asset 加载

### 同步加载

API：

```csharp
LoadAsset<T>(location, packageName)
LoadAsset(location, assetType, packageName)
```

适用场景：

- 已知资源很小
- 当前调用点明确允许同步加载
- 编辑器工具或低频初始化场景

### 异步加载（推荐）

API：

```csharp
LoadAssetAsync<T>(location, cancellationToken, packageName)
LoadAssetAsync(location, assetType, cancellationToken, packageName)
```

这是业务侧推荐的默认方式。

适用场景：

- 运行时业务逻辑
- UI 刷新
- 动态资源切换

### 回调风格

API：

```csharp
LoadAssetAsync<T>(location, Action<T> callback, packageName)
LoadAssetAsync(location, priority, loadAssetCallbacks, userData, packageName)
```

适用场景：

- 旧链路兼容
- 明确需要回调式接入的老代码

### 获取原始 Handle

API：

```csharp
LoadAssetSyncHandle<T>(...)
LoadAssetAsyncHandle<T>(...)
```

适用场景：

- 你确实需要直接操作 `AssetHandle`
- 需要更底层控制资源句柄和等待流程

如果只是正常拿资源对象，优先前面的高层 API，不要默认上来就拿 Handle。

## 资源卸载

当前统一卸载入口：

```csharp
GameModule.ResourceModule.UnloadAsset(asset);
GameModule.ResourceModule.UnloadUnusedAssets();
GameModule.ResourceModule.ForceUnloadAllAssets();
GameModule.ResourceModule.ForceUnloadUnusedAssets(true);
```

可先按下面规则理解：

| 场景 | 是否需要手动卸载 |
| --- | --- |
| `SetSprite` 加载的 UI 图片 | 通常不需要 |
| `LoadGameObject` / `LoadGameObjectAsync` 实例化对象 | 通常不需要，实例销毁时自动处理 |
| 其他 `LoadAsset<T>` / `LoadAssetAsync<T>` 得到的普通 Asset | 需要在不使用时主动 `UnloadAsset` |

## 资源信息查询

当前项目支持这些查询能力：

- `ContainsAsset(location, packageName)`
- `CheckLocationValid(location, packageName)`
- `GetAssetInfo(location, packageName)`
- `GetAssetInfos(tag, packageName)`
- `IsNeedDownloadFromRemote(location or assetInfo, packageName)`

这类 API 适合：

- 判断资源是否存在
- 判断定位地址是否合法
- 判断资源是否需要从远端下载
- 按 tag 查询资源清单信息

## 生命周期管理模式

### UI 中加载 Sprite

推荐：

- `Image.SetSprite(...)`
- `Image.SetSubSprite(...)`

通常无需手动释放。

### 加载 GameObject

推荐：

- `LoadGameObjectAsync(...)`

这类实例由 `AssetReference` 自动管理引用计数，通常无需手动释放资源句柄。

### 加载其他 Asset

例如：

- `Sprite`
- `TextAsset`
- `AudioClip`
- 其他普通 `UnityEngine.Object`

若通过 `LoadAsset<T>` / `LoadAssetAsync<T>` 直接拿到对象，通常在不再使用时必须主动：

```csharp
GameModule.ResourceModule.UnloadAsset(asset);
```

## 常用代码场景示例

### 设置 Image 图标

```csharp
private void RefreshIcon(string iconPath)
{
    m_imgIcon.SetSprite(iconPath, setNativeSize: true);
}
```

### 动态加载并实例化 Prefab

```csharp
private async UniTask<GameObject> CreateNpcAsync(string location, Transform parent, CancellationToken token)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync(location, parent, token);
}
```

### 预加载资源

```csharp
private async UniTask PreloadSoundAsync(string location, CancellationToken token)
{
    var clip = await GameModule.ResourceModule.LoadAssetAsync<AudioClip>(location, token);

    if (clip == null)
    {
        DLogger.Error($"Preload sound failed : {location}");
        return;
    }

    // 预加载完成后，如果这里只是提前拉起缓存，后续不立即使用，应在合适时机释放
    GameModule.ResourceModule.UnloadAsset(clip);
}
```

### 检查资源是否下载

```csharp
private bool IsIconReady(string location)
{
    var status = GameModule.ResourceModule.ContainsAsset(location);
    return status == CheckAssetStatus.AssetOnDisk;
}
```

### 游戏对象池

若资源需要频繁创建/销毁，不要只关注“怎么加载”，还要结合：

```csharp
GameModule.GameObjectPool
```

资源加载负责把对象拉起来，对象复用和频繁生成释放则应优先考虑对象池，而不是每次都重新异步加载实例化。

示例：

```csharp
private GameObjectPool m_effectPool;

private async UniTask InitEffectPoolAsync(Transform poolRoot, CancellationToken token)
{
    m_effectPool = GameObjectPool.Create(
        poolRoot,
        "HitEffect",
        initCapacity: 8,
        maxCapacity: 32,
        autoDestroyTime: 60f,
        dontDestroy: false,
        allowMultiSpawn: true);

    await m_effectPool.CreatePoolAsync(token);
}

private async UniTask<GameObject> SpawnHitEffectAsync(Transform parent, Vector3 position, CancellationToken token)
{
    if (m_effectPool == null)
    {
        return null;
    }

    return await m_effectPool.SpawnAsync(parent, position, Quaternion.identity, false, token);
}

private void RecycleHitEffect(GameObject effectGo)
{
    m_effectPool?.Recycle(effectGo);
}
```

这类场景适合对象池：

- 命中特效
- 飘字
- 子弹
- 高频重复生成/回收的表现对象

说明：

- 以上示例里的字符串应理解为“项目实际可寻址 location”
- 不要把示例误解成直接写磁盘路径或 `Assets/...` 路径
