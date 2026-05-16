using System.Net;

namespace WirelessAdbPackageManager.Models;

public sealed record ConnectionInfo(IPAddress Ip, int Port, string PairingCode)
{
    public string Endpoint => $"{Ip}:{Port}";
}
