---
name: dgame-dev
description: 开发和维护 DGame Fantasy 仓库时使用。适用于客户端与服务端架构分析、Unity Runtime/AOT/Editor 代码、HotFix/GameLogic/GameBattle/GameProto、Main/Entity/Hotfix 服务端工程、UI 系统、NetworkMgr 与客户端通信、服务器协议与路由、GameConfig/Luban 配置生成、自动生成链路、发布工具链、项目内调试、重构与规范约束判断。
---

# DGame 开发

按照仓库内已经存在的分层和约定开发 DGame，不要把它当成普通 Unity 项目随意落代码。

当需求涉及文件应该放在哪里、由哪一层负责、应该修改哪个程序集时，先读取 `references/project-map.md`。
当需求涉及客户端目录主线、程序集主干依赖、`GameLogic/GameProto/GameBattle` 归属、`NetworkMgr` 落位或客户端代码默认修改位置时，先读取 `references/client-dev.md`。
当需求涉及配置表、Luban、Excel 源表、生成脚本或配置产物链路时，先读取 `references/luban-game-config-codex.md`。
当需求涉及 `GameServer/Tools` 目录结构、服内协议、客户端与服务端协议、路由定义或协议导出工具使用方式时，先读取 `references/proto-message-define.md`。
当需求涉及客户端如何发起请求、接收响应、处理服务器推送、组织 `NetworkMgr` 或梳理 UI 到网络层的交互职责时，先读取 `references/client-server-communication-codex.md`。
当需求涉及客户端分层、启动链路、模块职责、UI 架构或 HotFix 与 Runtime 的协作边界时，先读取 `references/client-architecture-codex.md`。
当需求涉及服务端分层、启动链路、`Main/Entity/Hotfix` 工程职责、Scene 组织、Handler 落位或服务端代码归属时，先读取 `references/server-architecture.md`。
当需求涉及热更代码开发、HotFix 程序集划分、`GameStart`、Procedure 流程、HybridCLR、AOT 泛型补充或热更开发流程时，先读取 `references/client-hotfix-development-codex.md`。
当需求涉及帧同步相关的定点数学、定点物理、确定性随机数、`FixedPointPhysics`、`GameBattle` 中的位置/旋转/碰撞建模时，先读取 `references/frame-sync-fixedpoint-foundation-codex.md`。
当需求涉及热更资源包管理、资源版本更新、下载器、缓存清理、YooAsset 或 `GameModule.ResourceModule` 的使用方式时，先读取 `references/client-hotpatch-development-codex.md`。
当需求涉及资源管理、资源寻址、Sprite/Prefab/普通 Asset 加载与卸载、资源查询或 `GameModule.ResourceModule` 的使用方式时，先读取 `references/client-resource-management-codex.md`。
当需求涉及客户端模块职责、`GameModule`、模块应该从哪里获取、某个系统该依赖哪个模块时，先读取 `references/client-modules-codex.md`。
当需求涉及客户端 UI 开发、`UIWindow`、`UIWidget`、UIModule、列表与循环列表、Spine/粒子/序列帧 Widget、`IUIController`、`SwitchPageMgr` 或子页面体系时，先读取 `references/client-ui-development-codex.md`。
当需求涉及客户端红点开发、红点节点刷新、`RedDotModule`、`RedDotItem`、`RedDotPathDefine` 或 UI 红点接入时，先读取 `references/client-reddot-development-codex.md`。
当需求涉及代码规范、命名、UI 节点命名、异步编程、日志、中文文本、代码审查或 Git 工作流时，先读取 `references/client-conventions-codex.md`。
当需求涉及服务端命名、异步编程、日志、错误码、Scene 脚本落位、Handler/Helper/System 设计、代码审查或 Git 工作流时，先读取 `references/server-conventions.md`。
当需求涉及客户端事件系统、接口事件、UI 事件监听、`GameEventDriver`、`EEventGroup`、事件定义规范或事件排障时，先读取 `references/client-event-system-codex.md`。

若一个任务同时横跨客户端架构、网络通信、配置生成和服务端协议链路，按下面顺序建立上下文：

1. `references/project-map.md`
2. `references/client-architecture-codex.md` 或 `references/server-architecture.md`
3. `references/client-server-communication-codex.md` 或 `references/proto-message-define.md`
4. `references/luban-game-config-codex.md`
5. 对应端的 conventions 文档

