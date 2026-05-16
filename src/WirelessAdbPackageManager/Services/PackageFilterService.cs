using WirelessAdbPackageManager.Models;

namespace WirelessAdbPackageManager.Services;

public sealed class PackageFilterService
{
    public HashSet<AndroidPackage> CheckedPackages { get; } = new();

    public IReadOnlyList<AndroidPackage> Filter(IEnumerable<AndroidPackage> all, string? searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return all.ToList();
        }
        return all
            .Where(p => p.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
