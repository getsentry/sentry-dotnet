#if ANDROID
using AndroidX.Lifecycle;
using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class AppLifecycleSessionTrackerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();

    [Fact]
    public void Observe_LifecycleNotInitialized_StartsSession()
    {
        // Arrange
        var owner = new TestLifecycleOwner();
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.Observe(owner.Registry);

        // Assert
        _hub.Received(1).StartSession();
        GC.KeepAlive(owner);
    }

    [Fact]
    public void Observe_LifecycleCreated_WaitsForForeground()
    {
        // Arrange
        var owner = new TestLifecycleOwner();
        owner.Registry.HandleLifecycleEvent(Lifecycle.Event.OnCreate!);
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.Observe(owner.Registry);

        // Assert
        _hub.DidNotReceive().StartSession();
        GC.KeepAlive(owner);
    }

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
    public void OnStateChanged_OnStartWithActiveSession_ResumesSession()
    {
        // Arrange
        _hub.IsSessionActive.Returns(true);
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnStart!);

        // Assert
        _hub.Received(1).ResumeSession();
        _hub.DidNotReceive().StartSession();
        _hub.DidNotReceive().PauseSession();
    }

    [Fact]
    public void OnStateChanged_OnStartWithoutSession_StartsSession()
    {
        // Arrange
        _hub.IsSessionActive.Returns(false);
        using var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());

        // Act
        sut.OnStateChanged(Lifecycle.Event.OnStart!);

        // Assert
        _hub.Received(1).StartSession();
        _hub.DidNotReceive().ResumeSession();
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
        _hub.DidNotReceive().StartSession();
    }

    private sealed class TestLifecycleOwner : Java.Lang.Object, ILifecycleOwner
    {
        public TestLifecycleOwner() => Registry = LifecycleRegistry.CreateUnsafe(this);

        public LifecycleRegistry Registry { get; }

        public Lifecycle Lifecycle => Registry;
    }
}
#endif
