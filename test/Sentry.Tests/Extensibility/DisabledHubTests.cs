using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Tests.Extensibility
{
    public class DisabledHubTests
    {
        [Fact]
        public void IsEnabled_False() => Assert.False(DisabledHub.Instance.IsEnabled);

        [Fact]
        public void LastEventId_EmptyGuid() => Assert.Equal(default, DisabledHub.Instance.LastEventId);

        [Fact]
        public void ConfigureScopeAsync_ReturnsCompletedTask()
            // net45 doesn't have `CompletedTask` and returns `Task<object>`
            => Assert.Same(Task.CompletedTask.Status, DisabledHub.Instance.ConfigureScopeAsync(null).Status);

        [Fact]
        public void PushScope_ReturnsSelf()
            => Assert.Same(DisabledHub.Instance, DisabledHub.Instance.PushScope());

        [Fact]
        public void PushScope_WithState_ReturnsSelf()
            => Assert.Same(DisabledHub.Instance, DisabledHub.Instance.PushScope(null as object));

        [Fact]
        public void CaptureEvent_EmptyGuid()
            => Assert.Equal(Guid.Empty, DisabledHub.Instance.CaptureEvent(null));

        [Fact]
        public void ConfigureScope_NoOp() => DisabledHub.Instance.ConfigureScope(null);

        [Fact]
        public void WithScope_NoOp() => DisabledHub.Instance.WithScope(null);

        [Fact]
        public void BindClient_NoOp() => DisabledHub.Instance.BindClient(null);

        [Fact]
        public void Dispose_NoOp() => DisabledHub.Instance.Dispose();

    }
}
