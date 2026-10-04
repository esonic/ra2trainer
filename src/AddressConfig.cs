using System.Globalization;
using System.Text.Json;

namespace Ra2MoneyTrainer;

public sealed class AddressConfig
{
    public List<GameProfile> Profiles { get; set; } = [];

    public static AddressConfig Load(string path)
    {
        var config = JsonSerializer.Deserialize<AddressConfig>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new FormatException("配置文件为空。");
        if (config.Profiles.Count == 0) throw new FormatException("配置中缺少 profiles。");
        foreach (var profile in config.Profiles) profile.Validate();
        return config;
    }
}

public sealed class GameProfile
{
    public string Name { get; set; } = "";
    public string[] ProcessNames { get; set; } = [];
    public string Address { get; set; } = "";
    public bool ModuleRelative { get; set; }
    public string[] Offsets { get; set; } = [];
    public string Note { get; set; } = "";
    public override string ToString() => Name;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || ProcessNames.Length == 0 ||
            ProcessNames.Any(string.IsNullOrWhiteSpace))
            throw new FormatException("每个配置需要名称和进程名（不带 .exe）。");
        if (!string.IsNullOrWhiteSpace(Address)) AddressMath.Parse(Address);
        foreach (var offset in Offsets) AddressMath.Parse(offset);
    }
}

public static class AddressMath
{
    public static uint Parse(string value)
    {
        value = value.Trim();
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? uint.Parse(value[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
            : uint.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    // Each offset means: dereference a 32-bit pointer, then add this offset.
    // Empty offsets means that address itself is the money field.
    public static uint Resolve(GameProfile profile, uint moduleBase, Func<uint, uint> readPointer)
    {
        uint address = Parse(profile.Address);
        if (profile.ModuleRelative) address = checked(moduleBase + address);
        foreach (var offset in profile.Offsets)
        {
            uint pointer = readPointer(address);
            if (pointer == 0) throw new InvalidOperationException("玩家指针为空，请进入战局；若已进入，请校准地址。");
            address = checked(pointer + Parse(offset));
        }
        if (address == 0) throw new InvalidOperationException("金钱地址不能为零。");
        return address;
    }
}
