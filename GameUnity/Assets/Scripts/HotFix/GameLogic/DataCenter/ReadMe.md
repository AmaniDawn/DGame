# Fantasy 服务器框架更新注意事项

## Fantasy 框架更新后需要手动修改的内容

> 更新 Fantasy 框架时，以下修改可能会被覆盖，需要人工确认功能是否正常，如不正常，需要手动重新添加以确保功能的正确性。

---

### 1. Scene.cs - MessageDispatcherComponent 属性修改

**文件位置**: `Assets/Scripts/HotFix/Fantasy.Unity/Runtime/Core/Scene/Scene.cs`

**修改内容**: 将 `MessageDispatcherComponent` 属性改为 `public`

```csharp
/// <summary>
/// Scene下的网络消息派发组件 改成 public 方便客户端通过GameClient和协议号直接监听服务器数据的下发
/// </summary>
public MessageDispatcherComponent MessageDispatcherComponent { get; internal set; }
```

---

### 2. MessageDispatcherComponent.cs - 添加客户端消息注册支持

**文件位置**: `Assets/Scripts/HotFix/Fantasy.Unity/Runtime/Core/Network/Message/Dispatcher/MessageDispatcherComponent.cs`

#### 2.1 添加字段和方法（在类的适当位置）

```csharp
#if FANTASY_UNITY
        /*
         * 方便客户端通过 GameClient RegisterMsgHandler 注册的回调
         * 客户端可以通过 GameClient 和协议号直接监听服务器数据的下发
         */

        public readonly Dictionary<uint, List<Action<IMessage>>> MsgHandles = new Dictionary<uint, List<Action<IMessage>>>();

        public void RegisterMsgHandler(uint protocolCode, Action<IMessage> ctx)
        {
            if (!MsgHandles.ContainsKey(protocolCode))
            {
                MsgHandles[protocolCode] = new List<Action<IMessage>>();
            }

            MsgHandles[protocolCode].Add(ctx);
        }

        public void UnRegisterMsgHandler(uint protocolCode, Action<IMessage> ctx)
        {
            if (MsgHandles.TryGetValue(protocolCode, out var handle))
            {
                handle.Remove(ctx);
            }
        }
#endif
```

#### 2.2 修改消息派发逻辑

在 `MessageHandler()` 方法的消息处理部分，**在现有逻辑之前**添加：

```csharp
#if FANTASY_UNITY
            // 先触发通过 GameClient RegisterMsgHandler 注册的回调
            // 方便客户端通过 GameClient 和协议号直接监听服务器数据的下发
            if (MsgHandles.TryGetValue(protocolCode, out var handlers))
            {
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    var handler = handlers[i];
                    try
                    {
                        handler.Invoke((IMessage)message);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"MsgHandler error, protocolCode:{protocolCode}, error:{e}");
                    }
                }
                return;
            }
#endif
```

---

### 3. Log.cs - 确认条件宏定义与 DLogger 保持一致

**文件位置**: `Assets/Scripts/HotFix/Fantasy.Unity/Runtime/Core/Log/Log.cs`

**检查内容**: 确认 `Debug`、`Info`、`Warning`、`Error` 方法的 `[Conditional]` 属性与 `DGame.DLogger` 保持一致（`Trace` 和 `TraceInfo` 除外）

#### 各日志级别应包含的 Conditional 属性：

**Debug 方法**:
```csharp
[Conditional("ENABLE_DGAME_LOG")]
[Conditional("ENABLE_DGAME_DEBUG_LOG")]
[Conditional("ENABLE_DGAME_DEBUG_AND_ABOVE_LOG")]
public static void Debug(string msg) { ... }
```

**Info 方法**:
```csharp
[Conditional("ENABLE_DGAME_LOG")]
[Conditional("ENABLE_DGAME_INFO_LOG")]
[Conditional("ENABLE_DGAME_DEBUG_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_INFO_AND_ABOVE_LOG")]
public static void Info(string msg) { ... }
```

