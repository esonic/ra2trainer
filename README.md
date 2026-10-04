# Red Alert 2 Money Trainer

## English

A minimal money editor for the Steam versions of Red Alert 2 and Yuri's Revenge. Built with C# and WinForms on .NET Framework 4.8.

### Usage

Runtime: Windows 11 x64.

1. Run `Ra2MoneyTrainer.exe`, then start the game and enter a single-player match. Either startup order is supported.
2. The trainer automatically detects the game and displays your current credits.
3. Enter the desired amount in **Amount**, then click **Set money**, or press **Ctrl + Numpad 1** in-game (with Num Lock enabled). The hotkey applies the amount entered in the trainer, so you do not need to switch windows. It also works while the trainer is minimized.

The trainer registers this global hotkey while running and releases it on exit. Holding the hotkey will not trigger it repeatedly. If another program already uses it, the window will show a registration failure notice; you can still change the amount with **Set money**.

The trainer automatically detects the game and current amount. Setting money is a one-time change; the amount is not locked. After the game exits, the trainer keeps waiting for the next launch. Run only one game instance at a time.

Windows 11 includes .NET Framework 4.8 or later, so no additional runtime installation is required. If access is denied while the game is running as an administrator, run the trainer as an administrator too.

For single-player campaigns and skirmishes only. Other game versions or mods may require address adjustments.

### Development and Build

In Visual Studio Installer, install the **.NET desktop development** workload and the **.NET Framework 4.8 Targeting Pack**.

Open `src/Ra2MoneyTrainer.slnx` or `src/Ra2MoneyTrainer.csproj` in Visual Studio 2026, select **Release**, and build. The project targets x64.

Output: `src/bin/Release/net48/Ra2MoneyTrainer.exe`. Distribute this EXE alone; no additional DLLs or runtime configuration files are needed, and no single-file publishing step is required. `app.manifest` is embedded in the EXE.

You can also build on a Windows PC with the .NET SDK and .NET Framework 4.8 Targeting Pack installed:

```powershell
dotnet build src/Ra2MoneyTrainer.csproj -c Release
```

With the project root open in VS Code, press `Ctrl+Shift+B` to run the same Release build.

## 简体中文

一个极简的红色警戒2金钱修改器，支持 Steam 版《红色警戒2》和《尤里的复仇》。使用 C# + WinForms（.NET Framework 4.8）开发。

### 使用

运行环境：Windows 11 x64。

1. 运行 `Ra2MoneyTrainer.exe`，启动游戏并进入单人战局，启动顺序不限。
2. 程序自动识别游戏并显示当前金额。
3. 在 **Amount** 中输入目标金额，点击 **Set money**，或在游戏里按 **Ctrl + 小键盘 1**（开启 Num Lock）。快捷键会设置为 Amount 中的金额，无需切换窗口，修改器最小化时也可使用。

修改器运行期间会注册此全局快捷键，退出时释放；长按不会连续触发。如果快捷键被其他程序占用，窗口会显示注册失败提示，仍可点击 **Set money** 修改金额。

程序自动探测游戏及当前金额。修改为一次性设置，不会锁定金额。游戏退出后会继续等待下次启动；请只运行一个游戏实例。

Windows 11 自带 .NET Framework 4.8 或更新版本，无需额外安装运行时。若提示访问权限不足，且游戏以管理员身份运行，请也以管理员身份运行修改器。

仅用于单人战役和遭遇战。其他版本或 MOD 可能需要调整地址。

### 开发与构建

在 Visual Studio Installer 中安装“.NET 桌面开发”工作负载和 **.NET Framework 4.8 Targeting Pack**。

在 Visual Studio 2026 中打开 `src/Ra2MoneyTrainer.slnx` 或 `src/Ra2MoneyTrainer.csproj`，选择 **Release** 后生成。项目编译为 x64。

输出文件：`src/bin/Release/net48/Ra2MoneyTrainer.exe`。只需分发这个 EXE，不需要额外 DLL 或运行配置文件，也不需要单文件发布步骤。`app.manifest` 会嵌入 EXE。

也可在安装了 .NET SDK 和 4.8 Targeting Pack 的 Windows 电脑上执行：

```powershell
dotnet build src/Ra2MoneyTrainer.csproj -c Release
```

VS Code 打开项目根目录后，按 `Ctrl+Shift+B` 执行相同的 Release 构建。
