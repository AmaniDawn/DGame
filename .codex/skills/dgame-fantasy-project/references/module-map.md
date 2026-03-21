# Module Map

## Runtime Modules

`GameUnity/Assets/DGame/Runtime/Module` 下已确认的主要模块：

- `AnimModule`
- `AudioModule`
- `DebuggerModule`
- `FsmModule`
- `GameObjectPoolModule`
- `GameTimer`
- `InputModule`
- `LocalizationModule`
- `MonoDriver`
- `ObjectPoolModule`
- `ProcedureModule`
- `ResourceModule`
- `SceneModule`
- `SensitiveWordModule`

## HotFix Facade

`GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs` 是业务层常用模块入口。已确认暴露的常用属性包括：

- `RootModule`
- `FsmModule`
- `SensitiveWordModule`
- `AnimModule`
- `ResourceModule`
- `AudioModule`
- `SceneModule`
- `GameTimerModule`
- `DGame.IInputModule InputModule`
- `GameLogic.IInputModule Input`
- `LocalizationModule`
- `GameObjectPool`

在 HotFix 业务代码中，优先使用 `GameModule`，只有在封装 `GameModule` 本身或写底层基础设施时才直接碰 `ModuleSystem.GetModule<T>()`。

## GameLogic Modules

`GameUnity/Assets/Scripts/HotFix/GameLogic/Module` 下已确认的业务模块：

- `InputModule`
- `LocalizationModule`
- `RedDotModule`
- `SingletonSystem`
- `TextModule`
- `UIModule`

常见配套目录：

- `Common/FrameSprite`
- `DataCenter`
- `DataMgr`
- `NetworkMgr`
- `UI/GMPanel`
- `GameTickWatcher`

## Resource Access

资源能力来自 `IResourceModule`。已确认接口包含：

- `Initialize`
- `InitPackage`
- `ContainsAsset`
- `CheckLocationValid`
- `LoadAssetAsync`
- `LoadAsset`
- `UnloadAsset`
- `UnloadUnusedAssets`
- `ForceUnloadAllAssets`
- `ForceUnloadUnusedAssets`

结论：

- 异步资源加载基于 `UniTask`
- 资源后端基于 `YooAsset`
- 代码改动要同时考虑资源句柄、引用计数和回收路径

## Practical Rule

判断应修改哪一层时按这个顺序：

1. 只是调用已有能力：改 `GameLogic`
2. 需要新增业务模块包装：改 `GameLogic/Module`
3. 需要改基础资源/场景/音频/输入机制：改 `DGame/Runtime/Module`
4. 需要改网络接入与 Scene/Session 基础设施：看 `Fantasy.Unity`

