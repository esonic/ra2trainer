# 红警2金钱修改器

C# + WinForms，仅提供一次性设置金钱。独立打开，不需要把 DLL 放进游戏目录。面向 Windows 11 x64，目标游戏为 32 位 x86。

## 当前适配状态

- 红色警戒2：已提供独立配置和进程识别，**默认地址留空，需要用 Cheat Engine 校准后才能改钱**。
- 尤里的复仇：默认地址参考 AdjWang/RA2YurisRevengeTrainer 的 v4.2 `src/trainer.cpp`（玩家指针 `0x00A83D4C`，金钱偏移 `0x30C`），**尚未在 Steam 游戏上实测**。
- 当前工具不检测游戏文件版本。读出一个合理数字也不能证明地址正确；请与游戏金额对照，并通过金额变化确认。联机模式不在支持范围内。

## 使用

1. 解压整个发布包，保持 `Ra2MoneyTrainer.exe` 和 `addresses.json` 在同一目录。
2. 启动游戏并进入单人战局，再打开修改器，选择对应游戏。
3. 确认显示的金额与游戏一致，花钱或采矿后再观察两边金额是否同步。
4. 勾选“已核对当前金额与游戏一致”，输入目标金额并点击“修改金钱”。
5. 如果提示权限不足，且游戏以管理员身份运行，请也以管理员身份运行修改器。

修改器每秒读取金额，但只在点击按钮时写入。游戏退出、连接失败或玩家地址变化会取消核对状态。写入和回读不是游戏线程内的原子操作，请避开加载存档和换关；必要时暂停战局再修改。一次性修改不会在退出修改器时还原，保存游戏可能会保存修改后的金额。

## 用 Cheat Engine 校准

点击“编辑地址配置”，保存后点击“重新加载配置”。不需要重新编译。地址及偏移用带 `0x` 前缀的十六进制字符串；不带前缀则按十进制解析。进程名不带 `.exe`，根据任务管理器“详细信息”页确认；默认本体为 `game`，尤里为 `gamemd`。

### 直接金钱地址（适合先验证）

假设 CE 本局找到的金钱地址是 `0x12345678`，修改对应游戏配置为：

```json
"address": "0x12345678",
"moduleRelative": false,
"offsets": []
```

这里的地址只是示例。直接地址可能在重启、换关或读档后失效，需要重新定位。

### 玩家指针加金钱偏移

尤里的默认配置表示 `读取32位指针(0x00A83D4C) + 0x30C`：

```json
"address": "0x00A83D4C",
"moduleRelative": false,
"offsets": ["0x30C"]
```

多个偏移按从基地址向金钱字段的访问顺序排列：`["0x10", "0x20"]` 表示 `读取指针(读取指针(address) + 0x10) + 0x20`。CE 界面可能把最靠近最终字段的偏移显示在最上方，请按访问顺序转换。

### 模块相对地址

如果 CE 显示 `gamemd.exe+某偏移`，把该偏移填进 `address`，设置 `moduleRelative: true`。程序会加上所选游戏主 EXE 的基址。然后用 `offsets` 表示剩余指针链；若该位置本身就是金钱，则填空数组。只支持主 EXE，不支持其他 DLL 基址。

## 编译与验证

安装 .NET 10 SDK。Windows PowerShell 执行：

```powershell
dotnet run --project tests/AddressChecks.csproj -- src/Ra2MoneyTrainer/addresses.json
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

发布结果在 `artifacts/win-x64`，运行时包含在 EXE 中，用户无需额外安装 .NET。首次运行可能在临时目录解压原生运行时文件。

Linux 可以交叉编译：

```sh
dotnet publish src/Ra2MoneyTrainer/Ra2MoneyTrainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/win-x64
```

地址解析和配置检查可以在 Linux 执行，WinForms 界面、实际进程权限及游戏改钱必须在 Windows 上验证。请分别检查两款游戏：启动与退出、未进入战局、花钱/采矿同步、设置金额、读档、换关、重启后定位，以及错误配置提示。

参考仓库单独保留在 `RA2YurisRevengeTrainer/`。本工具独立实现，没有复制其注入、Hook、前端或依赖代码。
