using WirelessAdbPackageManager.Models;

namespace WirelessAdbPackageManager.Adb;

public interface IAdbClient
{
    Task<AdbResult> PairAsync(string ip, int port, string pairingCode, CancellationToken ct = default);
    Task<AdbResult> ConnectAsync(string ip, int port, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<AdbResult<DeviceInfo?>> GetDeviceAsync(string ip, CancellationToken ct = default);
    Task<AdbResult<IReadOnlyList<AndroidPackage>>> ListPackagesAsync(bool disabledOnly, CancellationToken ct = default);
    Task<AdbResult> InstallApkAsync(string apkPath, CancellationToken ct = default);
    Task<AdbResult> UninstallAsync(AndroidPackage package, CancellationToken ct = default);
    Task<AdbResult> DisableAsync(AndroidPackage package, CancellationToken ct = default);
    Task<AdbResult> EnableAsync(AndroidPackage package, CancellationToken ct = default);
}
