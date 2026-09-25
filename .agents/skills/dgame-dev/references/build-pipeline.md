# 构建打包工作流

> **适用场景**：Unity 整包、AssetBundle 资源包、Jenkins 命令行自动化打包 | **关联文档**：[hotfix-workflow.md](hotfix-workflow.md)、[hotpatch-workflow.md](hotpatch-workflow.md)

DGame 的构建统一收口在 `DGame.ReleaseTools`（`GameUnity/Assets/DGame/Editor/ReleaseTools/ReleaseTools.cs`）。菜单入口用于本地手动打包，`public static` 方法用于命令行/Jenkins 无人值守打包，两者共用同一套 `BuildInternal`（AB）和 `BuildImp`（整包）实现。

## 三类产物

| 产物 | 含义 | 输出目录 |
|------|------|----------|
| AssetBundle 资源包 | YooAsset `DefaultPackage` 资源，用于热更 | `Bundles/{平台}`（`一键打包AB` 走 `Builds/`） |
| Unity 整包 | 可执行安装包（exe/apk/XCode 工程） | `Build/{平台}` |
| StreamingAssets 内置资源 | 首包内置的 AB，随整包发布 | 由 `UpdateSettings` 的打包地址控制 |

所有菜单、窗口和命令行入口统一收口到 `ExecuteBuild`。构建会先切换目标平台；是否在完整构建前执行 GenerateAll 由入口显式决定，编译热更 DLL 本身不会隐式触发 GenerateAll。AB-only 构建不会自动 GenerateAll，适用于 AOT 未变化的日常热更资源构建。

## 前置：先转表

> **强制前置**：不管打 AB 还是打整包，**打包前必须先执行配置表转表**，保证包体内的配置表二进制是最新的。转表产物落在 `GameUnity/Assets/BundleAssets/Configs/Binary/`，会被 YooAsset 收集进 AB；漏转表会导致包内配置表停留在旧版本。

转表脚本在 `GameConfig/GenerateTool_Binary/`，客户端**优先使用懒加载转表** `gen_bin_client_lazyload.bat`（`.sh`）；导表规则见 `luban-dev`。`Tools/BuildTools/build_*` 脚本会在启动 Unity 前自动执行客户端 LazyLoad 转表；自定义流水线如果绕过这些脚本，仍需在调用 Unity 前显式转表。

`GameUnity/Configs/Json/` 是提交到 Git、用于审查配置变化的可读产物。该目录位于 Unity 的 `Assets` 之外，不会被 YooAsset 或 Player 收集，无需在构建前删除；包体使用的仍是 `GameUnity/Assets/BundleAssets/Configs/Binary/`。

## 前置：HybridCLR GenerateAll

> **强制前置**：在构建首包，或 AOT 程序集/泛型引用发生变化，且当前启用了 HybridCLR 热更新（`ENABLE_HYBRIDCLR` / `UpdateSettings.Enable` 为 true）时，**必须先执行一次 HybridCLR 的 GenerateAll**，再继续 BuildPlayer、`BuildDllCommand.BuildAndCopyDlls()` 和 AB 构建。

执行入口为 `HybridCLR/Generate/All`。当前 `ReleaseTools` 的菜单和命令行完整构建入口默认都不会自动执行 `GenerateAll`；可视化窗口提供独立按钮，首包或 AOT/泛型引用变化时应先手动执行。此步骤会刷新 HybridCLR 生成产物，例如 link、AOTGenericReferences、桥接/反向 PInvoke 等生成文件。

不涉及首包、不改 AOT、不启用 HybridCLR 热更时，不需要为了普通资源 AB 构建额外执行 GenerateAll。

## 菜单入口（本地手动）

| 菜单 | 方法 | 行为 |
|------|------|------|
| `DGame Tools/Build/一键打包AB` | `BuildCurrentPlatformAB`，快捷键 `F8` | 当前平台打 AB → `CopyStreamingAssetsFiles` |
| `DGame Tools/Build/AutoBuildWindow` | `AutoBuildWindow` | Windows AB + 整包 `Release_Windows.exe` |
| `DGame Tools/Build/AutoBuildAndroid` | `AutoBuildAndroid` | Android AB + 整包 `{版本}-Android.apk` |
| `DGame Tools/Build/AutoBuildIOS` | `AutoBuildIOS` | iOS AB + 整包 `XCode_Project` |

`AutoBuildXXX` 会先 `SwitchActiveBuildTarget`，再进入统一完整构建流程；这些入口当前都不自动执行 `GenerateAll`。为保持旧入口语义，这些完整构建入口显式关闭 `CopyToBuildAddress`；AB-only 和可视化窗口仍按各自的 `BuildPipelineConfig.CopyToBuildAddress` 决定是否同步。交互模式构建成功后用 `RevealInFinder` 打开输出目录；batchmode 不打开目录。

## 命令行入口（Jenkins 自动化）

`Tools/BuildTools/` 下的 `.bat`（Windows）/`.sh`（macOS）用 `-batchmode -quit -executeMethod` 拉起 Unity 无界面打包。命名规律：`build_{ab_,}{window,android}{_auto,_manual}`——带 `ab_` 只打资源包，不带则 AB+整包；`_auto` 自动版本号，`_manual` 交互输入版本。

