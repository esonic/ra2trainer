using System.Diagnostics;

namespace Ra2MoneyTrainer;

internal sealed class MainForm : Form
{
    private readonly Label state = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
    private readonly Label current = new() { AutoSize = true, Text = "Current money: —" };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
    private readonly NumericUpDown amount = new() { Minimum = 0, Maximum = int.MaxValue, Value = 100000, ThousandsSeparator = true, Width = 170 };
    private readonly Button apply = new() { Text = "Set money", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 3000 };
    private string? identity;

    public MainForm()
    {
        Text = "Red Alert 2 Trainer";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 200);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(16) };
        var valueRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        valueRow.Controls.Add(new Label { Text = "Amount:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        valueRow.Controls.Add(amount);
        valueRow.Controls.Add(apply);
        layout.Controls.AddRange([state, current, valueRow, result]);
        foreach (Control control in layout.Controls) control.Margin = new Padding(0, 0, 0, 12);
        Controls.Add(layout);

        apply.Click += (_, _) => WriteMoney();
        timer.Tick += (_, _) => RefreshState();
        FormClosed += (_, _) => timer.Dispose();
        RefreshState();
        timer.Start();
    }

    private static (Process Process, GameProfile Profile) FindGame()
    {
        var matches = new List<(Process Process, GameProfile Profile)>();
        try
        {
            foreach (var profile in GameProfiles.All)
                foreach (var process in Process.GetProcessesByName(profile.ProcessName))
                    matches.Add((process, profile));
            if (matches.Count == 0) throw new InvalidOperationException("Waiting for game…");
            if (matches.Count > 1) throw new InvalidOperationException("Multiple games detected. Keep only one running.");
            return matches[0];
        }
        catch { foreach (var match in matches) match.Process.Dispose(); throw; }
    }

    private static string GetIdentity(Process process, uint address) =>
        $"{process.Id}:{process.StartTime.Ticks}:{address}";

    private void RefreshState()
    {
        apply.Enabled = false;
        try
        {
            var game = FindGame();
            using var process = game.Process;
            state.Text = $"Connected: {game.Profile.Name}";
            if (game.Profile.Address is null)
                throw new InvalidOperationException($"{game.Profile.Name}: Set the address in GameProfiles.cs first.");
            using var memory = new GameMemory(process, game.Profile, writable: false);
            var money = memory.ReadMoney();
            if (money.Amount < 0) throw new InvalidOperationException("Invalid money value. Check the address.");
            string newIdentity = GetIdentity(process, money.Address);
            if (identity != newIdentity) result.Text = "";
            identity = newIdentity;
            current.Text = $"Current money: {money.Amount:N0}";
            apply.Enabled = true;
        }
        catch (Exception ex)
        {
            identity = null;
            result.Text = "";
            state.Text = ex.Message;
            current.Text = "Current money: —";
        }
    }

    private void WriteMoney()
    {
        string? expectedIdentity = identity;
        RefreshState();
        if (!apply.Enabled || identity != expectedIdentity) return;
        string message;
        try
        {
            var game = FindGame();
            using var process = game.Process;
            using var memory = new GameMemory(process, game.Profile, writable: true);
            var before = memory.ReadMoney();
            if (GetIdentity(process, before.Address) != expectedIdentity)
                throw new InvalidOperationException("Game or player address changed. Try again.");
            memory.SetMoney(decimal.ToInt32(amount.Value));
            message = $"Money set to {amount.Value:N0}";
        }
        catch (Exception ex) { message = ex.Message; }
        RefreshState();
        result.Text = message;
    }
}
