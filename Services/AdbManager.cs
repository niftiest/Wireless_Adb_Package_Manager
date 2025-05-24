using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms; // For MessageBox in Uninstall, consider removing later
using WirelessAdbPackageManager.Properties; // For Resources

namespace WirelessAdbPackageManager.Services
{
    public class AdbManager 
    {
        public bool Install() 
        {
            try
            {
                var tempPath = Path.GetTempPath();
                DumpResourceToFile("adb.exe", Resources.Adb, tempPath);
                DumpResourceToFile("AdbWinApi.dll", Resources.AdbWinApi, tempPath);
                DumpResourceToFile("AdbWinUsbApi.dll", Resources.AdbWinUsbApi, tempPath);
                return true;
            }
            catch (Exception ex) 
            {
                return false;
            }
        }

        public bool Uninstall() 
        {
            var resources = new List<string> { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };
            try
            {
                foreach (var resource in resources)
                {
                    var filePath = Path.Combine(Path.GetTempPath(), resource);
                    if (File.Exists(filePath)) 
                    {
                        File.Delete(filePath);
                    }
                }
                return true;
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Unable to uninstall adb resources: {ex.Message}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void DumpResourceToFile(string fileName, byte[] resource, string directoryPath) 
        {
            var filePath = Path.Combine(directoryPath, fileName);
            if (!File.Exists(filePath))
            {
                File.WriteAllBytes(filePath, resource);
            }
        }

        public async Task<string> RunCommandAsync(string command) 
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe", 
                    Arguments = $"/c \"{Path.Combine(Path.GetTempPath(), "adb.exe")}\" {command}", 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0) 
            {
                string errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                throw new AdbCommandException($"ADB command failed with exit code {process.ExitCode}: {errorMessage.Trim()}");
            }
            
            return output.Trim();
        }
    }

    public class AdbCommandException : Exception
    {
        public AdbCommandException() { }

        public AdbCommandException(string message) : base(message) { }

        public AdbCommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}
