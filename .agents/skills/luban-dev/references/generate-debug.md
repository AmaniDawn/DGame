# Luban 校验与生成排错

## 先取得真实错误

```powershell
python .agents/skills/luban-dev/scripts/luban_ai.py validate --target client
```

同时读取退出码、stdout JSON 的 `ok` / `errors` 和 stderr。优先使用错误中的文件、位置与字段路径定位；helper 的诊断只作补充。

需要检查模板和产物时运行 `python .agents/scripts/workflow.py luban test`，而不是对生产目录重复导表。错误来自新版本时，确认 `install.json`、版本锁及实际生成器一致。

## 排查顺序

| 类别 | 核对内容 |
| --- | --- |
| CLI | `--conf`、目标是否存在、工具版本与代码/数据格式是否匹配 |
| Schema | `value_type`、同名 Bean、继承、主键/mode、字段分组、注册与 `read_schema_from_file` |
| Data | 单元格类型、枚举名/别名、必填值、原始表头、多行或嵌套格式 |
| Validation | 引用目标、范围、路径根目录、容器大小；按失败数据修复 |
| Codegen | 标识符、语言关键字、自定义模板与生成器版本 |

错误分类和位置以实际 JSON 为准；上游某类异常可能缺少位置字段，此时结合原始错误和源定义搜索，不能编造行列号。

## 常见情况

- 新表没有输出：检查 `Datas/Defines/__tables__.xlsx` 的目标行、客户端分组和实际 Sheet。普通 Excel Sheet 的首单元格应以 `##` 开头。
- Bean 重复：`read_schema_from_file=true` 已从表头得到行类型时，不要在 `__beans__` 再定义同名 Bean。
- 字段错位：检查全部 `##var/##type` 续行与分组/注释原序，不把第二行类型当作数据。
- 引用校验失败：使用 `describe` / `schema` 确认表全名、目标组和主键，修正源数据。禁止为了生成通过而删除校验或添加 `unchecked`。
- 资源路径失败：按项目真实资源地址核对；主 CLI 的路径验证需明确 `pathValidator.rootDir`。
- 表尾空白警告：先用 helper `cleanup blank-rows TABLE` 预览，仅清理已确认的表尾全空行；不删除中间空行。
- 输出目录清理误删手写文件：先检查输出配置，生成目录只能存放生成产物；不要把业务源码目录作为临时导表目标。

## 主 CLI 结构化诊断

正式生成器已固定为 5.1.0，支持 `--errorFormat json` 与 `--strict`。客户端无产物校验可直接运行：

```powershell
dotnet GameConfig/Tools/LubanTools/Luban/Luban.dll --conf GameConfig/luban.conf -t client -f --strict --errorFormat json -x outputSaver=null
```

此处 `outputSaver=null` 不写产物。真正生成继续使用项目脚本与 Workflow；不要为了诊断重放带有生产输出和清理参数的命令。

## 修复后的验证

数据或 Schema 改动先用官方 `validate` 验证，再按影响范围检查隔离导表、生成代码、Binary 和运行时消费。结构变更或模板变更需要当前 DGame Unity 编译证据；测试没有可运行用例、Editor 不可达或命令超时时，记录实际限制。
