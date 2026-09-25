# DGame 评估案例

`evals.json` 保留 15 个案例的 ID 和提示词。`schemaVersion: 1` 区分两类规则：

- `expected_literals` / `forbidden_literals`：区分大小写的字面量子串匹配，不解释正则字符。`Entrance(object[]`、`objects[0]` 和 API 名称放在这里。
- `expected_patterns` / `forbidden_patterns`：Python `re.search` 正则，默认区分大小写、不启用 DOTALL。JSON 内的反斜杠需要写成 `\\`。

每个案例至少有一个 expected 规则；所有 expected 规则都应命中，所有 forbidden 规则都不应命中。未使用的规则列表可为空或省略。

## 格式门禁

从仓库根目录运行 `python .agents/scripts/workflow.py check`，或 `verify --profile docs/full`，会校验案例字段、唯一正整数 ID、唯一名称、非空提示和正则语法。报告中的 `evaluations` 记录案例数、各类规则数、案例内容哈希及 `scope: format-only`。

格式检查不会生成回答，也不代表 15 个案例的回答评估已通过。

## 实际回答记录

将已有回答整理成 JSON 文件，`id` 对应案例 ID，`source` 记录实际模型、会话或产物来源。例如以下结构中的 output 应替换为实际回答全文：

```json
{
  "skill_name": "dgame-dev",
  "responses": [
    {"id": 3, "source": "模型/会话/产物来源", "output": "实际回答全文"}
  ]
}
```

```powershell
python .agents/scripts/workflow.py eval --responses answers.json
```

结果保存在 `.agents/runs/<run-id>/eval-results.json`，包括案例与回答快照、来源、内容哈希、每条规则命中结果和统计；Workflow 的 `report.json`、`report.md` 记录最终状态及结果路径。

规则失败标记 `failed`；缺少回答的案例标记 `skipped`，没有失败但覆盖不全时整次评估为 `blocked`；只有全部案例提供回答且规则通过才为 `passed`。重复或未知 ID、空回答、缺失来源会被拒绝。

文本规则只能提供辅助证据：回答在反例或否定句中提及禁用 API 也可能命中，正确语义但措辞不同也可能漏报。结果使用 `scope: text-match-only`；代码正确性、生命周期和实际 Unity 行为仍需人工复核及对应测试。测试夹具只能用于验证匹配器，不作为真实模型评估成绩。
