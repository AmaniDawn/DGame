# DGame 客户端与服务器交互通讯指引

当需求涉及客户端如何发起请求、接收服务器响应、处理服务器推送、组织 `NetworkMgr`、消费 `GameProto` 协议结构或梳理 UI 到网络层的调用链时，先阅读本文件。

本文档聚焦客户端与服务器之间的交互通讯职责，不展开协议导出工具的细节。若需求涉及 `.proto` 源文件、路由定义、协议导出脚本或生成目录，请同时阅读 `proto-message-define.md`。

## 目录导航

- [DGame 客户端与服务器交互通讯指引](#dgame-客户端与服务器交互通讯指引)
  - [目录导航](#目录导航)
  - [核心入口：GameClient](#核心入口gameclient)
  - [职责主线](#职责主线)
  - [客户端侧分层](#客户端侧分层)
    - [GameStart 与初始化入口](#gamestart-与初始化入口)
    - [GameClient](#gameclient)
    - [UI 与业务模块](#ui-与业务模块)
    - [NetworkMgr](#networkmgr)
  - [项目中的实际交互链路](#项目中的实际交互链路)
    - [GameProto](#gameproto)
  - [协议来源与生成边界](#协议来源与生成边界)
  - [推荐交互流程](#推荐交互流程)
  - [消息类型理解](#消息类型理解)
  - [常见改动落点](#常见改动落点)
  - [修改顺序建议](#修改顺序建议)
  - [使用原则](#使用原则)

## 核心入口：GameClient

在当前项目里，客户端与服务器交互通讯的核心入口不是某个 `NetworkMgr`，而是：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/DataCenter/GameClient.cs`

`GameClient` 是客户端网络层的统一门面，实际负责：

- 初始化 Fantasy 网络运行时。
- 持有并维护全局唯一的网络通信 `Scene`。
- 建立和断开连接。
- 对外提供统一的协议发送与 RPC 调用入口。
- 注册和注销服务器推送消息处理器。
- 维护客户端网络状态。
- 启动心跳检测。
- 驱动断线重连监控。

理解客户端与服务端通讯时，应先把 `GameClient` 视为“网络总入口”，再看上层 `NetworkMgr` 如何在它之上组织业务流程。

这里有一个明确约束：

- 网络协议收发优先且应统一使用 `GameClient` 提供的接口。
- 不应在业务层直接调用 Fantasy 原生的 `Scene.Session.Send(...)`、`Scene.Session.Call(...)`、`Scene.Connect(...)` 等方法。
- 服务器推送监听优先使用 `GameClient.RegisterMsgHandler(...)` 和 `GameClient.UnRegisterMsgHandler(...)`。
- 如果确实需要调整底层调用方式，应优先封装进 `GameClient`，而不是让上层业务直接依赖 Fantasy 细节。

## 职责主线

客户端与服务器交互的主链路可以概括为：

`GameStart`
-> `GameClient`
-> `GameLogic/NetworkMgr`
-> `GameProto` 协议结构
-> 网络层发送与接收
-> 服务端处理逻辑
-> `GameClient` / `NetworkMgr` 回写客户端状态
-> UI、数据中心或事件系统

核心目标是把三类职责拆开：

- `GameClient` 负责网络连接与消息分发基础设施。
- 页面与表现层只负责触发操作和展示结果。
- 网络管理层负责消息收发、注册和响应处理。
- 协议结构层负责承载请求、响应和推送对应的数据结构。

如果这三层混在一起，后续通常会出现：

- UI 页面直接依赖底层网络细节。
- 同一协议被多个页面重复发送和监听。
- 协议升级时需要在多个业务文件里分散修改。

## 客户端侧分层

### GameStart 与初始化入口

项目实际启动链路里，网络初始化从这里开始：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`

当前实现中：

- `GameStart.Entrance(...)` 进入热更主流程。
- `StartGame()` 内部会执行 `GameClient.Instance.InitAsync(m_hotfixAssembly)`。
- `InitAsync(...)` 会先对热更程序集执行 `EnsureLoaded()`，再初始化 Fantasy，并创建全局唯一的通信 `Scene`。

结论：

- `GameClient` 不是登录时临时创建的对象，而是在游戏热更启动阶段就初始化。
- 后续所有 `ConnectAsync`、`Call`、`Send`、`RegisterMsgHandler` 都依赖这个由 `GameClient` 维护的全局 `Scene`。

### GameClient

从当前实现看，`GameClient` 是客户端与服务端交互通讯的基础设施层，不是纯业务层代码。

需要特别明确的一点是：

- 当前客户端运行时只维护一个全局 `Scene`。
- 这个 `Scene` 由 `GameClient.Scene` 持有。
- 其他业务层不应各自创建新的网络 `Scene`，也不应绕过 `GameClient` 私自维护会话对象。

它暴露的关键能力包括：

- `InitAsync(...)`
  初始化 Fantasy 运行时并创建 `Scene`
- `Connect(...)` / `ConnectAsync(...)`
  连接认证服或 Gate 服
- `Disconnect()`
  断开当前会话
- `Send<T>(...)`
  发送无需响应的消息
- `Call<T>(...)`
  发起 RPC 并等待服务端响应
- `RegisterMsgHandler(...)` / `UnRegisterMsgHandler(...)`
  注册或注销服务器推送消息处理器
- `StartHeartbeat()`
  登录成功后启动会话心跳
- `Reconnect()`
  使用上次地址和端口执行重连

当前项目里，`GameClient` 还直接管理了一个重要状态机：

- `StatusInit`
- `StatusConnected`
- `StatusReconnect`
- `StatusClose`
- `StatusLogin`
- `StatusRegister`
- `StatusEnter`

这个状态机会直接影响消息是否允许发送。当前实现里：

- `StatusRegister` 只允许注册请求。
- `StatusLogin` 只允许登录相关请求。
- `StatusEnter` 才允许进入正常业务通信阶段。

因此，修改登录、进服或重连流程时，不能只看 UI 和 `NetworkMgr`，必须同步检查 `GameClient.Status` 的流转是否还成立。

同时要保持一个开发约束：

- 上层模块调用协议时，优先通过 `GameClient.Send<T>(...)` 和 `GameClient.Call<T>(...)`。
- 上层模块监听服务器推送时，优先通过 `GameClient.RegisterMsgHandler(...)` 和 `GameClient.UnRegisterMsgHandler(...)`。
- 即使底层最终仍会落到 Fantasy Session，上层也不应直接依赖 Fantasy 的发送接口。
- 这样才能把连接状态校验、日志、心跳、重连、错误处理和消息分发入口统一收口在 `GameClient`。

### UI 与业务模块

UI 页面、控制器和一般业务模块的职责是：

- 收集用户输入。
- 决定何时发起某个业务动作。
- 展示网络结果。
- 根据结果刷新界面或派发后续业务逻辑。

这里的边界是：

- 可以调用某个 `NetworkMgr` 暴露出来的业务方法。
- 不应该直接散落底层 RPC 调用。
- 不应该自行维护重复的消息注册与反注册逻辑。

### NetworkMgr

客户端网络交互逻辑优先放在：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr`

`NetworkMgr` 的职责通常包括：

- 在 `OnInit()` 中通过 `GameClient.Instance.RegisterMsgHandler(...)` 注册服务器推送。
- 在需要移除监听时，通过 `GameClient.Instance.UnRegisterMsgHandler(...)` 注销服务器推送。
- 基于 `GameClient.Instance.Call(...)` 或 `Send(...)` 发起网络请求。
- 处理响应消息。
- 处理服务器主动推送。
- 把结果同步到 UI、数据中心或事件系统。

适合放在 `NetworkMgr` 的内容：

- 登录、注册、进服、拉取角色信息等请求流程。
- 某玩法域的请求封装与响应回调。
- 某类服务器通知消息的统一接收入口。

不适合放在 `NetworkMgr` 的内容：

- 具体界面节点的显隐控制。
- 与页面生命周期强绑定的纯表现层细节。
- 不依赖网络通讯的纯本地业务计算。
- 直接操作 Fantasy 原生网络发送接口。
- 直接操作 Fantasy 原生消息分发注册接口。

## 项目中的实际交互链路

结合当前代码，项目里的真实交互链路大致如下：

1. `GameStart` 在热更启动时调用 `GameClient.Instance.InitAsync(...)`。
2. `GameClient` 初始化 Fantasy，并创建一个全局唯一、由自身维护的通信 `Scene`。
3. 各个 `NetworkMgr` 在 `OnInit()` 中调用 `GameClient.Instance.RegisterMsgHandler(...)` 注册推送处理。
4. UI 页面或控制器触发业务动作后，调用对应 `NetworkMgr` 的业务方法。
5. `NetworkMgr` 内部通过 `GameClient.Instance.ConnectAsync(...)`、`Call(...)`、`Send(...)` 与服务端交互。
6. 服务端返回响应后，由 `NetworkMgr` 解析结果并更新数据中心、派发事件或刷新 UI。
7. 登录 Gate 成功后，由 `GameClient.Instance.StartHeartbeat()` 开启心跳，并将状态切到 `StatusEnter`。

当前项目中可直接参考的实现有：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr/LoginNetMgr.cs`
  负责认证服登录、注册、Gate 登录
- `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr/FuncOpenNetMgr.cs`
  负责注册功能开放推送，并在进入游戏后拉取功能开放列表

从这些实现可以看到一个明确约定：

- `GameClient` 负责“连接、会话、状态、消息派发基础设施”。
- `NetworkMgr` 负责“具体业务协议如何调用、结果如何处理”。
- UI 不直接操作 `Scene` 或底层 Session。
- 业务层协议收发和消息监听优先通过 `GameClient`，不直接调用 Fantasy 原生接口。

### GameProto

客户端协议消费层主要位于：

- `GameUnity/Assets/Scripts/HotFix/GameProto`

这里承担的职责包括：

- 协议结构定义的客户端消费结果。
- 配置结构与部分生成代码消费。
- 协议相关的公共结构体、枚举和辅助访问。

这里的边界是：

- `GameProto` 负责“协议结构是什么”。
- `NetworkMgr` 负责“什么时候发、收到了怎么处理”。
- UI 和业务层负责“为何触发这个行为、结果怎么展示”。

## 协议来源与生成边界

客户端与服务端通讯协议的源定义位于：

- `GameServer/Tools/NetworkProtocol/Outer`

路由定义位于：

- `GameServer/Tools/NetworkProtocol/RouteType.Config`

客户端生成结果位于：

- `GameUnity/Assets/Scripts/HotFix/GameProto/Generate/NetworkProtocol`

处理协议时应遵循以下边界：

- 新增或修改客户端与服务端协议，优先修改 `Outer/*.proto`。
- 新增或调整路由，优先修改 `RouteType.Config`。
- 不要手改客户端生成目录中的协议代码。
- 协议变更完成后，应通过协议导出工具重新生成客户端与服务端消费代码。

如果问题发生在“协议结构不对、字段缺失、消息类型不匹配”，先回源头看 `.proto` 和导出结果，不要先改 `GameLogic`。

## 推荐交互流程

典型客户端与服务端交互建议按下面流程组织：

1. UI 或业务模块触发动作，例如登录、进入场景、请求背包数据。
2. 对应 `NetworkMgr` 视情况先调用 `GameClient.ConnectAsync(...)` 建立连接。
3. 请求消息类型来自 `GameProto` 生成的协议结构。
4. `NetworkMgr` 通过 `GameClient.Call(...)` 或 `GameClient.Send(...)` 发起通信。
5. 服务端按 `Outer/*.proto` 对应协议处理请求。
6. 服务端返回响应，或在后续时机推送通知消息。
7. 客户端 `GameClient` 负责分发推送，`NetworkMgr` 收到结果后再更新数据中心、派发事件或通知 UI 刷新。

一个稳定的项目通常会坚持两点：

- UI 只依赖 `NetworkMgr` 提供的业务入口，不直接依赖底层网络发送 API。
- 消息注册和处理集中管理，不在多个页面里复制一份。

在当前项目里，这里的“底层网络发送 API”优先指：

- 不直接在业务层使用 Fantasy 的 `Scene.Session.Send(...)`
- 不直接在业务层使用 Fantasy 的 `Scene.Session.Call(...)`
- 不直接在业务层使用 `Scene.MessageDispatcherComponent.RegisterMsgHandler(...)`
- 不直接在业务层使用 `Scene.MessageDispatcherComponent.UnRegisterMsgHandler(...)`
- 而是统一改走 `GameClient.Send<T>(...)`、`GameClient.Call<T>(...)`
- 监听统一改走 `GameClient.RegisterMsgHandler(...)`、`GameClient.UnRegisterMsgHandler(...)`

## 消息类型理解

客户端日常接触最多的是以下几类消息：

- `IRequest`
  表示客户端发起请求，并且通常存在对应响应。
- `IResponse`
  表示请求对应的返回结果。
- `IMessage`
  表示服务器主动推送或单向通知。

从客户端职责看，应这样理解：

- 请求和响应通常成对由 `NetworkMgr` 封装。
- 服务器推送通常由 `NetworkMgr` 通过 `GameClient.RegisterMsgHandler(...)` 统一注册并转发给业务模块。
- 若推送会影响多个系统，优先转为事件或数据更新，不要直接耦合到某一个页面。

## 常见改动落点

| 需求类型 | 优先修改位置 |
|---------|---------|
| 新增客户端到服务端请求协议 | `GameServer/Tools/NetworkProtocol/Outer/*.proto` |
| 新增服务端到客户端通知协议 | `GameServer/Tools/NetworkProtocol/Outer/*.proto` |
| 调整协议路由 | `GameServer/Tools/NetworkProtocol/RouteType.Config` |
| 重新生成客户端协议消费代码 | `GameServer/Tools/ProtocolExportTool` |
| 调整连接、心跳、重连、消息注册基础设施 | `GameUnity/Assets/Scripts/HotFix/GameLogic/DataCenter/GameClient.cs` |
| 新增客户端请求封装与响应处理 | `GameUnity/Assets/Scripts/HotFix/GameLogic/NetworkMgr` |
| 新增协议结构消费逻辑 | `GameUnity/Assets/Scripts/HotFix/GameProto` |
| 新增网络结果驱动的界面刷新 | `GameUnity/Assets/Scripts/HotFix/GameLogic/UI` 或 `UIController` |

## 修改顺序建议

当需求涉及客户端与服务器交互时，推荐按下面顺序处理：

1. 先确认已有协议是否可以复用，避免重复定义消息。
2. 如果协议不足，再修改 `Outer/*.proto` 和相关路由配置。
3. 执行协议导出，确认客户端与服务端生成结果齐全。
4. 检查本次流程是否需要调整 `GameClient` 的连接、状态、心跳或重连行为。
5. 在客户端对应 `NetworkMgr` 中补请求发送、响应处理和推送注册。
6. 最后再接 UI、数据中心或事件系统。

这样可以避免先写完业务代码，最后才发现协议或路由不成立。

## 使用原则

- 不要把 RPC 调用散落在 UI 页面里。
- 不要绕过 `GameClient` 直接在业务层操作底层 `Scene.Session`。
- 不要在客户端其它模块中重复创建或维护新的网络 `Scene`。
- 不要在业务层直接调用 Fantasy 提供的网络协议收发方法，优先使用 `GameClient` 暴露的统一接口。
- 不要在业务层直接使用 Fantasy 消息分发器注册监听，优先使用 `GameClient.RegisterMsgHandler(...)`。
- 不要把消息结构手写在 `GameLogic`。
- 不要手改自动生成的协议消费代码。
- 一个业务域优先收敛到一个或少数几个对应的 `NetworkMgr`。
- 推送消息优先统一管理，再分发给具体模块。
- 涉及断线重连、心跳、连接状态控制时，优先检查 `GameClient` 和 `ClientConnectWatcher`。
- 协议字段变更优先考虑兼容性，不要把线上协议当成本地普通结构体随意重构。