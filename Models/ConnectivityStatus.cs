namespace WirelessAdbPackageManager.Models
{
    /// <summary>
    /// Represents the connectivity status of the ADB (Android Debug Bridge) connection.
    /// </summary>
    public static class ConnectivityStatus // Made static as it contains only static members
    {
        /// <summary>
        /// Gets or sets a value indicating whether the device is currently paired via ADB.
        /// </summary>
        public static bool IsPaired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device is currently connected via ADB.
        /// </summary>
        public static bool IsConnected { get; set; }
    }
}
