# DGame 服务端架构指引（精简版）

当需求涉及服务端分层、启动流程、工程职责、Scene 组织、Handler 落位或服务端代码应该修改在哪一层时，先读本文件。

目标：让 Codex 用最短阅读成本理解当前仓库的服务端分层、工程依赖、启动链路、Scene 业务组织与默认落位规则。

## 一图理解

```text
Main 宿主层
  Main / Program / NLog / Fantasy.Platform.Net.Entry
  负责进程入口、日志初始化、宿主装配与框架启动
          ↓
Hotfix 业务层
  Hotfix / Scene / Handler / Helper / System
  负责协议处理、场景业务逻辑、组件行为扩展、业务辅助逻辑
          ↓
Entity 数据与共享层
  Entity / Scene / Shared / Generate / Defines / Luban
  负责基础实体、Scene 组件、共享模型、错误码、协议与配置生成结果
          ↓
基础设施层
  Fantasy.Net / Luban / NLog / .NET
  负责网络框架、配置读取基础设施、日志和运行时环境
```

## 核心约束

- 服务端主工程分为 `Main`、`Entity`、`Hotfix` 三层，职责应保持清晰。
- 只有 `Hotfix` 是服务端热更业务层；`Main` 和 `Entity` 都按常规主工程程序集理解，不作为热更修改入口。
- `Main` 只负责宿主启动、日志初始化和框架装配，不承载业务逻辑。
- `Entity` 负责基础实体、Scene 组件、共享模型、错误码、协议/配置生成产物和底层共享数据。
- `Hotfix` 负责具体业务逻辑、消息处理、Scene 业务系统和业务辅助逻辑。
- 业务处理应优先落在 `Hotfix`，不要把复杂业务直接写进 `Main` 或生成目录。
- 自动生成目录只消费，不手改；定义和生成规则应回到源头目录处理。

### 工程依赖约束

- `Main` 依赖 `Hotfix` 和 `Entity`。
- `Hotfix` 依赖 `Entity`。
- `Entity` 作为服务端共享层，不依赖 `Hotfix`。

这意味着：

- 工程依赖链路应理解为 `Main -> Hotfix / Entity`，`Hotfix -> Entity`。
- 启动相关改动优先放 `Main`。
- 共享数据结构、Scene 组件和生成产物相关改动优先放 `Entity`。
- 协议 Handler、场景业务流程、业务 Helper 和业务 System 优先放 `Hotfix`。
- 新增 NuGet 第三方包时，优先评估是否应加在 `Entity`；因为 `Main` 和 `Hotfix` 都会引用 `Entity`，公共依赖放在 `Entity` 更容易复用和统一维护。

## 工程与归属

| 工程 | 路径 | 主要职责 |
| --- | --- | --- |
| `Main` | `GameServer/Server/Main/` | 进程入口、日志初始化、Fantasy.Net 启动、宿主装配 |
| `Entity` | `GameServer/Server/Entity/` | 基础实体、Scene 组件、共享模型、错误码、配置与协议生成结果 |
| `Hotfix` | `GameServer/Server/Hotfix/` | 唯一的服务端热更业务层，负责消息 Handler、业务 Helper、Scene System |

### 默认归属判断

- 宿主启动、程序入口、日志配置：`Main`
- Scene 组件、共享模型、基础定义、配置/协议生成消费：`Entity`
- 协议处理、登录流程、玩家业务、场景逻辑：`Hotfix`

额外约束：

- 如果需求强调“需要支持热更”，默认只能落在 `Hotfix`。
- `Main` 和 `Entity` 的改动应按常规服务端程序集改动理解，不属于热更业务修改。

## 启动链路

### 1. 进程入口

服务端启动入口位于 `GameServer/Server/Main/Program.cs`。

当前主链路为：

1. `AssemblyHelper.Initialize()`
2. 创建 `Fantasy.NLog("Server")`
3. 调用 `Fantasy.Platform.Net.Entry.Start(logger)`

可以先这样理解：

- `AssemblyHelper.Initialize()` 用于强制加载引用程序集，确保 `ModuleInitializer` 正常执行。
- `Fantasy.NLog("Server")` 负责初始化服务端日志基础设施。
- `Fantasy.Platform.Net.Entry.Start(logger)` 负责真正启动 Fantasy.Net 宿主。

结论：

- 服务端启动入口只有一处，启动流程相关修改优先落在 `Main/Program.cs`。
- 不要在 `Hotfix` 中另起一套进程启动链路。

### 2. 程序集装配关系

从项目引用关系可以确认当前装配方式：

- `Main.csproj` 引用 `Entity.csproj`
- `Main.csproj` 引用 `Hotfix.csproj`
- `Hotfix.csproj` 引用 `Entity.csproj`

这说明：

- `Main` 是服务端最终宿主装配层，直接引用 `Hotfix` 和 `Entity`。
- `Hotfix` 依赖 `Entity` 提供的基础类型、Scene 组件和生成结果。
- `Entity` 是服务端共享层，不反向依赖 `Hotfix`。

