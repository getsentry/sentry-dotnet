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

    [Fact]
    public void AttachScreenshots_Default()
    {
        var options = new SentryMauiOptions();
        Assert.False(options.AttachScreenshot);
    }
    [Fact]
    public void HandlerStrategy_Default()
    {
        // Arrange
        var options = new SentryMauiOptions();
        var expected = NdkHandlerStrategy.SENTRY_HANDLER_STRATEGY_DEFAULT;

        // Assert
        // testing default state here so nothing to assert

        // Act
        Assert.Equal(expected, options.HandlerStrategy);
    }
    [Fact]
    public void HandlerStrategy_set()
    {
        // Arrange
        var options = new SentryMauiOptions();
        var notExpected = NdkHandlerStrategy.SENTRY_HANDLER_STRATEGY_DEFAULT;

        // Assert
        options.HandlerStrategy = NdkHandlerStrategy.SENTRY_HANDLER_STRATEGY_CHAIN_AT_START;

        // Act
        Assert.NotEqual(notExpected, options.HandlerStrategy);
    }
}
