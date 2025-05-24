using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// using System.Windows.Forms; // Removed to ensure no other UI dependencies slip in. MessageBox will be fully qualified.
using WirelessAdbPackageManager.Models; // For ConnectivityStatus
using WirelessAdbPackageManager.Services; // For AdbManager
// No need to add 'using WirelessAdbPackageManager.Handlers;' as AppManager is already in this namespace. UIManager will be resolved directly.

namespace WirelessAdbPackageManager.Handlers
{
    /// <summary>
    /// Manages application logic, ADB interactions, and device state.
    /// </summary>
    public class AppManager 
    {
        /// <summary>
        /// Delegate for handling log messages.
        /// </summary>
        /// <param name="message">The log message.</param>
        public delegate void LogMessageHandler(string message);
        /// <summary>
        /// Delegate for handling updates to package lists.
        /// </summary>
        /// <param name="enabledPackages">A list of currently enabled packages.</param>
        /// <param name="disabledPackages">A list of currently disabled packages.</param>
        public delegate void PackageListUpdatedHandler(List<string> enabledPackages, List<string> disabledPackages);
        /// <summary>
        /// Delegate for handling changes in device connection status.
        /// </summary>
        /// <param name="isConnected">True if connected, false otherwise.</param>
        /// <param name="deviceName">The name of the connected device, if available.</param>
        public delegate void ConnectionStatusChangedHandler(bool isConnected, string deviceName);

        /// <summary>
        /// Occurs when a log message is generated.
        /// </summary>
        public event LogMessageHandler LogMessageGenerated;
        /// <summary>
        /// Occurs when the lists of enabled or disabled packages are updated.
        /// </summary>
        public event PackageListUpdatedHandler PackageListsUpdated;
        /// <summary>
        /// Occurs when the device's connection status changes.
        /// </summary>
        public event ConnectionStatusChangedHandler ConnectionStatusChanged;

        /// <summary>
        /// Delegate for handling failed operations.
        /// </summary>
        /// <param name="title">The title for the error message.</param>
        /// <param name="message">The error message.</param>
        public delegate void OperationFailedHandler(string title, string message);

        /// <summary>
        /// Occurs when an ADB or package operation fails and needs to inform the user.
        /// </summary>
        public event OperationFailedHandler OperationFailed;

        private readonly AdbManager _adbManager; 

        /// <summary>
        /// Gets or sets the list of currently enabled packages on the connected device.
        /// </summary>
        public List<string> enabledPackages = new(); 
        /// <summary>
        /// Gets or sets the list of currently disabled packages on the connected device.
        /// </summary>
        public List<string> disabledPackages = new(); 

        /// <summary>
        /// Initializes a new instance of the <see cref="AppManager"/> class.
        /// </summary>
        public AppManager()
        {
            _adbManager = new AdbManager();
        }

        /// <summary>
        /// Initializes ADB by installing necessary resources.
        /// </summary>
        /// <returns>True if ADB initialization was successful, false otherwise.</returns>
        public bool InitializeAdb()
        {
            return _adbManager.Install();
        }

