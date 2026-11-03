using Sentry.Integrations;

namespace Sentry.Tests.Integrations;

public class AutoSessionTrackingIntegrationTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly IDisposable _subscription = Substitute.For<IDisposable>();
    private int _subscribeCount;

    private AutoSessionTrackingIntegration GetSut() => new((_, _) =>
    {
        _subscribeCount++;
        return _subscription;
    });

    [Fact]
    public void Register_AutoSessionTrackingEnabled_StartsSessionAndSubscribesToAppLifecycle()
    {
        // Arrange
        var sut = GetSut();

        // Act
        sut.Register(_hub, new SentryOptions { AutoSessionTracking = true });

        // Assert
        _hub.Received(1).StartSession();
        _subscribeCount.Should().Be(1);
    }

    [Fact]
    public void Register_AutoSessionTrackingDisabled_DoesNothing()
    {
        // Arrange
        var sut = GetSut();

        // Act
        sut.Register(_hub, new SentryOptions { AutoSessionTracking = false });

        // Assert
        _hub.DidNotReceive().StartSession();
        _subscribeCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_Subscribed_DisposesSubscription()
    {
        // Arrange
        var sut = GetSut();
        sut.Register(_hub, new SentryOptions { AutoSessionTracking = true });

        // Act
        sut.Dispose();

        // Assert
        _subscription.Received(1).Dispose();
    }
}
