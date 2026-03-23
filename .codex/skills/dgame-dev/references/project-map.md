# DGame 项目地图

当需求不明确该把代码放在哪一层、哪个目录时，先读这个文件。

## 仓库根目录

- `GameUnity/`：Unity 主工程目录，包含解决方案、程序集、包和项目设置。
- `GameConfig/`：游戏配置源数据及相关生成输入。
- `Tools/`：构建、生成和辅助工具。
- `GameRelease/`：发布输出目录。

## Unity 代码布局

### 框架层

- `GameUnity/Assets/DGame/Runtime/Core`：底层框架系统。
- `GameUnity/Assets/DGame/Runtime/Module`：可复用运行时模块。
- `GameUnity/Assets/DGame/Runtime/Setting`：运行时设置及相关支持文件。

### 编辑器层

- `GameUnity/Assets/DGame/Editor/UnityToolBarExtend`：工具栏扩展和启动辅助。
- `GameUnity/Assets/DGame/Editor/Settings`：初始化和入口辅助。
- `GameUnity/Assets/DGame/Editor/LubanTools`：配置表相关编辑器工具。
- `GameUnity/Assets/DGame/Editor/HybridCLR`：HybridCLR 编辑器流程。
- `GameUnity/Assets/DGame/Editor/ReleaseTools`：发布和构建辅助工具。
- `GameUnity/Assets/DGame/Editor/SpineModelHelper`：Spine 相关辅助工具。

### HotFix 层

- `GameUnity/Assets/Scripts/HotFix/GameBase`：可复用的 HotFix 基础代码。
- `GameUnity/Assets/Scripts/HotFix/GameProto`：协议和配置相关 HotFix 代码。
- `GameUnity/Assets/Scripts/HotFix/GameLogic`：玩法与功能逻辑。

## GameLogic 子区域

- `Common`：功能侧复用逻辑。
- `DataCenter`：玩家数据、存档等数据归属逻辑。
- `Editor`：功能侧编辑器代码。
- `GameTickWatcher`：帧和 Tick 监听相关。
- `IEvent`：事件抽象和包装。
- `Module`：更高层的玩法模块。
- `UI`：界面、窗口和 UI 相关功能代码。
- `Utility`：本地辅助工具类。

## 场景与启动

- 主启动场景路径：`GameUnity/Assets/Scenes/GameStart`

## 落位判断规则

- 如果代码要在运行时对所有游戏生效，优先放到 `DGame/Runtime`。
- 如果代码属于业务逻辑、热更逻辑或具体功能行为，优先放到 `HotFix/GameLogic`。
- 如果代码只用于编辑器、生成、菜单、Inspector 或发布流程，优先放到 `DGame/Editor`。
- 如果文件来自配置生成或工具产物，修改前先回溯到 `GameConfig` 或 `Tools`。
