#if __IOS__
using Foundation;
using Sentry.Cocoa;
using UIKit;

namespace Sentry.Tests.Platforms.Cocoa;

public class AppLifecycleSessionTrackerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly NSNotificationCenter _center = new();

    private AppLifecycleSessionTracker GetSut()
    {
        var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions(), _center);
        sut.AddObservers();
        return sut;
    }

    private void Post(NSString notification) => _center.PostNotificationName(notification, null);

    [Theory]
    [InlineData(nameof(UIApplication.DidEnterBackgroundNotification))]
    [InlineData(nameof(UIApplication.WillTerminateNotification))]
    public void Notification_Background_PausesSession(string name)
    {
        // Arrange
        _hub.IsSessionActive.Returns(true);
        using var sut = GetSut();

        // Act
        Post(GetNotification(name));

        // Assert
        _hub.Received(1).PauseSession();
        _hub.DidNotReceive().ResumeSession();
    }

    [Theory]
    [InlineData(nameof(UIApplication.WillEnterForegroundNotification))]
    [InlineData(nameof(UIApplication.DidBecomeActiveNotification))]
    public void Notification_ForegroundWithActiveSession_ResumesSession(string name)
    {
        // Arrange
        _hub.IsSessionActive.Returns(true);
        using var sut = GetSut();

        // Act
        Post(GetNotification(name));

        // Assert
        _hub.Received(1).ResumeSession();
        _hub.DidNotReceive().StartSession();
        _hub.DidNotReceive().PauseSession();
    }

    [Theory]
    [InlineData(nameof(UIApplication.WillEnterForegroundNotification))]
    [InlineData(nameof(UIApplication.DidBecomeActiveNotification))]
    public void Notification_ForegroundWithoutSession_StartsSession(string name)
    {
        // Arrange
        _hub.IsSessionActive.Returns(false);
        using var sut = GetSut();

        // Act
        Post(GetNotification(name));

        // Assert
        _hub.Received(1).StartSession();
        _hub.DidNotReceive().ResumeSession();
    }

    [Fact]
    public void Notification_TerminateWithoutSession_DoesNotPause()
    {
        // Arrange
        _hub.IsSessionActive.Returns(false);
        using var sut = GetSut();

        // Act
        Post(UIApplication.WillTerminateNotification);

        // Assert
        _hub.DidNotReceive().PauseSession();
    }

    [Fact]
    public void Dispose_RemovesObservers()
    {
        // Arrange
        var sut = GetSut();

        // Act
        sut.Dispose();
        Post(UIApplication.DidEnterBackgroundNotification);
        Post(UIApplication.WillEnterForegroundNotification);

        // Assert
        _hub.DidNotReceive().PauseSession();
        _hub.DidNotReceive().ResumeSession();
        _hub.DidNotReceive().StartSession();
    }

    private static NSString GetNotification(string name) => name switch
    {
        nameof(UIApplication.DidEnterBackgroundNotification) => UIApplication.DidEnterBackgroundNotification,
        nameof(UIApplication.WillTerminateNotification) => UIApplication.WillTerminateNotification,
        nameof(UIApplication.WillEnterForegroundNotification) => UIApplication.WillEnterForegroundNotification,
        nameof(UIApplication.DidBecomeActiveNotification) => UIApplication.DidBecomeActiveNotification,
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };
}
#endif
