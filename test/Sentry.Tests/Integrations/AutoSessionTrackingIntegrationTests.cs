using Sentry.Integrations;

namespace Sentry.Tests.Integrations;

public class AutoSessionTrackingIntegrationTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly IDisposable _subscription = Substitute.For<IDisposable>();
    private int _subscribeCount;

    private AutoSessionTrackingIntegration GetSut(bool tracksAppLifecycle = true) => new((_, _) =>
    {
        _subscribeCount++;
        return tracksAppLifecycle ? _subscription : null;
    });

    [Fact]
    public void Register_AppLifecycleTracked_SubscribesWithoutStartingSession()
    {
        // Arrange
        var sut = GetSut();

        // Act
        sut.Register(_hub, new SentryOptions { AutoSessionTracking = true });

        // Assert
        _hub.DidNotReceive().StartSession();
        _subscribeCount.Should().Be(1);
    }

    [Fact]
    public void Register_AppLifecycleNotTracked_StartsSession()
    {
        // Arrange
        var sut = GetSut(tracksAppLifecycle: false);

        // Act
        sut.Register(_hub, new SentryOptions { AutoSessionTracking = true });

        // Assert
        _hub.Received(1).StartSession();
    }

    [Fact]
    public void Register_AppLifecycleTracked_RecoversPersistedSession()
    {
        // Arrange
        var sessionManager = Substitute.For<ISessionManager>();
        var options = new SentryOptions { Dsn = ValidDsn, AutoSessionTracking = false };
        using var hub = new Hub(options, Substitute.For<ISentryClient>(), sessionManager);
        options.AutoSessionTracking = true;
        var sut = GetSut();

        // Act
        sut.Register(hub, options);

        // Assert
        sessionManager.Received(1).TryRecoverPersistedSession();
        sessionManager.DidNotReceive().StartSession();
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
