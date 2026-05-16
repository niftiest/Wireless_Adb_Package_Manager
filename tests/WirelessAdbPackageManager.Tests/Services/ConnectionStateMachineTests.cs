using WirelessAdbPackageManager.Services;
using Xunit;

namespace WirelessAdbPackageManager.Tests.Services;

public class ConnectionStateMachineTests
{
    [Fact]
    public void InitialState_IsDisconnected()
    {
        var sm = new ConnectionStateMachine();
        Assert.Equal(ConnectionState.Disconnected, sm.State);
    }

    [Fact]
    public void MarkPaired_TransitionsAndFiresEvent()
    {
        var sm = new ConnectionStateMachine();
        ConnectionState? raised = null;
        sm.StateChanged += (_, s) => raised = s;
        sm.MarkPaired();
        Assert.Equal(ConnectionState.Paired, sm.State);
        Assert.Equal(ConnectionState.Paired, raised);
    }

    [Fact]
    public void MarkConnected_TransitionsAndFiresEvent()
    {
        var sm = new ConnectionStateMachine();
        ConnectionState? raised = null;
        sm.StateChanged += (_, s) => raised = s;
        sm.MarkConnected();
        Assert.Equal(ConnectionState.Connected, sm.State);
        Assert.Equal(ConnectionState.Connected, raised);
    }

    [Fact]
    public void Reset_ReturnsToDisconnected()
    {
        var sm = new ConnectionStateMachine();
        sm.MarkConnected();
        sm.Reset();
        Assert.Equal(ConnectionState.Disconnected, sm.State);
    }

    [Fact]
    public void StateChanged_DoesNotFireWhenStateUnchanged()
    {
        var sm = new ConnectionStateMachine();
        var count = 0;
        sm.StateChanged += (_, _) => count++;
        sm.MarkPaired();
        sm.MarkPaired();
        Assert.Equal(1, count);
    }
}
