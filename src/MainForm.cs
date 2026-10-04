using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ra2MoneyTrainer;

internal sealed class MainForm : Form
{
    private const int MoneyHotkeyId = 1, WmHotkey = 0x0312;
    private const uint ModControl = 0x0002, ModNoRepeat = 0x4000;
    private readonly Label state = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
    private readonly Label current = new() { AutoSize = true, Text = "Current money: —" };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
    private readonly Label hotkey = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
    private readonly NumericUpDown amount = new() { Minimum = 0, Maximum = int.MaxValue, Value = 100000, ThousandsSeparator = true, Width = 170 };
    private readonly Button apply = new() { Text = "Set money", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 3000 };
    private string? identity;
    private bool hotkeyRegistered;

    public MainForm()
    {
        Text = "Red Alert 2 Trainer";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(430, 250);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(16) };
        var valueRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        valueRow.Controls.Add(new Label { Text = "Amount:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
        valueRow.Controls.Add(amount);
        valueRow.Controls.Add(apply);
        layout.Controls.AddRange(new Control[] { state, current, valueRow, hotkey, result });
        foreach (Control control in layout.Controls) control.Margin = new Padding(0, 0, 0, 12);
        Controls.Add(layout);

        apply.Click += (_, _) => WriteMoney();
        timer.Tick += (_, _) => RefreshState();
        FormClosed += (_, _) => timer.Dispose();
        RefreshState();
        timer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        hotkeyRegistered = RegisterHotKey(Handle, MoneyHotkeyId,
            ModControl | ModNoRepeat, (uint)Keys.NumPad1);
        if (hotkeyRegistered)
            hotkey.Text = "Global hotkey: Ctrl + Num 1";
        else
        {
            int error = Marshal.GetLastWin32Error();
            hotkey.Text = $"Ctrl + Num 1 unavailable (Windows error {error}). " +
                "Another app may be using it. Set money still works.";
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (hotkeyRegistered)
        {
            UnregisterHotKey(Handle, MoneyHotkeyId);
            hotkeyRegistered = false;
        }
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == new IntPtr(MoneyHotkeyId) && hotkeyRegistered)
        {
            // Reuse the button's game/player checks without activating the trainer window.
            WriteMoney();
            return;
        }
        base.WndProc(ref m);
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
        // Compute the next state first, so polling never briefly disables a ready button
        // or replaces an error with "Connected" before the memory read has succeeded.
        bool ready = false;
        string nextState;
        string nextCurrent;
        bool clearResult;
        try
        {
            var game = FindGame();
            using var process = game.Process;
            if (game.Profile.Address is null)
                throw new InvalidOperationException($"{game.Profile.Name}: Set the address in GameProfiles.cs first.");
            using var memory = new GameMemory(process, game.Profile, writable: false);
            var money = memory.ReadMoney();
            if (money.Amount < 0) throw new InvalidOperationException("Invalid money value. Check the address.");
            string newIdentity = GetIdentity(process, money.Address);
            clearResult = identity != newIdentity;
            identity = newIdentity;
            nextState = $"Connected: {game.Profile.Name}";
            nextCurrent = $"Current money: {money.Amount:N0}";
            ready = true;
        }
        catch (Exception ex)
        {
            identity = null;
            clearResult = true;
            nextState = ex.Message;
            nextCurrent = "Current money: —";
        }

        // Update only changed properties; unchanged polls should cause no repaint.
        if (state.Text != nextState) state.Text = nextState;
        if (current.Text != nextCurrent) current.Text = nextCurrent;
        if (clearResult && result.Text.Length != 0) result.Text = "";
        if (apply.Enabled != ready) apply.Enabled = ready;
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
}
