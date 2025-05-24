using WirelessAdbPackageManager.Handlers;

using System.Linq; 
using System.Collections.Generic; 
using System; 
using System.Windows.Forms; 

namespace WirelessAdbPackageManager
{
    public partial class Form1 : Form
    {
        private readonly AppManager _appManager; 
        private List<string> _currentEnabledPackages = new List<string>();
        private List<string> _currentDisabledPackages = new List<string>();

        public Form1(AppManager appManager) 
        {
            _appManager = appManager; 

            InitializeComponent(); 

            _appManager.LogMessageGenerated += OnLogMessageGenerated;
            _appManager.PackageListsUpdated += OnPackageListsUpdated;
            _appManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _appManager.OperationFailed += OnAppManagerOperationFailed; 

            if (_appManager.InitializeAdb())
            {
            }
            else
            {
                _appManager.ShowError("Unable to install ADB, critical functionality may be disabled.");
            }
            UpdateActionButtonsState(); 
        }

        private void OnLogMessageGenerated(string message)
        {
            if (LogsTextBox.InvokeRequired)
            {
                LogsTextBox.Invoke(new Action(() => OnLogMessageGenerated(message)));
                return;
            }
            LogsTextBox.Text = string.IsNullOrEmpty(LogsTextBox.Text)
                ? message
                : $"{message}\r\n{LogsTextBox.Text}"; 
        }

        private void OnPackageListsUpdated(List<string> enabledPackages, List<string> disabledPackages)
        {
            if (EnabledPackagesCheckBoxList.InvokeRequired) 
            {
                EnabledPackagesCheckBoxList.Invoke(new Action(() => OnPackageListsUpdated(enabledPackages, disabledPackages)));
                return;
            }

            _currentEnabledPackages = new List<string>(enabledPackages);
            _currentDisabledPackages = new List<string>(disabledPackages);

            ApplyFiltersAndRefreshLists();
            UpdateActionButtonsState();
        }
        
        private void ApplyFiltersAndRefreshLists()
        {
            EnabledPackagesCheckBoxList.Items.Clear();
            string enabledFilter = EnabledPackagesSearchTextBox.Text;
            var filteredEnabled = string.IsNullOrEmpty(enabledFilter)
                ? _currentEnabledPackages
                : _currentEnabledPackages.Where(p => p.Contains(enabledFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            EnabledPackagesCheckBoxList.Items.AddRange(filteredEnabled.ToArray());

            DisabledPackagesCheckBoxList.Items.Clear();
            string disabledFilter = DisabledPackagesSearchTextBox.Text;
            var filteredDisabled = string.IsNullOrEmpty(disabledFilter)
                ? _currentDisabledPackages
                : _currentDisabledPackages.Where(p => p.Contains(disabledFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            DisabledPackagesCheckBoxList.Items.AddRange(filteredDisabled.ToArray());
        }

        private void OnConnectionStatusChanged(bool isConnected, string deviceName)
        {
            if (ConnectButton.InvokeRequired) 
            {
                ConnectButton.Invoke(new Action(() => OnConnectionStatusChanged(isConnected, deviceName)));
                return;
            }
            if (isConnected)
            {
                InstallButton.Enabled = true;
                IpAddressTextBox.Enabled = false;
                PortTextBox.Enabled = false;
                PairingCodeTextBox.Enabled = false;
                ConnectButton.Text = "DISCONNECT";
            }
            else
            {
                InstallButton.Enabled = false;
                IpAddressTextBox.Enabled = true;
                PortTextBox.Enabled = true;
                PairingCodeTextBox.Enabled = true;
                ConnectButton.Text = "CONNECT";
            }
            UpdateActionButtonsState(); 
        }
        
        private void UpdateActionButtonsState()
        {
            if (UninstallButton.InvokeRequired)
            {
                UninstallButton.Invoke(new Action(UpdateActionButtonsState));
                return;
            }
            bool isConnected = ConnectButton.Text.Equals("DISCONNECT");
            UninstallButton.Enabled = DisableButton.Enabled = (EnabledPackagesCheckBoxList.CheckedItems.Count > 0) && isConnected;
            EnableButton.Enabled = (DisabledPackagesCheckBoxList.CheckedItems.Count > 0) && isConnected;
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            if (ConnectButton.Text.Equals("DISCONNECT"))
            {
                await _appManager.DisconnectDevice(); 
            }
            else
            {
                string ip = IpAddressTextBox.Text;
                string port = PortTextBox.Text;
                string pairingCode = PairingCodeTextBox.Text;
                await _appManager.HandleConnection(ip, port, pairingCode); 
            }
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "APK Files (*.apk)|*.apk",
                Title = "Select an APK File",
                Multiselect = false 
            };

            if (openFileDialog.ShowDialog(this) == DialogResult.OK) 
            {
                string selectedFilePath = openFileDialog.FileName;
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    await _appManager.InstallPackage(selectedFilePath);
                }
            }
        }

        private async void UninstallButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = EnabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "uninstall -k --user 0", "Success", "Uninstalled {packageName}", "Unable to uninstall {packageName}"); 
            }
        }

        private async void DisableButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = EnabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "disable-user --user 0", "new state:", "Disabled {packageName}", "Unable to disable {packageName}");
            }
        }

        private async void EnableButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = DisabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "enable", "new state:", "Enabled {packageName}", "Unable to enable {packageName}");
            }
        }

        private void EnabledPackagesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void DisabledPackagesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void EnabledPackageFilter_TextChanged(object sender, EventArgs e)
        {
            string searchTerm = EnabledPackagesSearchTextBox.Text;
            var filtered = string.IsNullOrEmpty(searchTerm)
                           ? _currentEnabledPackages
                           : _currentEnabledPackages.Where(p => p.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            EnabledPackagesCheckBoxList.Items.Clear();
            EnabledPackagesCheckBoxList.Items.AddRange(filtered.ToArray());
            UpdateActionButtonsState(); 
        }

        private void DisabledPackageFilter_TextChanged(object sender, EventArgs e)
        {
            string searchTerm = DisabledPackagesSearchTextBox.Text;
            var filtered = string.IsNullOrEmpty(searchTerm)
                           ? _currentDisabledPackages
                           : _currentDisabledPackages.Where(p => p.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            DisabledPackagesCheckBoxList.Items.Clear();
            DisabledPackagesCheckBoxList.Items.AddRange(filtered.ToArray());
            UpdateActionButtonsState(); 
        }      
        
        private void OnAppManagerOperationFailed(string title, string message)
        {
            if (InvokeRequired) 
            {
                Invoke(new Action(() => OnAppManagerOperationFailed(title, message)));
                return;
            }
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}