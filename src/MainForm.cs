using System.Diagnostics;

namespace Ra2MoneyTrainer;

internal sealed class MainForm : Form
{
    private readonly ComboBox games = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
    private readonly Label state = new() { AutoSize = true, MaximumSize = new Size(420, 0) };
    private readonly Label current = new() { AutoSize = true, Text = "当前金额：—" };
    private readonly Label note = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Color.DimGray };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(420, 0) };
    private readonly NumericUpDown amount = new() { Minimum = 0, Maximum = int.MaxValue, Value = 100000, ThousandsSeparator = true, Width = 180 };
    private readonly CheckBox verified = new() { AutoSize = true, Text = "已核对当前金额与游戏一致（每局重新核对）" };
    private readonly Button apply = new() { Text = "修改金钱", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private readonly string configPath = Path.Combine(AppContext.BaseDirectory, "addresses.json");
    private string? identity;
    private bool ready;

    public MainForm()
    {
        Text = "红警2 · 金钱修改器";
        Font = new Font("Microsoft YaHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(470, 370);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(20) };
        var valueRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        valueRow.Controls.Add(new Label { Text = "目标金额：", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        valueRow.Controls.Add(amount);
        valueRow.Controls.Add(apply);
        var configRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        var edit = new Button { Text = "编辑地址配置", AutoSize = true };
        var reload = new Button { Text = "重新加载配置", AutoSize = true };
        configRow.Controls.AddRange([edit, reload]);
        layout.Controls.AddRange([games, state, current, valueRow, verified, result, configRow, note]);
        foreach (Control control in layout.Controls) control.Margin = new Padding(0, 0, 0, 12);
        Controls.Add(layout);

        games.SelectedIndexChanged += (_, _) => { ResetVerification(); result.Text = ""; RefreshState(); };
        verified.CheckedChanged += (_, _) => apply.Enabled = ready && verified.Checked;
        apply.Click += (_, _) => WriteMoney();
        reload.Click += (_, _) => LoadConfig();
        edit.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { configPath } }); }
            catch (Exception ex) { result.Text = ex.Message; }
        };
        timer.Tick += (_, _) => RefreshState();
        FormClosed += (_, _) => timer.Dispose();
        LoadConfig();
        timer.Start();
    }

    private void LoadConfig()
    {
        ResetVerification();
        games.Items.Clear();
        try
        {
            var config = AddressConfig.Load(configPath);
            games.Items.AddRange(config.Profiles.Cast<object>().ToArray());
            games.SelectedIndex = 0;
            result.Text = "配置已加载。";
        }
        catch (Exception ex)
        {
            ready = false;
            current.Text = "当前金额：—";
            state.Text = "配置加载失败";
            result.Text = ex.Message;
        }
    }

    private Process FindProcess(GameProfile profile)
    {
        // Refuse ambiguous targets rather than writing to an arbitrary game instance.
        var matches = new List<Process>();
        try
        {
            foreach (string name in profile.ProcessNames.Distinct(StringComparer.OrdinalIgnoreCase))
                matches.AddRange(Process.GetProcessesByName(name));
            if (matches.Count == 0) throw new InvalidOperationException("游戏未启动，或进程名与配置不符。");
            if (matches.Count > 1) throw new InvalidOperationException("发现多个匹配进程，请只保留一个游戏实例。");
            return matches[0];
        }
        catch { foreach (var process in matches) process.Dispose(); throw; }
    }

    private void RefreshState()
    {
        ready = false;
        if (games.SelectedItem is not GameProfile profile) { apply.Enabled = false; return; }
        note.Text = profile.Note;
        try
        {
            if (string.IsNullOrWhiteSpace(profile.Address))
                throw new InvalidOperationException("尚未配置金钱地址，请编辑地址配置后重新加载。");
            using var process = FindProcess(profile);
            using var memory = new GameMemory(process, profile, writable: false);
            var money = memory.ReadMoney();
            string newIdentity = $"{process.Id}:{process.StartTime.Ticks}:{money.Address}";
            if (identity != newIdentity) { verified.Checked = false; identity = newIdentity; }
            state.Text = $"已连接：{memory.ExecutableName}.exe · PID {memory.Pid}";
            current.Text = $"当前金额：{money.Amount:N0}";
            if (money.Amount < 0) throw new InvalidOperationException("读取金额为负数，请校准地址。");
            ready = true;
        }
        catch (Exception ex)
        {
            ResetVerification();
            state.Text = ex.Message;
            current.Text = "当前金额：—";
        }
        apply.Enabled = ready && verified.Checked;
    }

    private void WriteMoney()
    {
        string? expectedIdentity = identity;
        RefreshState();
        if (!ready || !verified.Checked || identity != expectedIdentity) return;
        if (games.SelectedItem is not GameProfile profile) return;
        try
        {
            using var process = FindProcess(profile);
            using var memory = new GameMemory(process, profile, writable: true);
            var before = memory.ReadMoney();
            if ($"{process.Id}:{process.StartTime.Ticks}:{before.Address}" != expectedIdentity)
                throw new InvalidOperationException("游戏或玩家地址已变化，请重新核对金额。");
            memory.SetMoney(decimal.ToInt32(amount.Value));
            result.Text = $"已写入 {amount.Value:N0}，请在游戏中核对。";
        }
        catch (Exception ex) { ResetVerification(); result.Text = ex.Message; }
        RefreshState();
    }

    private void ResetVerification()
    {
        identity = null;
        verified.Checked = false;
        apply.Enabled = false;
    }
}
