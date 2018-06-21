using System;
using System.Threading.Tasks;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class DisabledSentryClientTests
    {
        [Fact]
        public void IsEnabled_False() => Assert.False(DisabledSentryClient.Instance.IsEnabled);

        [Fact]
        public void ConfigureScopeAsync_ReturnsCompletedTask()
            => Assert.Same(Task.CompletedTask, DisabledSentryClient.Instance.ConfigureScopeAsync(null));

        [Fact]
        public void PushScope_ReturnsSeld()
            => Assert.Same(DisabledSentryClient.Instance, DisabledSentryClient.Instance.PushScope());

        [Fact]
        public void PushScope_WithState_ReturnsSeld()
            => Assert.Same(DisabledSentryClient.Instance, DisabledSentryClient.Instance.PushScope(null as object));

        [Fact]
        public void CaptureEvent_EmptyGuid()
            => Assert.Equal(Guid.Empty, DisabledSentryClient.Instance.CaptureEvent(null));

        [Fact]
        public void ConfigureScope_NoOp() => DisabledSentryClient.Instance.ConfigureScope(null);

        [Fact]
        public void BindClient_NoOp() => DisabledSentryClient.Instance.BindClient(null);

        [Fact]
        public void Dispose_NoOp() => DisabledSentryClient.Instance.Dispose();

    }
}