        /// <summary>
        /// Handles the overall connection process to a device using the provided details.
        /// This includes validation, checking existing pairing, pairing if necessary, and connecting.
        /// </summary>
        /// <param name="ipAddress">The IP address of the device.</param>
        /// <param name="port">The port number for pairing/connection.</param>
        /// <param name="pairingCode">The pairing code, if required for initial pairing.</param>
        public async Task HandleConnection(string ipAddress, string port, string pairingCode) 
        {
            if (!await ValidateForm(ipAddress, port, pairingCode))
            {
                return; // Validation failed, do not proceed.
            }

            await CheckIfAlreadyPaired(ipAddress);

            if (ConnectivityStatus.IsPaired && ConnectivityStatus.IsConnected)
            {
                string deviceType = await GetDeviceType(ipAddress); // Pass ipAddress
                string msg = string.IsNullOrEmpty(deviceType) ? $"Successfully connected to your device" : $"Successfully connected to your {deviceType}";
                LogMessageGenerated?.Invoke(msg); // Use event
                ConnectionStatusChanged?.Invoke(true, deviceType); // Raise event
                await RetrievePackageLists();
                return;
            }

            if (ConnectivityStatus.IsPaired)
            {
                bool isConnected = await ConnectToDevice(ipAddress, port);
                if (isConnected)
                {
                    // ConnectionStatusChanged and RetrievePackageLists will be handled by ConnectToDevice
                }
            }
            else
            {
                // UIManager.Form.ConnectButton.Enabled = false; // UI logic moves to Form1
                bool isPaired = await PairWithDevice(ipAddress, port, pairingCode);
                if (isPaired)
                {
                    ConnectivityStatus.IsPaired = true; // This state should be managed carefully
                    ShowInfo("ADB has paired with your device!\r\nUpdate the port as needed and hit Connect again to continue.");
                    // ConnectionStatusChanged?.Invoke(false, null); // Still not fully connected, just paired
                }
                else
                {
                    ShowError("Unable to pair with your device, please try again.");
                }
                // UIManager.Form.ConnectButton.Enabled = true; // UI logic moves to Form1
            }
        }

        /// <summary>
        /// Retrieves the model name of the device at the specified IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address of the device.</param>
        /// <returns>The model name of the device, or null if not found or not connected.</returns>
        public async Task<string> GetDeviceType(string ipAddress) 
        {
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe devices -l"); 
            var split = result.Split("\r\n").ToList();
            var deviceLine = split.FirstOrDefault(item => item.Contains(ipAddress)); // Use parameter
            if (deviceLine != null)
            {
                split = deviceLine.Split(" ").ToList();
                var deviceName = split.FirstOrDefault(item => item.Contains("model:"));
                if (deviceName != null)
                    return deviceName.Replace("model:", "");
            }
            return null;
        }

        /// <summary>
        /// Checks if the device at the specified IP address is already paired and connected.
        /// Updates <see cref="ConnectivityStatus"/> and raises <see cref="ConnectionStatusChanged"/> if already connected.
        /// </summary>
        /// <param name="ip">The IP address of the device to check.</param>
        /// <returns>True if the device is already paired and connected, false otherwise.</returns>
        public async Task<bool> CheckIfAlreadyPaired(string ip) 
        {
            LogMessageGenerated?.Invoke("Checking for existing pair with device");
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe devices"); 

            if (!result.Contains("List of devices attached")) return false;

            var connectedDevices = result.Split("\r\n").Where(s => s.Contains(":")).ToList();

            foreach (var device in connectedDevices)
            {
                if (device.Contains(ip) && !device.Contains("offline"))
                {
                    var port = device.Split(":")[1].Replace("device", "").Trim();
                    // UIManager.Form.PortTextBox.Text = port; // UI logic moves to Form1
                    ConnectivityStatus.IsPaired = true;
                    ConnectivityStatus.IsConnected = true;
                    // LogMessageGenerated?.Invoke($"Previously paired with device, updated port to {port}"); // Port update is UI concern
                    // ConnectionStatusChanged?.Invoke(true, await GetDeviceType()); // Handled in HandleConnection
                    return true;
                }
            }

            LogMessageGenerated?.Invoke("No previous connections detected");
            return false;
        }

        /// <summary>
        /// Attempts to pair with a device using the specified IP, port, and pairing code.
        /// </summary>
        /// <param name="ip">The IP address of the device.</param>
        /// <param name="port">The pairing port of the device.</param>
        /// <param name="pairingCode">The pairing code displayed on the device.</param>
        /// <returns>True if pairing was successful, false otherwise.</returns>
        public async Task<bool> PairWithDevice(string ip, string port, string pairingCode) 
        {
            LogMessageGenerated?.Invoke($"Attempting to pair with {ip}");
            await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe disconnect"); 
            await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe kill-server"); 
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe pair {ip}:{port} {pairingCode}"); 
            bool success = result.Contains($"Successfully paired to {ip}:{port}");
            if(success) LogMessageGenerated?.Invoke($"Successfully paired to {ip}:{port}"); else LogMessageGenerated?.Invoke($"Failed to pair: {result}");
            return success;
        }

