# DGame Agent Skills

`.agents/` 是仓库内唯一的 Agent Skills 与验证工具目录。两个工具共用根目录的 `AGENTS.md`，再按任务读取这里的技能：

- `skills/dgame-dev/SKILL.md`：DGame Unity 业务与架构
- `skills/luban-dev/SKILL.md`：Luban 配置、校验与导表
- `skills/unity-cli/SKILL.md`：Unity CLI、Workflow 与验证

技能目录遵循 Agent Skills 结构：入口为 `SKILL.md`，详细资料放在 `references/`，可执行辅助脚本放在 `scripts/`。`agents/openai.yaml` 只提供 Codex 的界面元数据，不影响 Claude Code 读取 `SKILL.md`。

从仓库根目录运行 Workflow：

```powershell
python .agents/scripts/workflow.py check
```

Claude Code 原生会读取仓库级 `AGENTS.md`。Claude 的技能菜单自动发现仍由其自身的 `.claude/skills` 目录控制；在只保留 `.agents` 的布局下，应通过 `AGENTS.md` 的任务路由读取对应 `SKILL.md`。Codex 使用同一入口和 `.agents/skills` 内容。
