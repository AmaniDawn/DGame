# Architecture

## Overview

当前项目的 Unity 客户端主体位于 `GameUnity/`，核心分层如下：

- `GameUnity/Assets/DGame/Runtime`
  DGame 运行时框架层，包含 Core、Module、Setting。
- `GameUnity/Assets/DGame/Editor`
  DGame 编辑器工具层，包含 HybridCLR、Luban、ReleaseTools、Toolbar、OpenFolder 等。
- `GameUnity/Assets/Scripts/HotFix/GameLogic`
  HotFix 业务逻辑层，包含 UI、模块、数据、网络、战斗、公共组件。
- `GameUnity/Assets/Scripts/HotFix/GameProto`
  协议/配置相关程序集。
- `GameUnity/Assets/Scripts/HotFix/Fantasy.Unity`
  Fantasy Unity 侧运行时与编辑器接入代码。
- `GameUnity/Assets/Editor`
  项目级编辑器工具和辅助窗口。

## Assembly Map

已确认存在的主要程序集定义：

- `GameUnity/Assets/DGame/Runtime/DGame.Runtime.asmdef`
- `GameUnity/Assets/DGame/Editor/DGame.Editor.asmdef`
- `GameUnity/Assets/DGame.AOT/DGame.AOT.asmdef`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameLogic.asmdef`
- `GameUnity/Assets/Scripts/HotFix/GameProto/GameProto.asmdef`
- `GameUnity/Assets/Scripts/HotFix/Fantasy.Unity/Fantasy.Unity.asmdef`
- `GameUnity/Assets/Scripts/HotFix/GameBattle/GameBattle.asmdef`

做改动时先判断目标属于哪一个程序集，避免把 Runtime 层依赖反向拉到 HotFix 层，或把业务逻辑塞进 Editor 程序集。

## Scene And Entry

已确认的场景/入口资源：

- `GameUnity/Assets/Scenes/GameStart/GameStart.unity`
- `GameUnity/Assets/Scenes/SceneUI.unity`
- `GameUnity/Assets/DGame/Runtime/Setting/Prefabs/GameEntry.prefab`

运行时基础单例入口由 `RootModule` 提供。`RootModule` 负责：

- 初始化字符串、日志、JSON helper
- 设置帧率、TimeScale、后台运行与休眠策略
- 处理低内存回收
- 驱动部分全局基础设施

如果任务和“启动顺序”有关，优先检查：

1. `RootModule`
2. `GameEntry.prefab`
3. 场景中的 `UIRoot`
4. Procedure/UpdateSettings/ResourceModule 相关配置

## Layer Boundaries

建议按下面边界理解代码：

- Runtime 层
  提供底层模块接口、基础工具、资源/场景/音频/输入/计时器等基础能力。
- HotFix GameLogic 层
  提供业务封装，如 `GameModule`、UI 框架、红点、文本、存档、数据中心、网络业务逻辑。
- Fantasy.Unity 层
  提供网络运行时基础设施与框架接入。
- Editor 层
  提供菜单命令、打包流程、生成工具和检查器扩展。

## Navigation Tips

遇到下列任务时优先看这些目录：

- 模块能力：`GameUnity/Assets/DGame/Runtime/Module`
- 业务模块封装：`GameUnity/Assets/Scripts/HotFix/GameLogic/Module`
- UI 框架：`GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule`
- 数据/网络：`GameUnity/Assets/Scripts/HotFix/GameLogic/DataCenter`、`NetworkMgr`
- 编辑器构建：`GameUnity/Assets/DGame/Editor/ReleaseTools`
- 热更/程序集构建：`GameUnity/Assets/DGame/Editor/HybridCLR`