        /// <summary>
        /// Attempts to connect to a device at the specified IP and port.
        /// Raises <see cref="ConnectionStatusChanged"/> and retrieves package lists upon successful connection.
        /// </summary>
        /// <param name="ip">The IP address of the device.</param>
        /// <param name="port">The connection port of the device.</param>
        /// <returns>True if connection was successful, false otherwise.</returns>
        public async Task<bool> ConnectToDevice(string ip, string port) 
        {
            LogMessageGenerated?.Invoke($"Attempting to connect to {ip}");
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe connect {ip}:{port}"); 
            LogMessageGenerated?.Invoke($"ADB: {result}");
            bool isConnected = result.Contains($"connected to {ip}:{port}");
            if (isConnected)
            {
                ConnectivityStatus.IsConnected = true; // Manage state
                string deviceType = await GetDeviceType(ipAddress); // Pass ipAddress
                LogMessageGenerated?.Invoke(string.IsNullOrEmpty(deviceType) ? $"Successfully connected to your device" : $"Successfully connected to your {deviceType}");
                ConnectionStatusChanged?.Invoke(true, deviceType);
                await RetrievePackageLists();
            }
            else
            {
                ConnectionStatusChanged?.Invoke(false, null);
            }
            return isConnected;
        }

        /// <summary>
        /// Disconnects from the currently connected device and clears package lists.
        /// Raises <see cref="ConnectionStatusChanged"/> and <see cref="PackageListsUpdated"/>.
        /// </summary>
        public async Task DisconnectDevice() 
        {
            await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe disconnect"); 
            await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe kill-server"); 
            LogMessageGenerated?.Invoke("Disconnected from device");
            ConnectivityStatus.IsConnected = false; 
            ConnectivityStatus.IsPaired = false; 
            ConnectionStatusChanged?.Invoke(false, null); 
            ClearPackageLists(); 
        }

        /// <summary>
        /// Initiates the installation of an APK file from the specified path.
        /// </summary>
        /// <param name="apkFilePath">The full path to the APK file to be installed.</param>
        public async Task InstallPackage(string apkFilePath) 
        {
            if (string.IsNullOrWhiteSpace(apkFilePath))
            {
                LogMessageGenerated?.Invoke("APK file path cannot be empty.");
                OperationFailed?.Invoke("Installation Failed", "No APK file path provided.");
                return;
            }

            try
            {
                // Path.GetFileName is safe and will extract the file name for logging/messaging.
                string apkFileName = Path.GetFileName(apkFilePath);
                await PerformPackageOperation(null, $"install \"{apkFilePath}\"", "Success", $"Installed {apkFileName}", $"Unable to install {apkFileName}");
            }
            catch (Exception ex)
            {
                // Log the exception and notify the user
                LogMessageGenerated?.Invoke($"Error during package installation: {ex.Message}");
                OperationFailed?.Invoke("Installation Error", $"An unexpected error occurred while trying to install {Path.GetFileName(apkFilePath)}.");
            }
        }

