using WirelessAdbPackageManager.Adb;
using WirelessAdbPackageManager.Models;
using WirelessAdbPackageManager.Services;
using WirelessAdbPackageManager.Utils;

namespace WirelessAdbPackageManager.UI;

public sealed partial class MainForm : Form
{
    private readonly IAdbClient _adb;
    private readonly DeviceConnectionService _connection;
    private readonly ConnectionStateMachine _state;
    private readonly PackageFilterService _filter;

    private readonly List<AndroidPackage> _enabledPackages = new();
    private readonly List<AndroidPackage> _disabledPackages = new();
    private bool _suppressItemCheck;
    private CancellationTokenSource? _activeCts;

    public MainForm(
        IAdbClient adb,
        DeviceConnectionService connection,
        ConnectionStateMachine state,
        PackageFilterService filter)
    {
        _adb = adb;
        _connection = connection;
        _state = state;
        _filter = filter;
        InitializeComponent();

        _connection.LogMessage += (_, message) => SafeInvoke(() => AppendLog(message));
        _state.StateChanged += (_, s) => SafeInvoke(() => OnStateChanged(s));
        FormClosing += (_, _) => { _activeCts?.Cancel(); _activeCts?.Dispose(); };
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (_state.State == ConnectionState.Connected)
        {
            await DisconnectAsync().ConfigureAwait(true);
            return;
        }

        if (!InputValidator.TryParseIp(IpAddressTextBox.Text, out var ip) || ip is null)
        {
            ShowError("Invalid IP Address");
            return;
        }
        if (!InputValidator.TryParsePort(PortTextBox.Text, out var port))
        {
            ShowError("Invalid Port");
            return;
        }
        if (!InputValidator.IsValidPairingCode(PairingCodeTextBox.Text))
        {
            ShowError("Invalid Pairing Code");
            return;
        }

        _activeCts?.Cancel();
        _activeCts?.Dispose();
        _activeCts = new CancellationTokenSource();
        ConnectButton.Enabled = false;
        try
        {
            var result = await _connection.ConnectAsync(
                new ConnectionInfo(ip, port, PairingCodeTextBox.Text),
                _activeCts.Token).ConfigureAwait(true);

            if (result.PairedJustNow)
            {
                ShowInfo("Adb has paired with your device!\r\nUpdate the port as needed and hit Connect again to continue.");
            }
            if (result.Connected)
            {
                if (result.UpdatedPort.HasValue)
                {
                    PortTextBox.Text = result.UpdatedPort.Value.ToString();
                }
                ReplacePackageLists(result.EnabledPackages, result.DisabledPackages);
            }
        }
        catch (OperationCanceledException) { /* user disconnected */ }
        catch (Exception ex)
        {
            ShowError($"Connection failed: {ex.Message}");
        }
        finally
        {
            ConnectButton.Enabled = true;
        }
    }

    private async Task DisconnectAsync()
    {
        _activeCts?.Cancel();
        try
        {
            await _connection.DisconnectAsync().ConfigureAwait(true);
        }
        finally
        {
            ClearPackageLists();
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "APK Files (*.apk)|*.apk",
            Title = "Select an APK File"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        InstallButton.Enabled = false;
        try
        {
            var result = await _adb.InstallApkAsync(dialog.FileName).ConfigureAwait(true);
            AppendLog(result.Message);
            if (!result.Success)
            {
                ShowError(result.Message);
            }
            await RefreshAfterPackageChangeAsync().ConfigureAwait(true);
        }
        finally
        {
            InstallButton.Enabled = _state.State == ConnectionState.Connected;
        }
    }

    private async void UninstallButton_Click(object? sender, EventArgs e)
        => await RunBulkAsync(SnapshotCheckedFrom(EnabledPackagesCheckBoxList), _adb.UninstallAsync).ConfigureAwait(true);

    private async void DisableButton_Click(object? sender, EventArgs e)
        => await RunBulkAsync(SnapshotCheckedFrom(EnabledPackagesCheckBoxList), _adb.DisableAsync).ConfigureAwait(true);

    private async void EnableButton_Click(object? sender, EventArgs e)
        => await RunBulkAsync(SnapshotCheckedFrom(DisabledPackagesCheckBoxList), _adb.EnableAsync).ConfigureAwait(true);

    private async Task RunBulkAsync(
        IReadOnlyList<AndroidPackage> targets,
        Func<AndroidPackage, CancellationToken, Task<AdbResult>> operation)
    {
        if (targets.Count == 0) return;

        SetBulkButtonsEnabled(false);
        try
        {
            foreach (var package in targets)
            {
                var result = await operation(package, CancellationToken.None).ConfigureAwait(true);
                AppendLog(result.Message);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Operation Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            await RefreshAfterPackageChangeAsync().ConfigureAwait(true);
        }
        finally
        {
            SetBulkButtonsEnabled(true);
        }
    }

    private async Task RefreshAfterPackageChangeAsync()
    {
        var disabled = await _adb.ListPackagesAsync(disabledOnly: true).ConfigureAwait(true);
        var all = await _adb.ListPackagesAsync(disabledOnly: false).ConfigureAwait(true);
        var disabledList = disabled.Success && disabled.Value is not null ? disabled.Value : Array.Empty<AndroidPackage>();
        var disabledSet = new HashSet<AndroidPackage>(disabledList);
        var enabledList = all.Success && all.Value is not null
            ? all.Value.Where(p => !disabledSet.Contains(p)).ToList()
            : new List<AndroidPackage>();
        ReplacePackageLists(enabledList, disabledList);
    }

    private void EnabledPackagesList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UninstallButton.Enabled = DisableButton.Enabled = EnabledPackagesCheckBoxList.CheckedItems.Count > 0;
    }

    private void DisabledPackagesList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        EnableButton.Enabled = DisabledPackagesCheckBoxList.CheckedItems.Count > 0;
    }

