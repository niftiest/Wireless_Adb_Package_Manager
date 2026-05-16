using System.Net;

namespace WirelessAdbPackageManager.Utils;

public static class InputValidator
{
    public static bool TryParseIp(string? input, out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        if (!IPAddress.TryParse(input.Trim(), out var parsed))
        {
            return false;
        }
        if (parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }
        address = parsed;
        return true;
    }

    public static bool TryParsePort(string? input, out int port)
    {
        port = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        if (!int.TryParse(input.Trim(), out var parsed))
        {
            return false;
        }
        if (parsed < 1 || parsed > 65535)
        {
            return false;
        }
        port = parsed;
        return true;
    }

    public static bool IsValidPairingCode(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }
        if (input.Length != 6)
        {
            return false;
        }
        foreach (var c in input)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }
        return true;
    }
}
