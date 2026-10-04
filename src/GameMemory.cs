using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Ra2MoneyTrainer;

internal sealed class GameMemory : IDisposable
{
    private const uint Query = 0x1000, Read = 0x10, Write = 0x20, Operation = 0x8;
    private readonly SafeProcessHandle handle;
    private readonly Process process;
    private readonly GameProfile profile;
    private readonly uint moduleBase;
    public int Pid => process.Id;
    public string ExecutableName => process.ProcessName;

    public GameMemory(Process process, GameProfile profile, bool writable)
    {
        this.process = process;
        this.profile = profile;
        handle = OpenProcess(Query | Read | (writable ? Write | Operation : 0), false, process.Id);
        try
        {
            if (handle.IsInvalid) throw Error("打开游戏进程");
            if (!IsWow64Process2(handle, out ushort machine, out ushort nativeMachine))
                throw Error("检测游戏位数");
            if (machine != 0x014c && !(machine == 0 && nativeMachine == 0x014c))
                throw new InvalidOperationException("目标不是 32 位 x86 游戏进程，请检查进程名。");
            if (profile.ModuleRelative)
                moduleBase = checked((uint)(process.MainModule
                    ?? throw new InvalidOperationException("无法读取游戏主模块。")).BaseAddress.ToInt64());
        }
        catch { handle.Dispose(); throw; }
    }

    public (uint Address, int Amount) ReadMoney()
    {
        if (process.HasExited) throw new InvalidOperationException("游戏已经退出。");
        uint address = AddressMath.Resolve(profile, moduleBase, ReadUInt32);
        return (address, unchecked((int)ReadUInt32(address)));
    }

    public void SetMoney(int amount)
    {
        // Resolve again on every operation; never cache a player pointer across maps/saves.
        var current = ReadMoney();
        if (current.Amount < 0) throw new InvalidOperationException("读到负数金额，请先校准地址。");
        // Recheck immediately before writing to reduce the chance of a stale player pointer.
        if (ReadMoney().Address != current.Address)
            throw new InvalidOperationException("玩家地址已变化，请等待战局稳定后重试。");
        byte[] bytes = BitConverter.GetBytes(amount);
        if (!WriteProcessMemory(handle, (nint)current.Address, bytes, 4, out nuint count))
            throw Error("写入金钱");
        if (count != 4) throw new IOException("金钱写入不完整。");
        if (ReadMoney().Amount != amount)
            throw new InvalidOperationException("写入后金额发生变化，请在游戏中核对，必要时暂停战局后重试。");
    }

    private uint ReadUInt32(uint address)
    {
        byte[] bytes = new byte[4];
        if (!ReadProcessMemory(handle, (nint)address, bytes, 4, out nuint count))
            throw Error("读取游戏内存");
        if (count != 4) throw new IOException("内存读取不完整。");
        return BitConverter.ToUInt32(bytes);
    }

    private static Exception Error(string operation)
    {
        int code = Marshal.GetLastWin32Error();
        return new Win32Exception(code, $"{operation}失败：{new Win32Exception(code).Message}" +
            (code == 5 ? "。若游戏以管理员身份运行，请以管理员身份运行修改器。" : "。请检查地址及游戏状态。"));
    }

    public void Dispose() => handle.Dispose();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(SafeProcessHandle process, nint address, [Out] byte[] buffer, nuint size, out nuint read);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(SafeProcessHandle process, nint address, byte[] buffer, nuint size, out nuint written);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process2(SafeProcessHandle process, out ushort processMachine, out ushort nativeMachine);
}
