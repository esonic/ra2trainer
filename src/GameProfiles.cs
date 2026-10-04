using System;

namespace Ra2MoneyTrainer;

// 地址统一在这里修改。游戏指针为 32 位；数字可直接使用 C# 十六进制常量。
internal static class GameProfiles
{
    public static readonly GameProfile[] All =
    {
        new GameProfile()
        {
            Name = "Red Alert 2",
            ProcessName = "game", // 不带 .exe；如有不同，以任务管理器为准。
            Address = 0x635DB4,
            ModuleRelative = true,
            Offsets = new uint[] { 0x24C }
        },
        new GameProfile()
        {
            Name = "Yuri’s Revenge",
            ProcessName = "gamemd",
            Address = 0x00A83D4C,
            ModuleRelative = false,
            Offsets = new uint[] { 0x30C }
        }
    };
}

internal sealed class GameProfile
{
    public string Name { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public uint? Address { get; set; }
    // true：Address 为主 EXE 模块内偏移；false：Address 为绝对地址。
    public bool ModuleRelative { get; set; }
    // 每个偏移的含义是“读32位指针，然后加偏移”。空数组表示直接金钱地址。
    public uint[] Offsets { get; set; } = Array.Empty<uint>();
}

internal static class AddressMath
{
    public static uint Resolve(GameProfile profile, uint moduleBase, Func<uint, uint> readPointer)
    {
        uint address = profile.Address ?? throw new InvalidOperationException("Money address is not set.");
        if (profile.ModuleRelative) address = checked(moduleBase + address);
        foreach (uint offset in profile.Offsets)
        {
            uint pointer = readPointer(address);
            if (pointer == 0) throw new InvalidOperationException("Waiting for a match. Check the address if already in-game.");
            address = checked(pointer + offset);
        }
        if (address == 0) throw new InvalidOperationException("Money address cannot be zero.");
        return address;
    }
}