## Entity 共享层结构

`GameServer/Server/Entity/` 当前可按下面方式理解：

- `Defines/`
  放错误码、锁类型等基础定义

- `Generate/`
  放服务端消费的协议和配置生成结果，以及配置加载入口

- `Luban/`
  放配置读取依赖代码

- `Scene/`
  放各 Scene 的组件与模型定义

- `Shared/`
  放跨 Scene 共享组件

### 当前关键目录职责

- `Defines/ErrorCode.cs`
  服务端错误码定义入口

- `Generate/ConfigSystem.cs`
  服务端配置加载入口，由 `GameConfig` 通过工具自动复制或生成

- `Generate/NetworkProtocol/`
  服务端协议生成结果目录

- `Scene/Authentication/`
  认证场景相关组件与模型

- `Scene/Gate/`
  Gate 场景相关组件与模型

- `Shared/`
  共享组件，例如 `AccountJwtComponent`、`SensitiveWordComponent`

### Entity Scene 目录规则

`Entity/Scene/` 的目录划分规则应与 `Hotfix/Scene/` 保持一致，统一按功能域先划分主目录。

当前可按下面方式理解：

- `Entity/Scene/Gate/`
  放 Gate 场景相关组件、模型和数据结构

- `Entity/Scene/Authentication/`
  放 Authentication 场景相关组件、模型和数据结构

- 后续若新增大厅类场景，应优先创建 `Entity/Scene/Hall/`

结论：

- `Hotfix` 的 Scene 目录怎么按功能域划分，`Entity` 的 Scene 目录就按同样规则划分。
- 不要在 `Entity/Scene/` 下按脚本类型横向混放不同场景的代码。
- Scene 相关实体组件、模型和数据结构，优先落到对应功能域目录。

结论：

- Scene 组件、共享数据和基础定义优先沉到 `Entity`。
- `Entity/Generate/` 下的内容属于生成消费层，不允许手动修改。
- 涉及 `Generate/` 目录的改动，优先回到定义源头并执行自动生成流程，不要直接改生成文件。

## Hotfix 业务层结构

`GameServer/Server/Hotfix/` 当前的主要业务组织方式是按 Scene 拆分：

```text
Hotfix/Scene/
├── Authentication/
│   ├── Handler/
│   ├── Helper/
│   └── System/
├── Gate/
│   ├── Handler/
│   ├── Helper/
│   └── System/
├── Shared/
│   ├── Helper/
│   └── System/
└── OnSceneCreate_Init.cs
```

### Scene 分层职责

- `Handler/`
  负责协议入口处理和请求响应收口

- `Helper/`
  负责业务辅助逻辑、扩展方法和复用型业务工具

- `System/`
  负责组件行为扩展、业务系统逻辑和实体生命周期协作

### 当前已确认的组织模式

从现有目录和代码可以确认：

- 协议 Handler 放在 `Hotfix/Scene/<Scene>/Handler/`
- 业务辅助逻辑放在 `Hotfix/Scene/<Scene>/Helper/`
- 场景组件行为和系统逻辑放在 `Hotfix/Scene/<Scene>/System/`
- 通用跨场景逻辑放在 `Hotfix/Scene/Shared/`
- 各个 Scene 下的脚本落点，优先按功能域划分主文件夹，再在该目录下继续拆分 `Handler`、`Helper`、`System`
- `Gate` 相关逻辑优先落在 `Scene/Gate/`
- `Authentication` 相关逻辑优先落在 `Scene/Authentication/`
- 后续若新增大厅类场景，应优先创建 `Scene/Hall/`，再按相同规则继续组织目录

结论：

- 新增协议处理器时，优先进对应 Scene 的 `Handler/`
- 复用型业务逻辑优先进入 `Helper/`
- 组件行为扩展和系统逻辑优先进入 `System/`
- 新增场景能力时，先按功能域建立对应 Scene 主目录，再在目录内部细分脚本落点
- `Hotfix/Scene/` 的功能域划分规则应与 `Entity/Scene/` 保持一致，避免两侧目录命名和归属不一致
- 不要把复杂业务全部塞进单个 Handler

## Scene 初始化链路

Scene 初始化入口位于：

- `GameServer/Server/Hotfix/Scene/OnSceneCreate_Init.cs`

当前初始化方式：

- 在 `OnCreateScene` 事件中，根据 `scene.SceneType` 给不同 Scene 挂载对应组件

当前已能确认的初始化职责：

- `Authentication` Scene 会挂载：
  - `AccountManagerComponent`
  - `AccountJwtComponent`

- `Gate` Scene 会挂载：
  - `AccountJwtComponent`
  - `PlayerManagerComponent`

这说明：