| 脚本 | executeMethod | 版本 |
|------|---------------|------|
| `build_ab_window_auto` | `BuildWindowsAB` | 自动 |
| `build_ab_window_manual` | `BuildWindowsABWithVersion` | `-version` |
| `build_ab_android_auto` | `BuildAndroidAB` | 自动 |
| `build_ab_android_manual` | `BuildAndroidABWithVersion` | `-version` |
| `build_window` | `AutoBuildWindow` | 自动 |
| `build_window_manual` | `BuildWindowWithVersion` | `-version` |
| `build_android` | `AutoBuildAndroid` | 自动 |
| `build_android_manual` | `BuildAndroidWithVersion` | `-version` |

命令行模板（`.bat`）：

```bat
"%UNITYEDITOR_PATH%\Unity.exe" -projectPath "%WORKSPACE%" -batchmode -quit ^
  -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.BuildWindowsAB ^
  -CustomArgs:Language=en_US;"%WORKSPACE%"
```

手动版本号在方法内通过 `GetCommandLineArg("-version")` 读取，脚本追加 `-version=%VERSION%`。

## 环境变量

所有脚本 `source`/`call` 同目录 `path_define`，Jenkins 节点按机器改这里即可：

| 变量 | 含义 | 示例 |
|------|------|------|
| `WORKSPACE` | Unity 工程目录 | `E:\UnityProject\DGame\GameUnity` |
| `UNITYEDITOR_PATH` | Unity 编辑器目录 | `E:\Editor\2022.3.62f3\Editor` |
| `BUILD_LOGFILE` | 打包日志 | `./Log/build.log` |
| `BUILD_DLL_LOGFILE` | DLL 编译日志 | `./Log/build_dll.log` |

> `path_define_tmp.{bat,sh}` 是占位模板，实际值以 `path_define.bat` 为准；新节点从模板复制后填真实路径。

## 版本号

- 自动：`GetBuildPackageVersion()` → `UpdateSettings.GetBuildPackageVersion()`，格式 `yyyy-MM-dd-分钟段`（每 10 分钟一段）。
- 手动：命令行 `-version` 传入，作为 YooAsset `PackageVersion`。

版本号最终写入 `BuildInternal` 的 `BuildParameters.PackageVersion`，决定 YooAsset 清单版本。

## AssetBundle 构建要点（BuildInternal）

- 管线默认 `ScriptableBuildPipeline`，压缩 `LZ4`；包名由构建配置读取，项目默认是 `DefaultPackage`。
- `ClearBuildCacheFiles = false` + `UseAssetDependencyDB = true`：启用增量构建，加快打包。
- `EnableSharePackRule = true`：共享资源打包；内置 Shader 单独成包（`GetBuiltinShaderBundleName`）避免重复。
- 加密服务从 `GameEntry.prefab` 的 `ResourceModuleDriver.EncryptionType` 取，与运行时解密一致。
- `ForceGenerateAtlas` 开启时同步原地刷新全部有效图集；只有全部更新成功后才清理孤儿图集，并保留既有 GUID，完成后才开始 AB 构建。
- AB-only 和可视化窗口打完 AB 后，`CopyStreamingAssetsFiles` 受 `BuildPipelineConfig.CopyToBuildAddress` 控制；旧 `AutoBuildXXX` 完整构建入口显式关闭该选项，避免额外覆盖外部 StreamingAssets 目录。

## Jenkins 落地

1. 节点安装对应 Unity 版本，改 `path_define` 的 `WORKSPACE`/`UNITYEDITOR_PATH`。
2. 拉取代码后调用对应 `build_*` 脚本；脚本先转表再启动 Unity。所有当前完整构建入口都不会自动执行 `GenerateAll`，首包或 AOT/泛型引用变化时需在流水线中显式执行。AB-only 构建仅用于 AOT 未变化的资源热更。
3. `-quit` 后用退出码判断成功失败并归档 `BUILD_LOGFILE`；Windows 脚本通过 `run_unity_with_log.ps1` 将 Unity 日志实时输出到控制台并同步写入 UTF-8 日志文件，且保留 Unity 原始退出码。当前所有 Windows `.bat` 在成功或失败后都会 `pause`，避免本地双击运行时窗口直接关闭。
4. 需要刷新 HybridCLR 生成产物时，确认对应入口会执行 GenerateAll，或在构建前显式执行；BuildPlayer 仍负责生成裁剪 AOT 产物。

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| 打包前没转表 | 任何 AB/整包构建前先转表，确保 `BundleAssets/Configs/Binary/` 是最新配置 |
| 首包或 AOT 变动后未执行 GenerateAll | 先显式执行 `HybridCLR/Generate/All`，或使用明确启用该步骤的完整构建入口 |
| AB-only 缺少裁剪 AOT DLL | DLL 复制会返回失败并终止构建；先执行完整构建生成 AOT 产物 |
| Jenkins 直接运行 Windows `.bat` | 当前脚本完成后会 `pause`；无人值守流水线需提供标准输入或在 CI 包装层处理暂停 |
| 改了 `path_define_tmp` 期待生效 | 实际读 `path_define.bat`，改这个 |
| AB 打完真机加载不到 | 确认 `IsAutoAssetCopyToBuildAddress` 已开，AB 已复制到首包内置目录 |
| 手动打包漏传版本 | `*WithVersion` 方法缺 `-version` 会直接报错返回 |

## 交叉引用

| 关联主题 | 文档 |
|---------|------|
| 热更 DLL 编译与 AOT metadata | [hotfix-workflow.md](hotfix-workflow.md) |
| YooAsset 包版本、下载器、缓存清理 | [hotpatch-workflow.md](hotpatch-workflow.md) |
| 资源加载与 BundleAssets 落位 | [resource-api.md](resource-api.md) |
