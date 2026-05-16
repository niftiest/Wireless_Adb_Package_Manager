using WirelessAdbPackageManager.Utils;
using Xunit;

namespace WirelessAdbPackageManager.Tests.Utils;

public class InputValidatorTests
{
    [Theory]
    [InlineData("192.168.0.5", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("not.an.ip", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("256.0.0.1", false)]
    public void TryParseIp_ValidatesIpV4(string input, bool expectedOk)
    {
        var ok = InputValidator.TryParseIp(input, out var parsed);
        Assert.Equal(expectedOk, ok);
        if (expectedOk)
        {
            Assert.NotNull(parsed);
        }
    }

    [Theory]
    [InlineData("45033", true, 45033)]
    [InlineData("1", true, 1)]
    [InlineData("65535", true, 65535)]
    [InlineData("0", false, 0)]
    [InlineData("65536", false, 0)]
    [InlineData("abc", false, 0)]
    [InlineData("", false, 0)]
    [InlineData("-1", false, 0)]
    public void TryParsePort_RequiresInRangeInteger(string input, bool expectedOk, int expectedPort)
    {
        var ok = InputValidator.TryParsePort(input, out var port);
        Assert.Equal(expectedOk, ok);
        if (expectedOk)
        {
            Assert.Equal(expectedPort, port);
        }
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidPairingCode_RequiresSixDigits(string? input, bool expected)
    {
        Assert.Equal(expected, InputValidator.IsValidPairingCode(input));
    }
}