    private void EnabledPackagesList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressItemCheck) return;
        if (e.Index < 0 || e.Index >= EnabledPackagesCheckBoxList.Items.Count) return;
        var pkg = (AndroidPackage)EnabledPackagesCheckBoxList.Items[e.Index]!;
        if (e.NewValue == CheckState.Checked) _filter.CheckedPackages.Add(pkg);
        else _filter.CheckedPackages.Remove(pkg);
    }

    private void DisabledPackagesList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressItemCheck) return;
        if (e.Index < 0 || e.Index >= DisabledPackagesCheckBoxList.Items.Count) return;
        var pkg = (AndroidPackage)DisabledPackagesCheckBoxList.Items[e.Index]!;
        if (e.NewValue == CheckState.Checked) _filter.CheckedPackages.Add(pkg);
        else _filter.CheckedPackages.Remove(pkg);
    }

    private void EnabledPackageFilter_TextChanged(object? sender, EventArgs e)
        => RenderList(EnabledPackagesCheckBoxList, _enabledPackages, EnabledPackagesSearchTextBox.Text);

    private void DisabledPackageFilter_TextChanged(object? sender, EventArgs e)
        => RenderList(DisabledPackagesCheckBoxList, _disabledPackages, DisabledPackagesSearchTextBox.Text);

    private void ReplacePackageLists(IReadOnlyList<AndroidPackage> enabled, IReadOnlyList<AndroidPackage> disabled)
    {
        _enabledPackages.Clear();
        _enabledPackages.AddRange(enabled);
        _disabledPackages.Clear();
        _disabledPackages.AddRange(disabled);

        var currentChecks = _filter.CheckedPackages.ToHashSet();
        currentChecks.IntersectWith(_enabledPackages.Concat(_disabledPackages));
        _filter.CheckedPackages.Clear();
        foreach (var p in currentChecks) _filter.CheckedPackages.Add(p);

        RenderList(EnabledPackagesCheckBoxList, _enabledPackages, EnabledPackagesSearchTextBox.Text);
        RenderList(DisabledPackagesCheckBoxList, _disabledPackages, DisabledPackagesSearchTextBox.Text);
    }

    private void ClearPackageLists()
    {
        _enabledPackages.Clear();
        _disabledPackages.Clear();
        _filter.CheckedPackages.Clear();
        RenderList(EnabledPackagesCheckBoxList, _enabledPackages, "");
        RenderList(DisabledPackagesCheckBoxList, _disabledPackages, "");
    }

    private void RenderList(CheckedListBox list, List<AndroidPackage> source, string searchTerm)
    {
        _suppressItemCheck = true;
        try
        {
            list.BeginUpdate();
            list.Items.Clear();
            var visible = _filter.Filter(source, searchTerm);
            foreach (var p in visible)
            {
                var index = list.Items.Add(p);
                if (_filter.CheckedPackages.Contains(p))
                {
                    list.SetItemChecked(index, true);
                }
            }
            list.EndUpdate();
        }
        finally
        {
            _suppressItemCheck = false;
        }
        EnabledPackagesList_SelectedIndexChanged(null, EventArgs.Empty);
        DisabledPackagesList_SelectedIndexChanged(null, EventArgs.Empty);
    }

    private void OnStateChanged(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Disconnected:
                ConnectButton.Text = "CONNECT";
                InstallButton.Enabled = false;
                IpAddressTextBox.Enabled = true;
                PortTextBox.Enabled = true;
                PairingCodeTextBox.Enabled = true;
                break;
            case ConnectionState.Paired:
                ConnectButton.Text = "CONNECT";
                IpAddressTextBox.Enabled = true;
                PortTextBox.Enabled = true;
                PairingCodeTextBox.Enabled = true;
                break;
            case ConnectionState.Connected:
                ConnectButton.Text = "DISCONNECT";
                InstallButton.Enabled = true;
                IpAddressTextBox.Enabled = false;
                PortTextBox.Enabled = false;
                PairingCodeTextBox.Enabled = false;
                break;
        }
    }

    private static IReadOnlyList<AndroidPackage> SnapshotCheckedFrom(CheckedListBox list)
        => list.CheckedItems.Cast<AndroidPackage>().ToList();

    private void SetBulkButtonsEnabled(bool enabled)
    {
        UninstallButton.Enabled = enabled && EnabledPackagesCheckBoxList.CheckedItems.Count > 0;
        DisableButton.Enabled  = enabled && EnabledPackagesCheckBoxList.CheckedItems.Count > 0;
        EnableButton.Enabled   = enabled && DisabledPackagesCheckBoxList.CheckedItems.Count > 0;
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("hh:mm:ss tt");
        var entry = $"{timestamp} - {message}";
        LogsTextBox.Text = string.IsNullOrEmpty(LogsTextBox.Text)
            ? entry
            : $"{entry}\r\n{LogsTextBox.Text}";
    }

    private void ShowError(string message)
        => MessageBox.Show(this, message, "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void ShowInfo(string message)
        => MessageBox.Show(this, message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }
}