        /// <summary>
        /// Performs a package operation (install, uninstall, disable, enable) on the connected device.
        /// </summary>
        /// <param name="packageNames">An enumerable collection of package names to perform the operation on. Can be null for operations that don't target specific packages (e.g. installing an APK specified directly in the 'command').</param>
        /// <param name="command">The ADB shell command for the operation (e.g., "uninstall -k --user 0"). For package-specific commands, "{packageName}" will be replaced with each package name.</param>
        /// <param name="expectedResult">The expected string in the ADB output for a successful operation.</param>
        /// <param name="successMessage">The base message to log upon success. For package-specific commands, "{packageName}" can be used as a placeholder.</param>
        /// <param name="errorMessage">The base message to show in case of an error. For package-specific commands, "{packageName}" can be used as a placeholder.</param>
        public async Task PerformPackageOperation(IEnumerable<string> packageNames, string command, string expectedResult, string successMessage, string errorMessage) 
        {
            if (packageNames == null || !packageNames.Any()) // Handles both null and empty list for package-specific operations
            {
                 // This case is typically for installing an APK where 'command' contains the full adb install command with file path.
                if (command.Contains("install")) // Check if it's an install command
                {
                    string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe {command}");
                    if (result.Contains(expectedResult))
                    {
                        LogMessageGenerated?.Invoke(successMessage); // successMessage for install should be complete
                    }
                    else
                    {
                        OperationFailed?.Invoke("Operation Failed", $"{errorMessage} \nDetails: {result}");
                    }
                }
                else if (packageNames == null && !command.Contains("install"))
                {
                    // If packageNames is null and it's not an install command, it's an invalid state for other operations.
                    LogMessageGenerated?.Invoke($"PerformPackageOperation called with null packageNames for a non-install command: {command}");
                    OperationFailed?.Invoke("Operation Error", "No packages specified for the operation.");
                    return; // Exit early
                }
                // If packageNames is empty but not null, and it's not an install command, it means no packages were selected for the operation.
                // This is a valid scenario (e.g., user clicked "uninstall" with no packages checked).
                // The loop below will simply not execute. We could log this if desired.
            }
            else
            {
                foreach (string packageName in packageNames)
                {
                    if (string.IsNullOrWhiteSpace(packageName)) continue; // Skip if a package name is empty

                    string adbCommand = $@"{Path.GetTempPath()}adb.exe shell pm {command.Replace("{packageName}", packageName)}";
                    // If the command is meant for "com.packageName" format, adjust here or in the caller.
                    // Assuming the command string is structured like "uninstall -k --user 0 {packageName}" or "disable-user --user 0 {packageName}"
                    // If it needs "com." prefix, it should be like "com.{packageName}" in the command string construction by the caller or here.
                    // For now, assuming the 'command' parameter is constructed to handle this.
                    // Example: if command is "uninstall", it should be "uninstall com.{packageName}"
                    // The current structure of calls from Form1 for uninstall/disable/enable is:
                    // adb shell pm <actual_command_like_uninstall> com.{actual_package_name}
                    // So, we need to ensure the 'command' parameter from Form1 is just "uninstall -k --user 0"
                    // and we append "com.{packageName}" here.
                    // Let's adjust: the `command` parameter should be the core action, e.g., "uninstall -k --user 0"
                    // and we append "com.{packageName}" here.
                    
                    string fullAdbCommand = $@"{Path.GetTempPath()}adb.exe shell pm {command} com.{packageName}";

                    string result = await _adbManager.RunCommandAsync(fullAdbCommand); 
                    if (result.Contains(expectedResult))
                    {
                        LogMessageGenerated?.Invoke(successMessage.Replace("{packageName}", packageName));
                    }
                    else
                    {
                        OperationFailed?.Invoke("Operation Failed", errorMessage.Replace("{packageName}", packageName) + $" \nDetails: {result}");
                    }
                }
            }
            await RetrievePackageLists();
        }

        /// <summary>
        /// Retrieves the lists of enabled and disabled packages from the connected device.
        /// Raises the <see cref="PackageListsUpdated"/> event.
        /// </summary>
        public async Task RetrievePackageLists() 
        {
            this.enabledPackages.Clear(); 
            this.disabledPackages.Clear();

            string disabledPackagesResult = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe shell pm list packages -d"); 
            if (disabledPackagesResult.Contains("package:com.") || disabledPackagesResult.Contains("package:")) 
            {
                this.disabledPackages = ExtractPackages(disabledPackagesResult);
            }

            string enabledPackagesResult = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe shell pm list packages"); 
            if (enabledPackagesResult.Contains("package:com.") || enabledPackagesResult.Contains("package:"))
            {
                LogMessageGenerated?.Invoke("Updating current package lists");
                List<string> allPackages = ExtractPackages(enabledPackagesResult);
                this.enabledPackages = allPackages.Except(this.disabledPackages).ToList();
            }
            else
            {
                LogMessageGenerated?.Invoke("No packages could be found, try reconnecting.");
            }
            
            PackageListsUpdated?.Invoke(new List<string>(this.enabledPackages), new List<string>(this.disabledPackages)); 
        }

