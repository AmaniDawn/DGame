# DGame 问题排查

> **适用场景**：编译报错、热更失败、资源加载失败、UI 不显示、事件无响应、Luban 配置读取失败 | **关联文档**：[resource-api.md](resource-api.md)、[event-system.md](event-system.md)、[hotfix-workflow.md](hotfix-workflow.md)

## 场景索引

| 场景 | 常见问题 |
|------|---------|
| 编译/热更 | AOT 泛型、Editor 正常真机报错、DLL 加载失败、生命周期签名错误 |
| 资源加载 | location 无效、缓存未更新、`SetSprite` callback 类型错误 |
| UI | 界面空白、Widget 创建失败、事件销毁后触发、生命周期签名错误 |
| 事件 | 接口事件无响应、监听收不到、不存在的清理 API |
| 内存/性能 | GC 频繁、启动慢、DrawCall 高 |
| Luban | 生成报错、`Tables` 为 null、业务封装读不到配置 |
| UniTask | 异常被吞、await 后对象已销毁 |

---

## 编译/热更问题

### AOT 泛型异常：`ExecutionEngineException`

热更代码使用了主包没有 AOT 实例的泛型。处理顺序：

1. BuildPlayer 生成裁剪 AOT DLL。
2. HybridCLR Generate / 编译热更 DLL。
3. 执行 `DGame Tools/Build/Build Dll And CopyTo AssemblyTextAssetPath`。
4. 确认 `Assets/BundleAssets/DLL/*.dll.bytes` 已更新。
5. 重新构建 YooAsset 资源包。

仍缺失时，补显式泛型引用或更新 AOTGenericReferences。

### Editor 正常，真机报错

Editor 走 Mono 或编辑器程序集，真机走 IL2CPP + HybridCLR。检查 `dynamic`、Emit、表达式树 `Compile()`、复杂反射泛型、Marshal/PInvoke、热更侧 Unity Attribute 自动执行等限制。开启 `Development Build` + `Script Debugging` 获取完整真机堆栈。

### iOS / IL2CPP 裁剪导致真机缺类型或方法

iOS 真机高频问题是 Editor 正常，但 IL2CPP 裁剪后反射、泛型、Unity 原生类型或第三方库成员缺失。排查顺序：

1. 确认 `GameUnity/Assets/HybridCLRGenerate/link.xml` 已随 HybridCLR 生成并参与构建。
2. 需要补充元数据的 AOT 程序集加入 `UpdateSettings.AOTMetaAssemblies`，当前默认含 `mscorlib.dll`、`System.dll`、`System.Core.dll`、`DGame.Runtime.dll`、`UniTask.dll`、`YooAsset.dll`、`UnityEngine.CoreModule.dll`。
3. 执行 BuildPlayer 后再复制 AOT 裁剪 DLL；`BuildDllCommand` 报 “裁剪后的AOT程序集在BuildPlayer时才能生成” 时，先构建一次 App。
4. 对 `link.xml` 仍无法保住的类型，在 AOT 层显式引用。DGame 已有 `GameUnity/Assets/DGame.AOT/Launcher/Scripts/AOT/DisStripCode.cs` 和 `HybridCLROptimizer.cs`，优先在这里补 `RegisterType<T>()` 或显式方法调用。
5. iOS 特定权限或原生类型，例如相机、WebCam、动画/物理类型，优先用 AOT 显式引用验证，不要只依赖热更侧反射访问。

`[Preserve]` 和 `link.xml` 不是万能方案；DGame 的 `DisStripCode` 注释已说明：主工程无引用时，`link.xml` 也可能无效，最好在 AOT 代码中显式保留引用。

### 热更 DLL 加载失败

1. `UpdateSettings.HotUpdateAssemblies` 是否包含 `GameProto.dll`、`GameBattle.dll`、`GameLogic.dll`。
2. `UpdateSettings.LogicMainDllName` 是否为 `GameLogic.dll`。
3. `Assets/BundleAssets/DLL/<dll>.dll.bytes` 是否存在并被 YooAsset 收集。
4. `LoadAssemblyProcedure` 是否成功加载 AOT metadata 和所有热更 DLL。
5. 主入口类型是否为 `GameStart`，方法是否为 public static `Entrance(object[] objects)`。

---

## 资源加载问题

### location 无效

1. `GameModule.ResourceModule.CheckLocationValid(location)` 是否为 true。
2. `GameModule.ResourceModule.ContainsAsset(location)` 返回的 `CheckAssetStatus` 是什么。
3. 资源是否位于 `Assets/BundleAssets/...` 并被 YooAsset 收集。
4. 启用 Addressable 时 location 是否与收集器地址一致。
5. 多包资源是否传了正确 `packageName`。

