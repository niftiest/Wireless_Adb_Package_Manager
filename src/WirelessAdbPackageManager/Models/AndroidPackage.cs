namespace WirelessAdbPackageManager.Models;

public sealed record AndroidPackage(string FullName)
{
    public override string ToString() => FullName;

    public static IReadOnlyList<AndroidPackage> ParseListOutput(string adbStdOut)
    {
        if (string.IsNullOrWhiteSpace(adbStdOut))
        {
            return Array.Empty<AndroidPackage>();
        }

        var result = new List<AndroidPackage>();
        foreach (var rawLine in adbStdOut.Split('\n'))
        {
            var line = rawLine.Trim();
            const string prefix = "package:";
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                var name = line[prefix.Length..].Trim();
                if (name.Length > 0)
                {
                    result.Add(new AndroidPackage(name));
                }
            }
        }
        return result;
    }
}
