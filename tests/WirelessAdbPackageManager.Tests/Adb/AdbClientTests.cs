using WirelessAdbPackageManager.Adb;
using WirelessAdbPackageManager.Models;
using WirelessAdbPackageManager.Tests.Fakes;
using Xunit;

namespace WirelessAdbPackageManager.Tests.Adb;

public class AdbClientTests
{
    [Fact]
    public async Task PairAsync_SuccessParsing()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(0, "Successfully paired to 192.168.0.5:45033 [guid=...]", "", false));

        var client = new AdbClient(fake);
        var result = await client.PairAsync("192.168.0.5", 45033, "123456");

        Assert.True(result.Success);
        Assert.Contains("192.168.0.5:45033", result.Message);
        Assert.Equal(new[] { "pair", "192.168.0.5:45033", "123456" }, fake.Invocations[^1]);
    }

    [Fact]
    public async Task PairAsync_FailureParsing()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(1, "", "failed to authenticate", false));

        var client = new AdbClient(fake);
        var result = await client.PairAsync("192.168.0.5", 45033, "123456");

        Assert.False(result.Success);
        Assert.Contains("authenticate", result.Message);
    }

    [Fact]
    public async Task PairAsync_Timeout_ReturnsTimedOutMessage()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(0, "", "", false));
        fake.Responses.Enqueue(new AdbProcessResult(-1, "", "", true));

        var client = new AdbClient(fake);
        var result = await client.PairAsync("192.168.0.5", 45033, "123456");

        Assert.False(result.Success);
        Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectAsync_SuccessParsing()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "connected to 192.168.0.5:5555", "", false));

        var client = new AdbClient(fake);
        var result = await client.ConnectAsync("192.168.0.5", 5555);

        Assert.True(result.Success);
        Assert.Equal(new[] { "connect", "192.168.0.5:5555" }, fake.Invocations[0]);
    }

    [Fact]
    public async Task ListPackagesAsync_ReturnsAllNonComEntries()
    {
        var fake = new FakeAdbProcessRunner();
        var stdout = string.Join('\n',
            "package:com.android.settings",
            "package:org.lineageos.eleven",
            "package:android",
            "package:net.dinglisch.android.taskerm");
        fake.Responses.Enqueue(new AdbProcessResult(0, stdout, "", false));

        var client = new AdbClient(fake);
        var result = await client.ListPackagesAsync(disabledOnly: false);

        Assert.True(result.Success);
        var names = result.Value!.Select(p => p.FullName).ToArray();
        Assert.Contains("com.android.settings", names);
        Assert.Contains("org.lineageos.eleven", names);
        Assert.Contains("android", names);
        Assert.Contains("net.dinglisch.android.taskerm", names);
    }

    [Fact]
    public async Task DisableAsync_BuildsExpectedShellCommand()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "Package com.foo new state: disabled-user", "", false));

        var client = new AdbClient(fake);
        var result = await client.DisableAsync(new AndroidPackage("com.foo"));

        Assert.True(result.Success);
        Assert.Equal(
            new[] { "shell", "pm", "disable-user", "--user", "0", "com.foo" },
            fake.Invocations[0]);
    }

    [Fact]
    public async Task UninstallAsync_BuildsExpectedShellCommand()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "Success", "", false));

        var client = new AdbClient(fake);
        var result = await client.UninstallAsync(new AndroidPackage("com.foo"));

        Assert.True(result.Success);
        Assert.Equal(
            new[] { "shell", "pm", "uninstall", "-k", "--user", "0", "com.foo" },
            fake.Invocations[0]);
    }

    [Fact]
    public async Task EnableAsync_BuildsExpectedShellCommand()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "Package com.foo new state: enabled", "", false));

        var client = new AdbClient(fake);
        var result = await client.EnableAsync(new AndroidPackage("com.foo"));

        Assert.True(result.Success);
        Assert.Equal(new[] { "shell", "pm", "enable", "com.foo" }, fake.Invocations[0]);
    }

    [Fact]
    public async Task GetDeviceAsync_ParsesOnlineDeviceWithModel()
    {
        var fake = new FakeAdbProcessRunner();
        var stdout = "List of devices attached\n192.168.0.5:5555         device product:foo model:Galaxy_Watch device:bar transport_id:1\n";
        fake.Responses.Enqueue(new AdbProcessResult(0, stdout, "", false));

        var client = new AdbClient(fake);
        var result = await client.GetDeviceAsync("192.168.0.5");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("Galaxy_Watch", result.Value!.Model);
        Assert.True(result.Value.IsOnline);
        Assert.Equal("192.168.0.5:5555", result.Value.Endpoint);
    }

    [Fact]
    public async Task GetDeviceAsync_ReturnsNullValueWhenIpNotPresent()
    {
        var fake = new FakeAdbProcessRunner();
        fake.Responses.Enqueue(new AdbProcessResult(0, "List of devices attached\n", "", false));

        var client = new AdbClient(fake);
        var result = await client.GetDeviceAsync("10.0.0.1");

        Assert.True(result.Success);
        Assert.Null(result.Value);
    }
}
