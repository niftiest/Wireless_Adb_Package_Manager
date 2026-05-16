using WirelessAdbPackageManager.Models;

namespace WirelessAdbPackageManager.Adb;

public sealed class AdbClient : IAdbClient
{
    private readonly IAdbProcessRunner _runner;
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ListTimeout  = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromSeconds(120);

    public AdbClient(IAdbProcessRunner runner) => _runner = runner;

    public async Task<AdbResult> PairAsync(string ip, int port, string pairingCode, CancellationToken ct = default)
    {
        await _runner.RunAsync(new[] { "disconnect" }, ShortTimeout, ct).ConfigureAwait(false);
        await _runner.RunAsync(new[] { "kill-server" }, ShortTimeout, ct).ConfigureAwait(false);

        var endpoint = $"{ip}:{port}";
        var result = await _runner.RunAsync(new[] { "pair", endpoint, pairingCode }, ShortTimeout, ct).ConfigureAwait(false);

        if (result.TimedOut)
        {
            return AdbResult.Fail($"ADB pair timed out after {ShortTimeout.TotalSeconds:0}s");
        }
        if (result.StdOut.Contains($"Successfully paired to {endpoint}", StringComparison.Ordinal))
        {
            return AdbResult.Ok($"Paired with {endpoint}");
        }
        return AdbResult.Fail($"Pair failed: {Trim(result.StdErr)} {Trim(result.StdOut)}".Trim());
    }

    public async Task<AdbResult> ConnectAsync(string ip, int port, CancellationToken ct = default)
    {
        var endpoint = $"{ip}:{port}";
        var result = await _runner.RunAsync(new[] { "connect", endpoint }, ShortTimeout, ct).ConfigureAwait(false);

        if (result.TimedOut)
        {
            return AdbResult.Fail($"ADB connect timed out after {ShortTimeout.TotalSeconds:0}s");
        }
        if (result.StdOut.Contains($"connected to {endpoint}", StringComparison.Ordinal))
        {
            return AdbResult.Ok($"Connected to {endpoint}");
        }
        return AdbResult.Fail($"Connect failed: {Trim(result.StdOut)} {Trim(result.StdErr)}".Trim());
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _runner.RunAsync(new[] { "disconnect" }, ShortTimeout, ct).ConfigureAwait(false);
        await _runner.RunAsync(new[] { "kill-server" }, ShortTimeout, ct).ConfigureAwait(false);
    }

    public async Task<AdbResult<DeviceInfo?>> GetDeviceAsync(string ip, CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(new[] { "devices", "-l" }, ShortTimeout, ct).ConfigureAwait(false);
        if (result.TimedOut)
        {
            return AdbResult<DeviceInfo?>.Fail("ADB devices timed out");
        }
        if (!result.StdOut.Contains("List of devices attached", StringComparison.Ordinal))
        {
            return AdbResult<DeviceInfo?>.Fail("Unable to enumerate ADB devices");
        }

        foreach (var rawLine in result.StdOut.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || !line.Contains(ip, StringComparison.Ordinal))
            {
                continue;
            }
            var firstSpace = line.IndexOf(' ');
            if (firstSpace <= 0) continue;
            var endpoint = line[..firstSpace];
            var rest = line[firstSpace..].TrimStart();
            var isOnline = rest.StartsWith("device", StringComparison.Ordinal);
            var modelToken = rest.Split(' ').FirstOrDefault(t => t.StartsWith("model:", StringComparison.Ordinal));
            var model = modelToken?["model:".Length..];
            return AdbResult<DeviceInfo?>.Ok(new DeviceInfo(endpoint, model, isOnline));
        }
        return AdbResult<DeviceInfo?>.Ok(null);
    }

    public async Task<AdbResult<IReadOnlyList<AndroidPackage>>> ListPackagesAsync(bool disabledOnly, CancellationToken ct = default)
    {
        var args = disabledOnly
            ? new[] { "shell", "pm", "list", "packages", "-d" }
            : new[] { "shell", "pm", "list", "packages" };
        var result = await _runner.RunAsync(args, ListTimeout, ct).ConfigureAwait(false);
        if (result.TimedOut)
        {
            return AdbResult<IReadOnlyList<AndroidPackage>>.Fail("ADB list packages timed out");
        }
        if (!result.Ok)
        {
            return AdbResult<IReadOnlyList<AndroidPackage>>.Fail($"List packages failed: {Trim(result.StdErr)}");
        }
        return AdbResult<IReadOnlyList<AndroidPackage>>.Ok(AndroidPackage.ParseListOutput(result.StdOut));
    }

    public async Task<AdbResult> InstallApkAsync(string apkPath, CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(new[] { "install", apkPath }, InstallTimeout, ct).ConfigureAwait(false);
        if (result.TimedOut) return AdbResult.Fail("ADB install timed out");
        return result.StdOut.Contains("Success", StringComparison.Ordinal)
            ? AdbResult.Ok($"Installed {apkPath}")
            : AdbResult.Fail($"Install failed for {apkPath}: {Trim(result.StdOut)} {Trim(result.StdErr)}".Trim());
    }

    public Task<AdbResult> UninstallAsync(AndroidPackage package, CancellationToken ct = default)
        => RunPmAsync("uninstall -k --user 0", package, "Success", "Uninstalled", "Unable to uninstall", ct);

    public Task<AdbResult> DisableAsync(AndroidPackage package, CancellationToken ct = default)
        => RunPmAsync("disable-user --user 0", package, "new state:", "Disabled", "Unable to disable", ct);

    public Task<AdbResult> EnableAsync(AndroidPackage package, CancellationToken ct = default)
        => RunPmAsync("enable", package, "new state:", "Enabled", "Unable to enable", ct);

    private async Task<AdbResult> RunPmAsync(
        string pmCommand,
        AndroidPackage package,
        string successMarker,
        string successWord,
        string failureWord,
        CancellationToken ct)
    {
        var args = new List<string> { "shell", "pm" };
        args.AddRange(pmCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        args.Add(package.FullName);
        var result = await _runner.RunAsync(args, ListTimeout, ct).ConfigureAwait(false);
        if (result.TimedOut)
        {
            return AdbResult.Fail($"{failureWord} {package.FullName}: timed out");
        }
        return result.StdOut.Contains(successMarker, StringComparison.Ordinal)
            ? AdbResult.Ok($"{successWord} {package.FullName}")
            : AdbResult.Fail($"{failureWord} {package.FullName}: {Trim(result.StdOut)} {Trim(result.StdErr)}".Trim());
    }

    private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
}
