# DGame 协议定义与导出指引

当需求涉及 `GameServer/Tools` 目录结构、服务器与服务器之间通讯协议、服务器与客户端之间通讯协议、路由定义或协议导出工具使用方式时，先阅读本文件。

本文档用于说明协议定义目录、协议组织方式、生成产物边界以及协议导出流程。处理协议相关任务时，优先遵循本文档中的目录职责和生成规则，不要直接修改自动生成文件。

## 目录导航

- [DGame 协议定义与导出指引](#dgame-协议定义与导出指引)
  - [目录导航](#目录导航)
  - [整体结构](#整体结构)
  - [协议定义目录](#协议定义目录)
    - [Inner 服内协议](#inner-服内协议)
    - [Outer 客户端与服务端协议](#outer-客户端与服务端协议)
    - [RouteType.Config 路由定义](#routetypeconfig-路由定义)
    - [RoamingType.Config 与 OpCode.Cache](#roamingtypeconfig-与-opcodecache)
  - [协议定义规则](#协议定义规则)
    - [基础 proto 约定](#基础-proto-约定)
    - [enum 定义示例](#enum-定义示例)
    - [消息类型标记](#消息类型标记)
    - [命名规则](#命名规则)
    - [字段变更注意事项](#字段变更注意事项)
  - [协议导出工具](#协议导出工具)
    - [ProtocolExportTool 目录](#protocolexporttool-目录)
    - [ExporterSettings.json](#exportersettingsjson)
    - [服务端生成路径](#服务端生成路径)
    - [客户端生成路径](#客户端生成路径)
    - [Run.bat 与 Run.sh](#runbat-与-runsh)
  - [推荐工作顺序](#推荐工作顺序)
  - [使用原则](#使用原则)

## 整体结构

协议定义与导出相关内容集中在：

```text
GameServer/Tools/
├── NetworkProtocol/      # 协议定义源目录
└── ProtocolExportTool/   # 协议导出工具目录
```

两者职责应明确区分：

- `NetworkProtocol/` 负责维护协议定义源文件、路由配置和协议辅助配置。
- `ProtocolExportTool/` 负责读取协议定义源，并将结果导出到服务端和客户端消费目录。

处理协议相关需求时，应先判断本次修改属于哪一类：

- 修改协议定义：进入 `NetworkProtocol/`
- 调整导出配置或执行导出：进入 `ProtocolExportTool/`
- 查看协议消费结果：进入服务端或客户端生成目录

## 协议定义目录

`GameServer/Tools/NetworkProtocol/` 的建议组织方式如下：

```text
GameServer/Tools/NetworkProtocol/
├── Inner/                     # 服务器与服务器之间通讯协议
│   ├── XxxInnerMessage.proto
│   └── YyyInnerMessage.proto
├── Outer/                     # 客户端与服务器之间通讯协议
│   ├── XxxMessage.proto
├── RouteType.Config           # 路由定义
├── RoamingType.Config         # 协议辅助定义
└── OpCode.Cache               # 导出辅助缓存
```

这里最重要的约束不是文件名本身，而是职责分层和组织方式：

- `Inner/` 只放服内协议。
- `Outer/` 只放客户端与服务端协议。
- 路由定义放在 `RouteType.Config`，不要混进 `.proto`。
- 协议辅助配置放在对应配置文件中，不要散落到生成目录里维护。

### Inner 服内协议

服务器与服务器之间通讯协议定义位于：

- `GameServer/Tools/NetworkProtocol/Inner/`

服内协议应遵循以下规则：

- 使用 `proto3` 语法。
- 包名按项目约定定义，并在同一类协议中保持一致。
- 支持通过注释指定协议序列化方式。
- 支持通过注释声明导出时需要附加的 `using` 命名空间。
- 按业务域或服务语义拆分成多个 `.proto` 文件。
- 不要把所有服内协议长期堆在单个文件中。

新增服内协议时，应优先判断：

- 是否应归入已有业务域的 `.proto`
- 是否需要新增新的业务域 `.proto`

如果需求涉及服务间 RPC、服内通知、服务间同步或内部消息定义，优先进入 `Inner/*.proto`。

### Outer 客户端与服务端协议

客户端与服务端之间通讯协议定义位于：

- `GameServer/Tools/NetworkProtocol/Outer/`

外部协议应遵循以下规则：

- 按业务域或服务语义拆分成多个 `.proto` 文件。
- 不要把所有外部协议都堆在单个文件中。
- 新增协议时，优先归入已有同域 `.proto`，只有在职责明显独立时再新增文件。

如果需求涉及登录协议、玩法协议、角色数据协议或客户端消息通知，优先进入 `Outer/*.proto`。

### RouteType.Config 路由定义

路由定义位于：

- `GameServer/Tools/NetworkProtocol/RouteType.Config`

路由定义应遵循以下规则：

- 路由号独立维护，不写在 `.proto` 文件中。
- `1000` 以内属于框架预留范围。
- 新增路由号时，应从 `1000` 以上开始分配。
- 路由值应保持唯一，避免和已有定义冲突。

### RoamingType.Config 与 OpCode.Cache

`NetworkProtocol/` 下还会包含协议辅助文件，例如：

- `RoamingType.Config`
- `OpCode.Cache`

处理这类文件时应遵循以下原则：

- `RoamingType.Config` 作为协议辅助配置使用。
- `OpCode.Cache` 属于导出辅助缓存或中间结果。
- 如无明确需求，不要直接手改 `OpCode.Cache`。
- 协议变更应优先回到 `.proto`、`RouteType.Config` 或导出配置源头处理。

## 协议定义规则

### 基础 proto 约定

协议文件统一使用：

- `syntax = "proto3";`

协议文件中应支持以下注释约定：

- 在 `message` 上方添加 `// Protocol ProtoBuf`
- 在 `message` 上方添加 `// Protocol MemoryPack`
- 通过 `// using ...` 声明导出时增加的自定义命名空间

这意味着：

- 序列化类型由协议定义源文件控制，而不是在生成目录中额外配置。
- 导出时附加命名空间也应通过协议定义源文件声明，而不是通过手改生成代码解决。

### enum 定义示例

协议中如果需要定义枚举，建议直接在对应业务域的 `.proto` 文件中维护，并与使用它的消息放在同一上下文内，便于阅读和导出。

示例：

```proto
syntax = "proto3";

package Fantasy.Network.Message;

enum LoginType
{
    LoginType_None = 0,
    LoginType_Guest = 1,
    LoginType_Account = 2,
}

message C2A_LoginRequest // IRequest,A2C_LoginResponse
{
    string UserName = 1;
    string Password = 2;
    LoginType LoginType = 3;
}
```

使用枚举时，建议遵循以下规则：

- `enum` 放在对应业务域的 `.proto` 文件中维护，不要放到生成目录中手改。
- 枚举名和枚举值前缀应保持可读性和唯一性，避免不同业务域之间语义冲突。
- 默认值使用 `0` 项，便于兼容 `proto3` 的默认值语义。
- 新增或调整枚举后，仍然需要通过 `ProtocolExportTool` 重新执行自动生成流程。

### 消息类型标记

消息类型和请求响应关系，统一通过 `message` 定义后的注释表达，例如：

```text
message C2A_LoginRequest // IRequest,A2C_LoginResponse
message A2C_LoginResponse // IResponse
message G2C_RepeatLogin // IMessage
```

推荐按以下规则理解：

- `IRequest`：请求消息
- `IResponse`：响应消息
- `IMessage`：普通通知或单向消息
- `IRequest,XXXResponse`：请求消息及其对应的响应类型

新增请求响应协议时，应保持这一表达方式，不要改成额外配置文件或其他注解形式。

### 命名规则

消息命名应保持方向前缀、服务语义和消息类型后缀一致。

常见形式包括：

- `C2A_`：客户端到认证服务器
- `A2C_`：认证服务器到客户端
- `C2G_`：客户端到 Gate 服务器
- `G2C_`：Gate 服务器到客户端

同时应保持以下习惯：

- 请求消息通常以 `Request` 结尾
- 响应消息通常以 `Response` 结尾
- 单向通知消息按业务语义命名
- 公共结构体和嵌套数据结构也放在对应 `.proto` 文件中统一维护

新增协议时，应保证：

- 同一业务域中的命名风格统一
- 不同服务方向的前缀规则统一
- 不在同一套协议中混用多种命名风格

### 字段变更注意事项

协议字段一旦进入使用阶段，变更时必须优先考虑兼容性，不要把字段调整当成普通重构处理。

特别注意：

- 协议声明时，字段编号必须从 `1` 开始按顺序递增，不可重复。
- 字段名和字段类型一旦确定后，不可轻易修改。
- 字段编号和字段声明顺序一旦进入稳定使用阶段，不可随意调整位置顺序。
- 新增字段通常可以前向兼容，旧版本会忽略无法识别的新字段。
- 删除字段通常不可前向兼容，容易导致旧版本或旧逻辑读取异常。
- 修改既有字段的语义、类型或编号，风险通常高于新增字段，应尽量避免。

测试要求：

- 协议变更后，需要先完成自动生成流程。
- 测试时应先在 Editor 模式下完成加载验证和格式验证。
- 确认客户端和服务端都能正常消费生成结果后，再继续后续联调或发布流程。

## 协议导出工具

### ProtocolExportTool 目录

`GameServer/Tools/ProtocolExportTool/` 中通常包含以下关键内容：

```text
GameServer/Tools/ProtocolExportTool/
├── ExporterSettings.json
├── Fantasy.ProtocolExportTool
├── Fantasy.ProtocolExportTool.dll
├── Fantasy.ProtocolExportTool.deps.json
├── Fantasy.ProtocolExportTool.runtimeconfig.json
├── Run.bat
├── Run.sh
├── runtimes/
└── zh-Hans/
```

可以按下面方式理解职责：

- `Fantasy.ProtocolExportTool.dll`：协议导出工具主体
- `ExporterSettings.json`：导出配置文件
- `Run.bat` / `Run.sh`：跨平台导出脚本入口
- `runtimes/`、`zh-Hans/`：工具运行依赖

### ExporterSettings.json

配置文件位于：

- `GameServer/Tools/ProtocolExportTool/ExporterSettings.json`

配置示例：

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "../NetworkProtocol",
            "Comment": "ProtoBuf文件所在的文件夹位置"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "../../Server/Entity/Generate/NetworkProtocol",
            "Comment": "ProtoBuf生成到服务端的文件夹位置"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "../../../GameUnity/Assets/Scripts/Hotfix/GameProto/Generate/NetworkProtocol",
            "Comment": "ProtoBuf生成到客户端的文件夹位置"
        }
    }
}
```

该配置主要用于指定：

- 协议源目录
- 服务端协议生成目录
- 客户端协议生成目录

如果需要调整协议导出路径，应优先修改 `ExporterSettings.json`，不要去改 `Run.bat` 或 `Run.sh` 中的执行逻辑。

### 服务端生成路径

服务端协议生成目录：

- `GameServer/Server/Entity/Generate/NetworkProtocol`

这里的文件用于服务端消费协议生成结果。

重要提示：

- 该目录下的文件属于自动生成产物，不要手动修改。
- 如果要修改已有协议或新增协议，必须先修改 `GameServer/Tools/NetworkProtocol/` 下的协议定义源文件。
- 修改源定义后，必须通过 `ProtocolExportTool` 执行自动生成流程，把变更同步到服务端生成目录。
- 不要直接在该目录中补字段、改消息名或新增消息；下次重新导出时，这些手改内容会被覆盖。

### 客户端生成路径

客户端协议生成目录：

- `GameUnity/Assets/Scripts/HotFix/GameProto/Generate/NetworkProtocol`

这里的文件用于客户端消费协议生成结果。

重要提示：

- 该目录下的文件属于自动生成产物，不要手动修改。
- 如果要修改已有协议或新增协议，必须先修改 `GameServer/Tools/NetworkProtocol/` 下的协议定义源文件。
- 修改源定义后，必须通过 `ProtocolExportTool` 执行自动生成流程，把变更同步到客户端生成目录。
- 不要直接在该目录中补字段、改消息名或新增消息；下次重新导出时，这些手改内容会被覆盖。

### Run.bat 与 Run.sh

导出脚本位于：

- Windows：`GameServer/Tools/ProtocolExportTool/Run.bat`
- macOS/Linux：`GameServer/Tools/ProtocolExportTool/Run.sh`

两个脚本应具备这些共同特征：

- 启动前检查本机是否安装 `dotnet`
- 要求 `.NET 8` 或更高版本
- 实际执行 `dotnet Fantasy.ProtocolExportTool.dll export --silent`
- 从 `ExporterSettings.json` 读取导出配置

可以把这两个脚本理解为协议导出的统一入口。日常执行协议生成时，优先直接使用这两个脚本，而不是手动拼接命令。

## 推荐工作顺序

处理协议相关开发时，推荐按下面顺序执行：

1. 先判断本次修改属于 `Inner` 还是 `Outer`。
2. 在对应 `.proto` 文件中修改已有协议，或按业务域新增 `.proto` 文件。
3. 如果涉及路由，补充或调整 `RouteType.Config`。
4. 如有必要，核对 `ExporterSettings.json` 的导出路径配置。
5. 执行 `Run.bat` 或 `Run.sh` 触发自动生成。
6. 检查服务端和客户端生成目录中的结果是否正确。

## 使用原则

处理协议定义和导出任务时，优先遵循以下原则：

1. 先改协议定义源文件，再走导出流程，不要直接手改生成结果。
2. 服内协议放 `Inner/`，客户端与服务端协议放 `Outer/`，不要混放。
3. 协议文件按业务域或服务语义拆分，不要把所有消息堆在单个文件中。
4. 路由定义统一维护在 `RouteType.Config`，不要把路由号散落到其他文件。
5. 协议序列化方式和 `using` 扩展优先通过 `.proto` 注释约定处理。
6. 如无明确需求，不要直接修改 `OpCode.Cache` 等导出辅助文件。
7. 调整导出路径时，先改 `ExporterSettings.json`，再核对消费侧目录和引用。
8. 导出失败时，先检查 `.NET` 版本、协议定义格式和 `ExporterSettings.json` 配置。