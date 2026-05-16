using WirelessAdbPackageManager.Adb;
using WirelessAdbPackageManager.Services;
using WirelessAdbPackageManager.UI;

namespace WirelessAdbPackageManager;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var resources = new AdbResourceManager();
        if (!resources.TryInstall(out var adbPath, out var error))
        {
            MessageBox.Show(
                $"Unable to install ADB, cannot proceed.{(string.IsNullOrEmpty(error) ? string.Empty : $"\r\n{error}")}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var runner = new AdbProcessRunner(adbPath);
        var client = new AdbClient(runner);
        var state = new ConnectionStateMachine();
        var filter = new PackageFilterService();
        var connection = new DeviceConnectionService(client, state);

        Application.Run(new MainForm(client, connection, state, filter));
    }
}
