using System;
using System.IO;
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
            if (handle.IsInvalid) throw Error("Open game process");
            if (!IsWow64Process2(handle, out ushort machine, out ushort nativeMachine))
                throw Error("Detect game architecture");
            if (machine != 0x014c && !(machine == 0 && nativeMachine == 0x014c))
                throw new InvalidOperationException("Target is not a 32-bit x86 game. Check the process name.");
            if (profile.ModuleRelative)
                moduleBase = checked((uint)(process.MainModule
                    ?? throw new InvalidOperationException("Cannot read the main game module.")).BaseAddress.ToInt64());
        }
        catch { handle.Dispose(); throw; }
    }

    public (uint Address, int Amount) ReadMoney()
    {
        if (process.HasExited) throw new InvalidOperationException("The game has exited.");
        uint address = AddressMath.Resolve(profile, moduleBase, ReadUInt32);
        return (address, unchecked((int)ReadUInt32(address)));
    }

    public void SetMoney(int amount)
    {
        // Resolve again on every operation; never cache a player pointer across maps/saves.
        var current = ReadMoney();
        if (current.Amount < 0) throw new InvalidOperationException("Money is negative. Check the address.");
        // Recheck immediately before writing to reduce the chance of a stale player pointer.
        if (ReadMoney().Address != current.Address)
            throw new InvalidOperationException("Player address changed. Wait for the match to stabilize and try again.");
        byte[] bytes = BitConverter.GetBytes(amount);
        if (!WriteProcessMemory(handle, new IntPtr((long)current.Address), bytes, new UIntPtr(4u), out UIntPtr count))
            throw Error("Write money");
        if (count.ToUInt64() != 4) throw new IOException("Incomplete money write.");
        if (ReadMoney().Amount != amount)
            throw new InvalidOperationException("Money changed after writing. Check in-game or pause and retry.");
    }

    private uint ReadUInt32(uint address)
    {
        byte[] bytes = new byte[4];
        if (!ReadProcessMemory(handle, new IntPtr((long)address), bytes, new UIntPtr(4u), out UIntPtr count))
            throw Error("Read game memory");
        if (count.ToUInt64() != 4) throw new IOException("Incomplete memory read.");
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static Exception Error(string operation)
    {
        int code = Marshal.GetLastWin32Error();
        return new Win32Exception(code, $"{operation} failed (Windows error {code}). " +
            (code == 5 ? "If the game runs as administrator, run this trainer as administrator too."
                       : "Check the address and game state."));
    }

    public void Dispose() => handle.Dispose();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(SafeProcessHandle process, IntPtr address, [Out] byte[] buffer, UIntPtr size, out UIntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(SafeProcessHandle process, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process2(SafeProcessHandle process, out ushort processMachine, out ushort nativeMachine);
}
