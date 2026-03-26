# DGame 服务端代码规范和设计模式（精简版）

当需求涉及服务端命名、异步编排、日志接口、错误码、Scene 目录落位、Handler/Helper/System 设计、代码审查或 Git 协作时，先读本文件。

目标：让 Codex 以最短阅读成本掌握 DGame 当前服务端代码约束。若与目标 Scene、工程或目录中的现有稳定模式冲突，先遵循目标区域附近实现，再做最小收敛。

## 使用原则

1. 先与目标工程、目标 Scene、目标目录中的现有风格保持一致，再补统一约束。
2. 服务端代码优先追求可读性、可维护性、可排查性和运行时稳定性。
3. 业务改动优先做最小完整实现，不顺手重构无关区域。
4. 若现有代码与规范冲突，优先局部收敛，不在无明确需求时全局推翻旧风格。
5. 涉及 `Generate/` 目录时，默认视为自动生成内容，不手改生成文件。

## 命名规范

### 类型命名

- 类型统一 `PascalCase`。
- 接口统一 `I` 前缀。
- 协议 Handler 使用 `XxxHandler`。
- 业务辅助类使用 `XxxHelper`。
- 组件行为扩展类使用 `XxxComponentSystem`。
- 实体组件使用 `XxxComponent`。
- 场景模型和实体类型优先用明确业务语义命名，不用无语义缩写。
- 错误码类型统一收敛在 `ErrorCode`。
- Scene 功能域目录名与类型语义保持一致，例如 `Authentication`、`Gate`、`Hall`。
- 协议类型、配置类型和生成类型沿用生成结果命名，不手动改缩写。

### 字段命名

- 私有字段：`m_ + camelCase`
- 静态私有字段：`s_ + camelCase`
- 公有字段/属性：`PascalCase`。
- 常量：全大写 + 下划线，或按现有 `ErrorCode` 风格保持一致。
- 字典、缓存和集合字段优先表达键值语义，不要只写无语义缩写。

示例：

```csharp
private ClientConnectWatcher m_clientConnectWatcher;
private static Dictionary<int, string> s_cacheMap;
public readonly Dictionary<(long AccountID, int ServerID), PlayerData> PlayerDataDict;
private static long GetCreateLockKey(long accountID, int serverID) => ...;
public const uint LOGIN_ACCOUNT_NOT_EXIST = 1005;
```

### 方法命名

- 方法统一 `PascalCase`。
- 行为方法优先动词开头。
- Scene 扩展方法优先用清晰业务语义，例如 `Login`、`Register`、`RecordRecentServer`。
- 创建类方法优先使用 `Create`。
- 校验类方法优先使用 `Check`、`Validate`、`Exist`、`TryGet`。
- 异步方法保持现有风格；若周边未统一加 `Async` 后缀，优先与现有实现一致。

## Scene 目录与代码落位

服务端脚本落位优先按功能域划分主目录，而不是按脚本类型横向堆放。

当前约定：

- `Scene/Authentication/`
  只放 Authentication 相关代码

- `Scene/Gate/`
  只放 Gate 相关代码

- 后续若新增大厅场景，应优先创建 `Scene/Hall/`

在每个 Scene 主目录下，再继续按职责拆分：

- `Handler/`
- `Helper/`
- `System/`

约束：

- `Gate` 相关逻辑优先落在 `Gate`
- `Authentication` 相关逻辑优先落在 `Authentication`
- `Hotfix/Scene/` 和 `Entity/Scene/` 使用同一套功能域划分规则
- 不要在不同 Scene 之间交叉堆放脚本
- 不要把大量无关逻辑继续堆进单个 Handler

## 异步编程

目标：可等待、可排查、可收口。

### 基本规则

- 服务端异步统一优先使用 `FTask` / `FTask<T>`。
- 需要等待的异步必须显式 `await`。
- 不要在服务端业务代码中混用多套异步范式。
- 不要通过阻塞方式等待异步结果。
- 异步链路中的失败路径应能回收、返回错误码或记录日志。
- 跨数据库、网络、锁和 Scene 组件的异步调用，要保证流程有明确收口。

### 锁与并发

- 需要保证唯一性或串行化的业务流程，优先复用现有 `CoroutineLockComponent`。
- 锁 Key 应具备稳定业务语义，不要随意拼接无约束字符串。
- 同一业务语义应尽量复用同一种加锁方式，避免局部重复发明新锁规则。
- 涉及数据库创建、唯一名生成、重复登录、在线状态切换等流程时，优先先检查是否已有锁约束。

