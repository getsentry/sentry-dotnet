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
    public void BeforeCaptureScreenshot_Default()
    {
        // Arrange
        Action emptyAction = () => { };
        var options = new SentryMauiOptions();

        // Act
        bool result = options.BeforeCaptureScreenshot == emptyAction;

        // Assert 
        Assert.False(result);
    }
    [Fact]
    public void BeforeCaptureScreenshot_Specified()
    {
        // Arrange
        Action emptyAction = () => { };
        var options = new SentryMauiOptions();

        // Act
        options.BeforeCaptureScreenshot = () =>
        {
            Console.WriteLine("BeforeCaptureScreenshot is being called");
        };

        // Assert
        Assert.True(options.BeforeCaptureScreenshot != emptyAction);
    }
}
