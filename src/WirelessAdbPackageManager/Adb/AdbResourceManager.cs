using System.Security.Cryptography;

namespace WirelessAdbPackageManager.Adb;

public sealed class AdbResourceManager
{
    private const string SubFolder = "WirelessAdbPackageManager";

    public bool TryInstall(out string adbPath, out string? error)
    {
        adbPath = string.Empty;
        error = null;

        try
        {
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                SubFolder,
                "bin");

            Directory.CreateDirectory(targetDir);

            WriteIfDifferent(Path.Combine(targetDir, "adb.exe"),          Properties.Resources.Adb);
            WriteIfDifferent(Path.Combine(targetDir, "AdbWinApi.dll"),    Properties.Resources.AdbWinApi);
            WriteIfDifferent(Path.Combine(targetDir, "AdbWinUsbApi.dll"), Properties.Resources.AdbWinUsbApi);

            adbPath = Path.Combine(targetDir, "adb.exe");
            return File.Exists(adbPath);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void WriteIfDifferent(string path, byte[] contents)
    {
        if (File.Exists(path) && HashEqual(path, contents))
        {
            return;
        }
        File.WriteAllBytes(path, contents);
    }

    private static bool HashEqual(string path, byte[] expected)
    {
        try
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var onDisk = sha.ComputeHash(stream);
            var expectedHash = sha.ComputeHash(expected);
            return onDisk.SequenceEqual(expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
