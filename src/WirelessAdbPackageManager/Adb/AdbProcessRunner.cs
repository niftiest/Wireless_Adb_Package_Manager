using System.Diagnostics;

namespace WirelessAdbPackageManager.Adb;

public sealed class AdbProcessRunner : IAdbProcessRunner
{
    private readonly string _adbPath;

    public AdbProcessRunner(string adbPath)
    {
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            throw new ArgumentException("adb path must be provided", nameof(adbPath));
        }
        _adbPath = adbPath;
    }

    public async Task<AdbProcessResult> RunAsync(
        IEnumerable<string> args,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };

        try
        {
            if (!process.Start())
            {
                return new AdbProcessResult(-1, string.Empty, "Failed to start adb process", false);
            }
        }
        catch (Exception ex)
        {
            return new AdbProcessResult(-1, string.Empty, ex.Message, false);
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new AdbProcessResult(-1, string.Empty, "Process timed out or was cancelled", TimedOut: true);
        }

        string stdout;
        string stderr;
        try
        {
            stdout = await stdoutTask.ConfigureAwait(false);
            stderr = await stderrTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new AdbProcessResult(-1, string.Empty, "Process timed out while reading output", TimedOut: true);
        }

        return new AdbProcessResult(process.ExitCode, stdout, stderr, TimedOut: false);
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch { /* best-effort */ }
    }
}
