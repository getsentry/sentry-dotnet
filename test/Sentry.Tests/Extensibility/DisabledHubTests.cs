namespace Sentry.Tests.Extensibility;

public class DisabledHubTests
{
    [Fact]
    public void IsEnabled_False() => Assert.False(DisabledHub.Instance.IsEnabled);

    [Fact]
    public void LastEventId_EmptyGuid() => Assert.Equal(default, DisabledHub.Instance.LastEventId);

    [Fact]
    public void ConfigureScopeAsync_ReturnsCompletedTask()
        => Assert.Equal(Task.CompletedTask, DisabledHub.Instance.ConfigureScopeAsync(null!));

    [Fact]
    public void PushScope_ReturnsSelf()
        => Assert.Same(DisabledHub.Instance, DisabledHub.Instance.PushScope());

    [Fact]
    public void PushScope_WithState_ReturnsSelf()
        => Assert.Same(DisabledHub.Instance, DisabledHub.Instance.PushScope(null as object));

    [Fact]
    public void CaptureEvent_EmptyGuid()
        => Assert.Equal(Guid.Empty, (Guid)DisabledHub.Instance.CaptureEvent(null!));

    [Fact]
    public void ConfigureScope_NoOp() => DisabledHub.Instance.ConfigureScope(null!);

    [Fact]
    public void BindClient_NoOp() => DisabledHub.Instance.BindClient(null!);

    [Fact]
    public void Dispose_NoOp() => DisabledHub.Instance.Dispose();

    [Fact]
    public async Task FlushAsync_NoOp() => await DisabledHub.Instance.FlushAsync();
}
