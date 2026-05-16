using WirelessAdbPackageManager.Adb;
using WirelessAdbPackageManager.Models;

namespace WirelessAdbPackageManager.Services;

public sealed class ConnectionAttemptResult
{
    public bool Connected { get; init; }
    public bool PairedJustNow { get; init; }
    public string? DeviceModel { get; init; }
    public int? UpdatedPort { get; init; }
    public IReadOnlyList<AndroidPackage> EnabledPackages { get; init; } = Array.Empty<AndroidPackage>();
    public IReadOnlyList<AndroidPackage> DisabledPackages { get; init; } = Array.Empty<AndroidPackage>();
}

public sealed class DeviceConnectionService
{
    private readonly IAdbClient _adb;
    private readonly ConnectionStateMachine _state;

    public event EventHandler<string>? LogMessage;

    public DeviceConnectionService(IAdbClient adb, ConnectionStateMachine state)
    {
        _adb = adb;
        _state = state;
    }

    public async Task<ConnectionAttemptResult> ConnectAsync(ConnectionInfo info, CancellationToken ct = default)
    {
        Log("Checking for existing pair with device");
        var device = await _adb.GetDeviceAsync(info.Ip.ToString(), ct).ConfigureAwait(false);

        if (device.Success && device.Value is not null && device.Value.IsOnline)
        {
            var endpoint = device.Value.Endpoint;
            var port = ParsePort(endpoint) ?? info.Port;
            Log($"Previously paired with device, updated port to {port}");
            _state.MarkConnected();

            var (enabled, disabled) = await RefreshPackagesAsync(ct).ConfigureAwait(false);

            var modelMessage = string.IsNullOrEmpty(device.Value.Model)
                ? "Successfully connected to your device"
                : $"Successfully connected to your {device.Value.Model}";
            Log(modelMessage);

            return new ConnectionAttemptResult
            {
                Connected = true,
                DeviceModel = device.Value.Model,
                UpdatedPort = port,
                EnabledPackages = enabled,
                DisabledPackages = disabled
            };
        }

        if (_state.State == ConnectionState.Paired)
        {
            Log($"Attempting to connect to {info.Ip}");
            var connectResult = await _adb.ConnectAsync(info.Ip.ToString(), info.Port, ct).ConfigureAwait(false);
            Log($"ADB: {connectResult.Message}");
            if (!connectResult.Success)
            {
                return new ConnectionAttemptResult { Connected = false };
            }
            _state.MarkConnected();
            var (enabled, disabled) = await RefreshPackagesAsync(ct).ConfigureAwait(false);
            return new ConnectionAttemptResult
            {
                Connected = true,
                EnabledPackages = enabled,
                DisabledPackages = disabled
            };
        }

        Log($"Attempting to pair with {info.Ip}");
        var pairResult = await _adb.PairAsync(info.Ip.ToString(), info.Port, info.PairingCode, ct).ConfigureAwait(false);
        if (!pairResult.Success)
        {
            Log(pairResult.Message);
            return new ConnectionAttemptResult { Connected = false };
        }
        _state.MarkPaired();
        Log(pairResult.Message);
        return new ConnectionAttemptResult { Connected = false, PairedJustNow = true };
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _adb.DisconnectAsync(ct).ConfigureAwait(false);
        _state.Reset();
        Log("Disconnected from device");
    }

    private async Task<(IReadOnlyList<AndroidPackage> Enabled, IReadOnlyList<AndroidPackage> Disabled)> RefreshPackagesAsync(CancellationToken ct)
    {
        var disabled = await _adb.ListPackagesAsync(disabledOnly: true, ct).ConfigureAwait(false);
        var disabledList = disabled.Success ? disabled.Value ?? Array.Empty<AndroidPackage>() : Array.Empty<AndroidPackage>();

        var all = await _adb.ListPackagesAsync(disabledOnly: false, ct).ConfigureAwait(false);
        if (!all.Success)
        {
            Log("No packages could be found, try reconnecting.");
            return (Array.Empty<AndroidPackage>(), disabledList);
        }
        var disabledSet = new HashSet<AndroidPackage>(disabledList);
        var enabledList = (all.Value ?? Array.Empty<AndroidPackage>())
            .Where(p => !disabledSet.Contains(p))
            .ToList();
        Log("Updating current package lists");
        return (enabledList, disabledList);
    }

    private static int? ParsePort(string endpoint)
    {
        var colon = endpoint.IndexOf(':');
        if (colon < 0 || colon == endpoint.Length - 1) return null;
        return int.TryParse(endpoint[(colon + 1)..], out var p) ? p : null;
    }

    private void Log(string message) => LogMessage?.Invoke(this, message);
}
