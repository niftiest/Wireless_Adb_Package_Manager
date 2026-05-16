using WirelessAdbPackageManager.Adb;

namespace WirelessAdbPackageManager.Tests.Fakes;

public sealed class FakeAdbProcessRunner : IAdbProcessRunner
{
    public List<string[]> Invocations { get; } = new();
    public Queue<AdbProcessResult> Responses { get; } = new();
    public AdbProcessResult DefaultResponse { get; set; } = new(0, string.Empty, string.Empty, false);

    public Task<AdbProcessResult> RunAsync(IEnumerable<string> args, TimeSpan timeout, CancellationToken ct = default)
    {
        Invocations.Add(args.ToArray());
        var response = Responses.Count > 0 ? Responses.Dequeue() : DefaultResponse;
        return Task.FromResult(response);
    }
}
