using WirelessAdbPackageManager.Models;
using WirelessAdbPackageManager.Services;
using Xunit;

namespace WirelessAdbPackageManager.Tests.Services;

public class PackageFilterServiceTests
{
    private static readonly AndroidPackage Alpha = new("com.alpha.app");
    private static readonly AndroidPackage Beta = new("com.beta.app");
    private static readonly AndroidPackage Gamma = new("org.gamma.tool");

    [Fact]
    public void Filter_EmptySearch_ReturnsAll()
    {
        var s = new PackageFilterService();
        var result = s.Filter(new[] { Alpha, Beta, Gamma }, "");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_SubstringMatchIsCaseInsensitive()
    {
        var s = new PackageFilterService();
        var result = s.Filter(new[] { Alpha, Beta, Gamma }, "ALPHA");
        Assert.Single(result);
        Assert.Equal(Alpha, result[0]);
    }

    [Fact]
    public void CheckedPackages_StartsEmpty()
    {
        var s = new PackageFilterService();
        Assert.Empty(s.CheckedPackages);
    }

    [Fact]
    public void CheckedPackages_PersistsAcrossFilters()
    {
        var s = new PackageFilterService();
        s.CheckedPackages.Add(Beta);
        _ = s.Filter(new[] { Alpha, Beta, Gamma }, "alpha");
        Assert.Contains(Beta, s.CheckedPackages);
    }
}