        /// <summary>
        /// Extracts package names from the raw string output of an ADB command like "pm list packages".
        /// </summary>
        /// <param name="adbOutput">The raw string output from the ADB command.</param>
        /// <returns>A list of extracted package names. Returns an empty list if no packages are found or if the input is invalid.</returns>
        private List<string> ExtractPackages(string adbOutput) 
        {
            if (string.IsNullOrWhiteSpace(adbOutput))
            {
                return new List<string>();
            }

            return adbOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(s => s.StartsWith("package:"))
                            .Select(s => s.Replace("package:", "").Trim())
                            .Where(s => !string.IsNullOrEmpty(s)) 
                            .ToList();
        }


        /// <summary>
        /// Validates the provided IP address, port, and pairing code.
        /// </summary>
        /// <param name="ipAddress">The IP address to validate.</param>
        /// <param name="port">The port number to validate.</param>
        /// <param name="pairingCode">The pairing code to validate.</param>
        /// <returns>True if all inputs are valid, false otherwise.</returns>
        public async Task<bool> ValidateForm(string ipAddress, string port, string pairingCode) 
        {
            bool result;

            result = await IsValidIPAddress(ipAddress); 
            if (!result)
            {
                ShowError("Invalid IP Address"); 
                return false;
            }

            result = await IsNumericAndLength(port, 5); 
            if (!result)
            {
                ShowError("Invalid Port");
                return false;
            }

            result = await IsNumericAndLength(pairingCode, 6); 
            if (!result)
            {
                ShowError("Invalid Pairing Code");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Displays an error message box.
        /// Note: Direct UI interaction; consider moving to UI layer or using an event.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public void ShowError(string message) 
        {
            System.Windows.Forms.MessageBox.Show(message, "Application Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }

        /// <summary>
        /// Displays an informational message box.
        /// Note: Direct UI interaction; consider moving to UI layer or using an event.
        /// </summary>
        /// <param name="message">The informational message to display.</param>
        public void ShowInfo(string message) 
        {
            System.Windows.Forms.MessageBox.Show(message, "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }

        /// <summary>
        /// Generates a log message with a timestamp and raises the <see cref="LogMessageGenerated"/> event.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void UpdateLog(string message) 
        {
            string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
            LogMessageGenerated?.Invoke($"{timestamp} - {message}"); 
        }

        /// <summary>
        /// Clears the internal lists of enabled and disabled packages and raises the <see cref="PackageListsUpdated"/> event.
        /// </summary>
        public void ClearPackageLists() 
        {
            this.enabledPackages.Clear();
            this.disabledPackages.Clear();
            PackageListsUpdated?.Invoke(new List<string>(this.enabledPackages), new List<string>(this.disabledPackages)); 
        }
        
        /// <summary>
        /// Extracts the package name from a CheckedListBox item.
        /// </summary>
        /// <param name="item">The item from the CheckedListBox.</param>
        /// <returns>The extracted package name.</returns>
        public string GetPackageName(object item) 
        {
            return item.ToString().Replace("package:com.", "");
        }


        /// <summary>
        /// Validates if the input string is a valid IP address.
        /// </summary>
        /// <param name="input">The string to validate.</param>
        /// <returns>True if the input is a valid IP address, false otherwise.</returns>
        public async Task<bool> IsValidIPAddress(string input) 
        {
            const string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

            return Regex.IsMatch(input, pattern);
        }

        public async Task<bool> IsNumericAndLength(string input, int length) // Instance method
        {
            return !string.IsNullOrEmpty(input) && input.Length == length && input.All(char.IsDigit);
        }
    }
}
