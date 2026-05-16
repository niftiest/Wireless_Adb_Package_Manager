namespace WirelessAdbPackageManager.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        IpAddressTextBox = new TextBox();
        PortTextBox = new TextBox();
        PairingCodeTextBox = new TextBox();
        EnabledPackagesCheckBoxList = new CheckedListBox();
        ConnectButton = new Button();
        LogsTextBox = new TextBox();
        UninstallButton = new Button();
        DisableButton = new Button();
        IpAddressLabel = new Label();
        PortLabel = new Label();
        PairingCodeLabel = new Label();
        EnabledPackagesLabel = new Label();
        LogsLabel = new Label();
        DisabledPackagesLabel = new Label();
        DisabledPackagesCheckBoxList = new CheckedListBox();
        EnableButton = new Button();
        EnabledPackagesSearchTextBox = new TextBox();
        EnabledSearchLabel = new Label();
        DisabledSearchLabel = new Label();
        DisabledPackagesSearchTextBox = new TextBox();
        InstallButton = new Button();
        SuspendLayout();

        IpAddressTextBox.BackColor = Color.FromArgb(250, 250, 250);
        IpAddressTextBox.Location = new Point(12, 27);
        IpAddressTextBox.MaxLength = 15;
        IpAddressTextBox.Name = "IpAddressTextBox";
        IpAddressTextBox.Size = new Size(103, 23);
        IpAddressTextBox.TabIndex = 0;
        IpAddressTextBox.Text = "192.168.0.5";

        PortTextBox.BackColor = Color.FromArgb(250, 250, 250);
        PortTextBox.Location = new Point(121, 27);
        PortTextBox.MaxLength = 5;
        PortTextBox.Name = "PortTextBox";
        PortTextBox.Size = new Size(50, 23);
        PortTextBox.TabIndex = 1;
        PortTextBox.Text = "45033";

        PairingCodeTextBox.BackColor = Color.FromArgb(250, 250, 250);
        PairingCodeTextBox.Location = new Point(177, 27);
        PairingCodeTextBox.MaxLength = 6;
        PairingCodeTextBox.Name = "PairingCodeTextBox";
        PairingCodeTextBox.Size = new Size(75, 23);
        PairingCodeTextBox.TabIndex = 2;
        PairingCodeTextBox.Text = "879502";

        EnabledPackagesCheckBoxList.CheckOnClick = true;
        EnabledPackagesCheckBoxList.FormattingEnabled = true;
        EnabledPackagesCheckBoxList.Location = new Point(12, 79);
        EnabledPackagesCheckBoxList.Name = "EnabledPackagesCheckBoxList";
        EnabledPackagesCheckBoxList.ScrollAlwaysVisible = true;
        EnabledPackagesCheckBoxList.Size = new Size(351, 310);
        EnabledPackagesCheckBoxList.Sorted = true;
        EnabledPackagesCheckBoxList.TabIndex = 3;
        EnabledPackagesCheckBoxList.SelectedIndexChanged += EnabledPackagesList_SelectedIndexChanged;
        EnabledPackagesCheckBoxList.ItemCheck += EnabledPackagesList_ItemCheck;

        ConnectButton.BackColor = Color.FromArgb(54, 153, 232);
        ConnectButton.FlatAppearance.BorderSize = 0;
        ConnectButton.FlatStyle = FlatStyle.Flat;
        ConnectButton.Location = new Point(258, 27);
        ConnectButton.Name = "ConnectButton";
        ConnectButton.Size = new Size(118, 23);
        ConnectButton.TabIndex = 4;
        ConnectButton.Text = "CONNECT";
        ConnectButton.UseVisualStyleBackColor = false;
        ConnectButton.Click += ConnectButton_Click;

        LogsTextBox.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        LogsTextBox.Location = new Point(12, 442);
        LogsTextBox.Multiline = true;
        LogsTextBox.Name = "LogsTextBox";
        LogsTextBox.ReadOnly = true;
        LogsTextBox.ScrollBars = ScrollBars.Vertical;
        LogsTextBox.Size = new Size(708, 152);
        LogsTextBox.TabIndex = 5;

        UninstallButton.BackColor = Color.FromArgb(184, 76, 74);
        UninstallButton.Enabled = false;
        UninstallButton.FlatAppearance.BorderSize = 0;
        UninstallButton.FlatStyle = FlatStyle.Flat;
        UninstallButton.ForeColor = Color.White;
        UninstallButton.Location = new Point(207, 395);
        UninstallButton.Name = "UninstallButton";
        UninstallButton.Size = new Size(75, 23);
        UninstallButton.TabIndex = 6;
        UninstallButton.Text = "UNINSTALL";
        UninstallButton.UseVisualStyleBackColor = false;
        UninstallButton.Click += UninstallButton_Click;

        DisableButton.BackColor = Color.FromArgb(54, 153, 232);
        DisableButton.Enabled = false;
        DisableButton.FlatAppearance.BorderSize = 0;
        DisableButton.FlatStyle = FlatStyle.Flat;
        DisableButton.ForeColor = Color.White;
        DisableButton.Location = new Point(288, 395);
        DisableButton.Name = "DisableButton";
        DisableButton.Size = new Size(75, 23);
        DisableButton.TabIndex = 7;
        DisableButton.Text = "DISABLE";
        DisableButton.UseVisualStyleBackColor = false;
        DisableButton.Click += DisableButton_Click;

        IpAddressLabel.AutoSize = true;
        IpAddressLabel.Location = new Point(12, 9);
        IpAddressLabel.Name = "IpAddressLabel";
        IpAddressLabel.Size = new Size(62, 15);
        IpAddressLabel.TabIndex = 8;
        IpAddressLabel.Text = "IP Address";

        PortLabel.AutoSize = true;
        PortLabel.Location = new Point(121, 9);
        PortLabel.Name = "PortLabel";
        PortLabel.Size = new Size(29, 15);
        PortLabel.TabIndex = 9;
        PortLabel.Text = "Port";

        PairingCodeLabel.AutoSize = true;
        PairingCodeLabel.Location = new Point(177, 9);
        PairingCodeLabel.Name = "PairingCodeLabel";
        PairingCodeLabel.Size = new Size(75, 15);
        PairingCodeLabel.TabIndex = 10;
        PairingCodeLabel.Text = "Pairing Code";

        EnabledPackagesLabel.AutoSize = true;
        EnabledPackagesLabel.Location = new Point(12, 61);
        EnabledPackagesLabel.Name = "EnabledPackagesLabel";
        EnabledPackagesLabel.Size = new Size(101, 15);
        EnabledPackagesLabel.TabIndex = 11;
        EnabledPackagesLabel.Text = "Enabled Packages";

        LogsLabel.AutoSize = true;
        LogsLabel.Location = new Point(12, 424);
        LogsLabel.Name = "LogsLabel";
        LogsLabel.Size = new Size(32, 15);
        LogsLabel.TabIndex = 12;
        LogsLabel.Text = "Logs";

        DisabledPackagesLabel.AutoSize = true;
        DisabledPackagesLabel.Location = new Point(369, 61);
        DisabledPackagesLabel.Name = "DisabledPackagesLabel";
        DisabledPackagesLabel.Size = new Size(104, 15);
        DisabledPackagesLabel.TabIndex = 14;
        DisabledPackagesLabel.Text = "Disabled Packages";

        DisabledPackagesCheckBoxList.CheckOnClick = true;
        DisabledPackagesCheckBoxList.FormattingEnabled = true;
        DisabledPackagesCheckBoxList.Location = new Point(369, 79);
        DisabledPackagesCheckBoxList.Name = "DisabledPackagesCheckBoxList";
        DisabledPackagesCheckBoxList.ScrollAlwaysVisible = true;
        DisabledPackagesCheckBoxList.Size = new Size(351, 310);
        DisabledPackagesCheckBoxList.Sorted = true;
        DisabledPackagesCheckBoxList.TabIndex = 13;
        DisabledPackagesCheckBoxList.SelectedIndexChanged += DisabledPackagesList_SelectedIndexChanged;
        DisabledPackagesCheckBoxList.ItemCheck += DisabledPackagesList_ItemCheck;

        EnableButton.BackColor = Color.FromArgb(54, 153, 232);
        EnableButton.Enabled = false;
        EnableButton.FlatAppearance.BorderSize = 0;
        EnableButton.FlatStyle = FlatStyle.Flat;
        EnableButton.ForeColor = Color.White;
        EnableButton.Location = new Point(645, 395);
        EnableButton.Name = "EnableButton";
        EnableButton.Size = new Size(75, 23);
        EnableButton.TabIndex = 15;
        EnableButton.Text = "ENABLE";
        EnableButton.UseVisualStyleBackColor = false;
        EnableButton.Click += EnableButton_Click;

        EnabledPackagesSearchTextBox.BackColor = Color.FromArgb(250, 250, 250);
        EnabledPackagesSearchTextBox.Location = new Point(60, 395);
        EnabledPackagesSearchTextBox.MaxLength = 100;
        EnabledPackagesSearchTextBox.Name = "EnabledPackagesSearchTextBox";
        EnabledPackagesSearchTextBox.Size = new Size(103, 23);
        EnabledPackagesSearchTextBox.TabIndex = 16;
        EnabledPackagesSearchTextBox.TextChanged += EnabledPackageFilter_TextChanged;

        EnabledSearchLabel.AutoSize = true;
        EnabledSearchLabel.Location = new Point(12, 399);
        EnabledSearchLabel.Name = "EnabledSearchLabel";
        EnabledSearchLabel.Size = new Size(42, 15);
        EnabledSearchLabel.TabIndex = 17;
        EnabledSearchLabel.Text = "Search";

        DisabledSearchLabel.AutoSize = true;
        DisabledSearchLabel.Location = new Point(369, 399);
        DisabledSearchLabel.Name = "DisabledSearchLabel";
        DisabledSearchLabel.Size = new Size(42, 15);
        DisabledSearchLabel.TabIndex = 18;
        DisabledSearchLabel.Text = "Search";

        DisabledPackagesSearchTextBox.BackColor = Color.FromArgb(250, 250, 250);
        DisabledPackagesSearchTextBox.Location = new Point(417, 395);
        DisabledPackagesSearchTextBox.MaxLength = 100;
        DisabledPackagesSearchTextBox.Name = "DisabledPackagesSearchTextBox";
        DisabledPackagesSearchTextBox.Size = new Size(103, 23);
        DisabledPackagesSearchTextBox.TabIndex = 19;
        DisabledPackagesSearchTextBox.TextChanged += DisabledPackageFilter_TextChanged;

        InstallButton.BackColor = Color.FromArgb(255, 231, 145);
        InstallButton.Enabled = false;
        InstallButton.FlatAppearance.BorderSize = 0;
        InstallButton.FlatStyle = FlatStyle.Flat;
        InstallButton.ForeColor = Color.Black;
        InstallButton.Location = new Point(601, 27);
        InstallButton.Name = "InstallButton";
        InstallButton.Size = new Size(118, 23);
        InstallButton.TabIndex = 20;
        InstallButton.Text = "INSTALL APK";
        InstallButton.UseVisualStyleBackColor = false;
        InstallButton.Click += InstallButton_Click;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(34, 34, 34);
        ClientSize = new Size(731, 612);
        Controls.Add(InstallButton);
        Controls.Add(DisabledPackagesSearchTextBox);
        Controls.Add(DisabledSearchLabel);
        Controls.Add(EnabledSearchLabel);
        Controls.Add(EnabledPackagesSearchTextBox);
        Controls.Add(EnableButton);
        Controls.Add(DisabledPackagesLabel);
        Controls.Add(DisabledPackagesCheckBoxList);
        Controls.Add(LogsLabel);
        Controls.Add(EnabledPackagesLabel);
        Controls.Add(PairingCodeLabel);
        Controls.Add(PortLabel);
        Controls.Add(IpAddressLabel);
        Controls.Add(DisableButton);
        Controls.Add(UninstallButton);
        Controls.Add(LogsTextBox);
        Controls.Add(ConnectButton);
        Controls.Add(EnabledPackagesCheckBoxList);
        Controls.Add(PairingCodeTextBox);
        Controls.Add(PortTextBox);
        Controls.Add(IpAddressTextBox);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        Name = "MainForm";
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Wireless Adb Package Manager";
        ResumeLayout(false);
        PerformLayout();
    }

    private TextBox IpAddressTextBox;
    private TextBox PortTextBox;
    private TextBox PairingCodeTextBox;
    private CheckedListBox EnabledPackagesCheckBoxList;
    private Button ConnectButton;
    private TextBox LogsTextBox;
    private Button UninstallButton;
    private Button DisableButton;
    private Label IpAddressLabel;
    private Label PortLabel;
    private Label PairingCodeLabel;
    private Label EnabledPackagesLabel;
    private Label LogsLabel;
    private Label DisabledPackagesLabel;
    private CheckedListBox DisabledPackagesCheckBoxList;
    private Button EnableButton;
    private TextBox EnabledPackagesSearchTextBox;
    private Label EnabledSearchLabel;
    private Label DisabledSearchLabel;
    private TextBox DisabledPackagesSearchTextBox;
    private Button InstallButton;
}