### 热更后旧资源未更新

1. CDN 版本文件是否更新。
2. `RequestPackageVersionAsync` 是否获取到新版本号。
3. `UpdatePackageManifestAsync` 是否成功。
4. `CreateResourceDownloader` 是否下载完成且无失败。
5. 本地验证时清理 `Application.persistentDataPath` 缓存或调用资源模块清理 API。

### 内存持续增长

常见原因：

- `LoadAssetAsync<T>` 后忘记 `UnloadAsset`。
- 静态变量长期持有 UnityEngine.Object。
- `AssetHandle` 未 `Dispose`。
- 异步加载完成时拥有者已销毁，回调仍设置对象。
- UI Widget 绕过 UI 生命周期手动 `Instantiate`。

---

## 内存/性能问题

### GC 频繁

Profiler 查看 GC Alloc 热点。常见处理：字符串拼接改 `StringBuilder`、避免闭包捕获的 Lambda、热路径的 `Where/Select/ToList` 改手写 `for`（不分配的 `Count`/`Contains` 可保留）、频繁 `new` 的可复用对象改 `MemoryPool.Spawn<T>()`（DGame 是 `Spawn`，不是 `Acquire`）。

### 启动慢

减少 `PRELOAD` 标签资源、用进度回调展示加载状态、大型 Prefab 异步实例化。

### DrawCall 高

Frame Debugger 查看合批。UI 同 Atlas 同 Canvas、3D 用 GPU Instancing、确认材质 Shader 一致。

---

## UI 问题

### 界面空白/节点找不到

1. 窗口资源地址是否与窗口类名或 UIModule 打开参数一致。
2. Prefab 是否在 `Assets/BundleAssets/UI/...` 并被 YooAsset 收集。
3. 场景是否存在 `UIRoot`。
4. Prefab 根节点是否有 `Canvas`。
5. `ScriptGenerator` 中节点路径和 UI 节点前缀是否与 Prefab 一致。

### Widget 创建失败

1. `CreateWidget<T>(goPath)` 只适用于 Prefab 中已有节点。
2. `CreateWidgetByType<T>` 使用 `typeof(T).Name` 作为资源地址。
3. `CreateWidgetByPathAsync<T>` 需要有效 `assetLocation`，并依赖当前 UI 的销毁取消令牌。
4. `AsyncAdjustItemNum` 返回 void 且内部 `.Forget()`，不能 await。
5. 需要等待列表创建完成时使用 `AsyncAwaitAdjustItemNum`。

---

## 事件系统问题

### 接口事件无响应

1. `GameEventLauncher.Init()` 是否在 `GameStart.Entrance` 执行。
2. 事件接口是否带 `[EventInterface(EEventGroup...)]`。
3. 当前事件组是否为 `GroupUI`、`GroupLogic` 或 `GroupLogic`。
4. 生成的 `Ixxx_Event` 是否存在。
5. 监听和发送参数签名是否完全一致。

### UI 事件销毁后仍触发

UI 内事件必须在 `RegisterEvent()` 中用 `AddUIEvent` 注册。不要在窗口 `OnCreate/OnRefresh` 中直接调用 `GameEvent.AddEventListener`，除非你同时在 `OnDestroy` 手动移除。

---

## Luban 问题

### 配置读取失败（Tables/值为 null）

1. 是否已导出二进制配置到 `Assets/BundleAssets/Configs/Binary/`，且被 YooAsset 收集。
2. 配置资源地址是否与 `Tables` loader 传入文件名一致。
3. `ConfigSystem.Instance.Tables` 是否已初始化（首次访问才懒加载）。
4. 业务是否误改了 `GameProto/LubanConfig/*.cs` 生成代码。

常见报错线索：

- `TbXxxConfig` 取到的值为空 / `GetOrDefault` 返回默认：多为**改表后没转表**，包内二进制仍是旧版；先转表（懒加载优先，见 build-pipeline.md）再确认 `BundleAssets/Configs/Binary/` 已更新。
- `Tables` 或资源 `NullReferenceException`：多为在资源模块就绪前（`GameStart.Entrance` 之前、静态字段初始化时）访问了 `ConfigSystem.Instance.Tables`，触发懒加载但 `IResourceModule` 未注册；改到模块初始化后或首次业务使用时读取。首次读表通常发生在 `PreloadProcedure` 之后，不要提前。

### 生成 / 编辑类报错

Excel、表结构、字段、Bean、枚举、`__tables__.xlsx` / `__beans__.xlsx` / `__enums__.xlsx`、导表脚本报错，一律使用 `luban-dev` 排查，不在本文件处理。

### ConfigSystem 位置

