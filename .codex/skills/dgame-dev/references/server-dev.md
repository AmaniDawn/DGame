# DGame 服务端开发

## 服务端目录主线

`GameServer` 可以按四块理解：

- `Configs`
  服务端消费的配置产物，分 `Binary` 和 `Json`

- `Server`
  服务端主工程

- `Tools`
  共享协议定义与导出工具中心

- `GameUnity`
  与服务端流程有关的一部分 Unity 侧协议生成目录

## 服务端启动入口

参考：

- `GameServer/Server/Main/Program.cs`

现有启动链路：

1. `AssemblyHelper.Initialize()`
2. 创建 `Fantasy.NLog("Server")`
3. `Fantasy.Platform.Net.Entry.Start(logger)`

结论：

- 启动宿主、日志初始化、进程入口修改，优先在 `Main`

## Server 分层

### Main

职责：

- 进程启动
- 日志初始化
- 宿主装配

### Entity

职责：

- 基础实体
- 配置生成结果
- 共享模型
- Scene 相关模型定义

配置消费入口参考：

- `GameServer/Server/Entity/Generate/ConfigSystem.cs`

### Hotfix

职责：

- 服务端业务逻辑层
- 按 Scene 再拆分为 `Handler`、`Helper`、`System`

参考：

- `GameServer/Server/Hotfix/Scene/Authentication/Handler/C2A_LoginRequestHandler.cs`

从现有代码看：

- 协议 Handler 放在 `Hotfix/Scene/<Scene>/Handler`
- 业务辅助逻辑放在 `Helper`
- 场景业务系统放在 `System`

结论：

- 新增消息处理器时，优先进对应 Scene 的 `Handler`
- 不要把复杂业务全部塞进单个 Handler

## 服务端配置消费

参考：

- `GameServer/Server/Entity/Generate/ConfigSystem.cs`

现有实现说明：

- `Tables` 懒加载
- 从 `Configs/Binary/{file}.bytes` 读取配置
- 生成代码属于 `Entity/Generate`

结论：

- 改服务端配置读取逻辑，先看 `Entity/Generate/ConfigSystem`
- 改配置源头，先回到 `GameConfig`

## 常见任务落点

| 常见任务 | 优先修改哪里 |
|---------|---------|
| 调整服务启动流程 | `GameServer/Server/Main` |
| 新增实体或共享模型 | `GameServer/Server/Entity` |
| 新增服务端配置消费逻辑 | `GameServer/Server/Entity/Generate` |
| 新增登录/认证/网关 Handler | `GameServer/Server/Hotfix/Scene/.../Handler` |
| 新增业务辅助逻辑 | `GameServer/Server/Hotfix/Scene/.../Helper` |
| 新增业务系统 | `GameServer/Server/Hotfix/Scene/.../System` |
