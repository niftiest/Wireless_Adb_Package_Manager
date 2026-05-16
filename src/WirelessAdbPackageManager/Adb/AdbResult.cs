namespace WirelessAdbPackageManager.Adb;

public sealed record AdbResult(bool Success, string Message)
{
    public static AdbResult Ok(string message) => new(true, message);
    public static AdbResult Fail(string message) => new(false, message);
}

public sealed record AdbResult<T>(bool Success, string Message, T? Value)
{
    public static AdbResult<T> Ok(T value, string message = "") => new(true, message, value);
    public static AdbResult<T> Fail(string message) => new(false, message, default);
}
