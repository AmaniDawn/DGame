# DGame 协议与配置链路

## 配置链路

配置的真实源头在 `GameConfig`。

关键目录：

- `GameConfig/Datas/Defines`
  Luban 元信息定义

- `GameConfig/Datas/运营`、`GameConfig/Datas/通用`、`GameConfig/Datas/道具`
  实际业务配置表

- `GameConfig/CustomTemplate/Client`
  客户端配置代码生成模板

- `GameConfig/CustomTemplate/Server`
  服务端配置代码生成模板

- `GameConfig/GenerateTool_Binary`
  导出二进制配置脚本

- `GameConfig/GenerateTool_Json`
  导出 Json 配置脚本

- `GameConfig/luban.conf`
  生成总入口

处理配置问题时，按这个顺序判断：

1. 先看源表和定义
2. 再看模板和导表脚本
3. 最后看客户端或服务端消费侧代码

## 共享协议链路

客户端与服务端共享的协议定义主源头在：

- `GameServer/Tools/NetworkProtocol/Inner`
  服务器与服务器之间通讯协议

- `GameServer/Tools/NetworkProtocol/Outer`
  客户端与服务器之间通讯协议

- `GameServer/Tools/NetworkProtocol/RouteType.Config`
  路由定义

- `GameServer/Tools/NetworkProtocol/RoamingType.Config`
  协议辅助定义

- `GameServer/Tools/NetworkProtocol/OpCode.Cache`
  协议缓存或导出辅助数据

- `GameServer/Tools/ProtocolExportTool`
  协议导出工具

- `GameServer/Tools/协议可视化工具`
  协议可视化工具

## 源头与消费侧的关系

主源头：

- `GameConfig` 负责配置定义
- `GameServer/Tools` 负责共享协议定义

消费侧或生成侧：

- `GameUnity/Assets/Scripts/HotFix/GameProto`
  客户端协议与配置消费代码

- `GameServer/Server/Entity/Generate`
  服务端配置与协议消费代码

结论：

- 改定义，先改主源头
- 改消费逻辑，才改客户端或服务端消费侧
- 不要优先手改生成结果

## 常见任务落点

| 常见任务 | 优先修改哪里 |
|---------|---------|
| 新增配置表字段 | `GameConfig/Datas` |
| 调整配置生成规则 | `GameConfig/CustomTemplate/*`、`luban.conf` |
| 新增客户端与服务端协议 | `GameServer/Tools/NetworkProtocol/Outer` |
| 新增服内通讯协议 | `GameServer/Tools/NetworkProtocol/Inner` |
| 修改路由定义 | `GameServer/Tools/NetworkProtocol/RouteType.Config` |
| 修改协议导出逻辑 | `GameServer/Tools/ProtocolExportTool` |
| 修改客户端配置消费逻辑 | `GameUnity/Assets/Scripts/HotFix/GameProto` |
| 修改服务端配置消费逻辑 | `GameServer/Server/Entity/Generate` |
