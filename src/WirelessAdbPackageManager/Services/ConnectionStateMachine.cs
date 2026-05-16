namespace WirelessAdbPackageManager.Services;

public enum ConnectionState { Disconnected, Paired, Connected }

public sealed class ConnectionStateMachine
{
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public event EventHandler<ConnectionState>? StateChanged;

    public void MarkPaired() => Transition(ConnectionState.Paired);
    public void MarkConnected() => Transition(ConnectionState.Connected);
    public void Reset() => Transition(ConnectionState.Disconnected);

    private void Transition(ConnectionState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(this, next);
    }
}
