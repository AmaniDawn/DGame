# Editor Workflow

## Confirmed Editor Areas

`GameUnity/Assets/DGame/Editor` 下已确认存在：

- `AtlasMakerTools`
- `DefineSymbols`
- `HybridCLR`
- `Inspector`
- `LubanTools`
- `Odin`
- `OpenFolder`
- `ReleaseTools`
- `Settings`
- `SpineModelHelper`
- `UnityToolBarExtend`
- `Utility`

`GameUnity/Assets/Editor` 下还有项目级工具，如：

- `DGameSettingsProvider`
- `FrameImportHelper`
- `ReferenceFinder`
- `ShaderVariantCollection`
- `TextDefineEditor`
- `UIScriptGenerator`

## Build And Release

已确认 `ReleaseTools` 提供菜单命令和打包流程，包含：

- 构建 AssetBundle
- 调用 `BuildDllCommand.BuildAndCopyDlls()`
- 拷贝 StreamingAssets
- 使用 YooAsset 构建参数

因此如果任务涉及：

- 打包失败
- AB 输出目录
- StreamingAssets 复制
- 热更 DLL 没有产出
- Jenkins 构建

优先阅读：

- `DGame/Editor/ReleaseTools`
- `DGame/Editor/HybridCLR`
- `DGame/Editor/Settings`

## Luban And Generated Content

项目存在 `LubanTools`，说明配置表生成已经有固定入口。生成相关问题不要只改输出代码，还要检查：

- 编辑器菜单或生成脚本
- 输出目录是否仍指向 `GameProto` 或其他约定位置
- 生成后引用代码是否同步

## Inspector And Project Settings

已确认存在多个自定义 Inspector，例如：

- `RootModuleInspector`
- `ProcedureSettingsInspector`
- `UpdateSettingsInspector`

如果用户要求“某个字段在 Inspector 里不生效”或“需要增加编辑器配置项”，优先检查：

1. 运行时数据对象或组件字段
2. 对应 Inspector 是否覆写了默认绘制
3. 是否存在 Settings/Provider 单独维护配置入口

## Unity MCP Workflow

当前环境支持 Unity MCP。涉及下面场景时优先使用它：

- 创建或修改场景节点
- 创建材质、Prefab、组件挂载
- 检查 Console 报错
- 执行 Unity 测试

推荐顺序：

1. `scene-list-opened` / `scene-get-data`
2. `gameobject-find`
3. `gameobject-component-get`
4. `gameobject-component-modify` 或 `gameobject-create`
5. `console-get-logs` / `tests-run`