**Warning 方法**:
```csharp
[Conditional("ENABLE_DGAME_LOG")]
[Conditional("ENABLE_DGAME_WARNING_LOG")]
[Conditional("ENABLE_DGAME_DEBUG_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_INFO_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_WARNING_AND_ABOVE_LOG")]
public static void Warning(string msg) { ... }
```

**Error 方法**:
```csharp
[Conditional("ENABLE_DGAME_LOG")]
[Conditional("ENABLE_DGAME_ERROR_LOG")]
[Conditional("ENABLE_DGAME_DEBUG_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_INFO_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_WARNING_AND_ABOVE_LOG")]
[Conditional("ENABLE_DGAME_ERROR_AND_ABOVE_LOG")]
public static void Error(string msg) { ... }
```

> **参考文件**: `DGame/Runtime/Core/DGameLog/DLogger.cs`

---

### 4. Obfuz 包引用检查

**检查内容**: 确认项目是否引入了 Obfuz 的包

**检查步骤**:

1. **检查 packages-lock.json 文件**
   - 打开 Unity 项目根目录下的 `Packages/packages-lock.json` 文件
   - 搜索 `com.code-philosophy.obfuz` 关键字
   - 如果找到该引用，说明项目已引入 Obfuz 包

   ```bash
   # 或使用命令快速检查
   grep "com.code-philosophy.obfuz" Packages/packages-lock.json
   ```

2. **添加 Obfuz.Runtime 引用**
   - 如果项目引用了 Obfuz 包，需要在 `Fantasy.Unity.asmdef` 文件和 `Fantasy.Editor.asmdef` 文件中添加对 `Obfuz.Runtime` 的引用

**修改方式**:

- 在 `Assets/Scripts/HotFix/Fantasy.Unity/Editor/Runtime/Fantasy.Editor.asmdef` 文件的 `references` 数组中添加：

```json
{
    "name": "Fantasy.Editor",
    "references": [
        "Fantasy.Unity",
        "Obfuz.Runtime"
    ],
    ...
}
```

- 在 `Assets/Scripts/HotFix/Fantasy.Unity/Fantasy.Unity.asmdef` 文件的 `references` 数组中添加：


```json
{
    "name": "Fantasy.Unity",
    "references": [
        "Obfuz.Runtime"
    ],
    ...
}
```

> **检查结果示例**:
>
> ```json
> // packages-lock.json 中存在以下内容则需添加引用
> "com.code-philosophy.obfuz": {
> "version": "https://github.com/focus-creative-games/obfuz.git",
> "depth": 0,
> "source": "git",
> ...
> }
> ```

---

## 使用示例

### 客户端监听服务器消息

```csharp
// 注册消息监听
GameClient.Instance.RegisterMsgHandler.RegisterMsgHandler(protocolCode, (message) =>
{
    // 处理消息
    var msg = (YourMessageType)message;
    // ...
});

// 取消监听
GameClient.Instance.UnRegisterMsgHandler.UnRegisterMsgHandler(protocolCode, handler);
```

---

## 检查清单

更新 Fantasy 框架后，请确认：

- [ ] Scene.cs 中 `MessageDispatcherComponent` 已改为 `public`
- [ ] MessageDispatcherComponent.cs 中已添加 `#if FANTASY_UNITY` 代码块
- [ ] `MessageHandler()` 方法中已添加客户端回调触发逻辑
- [ ] **Fantasy.Log 中 `Debug`、`Info`、`Warning`、`Error` 方法已添加条件宏定义**（`Trace` 和 `TraceInfo` 除外），防止打包输出相关的Debug代码
- [ ] 检查项目是否引入了 Obfuz 包，如果引用了，在 `Fantasy.Unity` 程序集和 `Fantasy.Editor` 程序集中添加 `Obfuz.Runtime` 引用
- [ ] 编译无错误