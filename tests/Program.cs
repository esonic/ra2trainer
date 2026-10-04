using Ra2MoneyTrainer;

static void Equal(uint actual, uint expected)
{
    if (actual != expected) throw new Exception($"Expected {expected:X}, got {actual:X}");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

Equal(AddressMath.Parse("0x00A83D4C"), 0x00A83D4C);
Equal(AddressMath.Parse("1000"), 1000);
Equal(AddressMath.Resolve(new() { Address = "0x1234" }, 0, _ => throw new Exception()), 0x1234);
Equal(AddressMath.Resolve(new() { Address = "0x20", ModuleRelative = true }, 0x400000, _ => 0), 0x400020);
Equal(AddressMath.Resolve(new() { Address = "0xA83D4C", Offsets = ["0x30C"] }, 0,
    address => address == 0xA83D4C ? 0x100000u : throw new Exception()), 0x10030C);
Equal(AddressMath.Resolve(new() { Address = "0x100", Offsets = ["0x10", "0x20"] }, 0,
    address => address switch { 0x100 => 0x200u, 0x210 => 0x300u, _ => throw new Exception() }), 0x320);
Throws<InvalidOperationException>(() => AddressMath.Resolve(new() { Address = "0x100", Offsets = ["0x10"] }, 0, _ => 0));
Throws<OverflowException>(() => AddressMath.Resolve(new() { Address = "0x100", Offsets = ["0x10"] }, 0, _ => uint.MaxValue));
Throws<FormatException>(() => AddressMath.Parse("A83D4C"));
var config = AddressConfig.Load(args[0]);
if (config.Profiles.Count != 2 || config.Profiles[0].Address != "") throw new Exception("Unexpected default profiles");
Console.WriteLine("10 address/config checks passed.");
