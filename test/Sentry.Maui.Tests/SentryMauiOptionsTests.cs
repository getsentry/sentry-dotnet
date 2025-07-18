using NSubstitute.Exceptions;

namespace Sentry.Maui.Tests;

public class SentryMauiOptionsTests
{
    private static SentryMauiOptions GetSut() => new()
    {
        NetworkStatusListener = FakeReliableNetworkStatusListener.Instance
    };

#if false
    [Fact]
    public void IncludeTextInBreadcrumbs_Default()
    {
        var options = GetSut();
        Assert.False(options.IncludeTextInBreadcrumbs);
    }

    [Fact]
    public void IncludeTitleInBreadcrumbs_Default()
    {
        var options = GetSut();
        Assert.False(options.IncludeTitleInBreadcrumbs);
    }

    [Fact]
    public void IncludeBackgroundingStateInBreadcrumbs_Default()
    {
        var options = GetSut();
        Assert.False(options.IncludeBackgroundingStateInBreadcrumbs);
    }

    [Fact]
    public void AutoSessionTracking_Default()
    {
        var options = GetSut();
        Assert.True(options.AutoSessionTracking);
    }

    [Fact]
    public void DetectStartupTime_Default()
    {
        var options = GetSut();
        Assert.Equal(StartupTimeDetectionMode.Fast, options.DetectStartupTime);
    }

    [Fact]
    public void CacheDirectoryPath_Default()
    {
        var options = GetSut();

#if PLATFORM_NEUTRAL
        Assert.Null(options.CacheDirectoryPath);
#else
        var expected = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
        Assert.Equal(expected, options.CacheDirectoryPath);
#endif
    }

#if ANDROID
    [Fact]
    public void HandlerStrategy_Default()
    {
        // Arrange
        var expected = Android.LogCatIntegrationType.None;
        var options = GetSut();

        // Assert
        Assert.Equal(expected, options.Android.LogCatIntegration);
    }

    [Fact]
    public void HandlerStrategy_Set()
    {
        // Arrange
        var expected = Android.LogCatIntegrationType.None;
        var options = GetSut();

        // Act
        options.Android.LogCatIntegration = Android.LogCatIntegrationType.All;

        // Assert
        Assert.NotEqual(expected, options.Android.LogCatIntegration);
    }
#endif

    [Fact]
    public void BeforeCaptureScreenshot_Set()
    {
        // Arrange
        var options = GetSut();
        options.AttachScreenshot = true;

        // Act
        options.SetBeforeScreenshotCapture((@event, hint) =>
        {
            return false;
        });

        // Assert
        Assert.NotNull(options.BeforeCaptureInternal);
    }
#endif

    [Fact]
    public void BeforeCaptureScreenshot_NotSet()
    {
        // Arrange
        var options = GetSut();
        options.AttachScreenshot = true;

        // Assert
        Assert.Null(options.BeforeCaptureInternal);
    }
}
