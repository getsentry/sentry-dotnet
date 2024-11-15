using NSubstitute.Exceptions;

namespace Sentry.Maui.Tests;

public class SentryMauiOptionsTests
{
    [Fact]
    public void IncludeTextInBreadcrumbs_Default()
    {
        var options = new SentryMauiOptions();
        Assert.False(options.IncludeTextInBreadcrumbs);
    }

    [Fact]
    public void IncludeTitleInBreadcrumbs_Default()
    {
        var options = new SentryMauiOptions();
        Assert.False(options.IncludeTitleInBreadcrumbs);
    }

    [Fact]
    public void IncludeBackgroundingStateInBreadcrumbs_Default()
    {
        var options = new SentryMauiOptions();
        Assert.False(options.IncludeBackgroundingStateInBreadcrumbs);
    }

    [Fact]
    public void AutoSessionTracking_Default()
    {
        var options = new SentryMauiOptions();
        Assert.True(options.AutoSessionTracking);
    }

    [Fact]
    public void DetectStartupTime_Default()
    {
        var options = new SentryMauiOptions();
        Assert.Equal(StartupTimeDetectionMode.Fast, options.DetectStartupTime);
    }

    [Fact]
    public void CacheDirectoryPath_Default()
    {
        var options = new SentryMauiOptions();

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
        var options = new SentryMauiOptions();

        // Assert
        Assert.Equal(expected, options.Android.LogCatIntegration);
    }

    [Fact]
    public void HandlerStrategy_Set()
    {
        // Arrange
        var expected = Android.LogCatIntegrationType.None;
        var options = new SentryMauiOptions();

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
        var options = new SentryMauiOptions();
        options.AttachScreenshot = true;

        // Act
        options.SetBeforeScreenshotCapture((@event, hint) =>
        {
            return false;
        });

        // Assert
        Assert.NotNull(options.BeforeCaptureInternal);

    }

    [Fact]
    public void BeforeCaptureScreenshot_NotSet()
    {
        // Arrange
        var options = new SentryMauiOptions();
        options.AttachScreenshot = true;

        // Assert
        Assert.Null(options.BeforeCaptureInternal);

    }
}
