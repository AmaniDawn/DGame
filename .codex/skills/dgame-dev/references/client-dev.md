# DGame 客户端开发

## 客户端目录主线

客户端相关代码主要集中在：

- `GameUnity/Assets/DGame`
  客户端基础框架与编辑器工具

- `GameUnity/Assets/Scripts/HotFix`
  客户端热更业务

- `GameUnity/Assets/DGame.AOT`
  AOT 相关代码

- `GameUnity/Assets/Obfuz`
  混淆或运行时支持代码

## 程序集分层

### 基础框架层

- `DGame.Runtime`
  位置：`GameUnity/Assets/DGame/Runtime`
  职责：模块系统、事件、资源、流程、调试、音频、输入、对象池等通用能力

- `DGame.FixedPointPhysics`
  位置：`GameUnity/Assets/DGame/FixedPointPhysics`
  职责：定点数学、定点碰撞、定点物理和相关编辑辅助

- `DGame.AOT`
  位置：`GameUnity/Assets/DGame.AOT`
  职责：AOT 相关适配

- `DGame.Obfuz`
  位置：`GameUnity/Assets/Obfuz`
  职责：混淆或运行时支持

### 编辑器工具层

- `DGame.Editor`
  位置：`GameUnity/Assets/DGame/Editor`
  职责：HybridCLR、LubanTools、Inspector、发布工具、工具栏扩展等

- `Fantasy.Editor`
  位置：`GameUnity/Assets/Scripts/HotFix/Fantasy.Unity/Editor/Runtime`
  职责：Fantasy 的 Unity 编辑器配套工具

### 业务与热更层

- `Fantasy.Unity`
  位置：`GameUnity/Assets/Scripts/HotFix/Fantasy.Unity`
  职责：业务热更层依赖的基础框架封装

- `GameProto`
  位置：`GameUnity/Assets/Scripts/HotFix/GameProto`
  职责：客户端协议、配置结构、Luban 运行时和生成结果消费

- `GameBattle`
  位置：`GameUnity/Assets/Scripts/HotFix/GameBattle`
  职责：战斗域逻辑

- `GameLogic`
  位置：`GameUnity/Assets/Scripts/HotFix/GameLogic`
  职责：主业务逻辑、UI、数据中心、网络管理、业务模块

## 程序集依赖主干

自有程序集的主干依赖关系可以概括成：

`DGame.Runtime` / `DGame.FixedPointPhysics`
-> `Fantasy.Unity` / `GameProto`
-> `GameBattle`
-> `GameLogic`

更具体地说：

- `DGame.Editor` 依赖 `DGame.Runtime`
- `DGame.AOT` 依赖 `DGame.Runtime`
- `GameProto` 依赖 `DGame.Runtime`、`Fantasy.Unity`
- `GameBattle` 依赖 `DGame.Runtime`、`Fantasy.Unity`、`GameProto`、`DGame.FixedPointPhysics`
- `GameLogic` 依赖 `DGame.Runtime`、`Fantasy.Unity`、`GameProto`、`GameBattle`、`DGame.AOT`

约束：

- 运行时程序集不要反向依赖编辑器程序集
- 会被多个业务模块复用的类型，不要直接放在 `GameLogic`

## 客户端开发约定

### 模块访问优先走 `GameModule`

参考：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs`

业务层优先使用：

- `GameModule.ResourceModule`
- `GameModule.GameTimerModule`
- `GameModule.LocalizationModule`
- `GameModule.AudioModule`

不要在每个业务类里重复手写 `ModuleSystem.GetModule<T>()`，除非是基础框架层代码。

### UI 开发风格

参考：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/Login/MainLoginUI.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/UIController/CommonUIController.cs`

从现有代码看，UI 侧常见模式是：

- UI 页面类放在 `GameLogic/UI`
- 页面内部用 `BindMemberProperty`、`RegisterEvent`、`OnCreate`、`OnDestroy`
- UI 事件通过 `AddUIEvent(...)` 注册
- 控制器类通过 `GameEvent.AddEventListener(...)` 监听业务事件
- 打开窗口常用 `UIModule.Instance.ShowWindowAsync<...>()`

结论：

- 新增 UI 页面，优先放 `GameLogic/UI`
- 新增 UI 协调逻辑，优先看是否应放 `UIController`
- UI 文案、状态、服务器配置消费，优先沿用现有 `GameProto` 配置访问方式

### 网络管理落点

参考：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr/LoginNetMgr.cs`

从现有代码看：

- 网络管理逻辑放在 `GameLogic/NetworkMgr`
- 消息注册由管理类在 `OnInit()` 中完成
- 登录、注册、进服等 RPC 调用都在网络管理器内部封装

结论：

- 新增客户端协议调用流程，优先放到对应 `NetworkMgr`
- 不要把 RPC 调用散落在 UI 页面里

### 配置消费落点

参考：

- `GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`

结论：

- 配置结构和配置读取逻辑属于 `GameProto`
- 业务只消费配置，不负责重新实现配置加载

## 常见任务落点

| 常见任务 | 优先修改哪里 |
|---------|---------|
| 新增通用运行时模块 | `GameUnity/Assets/DGame/Runtime/Module` |
| 新增编辑器菜单或 Inspector | `GameUnity/Assets/DGame/Editor` |
| 新增客户端 UI 页面 | `GameUnity/Assets/Scripts/HotFix/GameLogic/UI` |
| 新增 UI 控制器逻辑 | `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/UIController` |
| 新增客户端网络管理逻辑 | `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr` |
| 新增客户端配置消费逻辑 | `GameUnity/Assets/Scripts/HotFix/GameProto` |
| 新增战斗域逻辑 | `GameUnity/Assets/Scripts/HotFix/GameBattle` |
