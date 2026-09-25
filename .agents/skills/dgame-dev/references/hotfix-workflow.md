# 热更代码工作流

> **适用场景**：HybridCLR、`GameStart`、热更 DLL、AOT metadata、程序集边界 | **关联文档**：[architecture.md](architecture.md)、[hotpatch-workflow.md](hotpatch-workflow.md)

## 程序集边界

| 程序集 | 是否热更 | 说明 |
|--------|----------|------|
| `DGame.AOT` | 否 | 启动流程、DLL 加载、AOT metadata |
| `DGame.Runtime` | 否 | 框架运行时、模块接口与二次封装 |
| `GameProto` | 是 | Luban 生成代码和协议配置 |
| `GameBattle` | 是 | 战斗逻辑 |
| `GameLogic` | 是 | 主业务入口、UI、模块实现 |

`GameUnity/Assets/DGame/Runtime/Setting/UpdateSettings.asset` 当前配置：

| 字段 | 当前值 |
|------|--------|
| `HotUpdateAssemblies` | `GameProto.dll`、`GameBattle.dll`、`GameLogic.dll` |
| `LogicMainDllName` | `GameLogic.dll` |
| `AssemblyTextAssetPath` | `BundleAssets/DLL` |
| `AssemblyTextAssetExtension` | `.bytes` |
| `AOTMetaAssemblies` | `mscorlib.dll`、`System.dll`、`System.Core.dll`、`DGame.Runtime.dll`、`UniTask.dll`、`YooAsset.dll`、`UnityEngine.CoreModule.dll` |

---

## 热更入口

入口文件：`GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`。

```csharp
public static void Entrance(object[] objects)
{
    m_hotfixAssembly = (List<Assembly>)objects[0];
    GameEventLauncher.Init();
    DGame.Utility.UnityUtil.AddDestroyListener(OnDestroy);
    InitLanguageSettings();
    StartGame();
}
```

约束：

- 主入口类型名必须是 `GameStart`。
- 入口方法必须是 public static `Entrance(object[] objects)`。
- `GameEventLauncher.Init()` 是 DGame 接口事件初始化入口。

> **热更层无 Procedure**：启动状态机（`XxxProcedure`）全在 `DGame.AOT`，热更业务不写 Procedure。`GameStart.StartGame()` 直接 `GameModule.UIModule.ShowWindow<MainWindow>()` 驱动首屏，新流程从这里往下接，不要去热更层找热更域状态机。

---

## 日常开发步骤

| 场景 | 步骤 |
|------|------|
| Editor 内开发 | 直接改热更代码 Play，DLL 随工程编译，无需打包 |
| 模拟热更验证 | HybridCLR Generate → `DGame Tools/Build/Build Dll And CopyTo AssemblyTextAssetPath` 拷 DLL → ReleaseTools 打资源包 |
| 真机测试 | 出整包 → 热更资源部署到 CDN → 启动触发下载（详见 [hotpatch-workflow.md](hotpatch-workflow.md)） |

首包构建或 AOT 程序集/泛型引用变动时，如果启用了 HybridCLR 热更新（`ENABLE_HYBRIDCLR` / `UpdateSettings.Enable` 为 true），必须先执行一次 `HybridCLR/Generate/All`（GenerateAll 等价入口），再执行 BuildPlayer、`BuildDllCommand.BuildAndCopyDlls()` 和 AB 构建。

新功能落位顺序：事件接口 `IEvent/` → UI（`GameLogic/UI`）→ 业务模块（`GameLogic/Module`）→ 在 `GameStart` 或对应模块初始化处接入，不新增 AOT 层 Procedure。

---

## DLL 加载流程

`LoadAssemblyProcedure` 位于 `GameUnity/Assets/DGame.AOT/Procedure/LoadAssemblyProcedure.cs`：

1. 非 Editor 且启用 HybridCLR 时，先加载 `AOTMetaAssemblies` 对应 `.dll.bytes`。
2. 按 `HotUpdateAssemblies` 加载热更 DLL TextAsset。
3. 对每个热更 DLL 执行 `Assembly.Load(textAsset.bytes)`。
4. DLL 名等于 `LogicMainDllName` 时作为主业务程序集。
5. 所有热更程序集加入 `m_hotfixAssemblyList`。
6. TextAsset 加载完后调用 `m_resourceModule.UnloadAsset(textAsset)`。
7. 加载完成后反射调用 `GameStart.Entrance(object[] objects)`。

当 `UpdateSettings.EnableAddressable` 为 true 时，DLL location 可直接是 `GameLogic.dll`；否则拼接为 `Assets/BundleAssets/DLL/GameLogic.dll.bytes`。

---

## AOT metadata

运行时加载：

```csharp
HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(
    bytes,
    HomologousImageMode.SuperSet);
```

注意：

