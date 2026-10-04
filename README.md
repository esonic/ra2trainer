# 红警2金钱修改器

C# + WinForms，仅提供一次性设置金钱。界面全部使用英文，只有游戏状态、当前金额、目标金额和修改按钮。面向 Windows 11 x64，目标游戏为 32 位 x86。

## 使用方式

修改器和游戏的启动顺序不限。程序启动时立即探测，之后每 3 秒探测一次：`game.exe` 识别为红色警戒2，`gamemd.exe` 识别为尤里的复仇。游戏退出后恢复等待，下次启动会重新连接，无需手动选择游戏。如果两款游戏同时运行或检测到多个匹配进程，会提示只保留一个。

进入单人战局且能读取金额后，修改按钮自动可用。输入目标金额并点击即可，没有核对复选框。金额每 3 秒刷新，但只在点击时写入。若游戏以管理员身份运行而出现访问拒绝，请也以管理员身份运行修改器。

## 地址修改

所有地址集中在 `src/GameProfiles.cs`，修改后重新编译。

- 红色警戒2：`Address = null`，地址尚未确认，填写实际地址后才能改钱。
- 尤里的复仇：`Address = 0x00A83D4C`，`Offsets = [0x30C]`。
- 进程名也在该文件中，若安装版本不同，请按任务管理器“详细信息”页修改，不带 `.exe`。

### 直接金钱地址

把 CE 找到的实际地址填入 `Address`，设置 `ModuleRelative = false` 和 `Offsets = []`。例如下面仅演示写法，地址不是实际游戏地址：

```csharp
Address = 0x12345678,
ModuleRelative = false,
Offsets = []
```

直接地址可能在重启、读档或换关后失效，建议定位稳定的指针链。

### 玩家指针加偏移

尤里的默认地址表示 `读取32位指针(0x00A83D4C) + 0x30C`。多个偏移按从基地址到最终字段的访问顺序填写；`Offsets = [0x10, 0x20]` 表示 `读取指针(读取指针(Address) + 0x10) + 0x20`。CE 界面可能把最终字段偏移显示在最上方，请按访问顺序转换。

### 模块相对地址

如果 CE 显示 `gamemd.exe+偏移`，把偏移填入 `Address`，设置 `ModuleRelative = true`，程序会加上所选游戏主 EXE 的基址。剩余指针链填写到 `Offsets`；若该位置本身就是金钱字段，使用空数组。仅支持主 EXE 模块，不支持其他 DLL 基址。

## Windows 构建

安装支持 .NET 10 的 Visual Studio 和“.NET 桌面开发”工作负载，打开 `src/Ra2MoneyTrainer.csproj`，选择 Release 后生成，或按 F5 调试。普通生成的输出在 `src/bin/Release/net10.0-windows/`。

也可以使用 .NET 10 SDK 命令行生成：

```powershell
dotnet build src/Ra2MoneyTrainer.csproj -c Release
```

小体积单文件发布（运行电脑需安装 .NET 10 Desktop Runtime x64）：

```powershell
dotnet publish src/Ra2MoneyTrainer.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o artifacts/win-x64-small
```

自包含单文件发布（运行电脑无需额外安装 .NET）：

```powershell
dotnet publish src/Ra2MoneyTrainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/win-x64
```

可选的地址逻辑检查：

```powershell
dotnet run --project tests/AddressChecks.csproj
```