### 返回值与错误处理

- 业务异步优先返回明确结果对象或错误码，不要只靠异常表达常规业务失败。
- Handler 中优先把协议响应、错误码和业务异常分层处理。
- 可以预期的业务失败走错误码；真正异常再记录日志并兜底。

## 日志规范

- 服务端运行时代码统一优先使用 `Log`。
- 宿主级启动错误可使用 `Console.Error` 做进程级兜底输出。
- 临时测试日志用完即删，不要留下无业务语义的调试日志。
- 测试日志代码在调试完成后必须及时删除，不要把临时排查代码长期保留在正式逻辑中。
- 日志内容应包含足够的业务上下文，例如账号、服务器、Scene、协议名、关键参数。

示例：

```csharp
Log.Warning($"PlayerDataSystem Offline fail accountID: {self.AccountID} serverID: {self.ServerID} not found");
Log.Error(e);
```

约束：

- 不要记录无意义的“到此一游”日志。
- 不要在高频路径无节制打印日志。
- 涉及账号、Token、密钥等敏感信息时，避免直接输出完整原文。

## 错误码规范

- 可预期的业务失败优先使用 `ErrorCode` 返回。
- 错误码统一集中维护，不要在业务代码中散落魔法数字。
- 新增错误码时，名称应能直接表达业务语义。
- 同一业务域的错误码段尽量保持集中，避免无序穿插。
- 响应错误码时，优先返回最贴近真实失败原因的错误码，不要统一返回模糊错误。

## 注释与中文

- 业务注释可以使用中文，优先写清楚业务背景、限制条件和特殊时序。
- 对锁、并发、在线态、断线重连、顶号、数据库一致性等逻辑，建议保留必要注释。
- 代码命名中禁止出现中文。
- 运行时日志和注释可以写中文，但命名、类型名、方法名和字段名保持英文语义。

## 禁止的代码模式

- 禁止直接手改 `Generate/` 目录中的协议和配置生成文件。
- 禁止把复杂业务全部堆到 Handler。
- 禁止让 `Main` 承担业务逻辑。
- 禁止让 `Entity` 承担具体协议流程编排。
- 禁止跨 Scene 随意堆放脚本，破坏功能域目录边界。
- 禁止在没有锁约束的情况下处理需要唯一性保证的关键流程。
- 禁止在可预期业务失败上滥用异常代替错误码。
- 禁止新增第三方公共依赖时不先评估是否应放到 `Entity`。
- 禁止直接修改协议或配置生成结果来“临时修复”问题。

对应推荐替代：

- 改生成结果时，回到定义源头并重新执行自动生成流程。
- 复杂业务拆到 `Helper`、`System` 或组件扩展中。
- 启动相关改动放 `Main`，共享数据和组件放 `Entity`，业务流程放 `Hotfix`。
- 需要唯一性约束时，优先复用 `CoroutineLockComponent` 和已有锁类型。
- 可预期失败优先使用 `ErrorCode` 和结果对象。

## 推荐模式

- 协议入口处理优先放 `Handler`，只做参数校验、响应组装和流程编排。
- 可复用业务逻辑优先放 `Helper`。
- 组件行为扩展优先放 `XxxComponentSystem`。
- Scene 基础能力优先在 `OnSceneCreate_Init` 中统一挂载组件。
- 缓存、字典和数据库访问优先通过组件或系统收口，不要散落到多个 Handler 中。
- 协议定义和配置定义优先回源头目录处理，再通过工具生成消费代码。

## 事件模块

服务端事件模块基于 `Fantasy.Event` 使用。当前项目里已经明确落地的事件用法，是在 `Hotfix/Scene/OnSceneCreate_Init.cs` 中通过事件系统监听 Scene 创建事件，并在事件回调里统一挂载场景组件。

可以先这样理解当前用法：

- 事件处理器放在 `Hotfix` 层
- 通过继承 `AsyncEventSystem<TEvent>` 监听对应事件
- 在 `Handler(...)` 中拿到事件参数，再做场景初始化或业务收口
- 当前项目中最典型的事件是 `OnCreateScene`

当前项目里的实际用途：

