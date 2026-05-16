using WirelessAdbPackageManager.Models;
using Xunit;

namespace WirelessAdbPackageManager.Tests.Models;

public class AndroidPackageTests
{
    [Fact]
    public void ParseListOutput_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(AndroidPackage.ParseListOutput(string.Empty));
        Assert.Empty(AndroidPackage.ParseListOutput("   \r\n  "));
    }

    [Fact]
    public void ParseListOutput_StripsPackagePrefixOnly()
    {
        var input = "package:com.example.app\npackage:org.mozilla.firefox\n";
        var result = AndroidPackage.ParseListOutput(input);
        Assert.Equal(2, result.Count);
        Assert.Equal("com.example.app", result[0].FullName);
        Assert.Equal("org.mozilla.firefox", result[1].FullName);
    }

    [Fact]
    public void ParseListOutput_HandlesNonComPackages()
    {
        var input = string.Join('\n', new[]
        {
            "package:com.android.settings",
            "package:org.lineageos.eleven",
            "package:android",
            "package:net.dinglisch.android.taskerm",
            "package:io.appium.uiautomator2.server"
        });
        var result = AndroidPackage.ParseListOutput(input);
        var names = result.Select(p => p.FullName).ToArray();
        Assert.Contains("org.lineageos.eleven", names);
        Assert.Contains("android", names);
        Assert.Contains("net.dinglisch.android.taskerm", names);
        Assert.Contains("io.appium.uiautomator2.server", names);
    }

    [Fact]
    public void ParseListOutput_ToleratesCrlfLineEndings()
    {
        var input = "package:com.a\r\npackage:com.b\r\n";
        var result = AndroidPackage.ParseListOutput(input);
        Assert.Equal(new[] { "com.a", "com.b" }, result.Select(p => p.FullName));
    }

    [Fact]
    public void ParseListOutput_IgnoresLinesWithoutPrefix()
    {
        var input = "List of devices attached\npackage:com.a\nNOISE\n";
        var result = AndroidPackage.ParseListOutput(input);
        Assert.Single(result);
        Assert.Equal("com.a", result[0].FullName);
    }

    [Fact]
    public void ToString_ReturnsFullName()
    {
        Assert.Equal("com.foo.bar", new AndroidPackage("com.foo.bar").ToString());
    }
}
