using WirelessAdbPackageManager.Handlers; // For AppManager

using System.Linq; 
using System.Collections.Generic; 
using System; // For EventArgs, Action
using System.Windows.Forms; // For Form, Control event handlers etc.

namespace WirelessAdbPackageManager
{
    /// <summary>
    /// Main form for the Wireless ADB Package Manager application.
    /// Handles user interactions and displays information related to ADB package management.
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly AppManager _appManager; 
        private List<string> _currentEnabledPackages = new List<string>();
        private List<string> _currentDisabledPackages = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        /// <param name="appManager">The application manager instance for handling business logic.</param>
        public Form1(AppManager appManager) 
        {
            _appManager = appManager; 

            InitializeComponent(); 

            // Subscribe to AppManager events
            _appManager.LogMessageGenerated += OnLogMessageGenerated;
            _appManager.PackageListsUpdated += OnPackageListsUpdated;
            _appManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _appManager.OperationFailed += OnAppManagerOperationFailed; // Subscribe to the new event

            if (_appManager.InitializeAdb())
            {
                // ADB initialized successfully
            }
            else
            {
                _appManager.ShowError("Unable to install ADB, critical functionality may be disabled.");
            }
            UpdateActionButtonsState(); // Initial state of buttons
        }

        /// <summary>
        /// Handles the LogMessageGenerated event from AppManager to update the logs text box.
        /// Ensures thread-safe UI updates.
        /// </summary>
        /// <param name="message">The log message to display.</param>
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

        /// <summary>
        /// Handles the PackageListsUpdated event from AppManager.
        /// Stores the updated package lists locally and refreshes the UI lists.
        /// Ensures thread-safe UI updates.
        /// </summary>
        /// <param name="enabledPackages">The list of currently enabled packages.</param>
        /// <param name="disabledPackages">The list of currently disabled packages.</param>
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
        
        /// <summary>
        /// Applies current search filters to the locally stored package lists and updates the CheckBoxLists.
        /// </summary>
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

        /// <summary>
        /// Handles the ConnectionStatusChanged event from AppManager.
        /// Updates UI elements (buttons, text boxes) based on the connection status.
        /// Ensures thread-safe UI updates.
        /// </summary>
        /// <param name="isConnected">True if connected, false otherwise.</param>
        /// <param name="deviceName">The name of the connected device, if available.</param>
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
                // this.Text = $"Wireless ADB Package Manager - Connected to {deviceName}"; 
            }
            else
            {
                InstallButton.Enabled = false;
                IpAddressTextBox.Enabled = true;
                PortTextBox.Enabled = true;
                PairingCodeTextBox.Enabled = true;
                ConnectButton.Text = "CONNECT";
                // this.Text = "Wireless ADB Package Manager - Disconnected";
            }
            UpdateActionButtonsState(); 
        }
        
        /// <summary>
        /// Updates the enabled state of action buttons (Uninstall, Disable, Enable)
        /// based on item selections and connection status.
        /// Ensures thread-safe UI updates.
        /// </summary>
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

        /// <summary>
        /// Handles the Click event of the ConnectButton.
        /// Initiates connection or disconnection sequence via AppManager.
        /// </summary>
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
                // ValidateForm is called inside HandleConnection in AppManager
                await _appManager.HandleConnection(ip, port, pairingCode); 
            }
        }

        /// <summary>
        /// Handles the Click event of the InstallButton.
        /// Opens a file dialog for the user to select an APK, then initiates the installation process via AppManager.
        /// </summary>
        private async void InstallButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "APK Files (*.apk)|*.apk",
                Title = "Select an APK File",
                Multiselect = false // Ensure only one file can be selected
            };

            if (openFileDialog.ShowDialog(this) == DialogResult.OK) // Pass 'this' for proper owner
            {
                string selectedFilePath = openFileDialog.FileName;
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    await _appManager.InstallPackage(selectedFilePath);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the UninstallButton.
        /// Initiates package uninstallation for selected items via AppManager.
        /// </summary>
        private async void UninstallButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = EnabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "uninstall -k --user 0", "Success", "Uninstalled {packageName}", "Unable to uninstall {packageName}"); 
            }
        }

        /// <summary>
        /// Handles the Click event of the DisableButton.
        /// Initiates package disabling for selected items via AppManager.
        /// </summary>
        private async void DisableButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = EnabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "disable-user --user 0", "new state:", "Disabled {packageName}", "Unable to disable {packageName}");
            }
        }

        /// <summary>
        /// Handles the Click event of the EnableButton.
        /// Initiates package enabling for selected items via AppManager.
        /// </summary>
        private async void EnableButton_Click(object sender, EventArgs e)
        {
            var selectedPackages = DisabledPackagesCheckBoxList.CheckedItems.Cast<string>().ToList();
            if (selectedPackages.Any())
            {
                await _appManager.PerformPackageOperation(selectedPackages, "enable", "new state:", "Enabled {packageName}", "Unable to enable {packageName}");
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the EnabledPackagesCheckBoxList.
        /// Updates the state of action buttons.
        /// </summary>
        private void EnabledPackagesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonsState();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the DisabledPackagesCheckBoxList.
        /// Updates the state of action buttons.
        /// </summary>
        private void DisabledPackagesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonsState();
        }

        /// <summary>
        /// Handles the TextChanged event of the EnabledPackagesSearchTextBox.
        /// Filters the list of enabled packages displayed.
        /// </summary>
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

        /// <summary>
        /// Handles the TextChanged event of the DisabledPackagesSearchTextBox.
        /// Filters the list of disabled packages displayed.
        /// </summary>
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
        
        /// <summary>
        /// Handles the OperationFailed event from AppManager.
        /// Displays an error message to the user.
        /// Ensures thread-safe UI updates.
        /// </summary>
        /// <param name="title">The title of the error message box.</param>
        /// <param name="message">The error message to display.</param>
        private void OnAppManagerOperationFailed(string title, string message)
        {
            if (InvokeRequired) // Ensure thread safety
            {
                Invoke(new Action(() => OnAppManagerOperationFailed(title, message)));
                return;
            }
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}