- Authentication Scene 创建时挂载 `AccountManagerComponent`、`AccountJwtComponent`
- Gate Scene 创建时挂载 `AccountJwtComponent`、`PlayerManagerComponent`

结论：

- 事件系统适合做场景初始化、统一挂载组件、生命周期收口等跨业务入口逻辑
- 不要把普通协议业务流程强行改成事件驱动
- 事件处理器仍然应按功能域和职责落位，避免变成新的杂物入口

### 使用例子

下面是一个符合当前项目模式的服务端事件使用例子，用于在新增 `Hall` 场景时，在 Scene 创建后统一挂载组件：

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Event;

namespace Hotfix;

public sealed class OnSceneCreate_Init : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Hall:
                scene.AddComponent<HallManagerComponent>();
                scene.AddComponent<AccountJwtComponent>();
                break;
        }

        await FTask.CompletedTask;
    }
}
```

使用时建议遵循以下规则：

- 事件处理器优先放在 `Hotfix/Scene/` 下与功能域对应的位置。
- 事件里优先做组件挂载、初始化分发、生命周期收口，不要塞入长业务流程。
- 新增 Scene 的事件初始化逻辑时，`Hotfix/Scene/` 和 `Entity/Scene/` 仍要保持相同的功能域目录划分。

## 模块设计

### 新增协议 Handler

- 命名使用 `XxxHandler`。
- 继承现有协议处理基类，例如 `MessageRPC<TRequest, TResponse>`。
- `Run(...)` 中优先只做：
  - 参数校验
  - 调用 Scene/组件业务逻辑
  - 填充 `response`
  - 设置会话生命周期或资源收口

不要在 Handler 中：

- 长篇堆叠业务逻辑
- 直接维护复杂缓存结构
- 混入大量数据库细节

### 新增 Helper

- 复用型业务逻辑优先写成 `static` Helper 或 Scene 扩展方法。
- 命名使用 `XxxHelper`。
- Helper 应聚焦清晰职责，不要变成无边界杂物类。

### 新增 ComponentSystem

- 组件行为扩展优先使用 `XxxComponentSystem`。
- 生命周期相关系统按框架模式使用对应基类，例如 `DestroySystem<T>`。
- 扩展方法优先围绕组件自身职责组织，不要在同一个 `ComponentSystem` 中混入多个无关业务域。

## 代码审查清单

### 工程边界

- 改动是否落在正确工程：`Main`、`Entity` 或 `Hotfix`
- 是否错误地把业务逻辑放进 `Main`
- 是否错误地把协议流程编排放进 `Entity`
- 若需求要求支持热更，是否确实落在 `Hotfix`

### Scene 落位

- 是否先按功能域进入正确 Scene 主目录
- `Hotfix/Scene/` 与 `Entity/Scene/` 的目录命名是否一致
- 是否避免跨 Scene 混放代码
- 是否把复杂逻辑从 Handler 继续拆分到 `Helper` 或 `System`

### 异步与并发

- 是否统一使用 `FTask`
- 需要等待的异步是否都已 `await`
- 关键唯一性流程是否有锁保护
- 数据库、缓存、Session、在线态流程是否有明确收口
- 异常和业务失败是否被正确区分

### 配置与协议

- 是否误改了 `Generate/` 目录
- 改协议是否回到 `GameServer/Tools/NetworkProtocol/`
- 改配置或 `ConfigSystem` 是否回到 `GameConfig`
- 变更后是否应重新执行自动生成流程

### 日志与错误码

- 是否统一使用 `Log`
- 日志是否包含足够的排查上下文
- 可预期业务失败是否优先使用 `ErrorCode`
- 是否避免魔法数字和模糊错误码

## Git 工作流

分支约定：

- `main / master`：稳定版本，禁止直接提交
- `feature/xxx`：功能开发
- `fix/xxx`：Bug 修复
- `hotfix/xxx`：线上紧急修复

提交信息格式：

- `feat: 新增 Gate 登录流程`
- `fix: 修复玩家重连时 Session 绑定异常`
- `refactor: 拆分 Authentication 场景账号辅助逻辑`
- `perf: 优化玩家缓存查询与锁粒度`
- `docs: 更新服务端开发规范文档`

合并前检查：

1. 服务端工程编译无错误
2. 协议和配置生成结果已确认同步
3. 关键登录、在线态、断线重连流程已自测
4. 已完成代码审查清单自查