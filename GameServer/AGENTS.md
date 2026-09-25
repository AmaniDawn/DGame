# GameServer/AGENTS.md

本文件补充仓库根目录 [AGENTS.md](../AGENTS.md)，只适用于 `GameServer/` 服务端工程及其子目录。仓库级规则、用户优先级和通用编码准则继续有效。

## 工程事实

- 服务端解决方案是 `Server/Server.sln`，从仓库根目录执行命令时路径为 `GameServer/Server/Server.sln`。
- 服务端使用 .NET SDK 风格项目，当前 `Main`、`Entity`、`Hotfix` 三个项目目标框架为 `net8.0;net9.0`。
- 服务端分层为：`Server/Main` 负责启动入口，`Server/Entity` 负责 Entity、Component、共享模型和生成代码，`Server/Hotfix` 负责业务 System、Handler 和 Helper。
- Fantasy 启服配置位于 `Server/Entity/Fantasy.config`，配置模式说明位于同目录的 `Fantasy.xsd`。
- 协议源文件位于 `Tools/NetworkProtocol/Outer`、`Tools/NetworkProtocol/Inner`；导出工具位于 `Tools/ProtocolExportTool`，配置文件为 `ExporterSettings.json`。
- 配置生成代码位于 `Server/Entity/Generate/Config`，协议生成代码位于 `Server/Entity/Generate/NetworkProtocol`；客户端协议输出目录由导出配置指向 `GameUnity/Assets/Scripts/HotFix/GameProto/Generate/NetworkProtocol`。
- 配置运行时文件位于 `GameServer/Configs/`；配置源文件仍以仓库根目录 `GameConfig/` 为准。

当服务端协议、消息签名、连接流程或导出结果影响 Unity 客户端时，还必须读取 [GameUnity/AGENTS.md](../GameUnity/AGENTS.md)，并按其中的程序集、Fantasy Unity、生成产物和验证规则处理客户端侧输出。

## 技能路由

涉及 Fantasy 服务端、ECS Entity/Component/System、Scene/SubScene、FTask、Handler、事件、Timer、Address、Roaming、SphereEvent、Fantasy.config、数据库、HTTP、Session 或协议导出时，必须读取 [fantasy-net skill](../.agents/skills/fantasy-net/SKILL.md) 及对应 reference。涉及客户端连接、Session、消息 Handler 或 Unity 协议生成时，同时读取 [GameUnity/AGENTS.md](../GameUnity/AGENTS.md)。

涉及 Luban 表结构、配置源、导表或生成配置时，同时读取 [luban-dev skill](../.agents/skills/luban-dev/SKILL.md)；运行时消费链路再按需要读取 `dgame-dev`。

## 目录与修改约束

1. `Server/Main` 只保留启动和进程入口逻辑；启动程序集通过 `AssemblyHelper.Initialize()` 触发引用程序集初始化，再由 `Fantasy.Platform.Net.Entry.Start(...)` 启动框架。
2. Entity 数据与业务逻辑分离：Entity、Component 和共享模型放 `Server/Entity`，System、Handler、Helper 放 `Server/Hotfix`；不要把业务逻辑塞入生成文件或启动入口。
3. Fantasy 异步代码使用 `FTask` 及其相关 API；不要在服务端新增 `Task` 工作流替代 Fantasy 生命周期和调度机制。
4. Source Generator 生成的 `Server/Entity/Generate/**`、协议导出产物和配置导出产物不得手改；应修改协议、配置源或模板后重新生成。
5. `Fantasy.config` 的 machine、process、world、scene、端口和数据库关系必须与实际部署拓扑一致；修改前先核对 `references/config*.md` 以及当前源码调用点。
6. 协议修改必须同时核对 Outer/Inner 归属、服务端 Handler、客户端输出目录和导出结果；不要只改单侧生成代码。
7. 敏感信息、数据库连接字符串、运行时鉴权信息不得写入日志、报告或提交内容。

## 验证入口

从仓库根目录执行：

```powershell
dotnet build GameServer/Server/Server.sln --nologo
```

协议变更先检查 `GameServer/Tools/ProtocolExportTool/ExporterSettings.json` 和协议源目录，再使用项目现有导出工具生成并检查服务端、客户端产物。配置变更按 `luban-dev` 的 validate/preview/apply 流程执行；生产导表和真实部署需要明确授权。

无法连接外部 Fantasy 服务、缺少 .NET SDK、协议导出工具或测试没有可运行用例时，应记录实际阻塞原因，不使用其他工程或旧结果替代。

## 冲突与文档维护

reference 与当前 Fantasy 源码 API、配置节点、协议签名或目录不一致时，先用 `rg` 核对实际实现和调用点，优先信任当前源码，并在回复中记录冲突位置、正确事实和修正建议。
