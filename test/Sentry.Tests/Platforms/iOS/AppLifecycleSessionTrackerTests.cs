#if __IOS__
using Foundation;
using Sentry.Cocoa;
using UIKit;

namespace Sentry.Tests.Platforms.Cocoa;

public class AppLifecycleSessionTrackerTests
{
    private readonly IHub _hub = Substitute.For<IHub>();

    private AppLifecycleSessionTracker GetSut()
    {
        var sut = new AppLifecycleSessionTracker(_hub, new SentryOptions());
        sut.AddObservers();
        return sut;
    }

    private static void Post(NSString notification) =>
        NSNotificationCenter.DefaultCenter.PostNotificationName(notification, null);

    [Theory]
    [InlineData(nameof(UIApplication.DidEnterBackgroundNotification))]
    [InlineData(nameof(UIApplication.WillTerminateNotification))]
    public void Notification_Background_PausesSession(string name)
    {
        // Arrange
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
    public void Notification_Foreground_ResumesSession(string name)
    {
        // Arrange
        using var sut = GetSut();

        // Act
        Post(GetNotification(name));

        // Assert
        _hub.Received(1).ResumeSession();
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