- metadata 是给 AOT DLL 补充元数据，不是给热更 DLL 补充元数据。
- 加载的 DLL 必须来自 Unity Build 过程中生成的裁剪后 AOT DLL，例如 `AssembliesPostIl2CppStrip/{Target}`。
- `BuildDllCommand.CopyAotAssembliesToAssetPath` 明确要求先 BuildPlayer，再复制 AOT 裁剪 DLL。

---

## AOT 泛型补充步骤

当真机出现 AOT 泛型缺失、`ExecutionEngineException` 或 Editor 正常但 IL2CPP 异常时，按这个顺序处理：

1. 启用 HybridCLR 热更新时，先执行 `HybridCLR/Generate/All`，刷新 HybridCLR 生成产物和泛型引用。
2. 执行一次目标平台 BuildPlayer，生成裁剪后的 AOT DLL。
3. 使用菜单 `DGame Tools/Build/Build Dll And CopyTo AssemblyTextAssetPath` 编译并复制热更 DLL 与 AOT metadata。
4. 确认 DLL 已复制到 `GameUnity/Assets/BundleAssets/DLL/*.dll.bytes`。
5. 重新构建 YooAsset 资源包，保证 DLL bytes 进入包体或远端资源。
6. 真机运行时由 `LoadAssemblyProcedure` 先加载 AOT metadata，再加载 `GameProto.dll`、`GameBattle.dll`、`GameLogic.dll`。

如果仍缺失泛型，补显式泛型引用或生成对应 AOTGenericReferences，再重复上述步骤。

常见需要补充的泛型（AOT 未实例化、热更侧首次用到时易缺失）：

- `List<自定义类型>`、`Dictionary<自定义, 自定义>` 等容器
- `UniTask<自定义类型>` 异步返回值
- `Action<自定义类型>` / `Func<自定义类型>` 委托
- `MemoryPool.Spawn<自定义类型>()` 等泛型方法调用

处理办法：在 AOT 层（如 `AOTGenericReferences` 或任意 AOT 程序集）显式写出这些封闭泛型的引用，让 IL2CPP 预先实例化。

---

## HybridCLR 限制清单

热更代码中避免或谨慎使用：

| 限制点 | 处理建议 |
|--------|----------|
| AOT 程序集 internal 类型或 internal 成员 | 不从热更层依赖 AOT internal；通过 public API 或 DGame 封装暴露 |
| `dynamic` | 避免使用，优先静态类型 |
| 运行时 Emit | IL2CPP/HybridCLR 场景不可依赖 |
| 表达式树 `Compile()` | 不要依赖运行时代码生成 |
| 复杂反射创建泛型 | 补 AOT metadata 和显式泛型引用 |
| Marshal/PInvoke | 热更层谨慎使用，优先 AOT 层封装 |
| `[RuntimeInitializeOnLoadMethod]` | 热更侧不会按 Unity AOT 启动流程自然执行，放到 `GameStart` 显式调用 |
| `[RequireComponent]` | 热更侧自动收集有限制，必要时使用项目 collector 或运行时显式校验 |

`HybRidCLRNoSupportAttributeCollector.cs` 已处理部分热更侧不支持特性收集，但业务代码不要假设所有 Unity Attribute 都会自动生效。

---

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| `Entrance(Assembly[] assemblies)` | `Entrance(object[] objects)` |
| 忘记 `GameEventLauncher.Init()` | 在 `GameStart.Entrance` 早期调用 |
| 热更 DLL TextAsset 加载后不释放 | `LoadAssemblyProcedure` 中 `UnloadAsset(textAsset)` |
| 在 `GameProto` 写业务逻辑 | 放到 `GameLogic` 或 `GameBattle` |
| 用 `dynamic`/Emit 绕过 AOT 泛型问题 | 补 AOT metadata 和显式泛型引用 |
| 首包或 AOT 变动后未跑 GenerateAll | 启用 HybridCLR 热更新时先执行 `HybridCLR/Generate/All` |
| 手工复制原始 AOT DLL | 复制 BuildPlayer 后裁剪 DLL |
| 资源路径写 `BundleAssets/DLL` | DGame 使用 `BundleAssets/DLL` |

## 交叉引用

热更 DLL 更新依赖 YooAsset 资源包下载。资源包版本、清单、下载器链路在 [hotpatch-workflow.md](hotpatch-workflow.md)，关键方法：

- `GameModule.ResourceModule.RequestPackageVersionAsync(...)`：请求最新包版本
- `GameModule.ResourceModule.UpdatePackageManifestAsync(...)`：更新资源清单
- `GameModule.ResourceModule.CreateResourceDownloader(...)`：创建下载器
- `GameModule.ResourceModule.GetPackageVersion(...)`：读取本地包版本

| 关联主题 | 文档 |
|---------|------|
| 资源包下载与缓存清理 | [hotpatch-workflow.md](hotpatch-workflow.md) |
| 整包 / AB 构建 | [build-pipeline.md](build-pipeline.md) |
| 程序集分层与落位 | [architecture.md](architecture.md) |
