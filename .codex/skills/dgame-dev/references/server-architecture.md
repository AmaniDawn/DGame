# DGame 项目架构

## 项目总览

DGame Fantasy 的仓库主干分成三条核心主线：

- `GameConfig`
  配置源数据、Luban 元信息、导表脚本、自定义模板

- `GameServer`
  服务端工程、共享协议定义、协议导出工具、服务端配置消费

- `GameUnity`
  Unity 客户端工程、运行时框架、编辑器工具、热更业务

除此之外还有：

- `GameRelease`
  发布相关目录

- `Tools`
  仓库级工具或辅助资源

## 分层判断

碰到开发任务时先做这一层判断：

- 改“源数据或生成规则” -> `GameConfig`
- 改“客户端与服务端共享协议定义” -> `GameServer/Tools`
- 改“服务端业务实现” -> `GameServer/Server`
- 改“Unity 客户端框架或业务” -> `GameUnity`

## 关键入口

### 客户端热更入口

实际入口在：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`

从现有代码看，热更入口流程是：

1. 接收热更程序集列表
2. 初始化 `GameLogic.GameEventLauncher` 与 `GameBattle.GameEventLauncher`
3. 初始化语言设置
4. 初始化 `GameClient`
5. 打开 `MainLoginUI`

结论：

- 新增客户端启动行为时，优先接入 `GameStart`
- 不要绕开现有入口自建另一套初始化链路

### 服务端启动入口

实际入口在：

- `GameServer/Server/Main/Program.cs`

从现有代码看：

- 先调用 `AssemblyHelper.Initialize()`
- 再配置 `Fantasy.NLog`
- 最后启动 `Fantasy.Platform.Net.Entry.Start(logger)`

结论：

- 服务端宿主启动、日志初始化、主程序装配，优先落在 `Main`

## 真实代码约定

### 客户端模块访问

业务层已有统一入口：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs`

这个仓库的业务代码更适合这样访问模块：

- `GameModule.ResourceModule`
- `GameModule.GameTimerModule`
- `GameModule.LocalizationModule`

而不是在每个业务类里反复手写 `ModuleSystem.GetModule<T>()`。

### 客户端配置消费

客户端配置入口在：

- `GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`

特点：

- `Tables` 懒加载
- 通过 `IResourceModule.LoadAsset<TextAsset>(file)` 读取二进制配置
- 配置消费逻辑属于 `GameProto`

### 服务端配置消费

服务端配置入口在：

- `GameServer/Server/Entity/Generate/ConfigSystem.cs`

特点：

- 也是 `Tables` 懒加载
- 从 `Configs/Binary/*.bytes` 读取
- 配置消费逻辑属于服务端 `Entity/Generate`

## 常见判断规则

- 如果改的是“定义”，优先回源头目录，不优先改生成结果
- 如果改的是“消费逻辑”，再去 `GameProto`、`GameLogic` 或 `Server/Hotfix`
- 如果能力会被多个业务模块复用，优先评估是否应沉到基础层