## 工作规则

编辑前先检查目标区域。若现有 DGame 模块或 HotFix 程序集已经负责该行为，不要额外发明新层级。

优先做符合现有分层的最小改动：

- 框架能力和运行时代码放到 `GameUnity/Assets/DGame/Runtime`。
- Unity 编辑器工具代码放到 `GameUnity/Assets/DGame/Editor`。
- 热更业务和玩法功能代码放到 `GameUnity/Assets/Scripts/HotFix/GameLogic`。
- 可复用的 HotFix 基础代码放到 `GameUnity/Assets/Scripts/HotFix/GameBase`。
- 配置、协议、生成数据相关修改根据性质放到 `GameUnity/Assets/Scripts/HotFix/GameProto` 或 `GameConfig`。

始终考虑 TEngine 继承关系。新增服务或系统前，先确认 DGame 是否已经对原有能力做了二次封装或替换，例如 `GameTimer`、`ILocalizationModule`、`MemoryCollector`、`InputModule`、`AnimModule`、对象池或事件相关封装。

## 执行流程

1. 修改前先判断归属层级和程序集。
2. 先阅读同模块附近实现和已有模式。
3. 实现最小但完整的改动。
4. 在当前环境允许的范围内验证编译或资源生成影响。
5. 明确说明哪些校验仍然需要在 Unity 编辑器或生成工具里完成。

## 常见任务

### 运行时或框架层开发

在 `GameUnity/Assets/DGame/Runtime` 中处理。新增底层能力前，优先复用 `Core` 和 `Module` 里已有模块。

### 编辑器工具开发

在 `GameUnity/Assets/DGame/Editor` 中处理。优先沿用现有工具分组，例如工具栏、发布、HybridCLR、Luban、Spine、设置辅助等，不要新建无关的编辑器目录。

### HotFix 玩法与 UI 开发

在 `GameUnity/Assets/Scripts/HotFix/GameLogic` 中处理。玩法功能、UI、数据中心、红点系统、GM、序列帧和工具类都尽量落在现有 `GameLogic` 子目录中。

### 配置、数据与代码生成

先检查 `GameConfig` 和 `Tools`。若需求涉及生成内容，要区分源表、生成工具和生成后的 C# 消费端；除非任务明确要求，否则不要手改生成产物。

涉及 `GameConfig` 时，先确认本次修改属于以下哪一类：

- 修改 Excel 数据表内容。
- 修改 `__tables__`、`__beans__`、`__enums__` 等 schema 定义。
- 修改 Luban 配置、模板或生成脚本。
- 修改生成后的消费代码或加载链路。

### 服务端业务开发

先检查 `references/server-architecture.md` 和 `references/server-conventions.md`。服务端开发优先按 `Main / Entity / Hotfix` 三层判断归属：

- 宿主启动、日志装配放 `GameServer/Server/Main`
- 共享组件、基础定义、生成消费放 `GameServer/Server/Entity`
- 可热更的服务端业务逻辑放 `GameServer/Server/Hotfix`

### 协议与网络链路开发

先检查 `references/proto-message-define.md` 和 `references/client-server-communication-codex.md`。处理协议和网络链路时优先遵循：

- 改协议定义先回 `GameServer/Tools/NetworkProtocol`
- 改客户端网络流程先看 `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr`
- 改服务端协议处理先看 `GameServer/Server/Hotfix/Scene/<Scene>/Handler`
- 生成目录默认视为自动生成内容，不手改

## 校验方式

优先选择成本最低但有效的检查方式：

- 纯文件修改时，先检查邻近代码和引用是否一致。
- 涉及 C# 逻辑时，如果环境允许，运行本地构建或定向编译。
- 涉及编辑器流程时，明确说明最终验证需要使用 Unity 2021.3.30f1c1 打开工程并走对应菜单、场景或 Inspector 流程。
- 涉及启动流程时，注意主启动场景位于 `GameUnity/Assets/Scenes/GameStart`。

## 输出要求

在这个仓库里完成任务时：

- 说明本次修改属于哪一层。
- 说明对生成文件、HybridCLR、Luban 或 Unity 专属校验的假设。
- 若运行时改动同时影响 HotFix、配置或编辑器侧，要明确标出跨层影响。