- Scene 的运行时能力不是散落在 Handler 中，而是通过 Scene 创建时统一挂载组件完成。
- 如果某个 Scene 需要新增基础业务能力，优先考虑是否应在 `OnSceneCreate_Init` 中挂载新组件。

## 典型业务链路

### Authentication 场景

参考：

- `Hotfix/Scene/Authentication/Handler/C2A_LoginRequestHandler.cs`

从现有处理流程可以看出：

- Handler 负责接收请求、调用 `scene` 业务方法、组装响应并设置会话生命周期
- 配置数据直接通过 `TbServerConfig`、`TbFuncParamConfig` 等生成结果消费
- 账号信息查询和最近登录角色信息查询通过 Scene 组件协作完成

结论：

- Handler 只做协议入口、参数处理、错误码返回和流程编排
- 复杂逻辑应继续下沉到 Scene 组件、Helper 或 System

### Gate 场景

参考：

- `Hotfix/Scene/Gate/Handler/C2G_LoginRequestHandler.cs`

从现有处理流程可以看出：

- Handler 先做参数和 Token 校验
- 再通过 Scene 组件处理账号在线态、重连、顶号和玩家数据创建/上线
- 最终返回 `PlayerData` 并把 Session 和玩家数据关联

结论：

- Gate Handler 承担网关入口校验和业务编排
- 玩家状态、缓存、在线流程和 Session 绑定逻辑应通过组件系统协作完成

## 配置与协议协作

### 服务端配置消费

服务端配置入口位于：

- `GameServer/Server/Entity/Generate/ConfigSystem.cs`

当前配置消费方式可以这样理解：

- `Tables` 懒加载
- 通过 `LoadByteBuf` 从 `Configs/Binary/{file}.bytes` 读取配置
- 配置消费代码属于 `Entity/Generate`
- `ConfigSystem.cs` 本身也来自 `GameConfig` 侧工具流程，不作为服务端手写源码维护

结论：

- 改服务端配置读取逻辑，先看 `Entity/Generate/ConfigSystem.cs`
- `ConfigSystem.cs` 的源头在 `GameConfig`，需要调整时应回到 `GameConfig` 修改并重新走工具流程
- 改配置源头、生成模板或生成规则，应回到 `GameConfig`
- `Entity/Generate/Config/` 下的生成文件只消费，不允许手动修改
- 配置生成结果如需更新，优先执行自动生成流程

### 服务端协议消费

服务端协议生成目录位于：

- `GameServer/Server/Entity/Generate/NetworkProtocol/`

这里承接的是服务端消费的协议生成结果，例如：

- `OuterMessage.cs`
- `OuterOpcode.cs`
- `RouteType.cs`

结论：

- 协议定义应回到 `GameServer/Tools/NetworkProtocol/`
- 服务端只消费 `Entity/Generate/NetworkProtocol/` 中的生成结果
- 不允许直接修改协议生成文件
- 协议生成结果如需更新，优先执行自动生成流程

## 使用原则

1. 先判断需求属于 `Main`、`Entity`、`Hotfix` 还是协议/配置源头目录。
2. `Main` 只改宿主启动和日志装配，不承载业务逻辑。
3. `Entity` 负责共享层、组件层和生成消费层，不承载具体协议业务流程。
4. `Hotfix` 负责服务端业务逻辑，按 `Scene -> Handler / Helper / System` 组织实现。
5. Scene 基础组件优先在 `OnSceneCreate_Init` 中挂载，不要在各处零散初始化。
6. 新增 Handler 时，先找对应 Scene，再决定进入 `Handler`、`Helper` 还是 `System`。
7. 配置和协议生成结果只消费，不手改；改定义和生成规则要回到源头目录。
8. `Generate/` 目录默认视为自动生成目录，不允许手动修改；需要变更时优先执行自动生成流程。
9. 涉及配置读取时，先看 `Entity/Generate/ConfigSystem.cs`；涉及协议定义时，先看 `GameServer/Tools/NetworkProtocol/`。

## 常见落位

- 调整服务启动流程：`GameServer/Server/Main/`
- 新增错误码或基础定义：`GameServer/Server/Entity/Defines/`
- 新增 Scene 组件或共享组件：`GameServer/Server/Entity/Scene/`、`GameServer/Server/Entity/Shared/`
- 修改服务端配置消费：`GameServer/Server/Entity/Generate/`
- 新增认证场景 Handler：`GameServer/Server/Hotfix/Scene/Authentication/Handler/`
- 新增 Gate 场景 Handler：`GameServer/Server/Hotfix/Scene/Gate/Handler/`
- 新增业务辅助逻辑：`GameServer/Server/Hotfix/Scene/<Scene>/Helper/`
- 新增组件行为扩展或业务系统：`GameServer/Server/Hotfix/Scene/<Scene>/System/`

若需求同时跨越 `Entity`、`Hotfix`、协议生成或配置生成链路，输出时必须明确标出跨层影响，避免只改单层导致运行时行为不一致。