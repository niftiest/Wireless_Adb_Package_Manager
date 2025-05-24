using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms; // For MessageBox in Uninstall, consider removing later
using WirelessAdbPackageManager.Properties; // For Resources

namespace WirelessAdbPackageManager.Services
{
    /// <summary>
    /// Manages ADB (Android Debug Bridge) operations, including installation, uninstallation, and command execution.
    /// </summary>
    public class AdbManager 
    {
        /// <summary>
        /// Installs ADB resources by extracting them to a temporary path.
        /// </summary>
        /// <returns>True if installation was successful, false otherwise.</returns>
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
            catch (Exception ex) // It's better to catch specific exceptions if known, or log this.
            {
                // Consider logging the exception details (ex.ToString())
                return false;
            }
        }

        /// <summary>
        /// Uninstalls ADB resources by deleting them from the temporary path.
        /// </summary>
        /// <returns>True if uninstallation was successful, false otherwise.</returns>
        public bool Uninstall() 
        {
            var resources = new List<string> { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };
            try
            {
                foreach (var resource in resources)
                {
                    var filePath = Path.Combine(Path.GetTempPath(), resource);
                    if (File.Exists(filePath)) // Check if file exists before deleting
                    {
                        File.Delete(filePath);
                    }
                }
                return true;
            }
            catch (Exception ex) 
            {
                // This MessageBox makes the class UI-dependent. Consider replacing with logging or custom event.
                MessageBox.Show($"Unable to uninstall adb resources: {ex.Message}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Dumps an embedded resource to a specified file path.
        /// </summary>
        /// <param name="fileName">The name of the file to create.</param>
        /// <param name="resource">The byte array of the resource content.</param>
        /// <param name="directoryPath">The directory path where the file should be saved.</param>
        private static void DumpResourceToFile(string fileName, byte[] resource, string directoryPath) 
        {
            var filePath = Path.Combine(directoryPath, fileName);
            if (!File.Exists(filePath))
            {
                File.WriteAllBytes(filePath, resource);
            }
        }

        /// <summary>
        /// Executes an ADB command asynchronously.
        /// </summary>
        /// <param name="command">The ADB command to execute (e.g., "devices -l").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the standard output of the command.</returns>
        /// <exception cref="AdbCommandException">Thrown when the ADB command fails or returns an error.</exception>
        public async Task<string> RunCommandAsync(string command) 
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe", // Using cmd.exe to launch adb.exe which is expected to be in temp path or PATH
                    Arguments = $"/c \"{Path.Combine(Path.GetTempPath(), "adb.exe")}\" {command}", // Ensure adb.exe path is explicit
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

            // Check for errors even if ExitCode is 0, as some adb commands output to stderr on success.
            // However, a non-zero exit code is a definitive error.
            if (process.ExitCode != 0) 
            {
                // Prefer error stream content if available and significant
                string errorMessage = string.IsNullOrWhiteSpace(error) ? output : error;
                throw new AdbCommandException($"ADB command failed with exit code {process.ExitCode}: {errorMessage.Trim()}");
            }
            
            // If there's anything in the error stream but ExitCode is 0, it might be a warning or non-critical info.
            // For this application, we might want to log 'error' stream if it's not empty, even on success.
            // For now, returning 'output' as primary result. If 'error' contains crucial failure info not reflected in ExitCode, logic might need adjustment.

            return output.Trim();
        }
    }

    /// <summary>
    /// Represents errors that occur during ADB command execution.
    /// </summary>
    public class AdbCommandException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdbCommandException"/> class.
        /// </summary>
        public AdbCommandException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbCommandException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AdbCommandException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbCommandException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public AdbCommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}
