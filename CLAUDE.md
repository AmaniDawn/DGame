# Claude Code 项目入口

@AGENTS.md

公共项目规范、技能路由和验证入口见上方导入的 `AGENTS.md`。涉及 Unity 客户端、DGame 框架或 Unity 资源时，先读取 [GameUnity/AGENTS.md](GameUnity/AGENTS.md)；涉及 Fantasy 服务端、协议或服务端配置时，先读取 [GameServer/AGENTS.md](GameServer/AGENTS.md)。规则分别维护在对应的 `AGENTS.md` 中。

按路由直接读取 `.agents/skills/<技能名>/SKILL.md` 及相关 reference；技能菜单未列出时仍使用这些文件。项目验证从仓库根目录运行 `.agents/scripts/workflow.py`，具体参数遵循共享规范。
