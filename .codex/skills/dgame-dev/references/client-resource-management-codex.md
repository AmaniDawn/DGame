# DGame 资源管理（精简版）

当需求涉及资源寻址、Sprite/Prefab/普通 Asset 加载与卸载、资源查询或 `GameModule.ResourceModule` 的使用方式时，先读本文件。

目标：只保留 Codex 做资源加载和生命周期决策必须知道的信息。原始细节以 `references/originals/client-resource-management.md` 为准。

## 核心原则

- 资源访问统一走 `GameModule.ResourceModule`
- 禁止直接使用 `Resources.Load`、`Resources.LoadAsync` 或其他绕过资源模块的 `Resources` 接口
- UI 图片优先使用 `SetSprite` / `SetSubSprite`
- `GameObject` 不要先 `LoadAssetAsync<GameObject>` 再 `Instantiate`
- `Sprite` 不要先 `LoadAssetAsync<Sprite>` 再自己设置到 UI
- 运行时业务优先异步加载

## 寻址规则

- 统一使用 `location`
- 不传 `packageName` 时默认走 `DefaultPackage`
- 示例字符串表示项目实际可寻址 `location`，不是磁盘路径或 `Assets/...` 路径

## 加载规则

### UI 图片

推荐：

```csharp
m_imgIcon.SetSprite("item_1001");
m_imgIcon.SetSubSprite("CommonAtlas", "icon_mail");
```

结论：UI 图片直接走 `SetSprite` / `SetSubSprite`，不要自己管理 `Sprite` 加载和设置。

### GameObject

推荐：

```csharp
var go = await GameModule.ResourceModule.LoadGameObjectAsync("TipsUI", parent, token);
```

结论：

- 动态实例化 Prefab 优先 `LoadGameObject` / `LoadGameObjectAsync`
- 不要先 `LoadAssetAsync<GameObject>` 再 `Instantiate`
- 这类实例通常不需要自己手动 `UnloadAsset`

### 普通 Asset

常用：

- `LoadAsset<T>(...)`
- `LoadAssetAsync<T>(...)`

结论：普通 Asset 加载后，通常在不再使用时主动 `UnloadAsset`

## 生命周期规则

| 场景 | 处理方式 |
| --- | --- |
| UI 中加载 Sprite | `SetSprite` / `SetSubSprite`，通常无需手动释放 |
| 加载并实例化 GameObject | `LoadGameObject` / `LoadGameObjectAsync`，实例销毁时通常自动处理 |
| 加载普通 Asset | 使用后主动 `UnloadAsset` |

统一卸载入口：

- `UnloadAsset(asset)`
- `UnloadUnusedAssets()`
- `ForceUnloadAllAssets()`
- `ForceUnloadUnusedAssets(true)`

## 查询 API

- `ContainsAsset(location, packageName)`
- `CheckLocationValid(location, packageName)`
- `GetAssetInfo(location, packageName)`
- `GetAssetInfos(tag, packageName)`
- `IsNeedDownloadFromRemote(location or assetInfo, packageName)`

适用：

- 判断资源是否存在
- 判断地址是否合法
- 判断资源是否需要远端下载
- 按 tag 查询资源清单

## 最小样例

### 设置 UI 图片

```csharp
m_imgIcon.SetSprite("icon_mail");
```

### 动态加载 Prefab

```csharp
var go = await GameModule.ResourceModule.LoadGameObjectAsync("TipsUI", parent, token);
```

### 预加载普通资源

```csharp
var clip = await GameModule.ResourceModule.LoadAssetAsync<AudioClip>(location, token);
GameModule.ResourceModule.UnloadAsset(clip);
```

### 检查资源是否下载

```csharp
var status = GameModule.ResourceModule.ContainsAsset("icon_mail");
bool ready = status == CheckAssetStatus.AssetOnDisk;
```

## 对象池边界

- 高频重复创建/回收的表现对象，要结合 `GameModule.GameObjectPool`
- 资源加载负责把对象拉起；高频复用对象优先对象池，而不是每次重新加载实例化

典型场景：

- 命中特效
- 飘字
- 子弹
- 其他高频重复生成/回收对象
