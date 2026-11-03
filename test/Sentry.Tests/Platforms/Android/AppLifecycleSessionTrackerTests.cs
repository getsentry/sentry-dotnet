#if ANDROID
using AndroidX.Lifecycle;
using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class AppLifecycleSessionTrackerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();

    [Fact]
    public void OnStateChanged_OnStop_PausesSession()
    {
        // Arrange
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnStop!);

        // Assert
        _hub.Received(1).PauseSession();
        _hub.DidNotReceive().ResumeSession();
    }

    [Fact]
    public void OnStateChanged_OnStart_ResumesSession()
    {
        // Arrange
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnStart!);

        // Assert
        _hub.Received(1).ResumeSession();
        _hub.DidNotReceive().PauseSession();
    }

    [Fact]
    public void OnStateChanged_OtherEvents_DoNothing()
    {
        // Arrange
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnResume!);
        sut.OnStateChanged(Lifecycle.Event.OnPause!);

        // Assert
        _hub.DidNotReceive().PauseSession();
        _hub.DidNotReceive().ResumeSession();
    }

    [Fact]
    public void OnStateChanged_Disposed_DoesNothing()
    {
        // Arrange
        var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());
        sut.Dispose();

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnStop!);
        sut.OnStateChanged(Lifecycle.Event.OnStart!);

        // Assert
        _hub.DidNotReceive().PauseSession();
        _hub.DidNotReceive().ResumeSession();
    }
}
#endif
