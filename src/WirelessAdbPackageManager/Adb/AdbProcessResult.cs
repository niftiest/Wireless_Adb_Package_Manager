namespace WirelessAdbPackageManager.Adb;

public sealed record AdbProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut)
{
    public bool Ok => !TimedOut && ExitCode == 0;
}