DGame 的 `ConfigSystem.cs` 就在 `Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`。它由导表脚本从 `GameConfig/CustomTemplate/Client/Bin/ConfigSystem.cs` 复制，不属于 Luban 的表代码生成产物；调整加载逻辑时应修改模板后重新导表。消费链路见 [luban-config.md](luban-config.md)。

---

## UniTask 问题

### 异常被吞

`UniTaskVoid` 异常不按普通 `await` 链传播。优先让业务方法返回 `UniTask` 并在调用点 await；必须 fire-and-forget 时在方法内 try-catch。也可设置全局兜底：

```csharp
UniTaskScheduler.UnobservedExceptionHandler = e => DLogger.Error($"未处理 UniTask 异常: {e}");
```

### await 后对象已销毁

UI 异步逻辑传入销毁取消令牌：

```csharp
var token = gameObject.GetCancellationTokenOnDestroy();
await GameModule.ResourceModule.LoadAssetAsync<TextAsset>(location, token);
```

await 返回后仍要检查关键组件是否为空。

---

## 常见错误速查

| 错误 | 原因 | 修复 |
|------|------|------|
| `OnCreate(object userData)` 编译失败 | DGame UI 生命周期方法无参数 | `protected override void OnCreate()` |
| `OnRefresh(object userData)` 编译失败 | 同上 | `protected override void OnRefresh()` |
| `OnDestroy(bool isShutdown)` 编译失败 | 与 Procedure 生命周期混淆 | `protected override void OnDestroy()` |
| `SetSprite` callback 写成 `Action<Sprite>` | callback 参数不是 Sprite | Image 版用 `Action<Image>` |
| `SetSubSprite(..., callback)` 编译失败 | DGame `SetSubSprite` 无 callback | 只传 location、spriteName、nativeSize/token |
| `GameEvent.UnRegisterAll()` 编译失败 | DGame 没有该 API | 保存 handler 并 `RemoveEventListener` |
| `GameEventMgr.Clear()` 编译失败 | DGame 无公开 `GameEventMgr` 类 | UI 用 `AddUIEvent`，非 UI 手动移除 |
| `GameModule.Resource.HasAsset()` 编译失败 | DGame 无该入口和方法 | `GameModule.ResourceModule.ContainsAsset()` |
| `AdjustIconNum` 编译失败 | DGame 列表 API 叫 `AdjustItemNum` | 使用 `AdjustItemNum/AsyncAdjustItemNum/AsyncAwaitAdjustItemNum` |

### UIWindow 生命周期签名速查

```csharp
// 正确签名（UIBase 中定义）
protected override void OnCreate()
protected override void OnRefresh()
protected override void OnUpdate()
protected override void OnDestroy()

// 常见错误签名
protected override void OnCreate(object userData)
protected override void OnRefresh(object userData)
protected override void OnDestroy(bool isShutdown)
```

### SetSprite / SetSubSprite 签名速查

```csharp
image.SetSprite(string location, bool setNativeSize = false,
    Action<Image> callback = null, CancellationToken cancellationToken = default);

spriteRenderer.SetSprite(string location,
    Action<SpriteRenderer> callback = null, CancellationToken cancellationToken = default);

image.SetSubSprite(string location, string spriteName,
    bool setNativeSize = false, CancellationToken cancellationToken = default);

spriteRenderer.SetSubSprite(string location, string spriteName,
    CancellationToken cancellationToken = default);
```

### GameEvent 清理方法速查

```csharp
// UI 内部事件自动清理
AddUIEvent(eventType, handler);

// 非 UI 手动添加和移除
GameEvent.AddEventListener(eventType, handler);
GameEvent.RemoveEventListener(eventType, handler);
EventCenter.AddEvent.<组>.<方法>(handler);
EventCenter.RemoveEvent.<组>.<方法>(handler);

// UIBase 内部会清理 GameEventDriver，普通业务不要调用不存在的全局清理 API
```

不存在：`GameEvent.UnRegisterAll()`、`GameEvent.Shutdown()`、`GameEvent.ClearAll()`、`GameEventMgr.Clear()`。

---

## 交叉引用

| 主题 | 文档 |
|------|------|
| UI 生命周期 | [ui-lifecycle.md](ui-lifecycle.md) |
| UI 进阶模式 | [ui-patterns.md](ui-patterns.md) |
| 事件系统 | [event-system.md](event-system.md) |
| 事件反模式 | [event-antipatterns.md](event-antipatterns.md) |
| 资源加载 | [resource-api.md](resource-api.md) |
| 热更开发 | [hotfix-workflow.md](hotfix-workflow.md) |
| Luban 配置 | [luban-config.md](luban-config.md) |
