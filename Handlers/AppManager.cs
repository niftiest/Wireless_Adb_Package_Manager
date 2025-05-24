using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WirelessAdbPackageManager.Models; 
using WirelessAdbPackageManager.Services; 

namespace WirelessAdbPackageManager.Handlers
{
    public class AppManager 
    {
        public delegate void LogMessageHandler(string message);
        public delegate void PackageListUpdatedHandler(List<string> enabledPackages, List<string> disabledPackages);
        public delegate void ConnectionStatusChangedHandler(bool isConnected, string deviceName);

        public event LogMessageHandler LogMessageGenerated;
        public event PackageListUpdatedHandler PackageListsUpdated;
        public event ConnectionStatusChangedHandler ConnectionStatusChanged;

        public delegate void OperationFailedHandler(string title, string message);

        public event OperationFailedHandler OperationFailed;

        private readonly AdbManager _adbManager; 

        public List<string> enabledPackages = new(); 
        public List<string> disabledPackages = new(); 

        public AppManager()
        {
            _adbManager = new AdbManager();
        }

        public bool InitializeAdb()
        {
            return _adbManager.Install();
        }

        public async Task HandleConnection(string ipAddress, string port, string pairingCode) 
        {
            if (!await ValidateForm(ipAddress, port, pairingCode))
            {
                return; 
            }

            await CheckIfAlreadyPaired(ipAddress);

            if (ConnectivityStatus.IsPaired && ConnectivityStatus.IsConnected)
            {
                string deviceType = await GetDeviceType(ipAddress); 
                string msg = string.IsNullOrEmpty(deviceType) ? $"Successfully connected to your device" : $"Successfully connected to your {deviceType}";
                LogMessageGenerated?.Invoke(msg); 
                ConnectionStatusChanged?.Invoke(true, deviceType); 
                await RetrievePackageLists();
                return;
            }

            if (ConnectivityStatus.IsPaired)
            {
                bool isConnected = await ConnectToDevice(ipAddress, port);
                if (isConnected)
                {
                }
            }
            else
            {
                bool isPaired = await PairWithDevice(ipAddress, port, pairingCode);
                if (isPaired)
                {
                    ConnectivityStatus.IsPaired = true; 
                    ShowInfo("ADB has paired with your device!\r\nUpdate the port as needed and hit Connect again to continue.");
                }
                else
                {
                    ShowError("Unable to pair with your device, please try again.");
                }
            }
        }

        public async Task<string> GetDeviceType(string ipAddress) 
        {
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe devices -l"); 
            var split = result.Split("\r\n").ToList();
            var deviceLine = split.FirstOrDefault(item => item.Contains(ipAddress)); 
            if (deviceLine != null)
            {
                split = deviceLine.Split(" ").ToList();
                var deviceName = split.FirstOrDefault(item => item.Contains("model:"));
                if (deviceName != null)
                    return deviceName.Replace("model:", "");
            }
            return null;
        }

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
                    ConnectivityStatus.IsPaired = true;
                    ConnectivityStatus.IsConnected = true;
                    return true;
                }
            }

            LogMessageGenerated?.Invoke("No previous connections detected");
            return false;
        }

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

        public async Task<bool> ConnectToDevice(string ip, string port) 
        {
            LogMessageGenerated?.Invoke($"Attempting to connect to {ip}");
            string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe connect {ip}:{port}"); 
            LogMessageGenerated?.Invoke($"ADB: {result}");
            bool isConnected = result.Contains($"connected to {ip}:{port}");
            if (isConnected)
            {
                ConnectivityStatus.IsConnected = true; 
                string deviceType = await GetDeviceType(ipAddress); 
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
                string apkFileName = Path.GetFileName(apkFilePath);
                await PerformPackageOperation(null, $"install \"{apkFilePath}\"", "Success", $"Installed {apkFileName}", $"Unable to install {apkFileName}");
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"Error during package installation: {ex.Message}");
                OperationFailed?.Invoke("Installation Error", $"An unexpected error occurred while trying to install {Path.GetFileName(apkFilePath)}.");
            }
        }

        public async Task PerformPackageOperation(IEnumerable<string> packageNames, string command, string expectedResult, string successMessage, string errorMessage) 
        {
            if (packageNames == null || !packageNames.Any()) 
            {
                if (command.Contains("install")) 
                {
                    string result = await _adbManager.RunCommandAsync($@"{Path.GetTempPath()}adb.exe {command}");
                    if (result.Contains(expectedResult))
                    {
                        LogMessageGenerated?.Invoke(successMessage); 
                    }
                    else
                    {
                        OperationFailed?.Invoke("Operation Failed", $"{errorMessage} \nDetails: {result}");
                    }
                }
                else if (packageNames == null && !command.Contains("install"))
                {
                    LogMessageGenerated?.Invoke($"PerformPackageOperation called with null packageNames for a non-install command: {command}");
                    OperationFailed?.Invoke("Operation Error", "No packages specified for the operation.");
                    return; 
                }
            }
            else
            {
                foreach (string packageName in packageNames)
                {
                    if (string.IsNullOrWhiteSpace(packageName)) continue; 

                    string adbCommand = $@"{Path.GetTempPath()}adb.exe shell pm {command.Replace("{packageName}", packageName)}";
                    
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

        public void ShowError(string message) 
        {
            System.Windows.Forms.MessageBox.Show(message, "Application Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }

        public void ShowInfo(string message) 
        {
            System.Windows.Forms.MessageBox.Show(message, "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }

        public void UpdateLog(string message) 
        {
            string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
            LogMessageGenerated?.Invoke($"{timestamp} - {message}"); 
        }

        public void ClearPackageLists() 
        {
            this.enabledPackages.Clear();
            this.disabledPackages.Clear();
            PackageListsUpdated?.Invoke(new List<string>(this.enabledPackages), new List<string>(this.disabledPackages)); 
        }
        
        public string GetPackageName(object item) 
        {
            return item.ToString().Replace("package:com.", "");
        }


        public async Task<bool> IsValidIPAddress(string input) 
        {
            const string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                                   @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

            return Regex.IsMatch(input, pattern);
        }

        public async Task<bool> IsNumericAndLength(string input, int length) 
        {
            return !string.IsNullOrEmpty(input) && input.Length == length && input.All(char.IsDigit);
        }
    }
}
