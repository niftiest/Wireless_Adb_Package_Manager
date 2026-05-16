namespace WirelessAdbPackageManager.Adb;

public interface IAdbProcessRunner
{
    Task<AdbProcessResult> RunAsync(
        IEnumerable<string> args,
        TimeSpan timeout,
        CancellationToken ct = default);
}
