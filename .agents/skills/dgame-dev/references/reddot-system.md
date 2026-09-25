# 红点系统

> **适用场景**：RedDotModule、RedDotItem、RedDotPathDefine、红点 UI 绑定 | **关联文档**：[ui-lifecycle.md](ui-lifecycle.md)、[ui-patterns.md](ui-patterns.md)

## 核心类

| 类 | 职责 |
|----|------|
| `RedDotModule` | 红点树管理、节点注册、值变更、监听 |
| `RedDotNode` | 单个红点节点 |
| `RedDotItem` | UIWidget 红点控件 |
| `RedDotPathDefine` | 红点路径和 ID 注册入口 |
| `RedDotPathDefine_Gen.g.cs` | 生成的红点路径代码 |

## 初始化

`RedDotModule.OnInit()` 创建 Root，并调用：

```csharp
RedDotPathDefine.RegisterAll();
```

不要使用已废弃的 `RedDotTreeConfig.RegisterAll()`；源码已标记 `[Obsolete("不使用这个注册 使用RedDotPathDefine_Gen.RegisterAll")]`。

业务访问：

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
GameModule.RedDotModule.ClearNodeValue(RedDotPathDefine.Main.Mail.System);
```

`RedDotPathDefine_Gen.g.cs` 生成的是嵌套静态类 + `const int`，例如 `RedDotPathDefine.Main.Mail.System`、`RedDotPathDefine.Main.Bag.Item`；不要按顶层平铺常量猜测路径名。

## UI 绑定

`CreateRedDot` 和 `CreateRedDotAsync` 是 `UIBase` 方法，`UIWindow` / `UIWidget` 内可直接调用；它们内部用 `CreateWidgetByType<RedDotItem>` / `CreateWidgetByTypeAsync<RedDotItem>` 创建控件并调用 `RedDotItem.Init(redDotNodeID)`。

```csharp
var redDot = CreateRedDot(RedDotPathDefine.Main.Mail.System, parent);
var redDot = await CreateRedDotAsync(RedDotPathDefine.Main.Bag.Item, parent);
```

`RedDotItem.Init(int redDotNodeID)` 会监听节点变化，并在 `OnDestroy` 移除监听。

## 节点类型

- `RedDotType.Dot`：普通红点。
- `RedDotType.Number`：数字红点，超过 99 显示 `99+`。
- `RedDotType.New`：NEW 标签。
- `RedDotType.Custom`：自定义类型，当前枚举值为 100。

## 聚合策略

父节点根据 `RedDotAggregateStrategy` 汇总子节点值：

| 策略 | 含义 | 典型用途 |
|------|------|----------|
| `Sum` | 子节点值求和 | 数字红点总数 |
| `Or` | 任一子节点大于 0 则父节点为 1 | 普通红点、新功能提示 |
| `Max` | 取子节点最大值 | 需要显示最大优先级或最大数量的场景 |

生成代码会把配置里的策略写入 `RedDotPathDefine_Gen.g.cs` 的 `mgr.Register(..., RedDotAggregateStrategy.X)`；业务通常只设置叶子节点值。

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| UI 手动监听节点后忘记移除 | 使用 `CreateRedDot` 创建 `RedDotItem` |
| 业务代码散落字符串路径 | 优先使用 `RedDotPathDefine` ID |
| 未注册节点就 `SetValue` | 确认 `RegisterAll()` 包含节点 |
| 对非叶子节点直接 `SetValue` | 只设置叶子节点，父节点由聚合策略计算 |
| UI 销毁后仍回调 | 检查是否绕过 `RedDotItem.OnDestroy` |
| 调用 `RedDotTreeConfig.RegisterAll()` | 使用 `RedDotPathDefine.RegisterAll()` |
