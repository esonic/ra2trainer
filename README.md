# Red Alert 2 Money Trainer

一个极简的红色警戒2金钱修改器，支持 Steam 版《红色警戒2》和《尤里的复仇》。使用 C# + WinForms（.NET Framework 4.8）开发。

## 使用

运行环境：Windows 11 x64。

1. 运行 `Ra2MoneyTrainer.exe`，启动游戏并进入单人战局，启动顺序不限。
2. 程序自动识别游戏并显示当前金额。
3. 在 **Amount** 中输入目标金额，点击 **Set money**。

程序自动探测游戏及当前金额。修改为一次性设置，不会锁定金额。游戏退出后会继续等待下次启动；请只运行一个游戏实例。

Windows 11 自带 .NET Framework 4.8 或更新版本，无需额外安装运行时。若提示访问权限不足，且游戏以管理员身份运行，请也以管理员身份运行修改器。

仅用于单人战役和遭遇战。其他版本或 MOD 可能需要调整地址。

## 开发与构建

在 Visual Studio Installer 中安装“.NET 桌面开发”工作负载和 **.NET Framework 4.8 Targeting Pack**。

在 Visual Studio 2026 中打开 `src/Ra2MoneyTrainer.slnx` 或 `src/Ra2MoneyTrainer.csproj`，选择 **Release** 后生成。项目编译为 x64。

输出文件：`src/bin/Release/net48/Ra2MoneyTrainer.exe`。只需分发这个 EXE，不需要额外 DLL 或运行配置文件，也不需要单文件发布步骤。`app.manifest` 会嵌入 EXE。

也可在安装了 .NET SDK 和 4.8 Targeting Pack 的 Windows 电脑上执行：

```powershell
dotnet build src/Ra2MoneyTrainer.csproj -c Release
```

VS Code 打开项目根目录后，按 `Ctrl+Shift+B` 执行相同的 Release 构建。
