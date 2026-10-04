# Red Alert 2 Money Trainer

一个极简的红色警戒2金钱修改器，支持 Steam 版《红色警戒2》和《尤里的复仇》。使用 C# + WinForms 开发，界面为英文。

## 使用

运行环境：Windows 11 x64。

1. 运行 `Ra2MoneyTrainer.exe`，启动游戏并进入单人战局，启动顺序不限。
2. 程序自动识别游戏并显示当前金额。
3. 在 **Amount** 中输入目标金额，点击 **Set money**。

程序每 3 秒探测游戏并刷新金额。修改为一次性设置，不会锁定金额。游戏退出后会继续等待下次启动；请只运行一个游戏实例。

依赖运行时的发布版本需要安装 **.NET 10 Desktop Runtime x64**，自包含版本无需额外安装 .NET。若提示访问权限不足，且游戏以管理员身份运行，请也以管理员身份运行修改器。

仅用于单人战役和遭遇战。其他版本或 MOD 可能需要调整地址。

## 开发与构建

安装 .NET 10 SDK；使用 Visual Studio 时，需支持 .NET 10 并安装“.NET 桌面开发”工作负载。

在 Visual Studio 中打开 `src/Ra2MoneyTrainer.slnx` 或 `src/Ra2MoneyTrainer.csproj`，即可生成或调试。也可在项目根目录执行：

```powershell
dotnet build src/Ra2MoneyTrainer.csproj -c Release
```

输出目录：`src/bin/Release/net10.0-windows/`。

### VS Code 发布

在 VS Code 中打开项目根目录，按 `Ctrl+Shift+B` 默认执行 **Publish single EXE (small)**，输出到 `artifacts/win-x64-small/`。该版本需要 .NET 10 Desktop Runtime x64。

如需包含运行时，执行“终端 → 运行任务”，选择 **Publish single EXE (self-contained)**，输出到 `artifacts/win-x64/`。

任务定义在 `.vscode/tasks.json`。分发发布目录里的 `Ra2MoneyTrainer.exe` 即可；普通 `dotnet build` 的输出仍由多个文件组成。

### 命令行发布

小体积单文件版本，需要 .NET 10 Desktop Runtime x64：

```powershell
dotnet publish src/Ra2MoneyTrainer.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o artifacts/win-x64-small
```

自包含单文件版本，包含运行时：

```powershell
dotnet publish src/Ra2MoneyTrainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/win-x64
```

## 地址适配

地址定义在 [src/GameProfiles.cs](src/GameProfiles.cs)，修改后重新编译。当前配置：

| 游戏 | 进程 | 玩家指针入口 | 金钱偏移 |
|---|---|---|---|
| Red Alert 2 | `game.exe` | 主 EXE 基址 + `0x635DB4` | `0x24C` |
| Yuri’s Revenge | `gamemd.exe` | 绝对地址 `0x00A83D4C` | `0x30C` |

程序读取入口处的 32 位指针，再加金钱偏移，得到金钱字段地址。每次操作重新读取指针，不缓存玩家地址。

配置字段：

- `ProcessName`：进程名，不带 `.exe`。
- `Address`：绝对地址，或主 EXE 模块内偏移。
- `ModuleRelative`：为 `true` 时，将主 EXE 基址加到 `Address`。
- `Offsets`：按访问顺序排列；每一步先读取 32 位指针，再加该偏移。空数组表示 `Address` 本身就是金钱字段。

例如原版的定位过程是：

```text
玩家地址 = ReadUInt32(game.exe 基址 + 0x635DB4)
金钱地址 = 玩家地址 + 0x24C
```

仅支持主 EXE 模块相对地址。使用直接金钱地址时，重启、读档或换关可能使地址失效。适配新版本后，应验证金额读写、重启、换关和读档。
